using System.Collections.Concurrent;
using System.Net;
using System.Text;

namespace BookingWeb.IntegrationTests.Infrastructure;

public sealed class FakeNominatimHandler : HttpMessageHandler
{
    public const string KyivSearchJson = """
        [{
          "place_id": 1,
          "lat": "50.4473",
          "lon": "30.5225",
          "display_name": "Хрещатик, Київ, Україна",
          "boundingbox": ["50.4400", "50.4500", "30.5200", "30.5300"]
        }]
        """;

    public const string KyivReverseJson = """
        {
          "place_id": 2,
          "lat": "50.4501",
          "lon": "30.5234",
          "display_name": "Хрещатик, Київ, 01001, Україна",
          "address": { "road": "Хрещатик", "city": "Київ", "state": "Київ", "country": "Україна", "country_code": "ua" }
        }
        """;

    private readonly ConcurrentQueue<SentRequest> _requests = new();
    private volatile Func<HttpRequestMessage, HttpResponseMessage> _responder = DefaultResponse;

    public sealed record SentRequest(Uri Uri, string UserAgent);

    public IReadOnlyList<SentRequest> Requests => _requests.ToList();

    public void RespondWith(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;

    public void RespondWithJson(string json) => _responder = _ => Json(json);

    public void RespondWithStatus(HttpStatusCode status) => _responder = _ => new HttpResponseMessage(status);

    public void Throw(Exception exception) => _responder = _ => throw exception;

    public void Reset()
    {
        _requests.Clear();
        _responder = DefaultResponse;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // A snapshot, because the request object may be disposed after the call.
        _requests.Enqueue(new SentRequest(request.RequestUri!, request.Headers.UserAgent.ToString()));
        return Task.FromResult(_responder(request));
    }

    private static HttpResponseMessage DefaultResponse(HttpRequestMessage request) =>
        request.RequestUri!.AbsolutePath.EndsWith("/reverse") ? Json(KyivReverseJson) : Json(KyivSearchJson);

    private static HttpResponseMessage Json(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
}
