using System.Net.Http;

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

        if (port is null)
        {
            return; // soundboard jamais lancée / fichier absent -> échec silencieux
        }

        try
        {
            await _http.PostAsync($"http://127.0.0.1:{port}/play/{soundId}", content: null);
        }
        catch
        {
            // Soundboard fermée, port périmé, etc. -> échec silencieux voulu,
            // le macro pad ne doit jamais bloquer sur cet appel.
        }
    }

    private int? ReadCurrentPort()
    {
        if (!File.Exists(_portFilePath))
        {
            return null;
        }

        var content = File.ReadAllText(_portFilePath).Trim();
        return int.TryParse(content, out var port) ? port : null;
    }
}