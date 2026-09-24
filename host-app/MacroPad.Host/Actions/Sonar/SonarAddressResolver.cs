using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace MacroPad.Host;
public static class SonarAddressResolver
{
    private static readonly HttpClient Client = CreateInsecureClient();

    private static HttpClient CreateInsecureClient()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true
        };
        return new HttpClient(handler);
    }

    public static async Task<string?> GetSonarAddressAsync()
    {
        var corePropsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "SteelSeries", "SteelSeries Engine 3", "coreProps.json");

        if (!File.Exists(corePropsPath))
            return null;

        using var coreDoc = JsonDocument.Parse(File.ReadAllText(corePropsPath));
        if (!coreDoc.RootElement.TryGetProperty("ggEncryptedAddress", out var ggAddr))
            return null;

        var subAppsJson = await Client.GetStringAsync($"https://{ggAddr.GetString()}/subApps");
        using var subAppsDoc = JsonDocument.Parse(subAppsJson);

        var webServerAddress = subAppsDoc.RootElement
            .GetProperty("subApps")
            .GetProperty("sonar")
            .GetProperty("metadata")
            .GetProperty("webServerAddress")
            .GetString();

        return webServerAddress?.Replace("http://", "").Replace("https://", "");
    }
}