using System.Net.Http;
using System.Text.Json;

namespace MacroPad.Soundboard;

public sealed class SoundboardClient
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromMilliseconds(500) };
    private readonly string _portFilePath;

    public SoundboardClient(string portFilePath)
    {
        _portFilePath = portFilePath;
    }

    public async Task PlaySoundAsync(string soundId)
    {
        var port = ReadCurrentPort();
        if (port is null) return;

        try { await _http.PostAsync($"http://127.0.0.1:{port}/play/{soundId}", content: null); }
        catch { }
    }

    public async Task AdjustVolumeAsync(int deltaPercent)
    {
        var port = ReadCurrentPort();
        if (port is null) return;

        try { await _http.PostAsync($"http://127.0.0.1:{port}/volume/adjust/{deltaPercent}", content: null); }
        catch { }
    }

    public async Task<double?> GetVolumeAsync()
    {
        var port = ReadCurrentPort();
        if (port is null) return null;

        try
        {
            var json = await _http.GetStringAsync($"http://127.0.0.1:{port}/volume");
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("volume").GetDouble();
        }
        catch { return null; }
    }

    private int? ReadCurrentPort()
    {
        if (!File.Exists(_portFilePath)) return null;
        var content = File.ReadAllText(_portFilePath).Trim();
        return int.TryParse(content, out var port) ? port : null;
    }
}