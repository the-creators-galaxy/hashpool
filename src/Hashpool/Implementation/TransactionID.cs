using Google.Protobuf;
using Hashpool;

namespace Proto;

public sealed partial class TransactionID : IMessage<TransactionID>
{
    public bool IsProperTransactionId
    {
        get
        {
            if (_unknownFields?.CalculateSize() > 0 ||
                transactionValidStart_ is null ||
                accountID_ is null ||
                !accountID_.IsProperAccountId)
            {
                return false;
            }
            return true;
        }
    }
    public string ToKeyString()
    {
        var value = $"{accountID_.ToKeyString()}@{transactionValidStart_.Seconds}.{transactionValidStart_.Nanos.ToString("D9")}";
        if (nonce_ != 0)
        {
            value = $"{value}:{nonce_}";
        }
        if (scheduled_)
        {
            value = value + "+scheduled";
        }
        return value;
    }

    public static TransactionID FromKeyString(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            (var scheduled, value) = parseOutScheduled(value);
            (var nonce, value) = parseOutNonce(value);
            var (account, timestamp) = parseOutAccountAndTimestamp(value);
            return new TransactionID()
            {
                accountID_ = account,
                transactionValidStart_ = timestamp,
                nonce_ = nonce,
                scheduled_ = scheduled
            };
        }
        throw new HashpoolException(HashpoolCode.InvalidTransactionId, "Unable to parse string into a transaction id.");
    }

    private static (bool, string) parseOutScheduled(string value)
    {
        if (value.EndsWith("+scheduled", StringComparison.InvariantCultureIgnoreCase))
        {
            return (true, value.Substring(0, value.Length - 10));
        }
        return (false, value);
    }

    private static (int, string) parseOutNonce(string value)
    {
        var parts = value.Split(':');
        if (parts.Length == 1)
        {
            return (0, value);
        }
        else if (parts.Length == 2 && int.TryParse(parts[1], out int nonce) && nonce >= 0)
        {
            return (nonce, parts[1]);
        }
        throw new HashpoolException(HashpoolCode.InvalidTransactionId, "Unable to parse nonce from transaction string.");
    }

    private static (AccountID, Timestamp) parseOutAccountAndTimestamp(string value)
    {
        var parts = value.Split('@');
        if (parts.Length == 2)
        {
            return (AccountID.FromKeyString(parts[0]), Timestamp.FromKeyString(parts[1]));
        }
        throw new HashpoolException(HashpoolCode.InvalidTransactionId, "Unable to parse string into a transaction id.");
    }
}
