using Google.Protobuf;

namespace Proto;

public sealed partial class TransactionBody : IMessage<TransactionBody>
{
    public bool IsProperTransactionBody
    {
        get
        {
            if (transactionID_ is null ||
                !transactionID_.IsProperTransactionId ||
                nodeAccountID_ is null ||
                !nodeAccountID_.IsProperAccountId ||
                transactionValidDuration_ is null ||
                dataCase_ == DataOneofCase.None ||
                data_ is null)
            {
                return false;
            }
            return true;
        }

    }
}
