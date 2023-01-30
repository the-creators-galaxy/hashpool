#pragma warning disable CS8618
using System.Text.Json.Serialization;

namespace HashpoolApi.Models;
/// <summary>
/// Contains information regarding the state and 
/// configuration of this hashpool.
/// </summary>
public class HashpoolInfo
{
    /// <summary>
    /// The mirror node this cache relies upon for 
    /// retrieving the list of known Hedera gossip nodes.  
    /// This determines which network the hashpool supports.
    /// </summary>
    [JsonPropertyName("mirror_node")]
    public string MirrorNode { get; set; }
    /// <summary>
    /// A list of Hedera gossip node channels known to this 
    /// mirror node.  Transactions submitted to nodes with 
    /// wallets not known to this cache will fail.
    /// </summary>
    [JsonPropertyName("channels")]
    public ChannelInfo[] Channels { get; set; }
    /// <summary>
    /// The timestamp when this information was generated.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; }
    /// <summary>
    /// The current number of transactions (pending, 
    /// processing and completed) held within the cache.
    /// </summary>
    [JsonPropertyName("transaction_count")]
    public long Count { get; set; }
}
