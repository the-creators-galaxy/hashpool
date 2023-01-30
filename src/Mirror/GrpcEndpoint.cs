using System.Text.Json.Serialization;

namespace Mirror;

public class GrpcEndpoint
{
    [JsonPropertyName("ip_address_v4")]
    public string? Address { get; set; }
    [JsonPropertyName("port")]
    public int Port { get; set; }
}