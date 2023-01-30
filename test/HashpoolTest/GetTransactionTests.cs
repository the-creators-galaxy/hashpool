using Google.Protobuf;
using HashpoolTest.Fixtures;
using Microsoft.AspNetCore.Mvc;
using Proto;
using System.Net;
using static HashpoolTest.Fixtures.TestTransaction;

namespace HashpoolTest;

[Collection(nameof(TestContext))]
public class GetTransactionTests
{
    private readonly TestContext _ctx;

    public GetTransactionTests(TestContext ctx)
    {
        _ctx = ctx;
    }
    [Fact(DisplayName = "Transactions: Can Retrieve a Submitted Transaction")]
    public async Task CanRetrieveASubmittedTransaction()
    {
        var transactionBody = CreateDefaultTransactionBody(_ctx, TimeSpan.FromMinutes(1));
        var response = await _ctx.PostTransactionsAsync(transactionBody);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var getUri = response.Headers.Location!.ToString();
        Assert.Contains(transactionBody.TransactionID.ToKeyString(), getUri);

        var echoedTransaction = await response.Content.ReadAsByteArrayAsync();
        var copiedTransaction = await _ctx.HashpoolClient.GetByteArrayAsync(getUri)!;
        Assert.NotNull(copiedTransaction);
        Assert.True(Enumerable.SequenceEqual(echoedTransaction, copiedTransaction));

        var echoedBody = TransactionBody.Parser.ParseFrom(SignedTransaction.Parser.ParseFrom(echoedTransaction).BodyBytes);
        var copiedBody = TransactionBody.Parser.ParseFrom(SignedTransaction.Parser.ParseFrom(copiedTransaction).BodyBytes);
        Assert.Equal(transactionBody, echoedBody);
        Assert.Equal(transactionBody, copiedBody);
        Assert.Equal(echoedBody, copiedBody);
    }

    [Fact(DisplayName = "Transactions: Retreiving Unsubmitted Transaction Returns Error")]
    public async Task RetreivingUnsubmittedTransactionReturnsError()
    {
        var txId = CreateTransactionId(_ctx.Payer, TimeSpan.FromMinutes(2)).ToKeyString();
        var response = await _ctx.HashpoolClient.GetAsync($"/transactions/{txId}/protobuf");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var err = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(err);
        Assert.Equal("Not Found", err.Title);
        Assert.Null(err.Detail);
        Assert.NotNull(err.Type);
        Assert.Null(err.Instance);
        Assert.NotEmpty(err.Extensions);
        Assert.Equal(404, err.Status);
    }
    [Fact(DisplayName = "Transactions: Can Retrieve a Transaction Receipt")]
    public async Task CanRetrieveATransactionReceipt()
    {
        var transactionBody = CreateDefaultTransactionBody(_ctx, TimeSpan.Zero);
        var transactionBytes = transactionBody.ToByteArray();
        var privateKey = Ed25519Util.PrivateParamsFromDerOrRaw(_ctx.PrivateKey);
        var (publicKey, signature) = Ed25519Util.Sign(transactionBytes, privateKey);
        var sigMap = new SignatureMap();
        sigMap.SigPair.Add(new SignaturePair
        {
            Ed25519 = ByteString.CopyFrom(signature),
            PubKeyPrefix = ByteString.CopyFrom(publicKey)
        });
        var response = await _ctx.PostTransactionsAsync(transactionBody, sigMap);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var receiptBytes = await _ctx.HashpoolClient.GetByteArrayAsync($"/transactions/{transactionBody.TransactionID.ToKeyString()}/receipt")!;
        Assert.NotNull(receiptBytes);

        var receipt = TransactionReceipt.Parser.ParseFrom(receiptBytes);
        Assert.Equal(ResponseCodeEnum.Success, receipt.Status);
    }
}