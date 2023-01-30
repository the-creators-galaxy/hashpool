namespace Hashpool
{
    public enum HashpoolCode
    {
        InvalidTransactionId,
        DuplicateTransactionId,
        TransactionNotFound,
        GossipNodeNotAvailable,
        TransactionAlreadyExpired,
        UnsupportedTransactionType,
        UnparsableTransactionProtobuf,
        UnparsableSignatureMapProtobuf,
        TooLateToSignTransaction,
        TransactionReceiptNotFound,
        TransactionNotSubmitted,
        NetworkTooBusyToGetReceipt,
        TransactionFailedPrecheck
    }
}
