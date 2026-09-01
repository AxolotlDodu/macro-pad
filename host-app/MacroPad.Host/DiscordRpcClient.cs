using System.IO.Pipes;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace MacroPad.Host;

/// <summary>
/// Client RPC local Discord (pipe nommé \\.\pipe\discord-ipc-N). Gère le handshake,
/// l'autorisation OAuth2 (popup Discord une seule fois, puis token mis en cache sur disque),
/// et l'envoi de commandes comme SET_VOICE_SETTINGS.
/// </summary>
public class DiscordRpcClient : IDisposable
{
    private const int OpHandshake = 0;
    private const int OpFrame = 1;

    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _tokenCachePath;
    private static readonly HttpClient Http = new();

    private NamedPipeClientStream? _pipe;
    private readonly object _pipeLock = new();
    private bool _authenticated;

    public DiscordRpcClient(string clientId, string clientSecret)
    {
        _clientId = clientId;
        _clientSecret = clientSecret;
        _tokenCachePath = Path.Combine(AppContext.BaseDirectory, "discord_token.json");
    }

    /// <summary>Idempotent : ne refait le handshake/auth que si nécessaire.</summary>
    public async Task<bool> EnsureReadyAsync()
    {
        lock (_pipeLock)
        {
            if (_pipe is { IsConnected: true } && _authenticated) return true;
        }

        if (!await ConnectPipeAsync()) return false;

        var accessToken = LoadCachedToken();
        if (accessToken is null)
        {
            var code = await AuthorizeAsync();
            if (code is null) return false;

            accessToken = await ExchangeCodeAsync(code);
            if (accessToken is null) return false;

            SaveCachedToken(accessToken);
        }

        if (!await AuthenticateAsync(accessToken))
        {
            // Token expiré/révoqué : on efface le cache et on retentera au prochain appel.
            if (File.Exists(_tokenCachePath)) File.Delete(_tokenCachePath);
            return false;
        }

        _authenticated = true;
        return true;
    }

    public async Task<JsonElement?> SetVoiceSettingsAsync(bool mute, bool deaf)
    {
        var args = new Dictionary<string, object> { ["mute"] = mute, ["deaf"] = deaf };
        var response = await SendCommandAsync("SET_VOICE_SETTINGS", args);
        return response?.GetProperty("data");
    }

    // ---------- Pipe bas niveau ----------

    private async Task<bool> ConnectPipeAsync()
    {
        for (int i = 0; i < 10; i++)
        {
            try
            {
                var pipe = new NamedPipeClientStream(".", $"discord-ipc-{i}", PipeDirection.InOut, PipeOptions.Asynchronous);
                await pipe.ConnectAsync(500);

                await WriteFrameAsync(pipe, OpHandshake, new { v = 1, client_id = _clientId });
                var (opcode, _) = await ReadFrameAsync(pipe);
                if (opcode != OpFrame) { pipe.Dispose(); continue; }

                lock (_pipeLock) { _pipe = pipe; }
                return true;
            }
            catch (Exception ex) when (ex is IOException or TimeoutException)
            {
                // Ce pipe n'existe pas / pas Discord -> on essaie le suivant.
            }
        }

        Console.WriteLine("[DiscordRpc] Impossible de trouver le pipe Discord (client fermé ?).");
        return false;
    }

    private async Task<JsonElement?> SendCommandAsync(string cmd, Dictionary<string, object> args)
    {
        NamedPipeClientStream? pipe;
        lock (_pipeLock) { pipe = _pipe; }
        if (pipe is null || !pipe.IsConnected) return null;

        var nonce = Guid.NewGuid().ToString();
        await WriteFrameAsync(pipe, OpFrame, new { cmd, args, nonce });

        // On ignore les DISPATCH sans nonce correspondant, jusqu'à trouver notre réponse.
        for (int i = 0; i < 10; i++)
        {
            var (opcode, json) = await ReadFrameAsync(pipe);
            if (opcode != OpFrame) continue;

            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("nonce", out var n) && n.GetString() == nonce)
                return JsonDocument.Parse(json).RootElement.Clone();
        }

        return null;
    }

    private async Task<string?> AuthorizeAsync()
    {
        var args = new Dictionary<string, object> { ["client_id"] = _clientId, ["scopes"] = new[] { "rpc" } };
        var response = await SendCommandAsync("AUTHORIZE", args);
        if (response is null) return null;

        return response.Value.GetProperty("data").GetProperty("code").GetString();
    }

    private async Task<bool> AuthenticateAsync(string accessToken)
    {
        var args = new Dictionary<string, object> { ["access_token"] = accessToken };
        var response = await SendCommandAsync("AUTHENTICATE", args);
        if (response is null) return false;
        return !(response.Value.TryGetProperty("evt", out var evt) && evt.GetString() == "ERROR");
    }

    private async Task<string?> ExchangeCodeAsync(string code)
    {
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret,
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = "http://localhost"
        });

        try
        {
            var response = await Http.PostAsync("https://discord.com/api/oauth2/token", form);
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[DiscordRpc] Échec échange token : {body}");
                return null;
            }

            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.GetProperty("access_token").GetString();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DiscordRpc] Exception échange token : {ex.Message}");
            return null;
        }
    }

    private string? LoadCachedToken()
    {
        if (!File.Exists(_tokenCachePath)) return null;
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(_tokenCachePath));
            return doc.RootElement.GetProperty("access_token").GetString();
        }
        catch { return null; }
    }

    private void SaveCachedToken(string accessToken)
    {
        File.WriteAllText(_tokenCachePath, JsonSerializer.Serialize(new { access_token = accessToken }));
    }

    private static async Task WriteFrameAsync(NamedPipeClientStream pipe, int opcode, object payload)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(payload);
        var header = new byte[8];
        BitConverter.GetBytes(opcode).CopyTo(header, 0);
        BitConverter.GetBytes(json.Length).CopyTo(header, 4);

        var buffer = new byte[header.Length + json.Length];
        header.CopyTo(buffer, 0);
        json.CopyTo(buffer, header.Length);

        await pipe.WriteAsync(buffer);
        await pipe.FlushAsync();
    }

    private static async Task<(int opcode, string json)> ReadFrameAsync(NamedPipeClientStream pipe)
    {
        var header = new byte[8];
        await ReadExactAsync(pipe, header, 8);

        int opcode = BitConverter.ToInt32(header, 0);
        int length = BitConverter.ToInt32(header, 4);

        var body = new byte[length];
        await ReadExactAsync(pipe, body, length);

        return (opcode, Encoding.UTF8.GetString(body));
    }

    private static async Task ReadExactAsync(Stream stream, byte[] buffer, int count)
    {
        int offset = 0;
        while (offset < count)
        {
            int read = await stream.ReadAsync(buffer.AsMemory(offset, count - offset));
            if (read == 0) throw new IOException("Pipe Discord fermé.");
            offset += read;
        }
    }

    public void Dispose() => _pipe?.Dispose();

    public async Task<(bool mute, bool deaf)?> GetVoiceSettingsAsync()
    {
        var response = await SendCommandAsync("GET_VOICE_SETTINGS", new Dictionary<string, object>());
        if (response is null) return null;

        var data = response.Value.GetProperty("data");
        return (data.GetProperty("mute").GetBoolean(), data.GetProperty("deaf").GetBoolean());
    }
}