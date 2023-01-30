using Google.Protobuf;
using Hashpool;
using HashpoolApi.Models;
using Microsoft.AspNetCore.Mvc;
using Proto;

namespace HashpoolApi.Controllers;

/// <summary>
/// Orchestrates the signing and submission of Hedera transactions.
/// </summary>
[ApiController]
[Route("[controller]")]
public class TransactionsController : ControllerBase
{
    /// <summary>
    /// System logger for this controller.
    /// </summary>
    private readonly ILogger<TransactionsController> _logger;
    /// <summary>
    /// The registry of currently held partially signed or 
    /// awaiting transactions.
    /// </summary>
    private readonly HashpoolRegistry _registry;
    /// <summary>
    /// Transaction Controller constructor.
    /// </summary>
    /// <param name="registry">
    /// The registry of currently held partially signed or 
    /// awaiting transactions.
    /// </param>
    /// <param name="logger">
    /// System logger for this controller.
    /// </param>
    public TransactionsController(HashpoolRegistry registry, ILogger<TransactionsController> logger)
    {
        _registry = registry;
        _logger = logger;
    }
    /// <summary>
    /// Retrieves information regarding the state of the transaction.  
    /// </summary>
    /// <remarks>
    /// For transactions that have been submitted, it will include the 
    /// network's precheck code from the submission as well.  
    /// Transactions will remain in the cache for up to three minutes 
    /// after submission to the Hedera Network.
    /// </remarks>
    /// <param name="id">
    /// The ID of the transaction in 0.0.0@000000.00000000 form.
    /// </param>
    /// <response code="200">
    /// A JSON structure conveying the details of the state and history 
    /// of the transaction.  This information does not include the 
    /// transaction protobuf bytes itself.
    /// </response>
    /// <response code="404">
    /// If the transaction is not found in the cache.  Executed 
    /// transactions are removed from the cache three minutes after 
    /// submission to the Hedera Network.
    /// </response>
    [HttpGet("{id}", Name = "GetTransactionInfo")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<TransactionInfo> GetTransactionInfo(string id)
    {
        if (_registry.TryGetTransaction(TransactionID.FromKeyString(id), out HashpoolTransaction transaction))
        {
            var history = transaction.History.Select(r => new TransactionHistoryInfo
            {
                Timestamp = (r.Timestamp - DateTime.UnixEpoch).TotalSeconds.ToString("F9"),
                Description = r.Description
            }).ToArray();
            var precheckCode = transaction.Status == HashpoolTransactionStatus.Completed ? transaction.PrecheckCode : ResponseCodeEnum.Unknown;
            return Ok(new TransactionInfo
            {
                TransactionId = transaction.TransactionId.ToKeyString(),
                Node = transaction.NodeAccountId.ToKeyString(),
                Duration = transaction.TransactionDuration,
                Type = transaction.TransactionType.ToString(),
                Status = transaction.Status,
                SignedBy = transaction.Signatures.Select(p => p.PubKeyPrefix.ToBase64()).ToArray(),
                History = history,
                PrecheckCode = precheckCode
            });
        }
        return NotFound();
    }
    /// <summary>
    /// Retrieves the transaction receipt from the gossip network if available.
    /// </summary>
    /// <param name="id">
    /// The ID of the transaction in 0.0.0@000000.00000000 form.
    /// </param>
    /// <response code="200">
    /// The receipt returned from the network, encoded in native HAPI protobuf.
    /// </response>
    /// <response code="400">
    /// The receipt could not be obtained from the network, which can be
    /// the result of multiple reasons, including asking for the receipt
    /// before the transaction can be submitted to the network.
    /// </response>
    /// <response code="404">
    /// If the transaction is not found in the cache.  Executed 
    /// transactions are removed from the cache three minutes after 
    /// submission to the Hedera Network.
    /// </response>
    [HttpGet("{id}/receipt", Name = "GetTransactionReceipt")]
    [Produces("application/octet-stream", "application/base64", "text/plain")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ByteString>> GetTransactionReceipt(string id)
    {
        if (_registry.TryGetTransaction(TransactionID.FromKeyString(id), out HashpoolTransaction transaction))
        {
            var receipt = await _registry.GetTransactionReceiptAsync(transaction);
            if (receipt is not null)
            {
                return Ok(receipt.ToByteString());
            }
        }
        return NotFound();
    }
    /// <summary>
    /// Retrieves the transactions encoded as a SignedTransctionBytes protobuf 
    /// byte array.
    /// </summary>
    /// <remarks>
    /// This includes the details of the transaction and any 
    /// signatures that have been added to the transaction.
    /// </remarks>
    /// <param name="id">
    /// The ID of the transaction in 0.0.0@000000.00000000 form.
    /// </param>
    /// <response code="200">
    /// The transaction's protobuf as a byte array encoded as requested. 
    /// </response>
    /// <response code="404">
    /// If the transaction is not found in the cache.  Executed 
    /// transactions are removed from the cache three minutes after 
    /// submission to the Hedera Network.
    /// </response>
    [HttpGet("{id}/protobuf", Name = "GetTransactionProtobuf")]
    [Produces("application/octet-stream", "application/base64", "text/plain")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<ByteString> GetTransaction(string id)
    {
        if (_registry.TryGetTransaction(TransactionID.FromKeyString(id), out HashpoolTransaction transaction))
        {
            return Ok(transaction.ToSignedTransactionBytes());
        }
        return NotFound();
    }
    /// <summary>
    /// Retrieves the list of transactions currently held within the cache.
    /// </summary>
    /// <response code="200">
    /// An array of transaction summary objects.
    /// </response>
    [HttpGet(Name = "GetTransactions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IEnumerable<TransactionSummary> GetTransactions()
    {
        return _registry.GetTransactions().Select(transaction =>
        {
            var precheckCode = transaction.Status == HashpoolTransactionStatus.Completed ? transaction.PrecheckCode : ResponseCodeEnum.Unknown;
            return new TransactionSummary
            {
                TransactionId = transaction.TransactionId.ToKeyString(),
                Node = transaction.NodeAccountId.ToKeyString(),
                Duration = transaction.TransactionDuration,
                Type = transaction.TransactionType.ToString(),
                Status = transaction.Status,
                PrecheckCode = precheckCode
            };
        });
    }
    /// <summary>
    /// Adds a transaction to the pending list of transactions in the cache. 
    /// </summary>
    /// <remarks>
    /// If the transaction already exists, the signatures contained in this 
    /// submission will be copied to the list already held in the cache.
    /// </remarks>
    /// <param name="signedTransactionBytes">
    /// A byte array of the protobuf representing a SignedTransaction structure, 
    /// which may or not contain a signature map with preliminary signatures.
    /// </param>
    /// <response code="201">
    /// The bytes of the protobuf of the resulting SignedTransaction 
    /// (encoded as requested), containing any known signatures for the transaction.
    /// </response>
    /// <response code="400">
    /// If the transaction protobuf is malformed or not readable by the
    /// parser, or if the cache holds a transaction with the same ID
    /// but the BodyBytes of this transaction differ.
    /// </response>
    [HttpPost]
    [Consumes("application/octet-stream", "application/base64", "text/plain")]
    [Produces("application/octet-stream", "application/base64", "text/plain")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<string> Post([FromBody] ByteString signedTransactionBytes)
    {
        var tx = new HashpoolTransaction(signedTransactionBytes);
        _registry.AddTransaction(tx);
        if (_registry.TryGetTransaction(tx.TransactionId, out HashpoolTransaction possiblyMergedTransaction))
        {
            return CreatedAtRoute("GetTransactionProtobuf", new { id = tx.TransactionId.ToKeyString() }, possiblyMergedTransaction.ToSignedTransactionBytes());
        }
        // Ok, this could possibly happen if the transaction somehow
        // managed to execute and purge from the database before the thread
        // tried to retreive the copy.
        return CreatedAtRoute("GetTransactionProtobuf", new { id = tx.TransactionId.ToKeyString() }, tx.ToSignedTransactionBytes());
    }
    /// <summary>
    /// Adds one or more signatures to a transaction held in cache.
    /// </summary>
    /// <param name="id">
    /// The ID of the transaction in 0.0.0@000000.00000000 form.
    /// </param>
    /// <param name="signatureMapBytes">
    /// The bytes of the protobuf representing a SignatureMap object 
    /// which can contain one or more signatures for the given transaction.
    /// </param>
    /// <response code="200">
    /// The bytes of the protobuf of the resulting SignedTransaction 
    /// (encoded as requested), containing any known signatures for the transaction.
    /// </response>
    /// <response code="400">
    /// If the transaction has already been submitted to the network for processing.
    /// </response>
    /// <response code="404">
    /// If the transaction is not found in the cache.  
    /// </response>
    [HttpPost("{id}/signatures")]
    [Consumes("application/octet-stream", "application/base64", "text/plain")]
    [Produces("application/octet-stream", "application/base64", "text/plain")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<ByteString> SignTransaction(string id, [FromBody] ByteString signatureMapBytes)
    {
        if (_registry.TryGetTransaction(TransactionID.FromKeyString(id), out HashpoolTransaction transaction))
        {
            transaction.AddSignatures(signatureMapBytes);
            return Ok(transaction.ToSignedTransactionBytes());
        }
        return NotFound();
    }
}