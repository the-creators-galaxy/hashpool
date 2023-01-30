#pragma warning disable CS8618 
using System.Text.Json.Serialization;

namespace Mirror;

public class GossipNode
{
    [JsonPropertyName("node_account_id")]
    public string Account { get; set; }

    [JsonPropertyName("service_endpoints")]
    public GrpcEndpoint[] Endpoints { get; set; }
}
