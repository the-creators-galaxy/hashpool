using System.Net.Http.Json;

namespace Mirror;

public class MirrorRestClient
{
    private readonly string _endpointUrl;
    private readonly HttpClient _client;

    public string EndpointUrl => _endpointUrl;

    public MirrorRestClient(string endpointUrl)
    {
        _endpointUrl = endpointUrl.EndsWith('/') ? endpointUrl[..^1] : endpointUrl;
        _client = new HttpClient();
    }

    public IAsyncEnumerable<GossipNode> GetGossipNodesAsync()
    {
        return GetPagedItems<GossipNodeList, GossipNode>("network/nodes");
    }

    private async IAsyncEnumerable<TItem> GetPagedItems<TList, TItem>(string path) where TList : PagedList<TItem>
    {
        var fullPath = "/api/v1/" + path;
        do
        {
            var payload = await _client.GetFromJsonAsync<TList>(_endpointUrl + fullPath);
            if (payload is not null)
            {
                foreach (var item in payload.GetItems())
                {
                    yield return item;
                }
            }
            fullPath = payload?.Links?.Next;
        }
        while (!string.IsNullOrWhiteSpace(fullPath));
    }
}
