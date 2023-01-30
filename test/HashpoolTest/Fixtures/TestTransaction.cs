using Google.Protobuf;
using Proto;

namespace HashpoolTest.Fixtures;

public static class TestTransaction
{
    private static readonly long NanosPerTick = 1_000_000_000L / TimeSpan.TicksPerSecond;

    public static (long seconds, int nanos) TimeSpanFromDate(DateTime dateTime)
    {
        TimeSpan timespan = dateTime - DateTime.UnixEpoch;
        long seconds = (long)timespan.TotalSeconds;
        int nanos = (int)((timespan.Ticks - (seconds * TimeSpan.TicksPerSecond)) * NanosPerTick);
        return (seconds, nanos);
    }

    public static DateTime DateFromTimeStamp(Timestamp timestamp)
    {
        return DateTime.UnixEpoch.AddTicks(timestamp.Seconds * TimeSpan.TicksPerSecond + timestamp.Nanos / NanosPerTick);
    }
    public static DateTime DateFromTimeStampKey(string timestamp)
    {
        return DateTime.UnixEpoch.AddSeconds(double.Parse(timestamp));
    }

    public static TransactionID CreateTransactionId(string payer, TimeSpan offset)
    {
        var (seconds, nanos) = TimeSpanFromDate(DateTime.UtcNow.Add(offset));
        return new TransactionID
        {
            AccountID = AccountID.FromKeyString(payer),
            TransactionValidStart = new Timestamp
            {
                Seconds = seconds,
                Nanos = nanos
            }
        };
    }

    public static TransactionBody CreateDefaultTransactionBody(TestContext context, TimeSpan offset)
    {
        return CreateTransactionBody(offset, context.Payer, context.GossipNode!.Account!, context.Payer, context.GossipNode!.Account!, 1);
    }

    public static TransactionBody CreateTransactionBody(TimeSpan offset, string payer, string node, string fromAccount, string toAccount, long amount)
    {
        var transfers = new TransferList();
        transfers.AccountAmounts.Add(new AccountAmount { AccountID = AccountID.FromKeyString(fromAccount), Amount = -amount });
        transfers.AccountAmounts.Add(new AccountAmount { AccountID = AccountID.FromKeyString(toAccount), Amount = amount });
        var cryptoTransferTransactionBody = new CryptoTransferTransactionBody { Transfers = transfers };
        return new TransactionBody
        {
            TransactionID = CreateTransactionId(payer, offset),
            NodeAccountID = AccountID.FromKeyString(node),
            TransactionFee = 2_00_000_000,
            TransactionValidDuration = new Duration { Seconds = 180 },
            CryptoTransfer = cryptoTransferTransactionBody,
        };
    }

    public static ByteString CreateSignedTransactionBytes(TransactionBody transactionBody, SignatureMap? sigMap = null)
    {
        return new SignedTransaction
        {
            BodyBytes = transactionBody.ToByteString(),
            SigMap = sigMap
        }.ToByteString();
    }
}