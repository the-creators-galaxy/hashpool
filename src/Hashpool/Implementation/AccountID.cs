using Google.Protobuf;
using Hashpool;
using System.Diagnostics.CodeAnalysis;

namespace Proto;

public sealed partial class AccountID : IMessage<AccountID>
{
    public bool IsProperAccountId
    {
        get
        {
            if (_unknownFields?.CalculateSize() > 0 ||
                shardNum_ < 0 ||
                realmNum_ < 0 ||
                accountCase_ == AccountOneofCase.None ||
                account_ is null)
            {
                return false;
            }
            return true;
        }
    }

    public string ToKeyString()
    {
        if (accountCase_ == AccountOneofCase.Alias)
        {
            return $"{shardNum_}.{realmNum_}.{Convert.ToHexString(Alias.ToArray())}";
        }
        else
        {
            return $"{shardNum_}.{realmNum_}.{AccountNum}";
        }
    }
    public static bool TryParseFromKeyString(string? value, [NotNullWhen(true)] out AccountID? accountId)
    {
        if (!string.IsNullOrEmpty(value))
        {
            AccountID account = new();
            var parts = value.Split('.');
            if (parts.Length == 3)
            {
                if (long.TryParse(parts[0], out account.shardNum_) && long.TryParse(parts[1], out account.realmNum_) && account.shardNum_ >= 0 && account.realmNum_ >= 0)
                {
                    if (long.TryParse(parts[2], out long num) && num >= 0)
                    {
                        account.AccountNum = num;
                        accountId = account;
                        return true;
                    }
                    else
                    {
                        var alias = Convert.FromHexString(parts[2]);
                        account.Alias = ByteString.CopyFrom(alias);
                        accountId = account;
                        return true;
                    }
                }
            }
        }
        accountId = default;
        return false;
    }
    public static AccountID FromKeyString(string value)
    {
        if (TryParseFromKeyString(value, out AccountID? accountId))
        {
            return accountId;
        }
        throw new HashpoolException(HashpoolCode.InvalidTransactionId, "Unable to parse account from transaction id string.");
    }

}
