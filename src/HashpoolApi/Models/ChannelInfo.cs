#pragma warning disable CS8618
using System.Text.Json.Serialization;

namespace HashpoolApi.Models;
/// <summary>
/// Mapping of a Hedera Network Node’s Wallet to known GRPC endpoints.
/// </summary>
public class ChannelInfo
{
    /// <summary>
    /// The Gossip Node's Wallet Account ID
    /// </summary>
    [JsonPropertyName("account")]
    public string Account { get; set; }
    /// <summary>
    /// Known GRPC endpoints for the associated 
    /// Hedera Gossip Node.
    /// </summary>
    [JsonPropertyName("endpoints")]
    public string[] Endpoints { get; set; }
}
