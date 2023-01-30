using Google.Protobuf;
using Hashpool;
using HashpoolTest.Fixtures;
using Proto;
using System.Net;
using static HashpoolTest.Fixtures.TestTransaction;

namespace HashpoolTest;

[Collection(nameof(TestContext))]
public class GetTransactionInfoTests
{
    private readonly TestContext _ctx;

    public GetTransactionInfoTests(TestContext ctx)
    {
        _ctx = ctx;
    }
    [Fact(DisplayName = "Transaction Info: Can Get Transaction Info")]
    public async Task ATransactionIncreasesTransactionCount()
    {
        var transactionBody = CreateDefaultTransactionBody(_ctx, TimeSpan.FromMinutes(1));
        var transactionId = transactionBody.TransactionID.ToKeyString();
        var response = await _ctx.PostTransactionsAsync(transactionBody);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var info = await _ctx.GetTransactionInfoAsync(transactionId);
        Assert.NotNull(info);
        Assert.Equal(transactionId, info.TransactionId);
        Assert.Equal(transactionBody.NodeAccountID.ToKeyString(), info.Node);
        Assert.Equal(transactionBody.TransactionValidDuration.Seconds, info.Duration);
        Assert.Equal(transactionBody.DataCase.ToString(), info.Type);
        Assert.Equal(HashpoolTransactionStatus.Queued, info.Status);
        Assert.NotNull(info.SignedBy);
        Assert.NotNull(info.History);
        Assert.Equal(ResponseCodeEnum.Unknown, info.PrecheckCode);
    }

    [Fact(DisplayName = "Transaction Info: Signature Count is Zero when no Signatures")]
    public async Task CountIsZeroWhenNoSignatures()
    {
        var transactionBody = CreateDefaultTransactionBody(_ctx, TimeSpan.FromMinutes(1));
        var transactionId = transactionBody.TransactionID.ToKeyString();
        var createResponse = await _ctx.PostTransactionsAsync(transactionBody);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var info = await _ctx.GetTransactionInfoAsync(transactionId);
        Assert.Empty(info.SignedBy);
    }

    [Fact(DisplayName = "Transaction Info: Signature Count is One When Created With Signature")]
    public async Task SignatureCountIsOneWhenCreatedWithSignature()
    {
        var transactionBody = CreateDefaultTransactionBody(_ctx, TimeSpan.FromMinutes(1));
        var sigMap = _ctx.SignWithPayer(transactionBody.ToByteArray());
        var createResponse = await _ctx.PostTransactionsAsync(transactionBody, sigMap);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var info = await _ctx.GetTransactionInfoAsync(transactionBody.TransactionID.ToKeyString());
        Assert.Single(info.SignedBy);
        Assert.Equal(Convert.ToBase64String(_ctx.PublicKey.Span[^32..]), info.SignedBy[0]);
    }

    [Fact(DisplayName = "Transaction Info: Signature Count is One After adding Signature")]
    public async Task SignatureCountIsOneAfterAddingSignature()
    {
        var transactionBody = CreateDefaultTransactionBody(_ctx, TimeSpan.FromMinutes(1));
        var transactionId = transactionBody.TransactionID.ToKeyString();
        var createResponse = await _ctx.PostTransactionsAsync(transactionBody);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var sigMap = _ctx.SignWithPayer(transactionBody.ToByteArray());
        var signResponse = await _ctx.PostTransactionsSignaturesAsync(transactionId, sigMap);
        Assert.Equal(HttpStatusCode.OK, signResponse.StatusCode);
        var info = await _ctx.GetTransactionInfoAsync(transactionId);
        Assert.Single(info.SignedBy);
        Assert.Equal(Convert.ToBase64String(_ctx.PublicKey.Span[^32..]), info.SignedBy[0]);
    }

    [Fact(DisplayName = "Transaction Info: One History Record Produced When no Initial Signatures")]
    public async Task OneHistoryRecordProducedWhenNoInitialSignatures()
    {
        var transactionBody = CreateDefaultTransactionBody(_ctx, TimeSpan.FromMinutes(1));
        var transactionId = transactionBody.TransactionID.ToKeyString();
        var createResponse = await _ctx.PostTransactionsAsync(transactionBody);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var info = await _ctx.GetTransactionInfoAsync(transactionId);
        Assert.Single(info.History);
        Assert.Equal("Transaction Received", info.History[0].Description);
    }

    [Fact(DisplayName = "Transaction Info: History Count is Two After adding Signature")]
    public async Task HistoryCountIsTwoAfterAddingSignature()
    {
        var transactionBody = CreateDefaultTransactionBody(_ctx, TimeSpan.FromMinutes(1));
        var transactionId = transactionBody.TransactionID.ToKeyString();
        var createResponse = await _ctx.PostTransactionsAsync(transactionBody);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var sigMap = _ctx.SignWithPayer(transactionBody.ToByteArray());
        var signResponse = await _ctx.PostTransactionsSignaturesAsync(transactionId, sigMap);
        Assert.Equal(HttpStatusCode.OK, signResponse.StatusCode);
        var info = await _ctx.GetTransactionInfoAsync(transactionId);
        Assert.Equal(2, info.History.Length);
    }

    [Fact(DisplayName = "Transaction Info: Default Status is Queued when no Signatures")]
    public async Task DefaultStatusIsQueuedWhenNoSignatures()
    {
        var transactionBody = CreateDefaultTransactionBody(_ctx, TimeSpan.FromMinutes(1));
        var transactionId = transactionBody.TransactionID.ToKeyString();
        var createResponse = await _ctx.PostTransactionsAsync(transactionBody);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var info = await _ctx.GetTransactionInfoAsync(transactionId);
        Assert.Equal(HashpoolTransactionStatus.Queued, info.Status);
    }

    [Fact(DisplayName = "Transaction Info: Default Status is Queued with One Signature")]
    public async Task DefaultStatusIsQueuedWithOneSignature()
    {
        var transactionBody = CreateDefaultTransactionBody(_ctx, TimeSpan.FromMinutes(1));
        var sigMap = _ctx.SignWithPayer(transactionBody.ToByteArray());
        var createResponse = await _ctx.PostTransactionsAsync(transactionBody, sigMap);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var transactionId = transactionBody.TransactionID.ToKeyString();
        var info = await _ctx.GetTransactionInfoAsync(transactionId);
        Assert.Equal(HashpoolTransactionStatus.Queued, info.Status);
    }

    [Fact(DisplayName = "Transaction Info: Status Is not Queued after Processing has Started")]
    public async Task StatusIsNotQueuedAfterProcessingHasStarted()
    {
        // This should immediately process upon submission.
        var transactionBody = CreateDefaultTransactionBody(_ctx, TimeSpan.FromSeconds(-1));
        var sigMap = _ctx.SignWithPayer(transactionBody.ToByteArray());
        var createResponse = await _ctx.PostTransactionsAsync(transactionBody, sigMap);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        await Task.Delay(2000);
        var transactionId = transactionBody.TransactionID.ToKeyString();
        var info = await _ctx.GetTransactionInfoAsync(transactionId);
        Assert.NotEqual(HashpoolTransactionStatus.Queued, info.Status);
    }

    [Fact(DisplayName = "Transactions Info: Unsigned Transaction Is Still Submitted to Node")]
    public async Task UnsignedTransactionIsStillSubmittedToNode()
    {
        // This should immediately process upon submission.
        var transactionBody = CreateDefaultTransactionBody(_ctx, TimeSpan.FromSeconds(-1));
        var createResponse = await _ctx.PostTransactionsAsync(transactionBody);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        await Task.Delay(2000);
        var transactionId = transactionBody.TransactionID.ToKeyString();
        var info = await _ctx.GetTransactionInfoAsync(transactionId);
        Assert.Equal(HashpoolTransactionStatus.Completed, info.Status);
        Assert.Equal(ResponseCodeEnum.InvalidSignature, info.PrecheckCode);
    }

}
