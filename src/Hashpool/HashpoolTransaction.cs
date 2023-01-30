namespace Hashpool;

using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using Proto;
using System.Collections.Concurrent;

public record HashpoolTransaction
{
    private readonly TransactionID _transactionId;
    private readonly AccountID _nodeAccountId;
    private readonly TransactionBody.DataOneofCase _transactionType;
    private readonly long _transactionDuration;
    private readonly ByteString _bodyBytes;
    private readonly ConcurrentBag<SignaturePair> _signatures;
    private readonly DateTime _createdTime;
    private readonly ConcurrentBag<HashpoolHistoryRecord> _history;

    public TransactionID TransactionId => _transactionId;
    public long TransactionDuration => _transactionDuration;
    public AccountID NodeAccountId => _nodeAccountId;
    public TransactionBody.DataOneofCase TransactionType => _transactionType;
    public HashpoolTransactionStatus Status { get; internal set; }
    public ResponseCodeEnum PrecheckCode { get; internal set; }
    public ByteString BodyBytes => _bodyBytes;
    public ConcurrentBag<SignaturePair> Signatures => _signatures;
    public ConcurrentBag<HashpoolHistoryRecord> History => _history;


    public HashpoolTransaction(ByteString signedTransactionBytes)
    {
        var (signedTransaction, transactionBody) = ParseProtobufBytes(signedTransactionBytes);
        _bodyBytes = signedTransaction.BodyBytes;
        _transactionId = transactionBody.TransactionID;
        _nodeAccountId = transactionBody.NodeAccountID;
        _transactionType = transactionBody.DataCase;
        Status = HashpoolTransactionStatus.Queued;
        _transactionDuration = transactionBody.TransactionValidDuration.Seconds;
        _signatures = signedTransaction.SigMap is null ? new ConcurrentBag<SignaturePair>() : new ConcurrentBag<SignaturePair>(signedTransaction.SigMap.SigPair);
        _createdTime = DateTime.UtcNow;
        _history = new ConcurrentBag<HashpoolHistoryRecord>();
        _history.Add(new HashpoolHistoryRecord(_createdTime, "Transaction Received"));
        foreach (var sig in _signatures)
        {
            _history.Add(new HashpoolHistoryRecord(_createdTime, $"Signed by {sig.PubKeyPrefix.ToBase64()}"));
        }
    }
    public void MergeTransaction(HashpoolTransaction newTransaction)
    {
        if (_bodyBytes.Equals(newTransaction.BodyBytes))
        {
            AddSignatures(newTransaction.Signatures);
        }
        else
        {
            throw new HashpoolException(HashpoolCode.DuplicateTransactionId, "Transaction already exists with the same ID but different transaction body bytes.");
        }
    }
    public void AddSignatures(ByteString signatureMapBytes)
    {
        AddSignatures(ParseSignatureMapBytes(signatureMapBytes).SigPair);
    }

    public void AddSignatures(IEnumerable<SignaturePair> signatures)
    {
        if (Status != HashpoolTransactionStatus.Queued)
        {
            throw new HashpoolException(HashpoolCode.TooLateToSignTransaction, "Transaction has already been submitted to the network, it can not longer be signed.");
        }
        var timestamp = DateTime.UtcNow;
        foreach (var sig in signatures)
        {
            _signatures.Add(sig);
            _history.Add(new HashpoolHistoryRecord(timestamp, $"Signed by {sig.PubKeyPrefix.ToBase64()}"));
        }
    }

    public ByteString ToSignedTransactionBytes()
    {
        var sigMap = new SignatureMap();
        sigMap.SigPair.AddRange(Signatures);
        return new SignedTransaction
        {
            BodyBytes = BodyBytes,
            SigMap = sigMap
        }.ToByteString();
    }

    public Func<Transaction, Metadata?, DateTime?, CancellationToken, AsyncUnaryCall<TransactionResponse>> GetServiceMethod(GrpcChannel channel)
    {
        return _transactionType switch
        {
            TransactionBody.DataOneofCase.ContractCall => new SmartContractService.SmartContractServiceClient(channel).contractCallMethodAsync,
            TransactionBody.DataOneofCase.ContractCreateInstance => new SmartContractService.SmartContractServiceClient(channel).createContractAsync,
            TransactionBody.DataOneofCase.ContractUpdateInstance => new SmartContractService.SmartContractServiceClient(channel).updateContractAsync,
            TransactionBody.DataOneofCase.ContractDeleteInstance => new SmartContractService.SmartContractServiceClient(channel).deleteContractAsync,
            TransactionBody.DataOneofCase.EthereumTransaction => new SmartContractService.SmartContractServiceClient(channel).callEthereumAsync,
            //TransactionBody.DataOneofCase.CryptoAddLiveHash = Not Supported
            TransactionBody.DataOneofCase.CryptoApproveAllowance => new CryptoService.CryptoServiceClient(channel).approveAllowancesAsync,
            TransactionBody.DataOneofCase.CryptoDeleteAllowance => new CryptoService.CryptoServiceClient(channel).deleteAllowancesAsync,
            TransactionBody.DataOneofCase.CryptoCreateAccount => new CryptoService.CryptoServiceClient(channel).createAccountAsync,
            TransactionBody.DataOneofCase.CryptoDelete => new CryptoService.CryptoServiceClient(channel).cryptoDeleteAsync,
            //TransactionBody.DataOneofCase.CryptoDeleteLiveHash = Not Supported
            TransactionBody.DataOneofCase.CryptoTransfer => new CryptoService.CryptoServiceClient(channel).cryptoTransferAsync,
            TransactionBody.DataOneofCase.CryptoUpdateAccount => new CryptoService.CryptoServiceClient(channel).updateAccountAsync,
            TransactionBody.DataOneofCase.FileAppend => new FileService.FileServiceClient(channel).appendContentAsync,
            TransactionBody.DataOneofCase.FileCreate => new FileService.FileServiceClient(channel).createFileAsync,
            TransactionBody.DataOneofCase.FileDelete => new FileService.FileServiceClient(channel).deleteFileAsync,
            TransactionBody.DataOneofCase.FileUpdate => new FileService.FileServiceClient(channel).updateFileAsync,
            //TransactionBody.DataOneofCase.SystemDelete = Not Supported, need to inspect payload to discern file vs contract.
            //TransactionBody.DataOneofCase.SystemUndelete = Not Supported, need to inspect payload to discern file vs contract.
            TransactionBody.DataOneofCase.Freeze => new FreezeService.FreezeServiceClient(channel).freezeAsync,
            TransactionBody.DataOneofCase.ConsensusCreateTopic => new ConsensusService.ConsensusServiceClient(channel).createTopicAsync,
            TransactionBody.DataOneofCase.ConsensusUpdateTopic => new ConsensusService.ConsensusServiceClient(channel).updateTopicAsync,
            TransactionBody.DataOneofCase.ConsensusDeleteTopic => new ConsensusService.ConsensusServiceClient(channel).deleteTopicAsync,
            TransactionBody.DataOneofCase.ConsensusSubmitMessage => new ConsensusService.ConsensusServiceClient(channel).submitMessageAsync,
            TransactionBody.DataOneofCase.UncheckedSubmit => new NetworkService.NetworkServiceClient(channel).uncheckedSubmitAsync,
            TransactionBody.DataOneofCase.TokenCreation => new TokenService.TokenServiceClient(channel).createTokenAsync,
            TransactionBody.DataOneofCase.TokenFreeze => new TokenService.TokenServiceClient(channel).freezeTokenAccountAsync,
            TransactionBody.DataOneofCase.TokenUnfreeze => new TokenService.TokenServiceClient(channel).unfreezeTokenAccountAsync,
            TransactionBody.DataOneofCase.TokenGrantKyc => new TokenService.TokenServiceClient(channel).grantKycToTokenAccountAsync,
            TransactionBody.DataOneofCase.TokenRevokeKyc => new TokenService.TokenServiceClient(channel).revokeKycFromTokenAccountAsync,
            TransactionBody.DataOneofCase.TokenDeletion => new TokenService.TokenServiceClient(channel).deleteTokenAsync,
            TransactionBody.DataOneofCase.TokenUpdate => new TokenService.TokenServiceClient(channel).updateTokenAsync,
            TransactionBody.DataOneofCase.TokenMint => new TokenService.TokenServiceClient(channel).mintTokenAsync,
            TransactionBody.DataOneofCase.TokenBurn => new TokenService.TokenServiceClient(channel).burnTokenAsync,
            TransactionBody.DataOneofCase.TokenWipe => new TokenService.TokenServiceClient(channel).wipeTokenAccountAsync,
            TransactionBody.DataOneofCase.TokenAssociate => new TokenService.TokenServiceClient(channel).associateTokensAsync,
            TransactionBody.DataOneofCase.TokenDissociate => new TokenService.TokenServiceClient(channel).dissociateTokensAsync,
            TransactionBody.DataOneofCase.TokenFeeScheduleUpdate => new TokenService.TokenServiceClient(channel).updateTokenFeeScheduleAsync,
            TransactionBody.DataOneofCase.TokenPause => new TokenService.TokenServiceClient(channel).pauseTokenAsync,
            TransactionBody.DataOneofCase.TokenUnpause => new TokenService.TokenServiceClient(channel).unpauseTokenAsync,
            TransactionBody.DataOneofCase.ScheduleCreate => new ScheduleService.ScheduleServiceClient(channel).createScheduleAsync,
            TransactionBody.DataOneofCase.ScheduleDelete => new ScheduleService.ScheduleServiceClient(channel).deleteScheduleAsync,
            TransactionBody.DataOneofCase.ScheduleSign => new ScheduleService.ScheduleServiceClient(channel).signScheduleAsync,
            //TransactionBody.DataOneofCase.NodeStakeUpdate = Not Supported
            TransactionBody.DataOneofCase.Prng => new UtilService.UtilServiceClient(channel).prngAsync,
            _ => throw new HashpoolException(HashpoolCode.UnsupportedTransactionType, "The type of transaction that was submitted is not supported at this time."),
        };
    }

    private static (SignedTransaction signedTransaction, TransactionBody transactionBody) ParseProtobufBytes(ByteString signedTransactionBytes)
    {
        try
        {
            var signedTransaction = SignedTransaction.Parser.ParseFrom(signedTransactionBytes);
            if (!signedTransaction.IsProperSignedTransaction)
            {
                throw new HashpoolException(HashpoolCode.UnparsableTransactionProtobuf, "Expected a SignedTransaction protobuf message, received something else.");
            }
            var transactionBody = TransactionBody.Parser.ParseFrom(signedTransaction.BodyBytes);
            if (!transactionBody.IsProperTransactionBody)
            {
                throw new HashpoolException(HashpoolCode.UnparsableTransactionProtobuf, "The transaction bodyBytes for the signed transaction message was not valid.");
            }
            return (signedTransaction, transactionBody);
        }
        catch (InvalidProtocolBufferException)
        {
            throw new HashpoolException(HashpoolCode.UnparsableTransactionProtobuf, "The transaction bodyBytes for the signed transaction message was not valid.");
        }
    }
    private static SignatureMap ParseSignatureMapBytes(ByteString signatureMapBytes)
    {
        try
        {
            var sigMap = SignatureMap.Parser.ParseFrom(signatureMapBytes);
            if (!sigMap.IsProperSignatureMap)
            {
                throw new HashpoolException(HashpoolCode.UnparsableSignatureMapProtobuf, "Expected a SignatureMap protobuf message with at least one signature, received something else.");
            }
            return sigMap;
        }
        catch (InvalidProtocolBufferException)
        {
            throw new HashpoolException(HashpoolCode.UnparsableSignatureMapProtobuf, "The signature map bytes was not valid.");
        }
    }
}
