using System.Net.Http;
using System.Text;

public class HttpAction : IAction
{
    private static readonly HttpClient Client = new();

    private readonly HttpMethod _method;
    private readonly string _url;
    private readonly string? _body;

    public HttpAction(string method, string url, string? body)
    {
        _method = new HttpMethod(method.ToUpperInvariant());
        _url = url;
        _body = body;
    }

    public void Execute()
    {
        var request = new HttpRequestMessage(_method, _url);
        if (_body is not null)
            request.Content = new StringContent(_body, Encoding.UTF8, "application/json");

        Client.SendAsync(request).ContinueWith(async t =>
        {
            if (t.IsFaulted)
            {
                Console.WriteLine($"[HttpAction] Échec requête {_method} {_url} : {t.Exception?.GetBaseException().Message}");
                return;
            }

            var response = t.Result;
            var body = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[HttpAction] {_method} {_url} → HTTP {(int)response.StatusCode} : {body}");
        }, TaskScheduler.Default);
    }
}