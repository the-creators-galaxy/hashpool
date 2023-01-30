#pragma warning disable CS8618
using System.Text.Json.Serialization;

namespace HashpoolApi.Models;
/// <summary>
/// History Actions taken on a transaction.
/// </summary>
public class TransactionHistoryInfo
{
    /// <summary>
    /// Date/Time the action took place.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; }
    /// <summary>
    /// Description of the action.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; }
}
