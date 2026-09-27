using System.Globalization;
using System.Net.Http;

namespace MacroPad.Host;

public class SonarVolumeEncoderAction : IEncoderAction
{
    private static readonly HttpClient Client = CreateClient();
    private readonly string _sonarAddress;
    private readonly string _channel;
    private readonly double _step;

    private readonly IPadNotifier? _notifier;

    public SonarVolumeEncoderAction(string sonarAddress, string channel, double step = 0.05, IPadNotifier? notifier = null)
    {
        _sonarAddress = sonarAddress;
        _channel = channel;
        _step = step;
        _notifier = notifier;
    }

    public async void Execute(int ticks)
    {
        var streamerMode = await SonarStreamerMode.IsActiveAsync(Client, _sonarAddress);
        var sliders = streamerMode
            ? SonarStreamerMode.VolumeSlidersFor(_channel).Cast<string?>().ToArray()
            : new string?[] { null };

        double lastApplied = 0;
        bool anySuccess = false;

        foreach (var slider in sliders)
        {
            double currentVolume = await SonarVolumeState.GetOrFetchAsync(Client, _sonarAddress, _channel, slider);
            double newVolume = Math.Clamp(currentVolume + (ticks * _step), 0.0, 1.0);
            string volumeString = newVolume.ToString("0.00", CultureInfo.InvariantCulture);

            string url = slider is null
                ? $"http://{_sonarAddress}/volumeSettings/classic/{_channel}/Volume/{volumeString}"
                : $"http://{_sonarAddress}/volumeSettings/streamer/{slider}/{_channel}/Volume/{volumeString}";

            try
            {
                var response = await Client.PutAsync(url, null);
                if (response.IsSuccessStatusCode)
                {
                    SonarVolumeState.Set(_channel, newVolume, slider);
                    lastApplied = newVolume;
                    anySuccess = true;
                    Console.WriteLine($"[SonarVolume] {_channel}{(slider is null ? "" : $"/{slider}")} -> {newVolume:P0} (OK)");
                }
                else
                {
                    Console.WriteLine($"[SonarVolume] ERREUR ({response.StatusCode}) : {url}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SonarVolume] EXCEPTION ({_channel}) : {ex.Message}");
            }
        }

        if (anySuccess)
            _notifier?.ShowNotification($"{SonarChannels.GetDisplayName(_channel)}: {(int)Math.Round(lastApplied * 100)}%");
    }

    private static HttpClient CreateClient() => new();
}