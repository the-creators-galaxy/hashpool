using Proto;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Hashpool;
public class HashpoolRegistry
{
    private readonly ExecutionEngine _engine;
    private readonly ConcurrentDictionary<TransactionID, HashpoolTransaction> _transactions;

    public long Count => _transactions.LongCount();

    public HashpoolRegistry(ExecutionEngine engine)
    {
        _engine = engine;
        _transactions = new();
    }

    public void AddTransaction(HashpoolTransaction transaction)
    {
        _transactions.AddOrUpdate(transaction.TransactionId, RegisterTransaction, MergeTransaction, transaction);
    }

    public IEnumerable<HashpoolTransaction> GetTransactions()
    {
        return _transactions.Values;
    }

    public bool TryGetTransaction(TransactionID transactionId, [NotNullWhen(true)] out HashpoolTransaction transaction)
    {
        return _transactions.TryGetValue(transactionId, out transaction!);
    }

    public async Task<TransactionReceipt> GetTransactionReceiptAsync(HashpoolTransaction transaction)
    {
        if (transaction.Status == HashpoolTransactionStatus.Queued)
        {
            // Wait a nominal amount of time if we're "close" to submission.
            var sleep = DateTime.UnixEpoch.AddSeconds(transaction.TransactionId.TransactionValidStart.Seconds) - DateTime.UtcNow;
            if (sleep.TotalMilliseconds < 500)
            {
                await Task.Delay(600);
            }
            else
            {
                throw new HashpoolException(HashpoolCode.TransactionNotSubmitted, "Transaction's receipt was not retrievable because it has not been submitted to the network.");
            }
        }
        if (transaction.Status == HashpoolTransactionStatus.Submitting)
        {
            // Wait up to 30 seconds for precheck submission process to complete
            for (var retry = 0; transaction.Status == HashpoolTransactionStatus.Submitting && retry < 60; retry++)
            {
                await Task.Delay(500);
            }
        }
        if (transaction.Status == HashpoolTransactionStatus.Submitting)
        {
            // We waited long enough, something is off.
            throw new HashpoolException(HashpoolCode.NetworkTooBusyToGetReceipt, "Transaction has issues with submission, the network may be too busy, unable to retrieve receipt at this time.");
        }
        if (transaction.PrecheckCode != ResponseCodeEnum.Ok)
        {
            throw new HashpoolException(HashpoolCode.TransactionFailedPrecheck, $"Transaction failed precheck with code {transaction.PrecheckCode}, there is no receipt to retrieve.");
        }
        return await _engine.GetReceiptAsync(transaction);
    }

    private HashpoolTransaction RegisterTransaction(TransactionID _, HashpoolTransaction newTransaction)
    {
        ScheduleTransactionAsync(newTransaction);
        return newTransaction;
    }

    private HashpoolTransaction MergeTransaction(TransactionID _, HashpoolTransaction existingTransaction, HashpoolTransaction newTransaction)
    {
        existingTransaction.MergeTransaction(newTransaction);
        return existingTransaction;
    }

    private void ScheduleTransactionAsync(HashpoolTransaction transaction)
    {
        var sleep = DateTime.UnixEpoch.AddSeconds(transaction.TransactionId.TransactionValidStart.Seconds) - DateTime.UtcNow;
        if (sleep.TotalSeconds < -transaction.TransactionDuration)
        {
            throw new HashpoolException(HashpoolCode.TransactionAlreadyExpired, "The transaction's start time & duration are in the past, transaction would not suceed if submitted.");
        }
        Task.Run(async () =>
        {
            if (sleep.TotalMilliseconds > 0)
            {
                await Task.Delay(sleep);
            }
            try
            {
                await _engine.ExecuteAsync(transaction);
            }
            finally
            {
                await Task.Delay(TimeSpan.FromMinutes(3));
                _transactions.TryRemove(transaction.TransactionId, out var _);
            }
        });
    }
}
