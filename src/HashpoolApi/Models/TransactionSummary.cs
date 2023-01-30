#pragma warning disable CS8618
using Hashpool;
using Proto;
using System.Text.Json.Serialization;

namespace HashpoolApi.Models;
/// <summary>
/// Metadata concerning a transaction held within the cache.
/// </summary>
public class TransactionSummary
{
    /// <summary>
    /// The ID of the transaction in 0.0.0@0000.000000000 form.
    /// </summary>
    [JsonPropertyName("transaction_id")]
    public string TransactionId { get; set; }
    /// <summary>
    /// The Hedera node that the transaction will be submitted to.
    /// </summary>
    [JsonPropertyName("node")]
    public string Node { get; set; }
    /// <summary>
    /// The lifetime duration of the transaction in seconds 
    /// (typically 120-180).
    /// </summary>
    [JsonPropertyName("duration")]
    public long Duration { get; set; }
    /// <summary>
    /// The type of transaction.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }
    /// <summary>
    /// The current status of the transaction, one of: Queued, 
    /// Submitting or Completed.
    /// </summary>
    [JsonPropertyName("status")]
    public HashpoolTransactionStatus Status { get; set; }
    /// <summary>
    /// The PreCheck code returned from the Hedera Network 
    /// upon submission, or UNKNOWN if the transaction has 
    /// not yet been submitted.
    /// </summary>
    [JsonPropertyName("precheck_code")]
    public ResponseCodeEnum PrecheckCode { get; set; }
}
