using System;
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
        // 1. Récupération du volume actuel depuis le cache
        double currentVolume = await SonarVolumeState.GetOrFetchAsync(Client, _sonarAddress, _channel);

        // 2. Calcul du nouveau volume
        double newVolume = Math.Clamp(currentVolume + (ticks * _step), 0.0, 1.0);

        // Debug : Affichage du calcul local dans la console
        Console.WriteLine($"[SonarVolume] Canal: {_channel} | Ticks: {ticks} | Volume précédent: {currentVolume:P0} -> Nouveau volume: {newVolume:P0}");

        // 3. Formatage de la valeur en notation anglo-saxonne (point décimal)
        string volumeString = newVolume.ToString("0.00", CultureInfo.InvariantCulture);

        // 4. Construction de l'URL et envoi de la requête PUT
        string url = $"http://{_sonarAddress}/volumeSettings/classic/{_channel}/Volume/{volumeString}";

        try
        {
            var response = await Client.PutAsync(url, null);

            // Debug : Affichage de la réponse HTTP de Sonar
            if (response.IsSuccessStatusCode)
            {
                SonarVolumeState.Set(_channel, newVolume);
                var percent = (int)Math.Round(newVolume * 100);
                _notifier?.ShowNotification($"{SonarChannels.GetDisplayName(_channel)}: {percent}%");
                Console.WriteLine($"[SonarVolume] OK ({response.StatusCode}) -> URL appelée : {url}");
            }
            else
            {
                Console.WriteLine($"[SonarVolume] ERREUR ({response.StatusCode}) lors de l'appel : {url}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SonarVolume] EXCEPTION ({_channel}) : {ex.Message}");
        }
    }

    private static HttpClient CreateClient()
    {
        // Remplace par ta méthode d'instanciation de HttpClient existante dans le projet
        return new HttpClient();
    }
}