using Google.Protobuf;
using Grpc.Core;
using Proto;

namespace Hashpool
{
    public class ExecutionEngine
    {
        private readonly ChannelRegistry _channels;
        public ExecutionEngine(ChannelRegistry channels)
        {
            _channels = channels;
        }
        public async Task ExecuteAsync(HashpoolTransaction transaction)
        {
            try
            {
                transaction.Status = HashpoolTransactionStatus.Submitting;
                transaction.PrecheckCode = await ExecuteImplementation(transaction);
                transaction.History.Add(new HashpoolHistoryRecord(DateTime.UtcNow, "Submission Received Response Code: " + transaction.PrecheckCode.ToString()));
            }
            catch (Exception ex)
            {
                transaction.PrecheckCode = (ResponseCodeEnum)(-2);
                transaction.History.Add(new HashpoolHistoryRecord(DateTime.UtcNow, "Submission failed: " + ex.Message));
            }
            finally
            {
                transaction.Status = HashpoolTransactionStatus.Completed;
                transaction.History.Add(new HashpoolHistoryRecord(DateTime.UtcNow, "Processing Completed."));
            }
        }

        public async Task<TransactionReceipt> GetReceiptAsync(HashpoolTransaction transaction)
        {
            var query = new Query
            {
                TransactionGetReceipt = new TransactionGetReceiptQuery
                {
                    TransactionID = transaction.TransactionId
                }
            };
            var channel = await _channels.GetChannel(transaction.NodeAccountId);
            if (channel is null)
            {
                throw new HashpoolException(HashpoolCode.GossipNodeNotAvailable, "Transaction's target gossip node was not found.");
            }
            var retryCount = 0;
            var maxRetries = 30;
            var retryDelay = TimeSpan.FromMilliseconds(200);
            for (; retryCount < maxRetries; retryCount++)
            {
                try
                {
                    var client = new CryptoService.CryptoServiceClient(channel);
                    var response = (await client.getTransactionReceiptsAsync(query)).TransactionGetReceipt;
                    if (response is not null)
                    {
                        if (response.Header?.NodeTransactionPrecheckCode != ResponseCodeEnum.Busy)
                        {
                            var receipt = response.Receipt;
                            if (receipt is not null && receipt.Status != ResponseCodeEnum.Unknown)
                            {
                                return receipt;
                            }
                        }
                    }
                }
                catch (RpcException rpcex) when (rpcex.StatusCode == StatusCode.Unavailable)
                {
                    // GRPC issues, will retry, but with a different endpoint (if exists) to be safe
                    channel = await _channels.GetChannel(transaction.NodeAccountId);
                }
                await Task.Delay(retryDelay * (retryCount + 1)).ConfigureAwait(false);
            }
            throw new HashpoolException(HashpoolCode.TransactionReceiptNotFound, "Transaction's receipt was not retrievable from the network.");
        }
        private async Task<ResponseCodeEnum> ExecuteImplementation(HashpoolTransaction transaction)
        {
            var channel = await _channels.GetChannel(transaction.NodeAccountId);
            if (channel is null)
            {
                throw new HashpoolException(HashpoolCode.GossipNodeNotAvailable, "Transaction's target gossip node was not found.");
            }
            var sendRequest = transaction.GetServiceMethod(channel);
            var sigMap = new SignatureMap();
            sigMap.SigPair.AddRange(transaction.Signatures);
            var signedTransaction = new Transaction
            {
                SignedTransactionBytes = new SignedTransaction
                {
                    BodyBytes = transaction.BodyBytes,
                    SigMap = sigMap
                }.ToByteString()
            };
            try
            {
                var retryCount = 0;
                var maxRetries = 20;
                var retryDelay = TimeSpan.FromMilliseconds(200);
                for (; retryCount < maxRetries; retryCount++)
                {
                    try
                    {
                        var tenativeResponse = await sendRequest(signedTransaction, null, null, default);
                        var responseCode = tenativeResponse.NodeTransactionPrecheckCode;
                        if (responseCode != ResponseCodeEnum.Busy && responseCode != ResponseCodeEnum.InvalidTransactionStart)
                        {
                            return responseCode;
                        }
                    }
                    catch (RpcException rpcex) when (rpcex.StatusCode == StatusCode.Unavailable || rpcex.StatusCode == StatusCode.Unknown)
                    {
                        // This transaction may have actully successfully been processed, in which case 
                        // the receipt will already be in the system.  Check to see if it is there.
                        await Task.Delay(retryDelay * retryCount).ConfigureAwait(false);
                        var responseCode = await CheckForReceipt().ConfigureAwait(false);
                        if (responseCode != ResponseCodeEnum.ReceiptNotFound &&
                            responseCode != ResponseCodeEnum.Busy &&
                            responseCode != ResponseCodeEnum.InvalidTransactionStart)
                        {
                            return responseCode;
                        }
                    }
                    await Task.Delay(retryDelay * (retryCount + 1)).ConfigureAwait(false);
                }
                return (await sendRequest(signedTransaction, null, null, default)).NodeTransactionPrecheckCode;

                async Task<ResponseCodeEnum> CheckForReceipt()
                {
                    // The receipt may actually be in the system.
                    var transactionId = transaction.TransactionId;
                    var query = new Query
                    {
                        TransactionGetReceipt = new TransactionGetReceiptQuery
                        {
                            TransactionID = transactionId
                        }
                    };
                    for (; retryCount < maxRetries; retryCount++)
                    {
                        try
                        {
                            var client = new CryptoService.CryptoServiceClient(channel);
                            var receipt = await client.getTransactionReceiptsAsync(query);
                            return receipt.TransactionGetReceipt.Header.NodeTransactionPrecheckCode;
                        }
                        catch (RpcException rpcex) when (rpcex.StatusCode == StatusCode.Unavailable)
                        {
                            await Task.Delay(retryDelay * (retryCount + 1)).ConfigureAwait(false);
                        }
                    }
                    return ResponseCodeEnum.Unknown;
                }
            }
            catch (RpcException)
            {
                // There is no code for bad gateway/unreachable network
                // This is not ideal, should revisit when the implementation
                // is less naive.
                return (ResponseCodeEnum)(-1);
            }
        }
    }
}
