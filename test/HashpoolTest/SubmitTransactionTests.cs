using HashpoolTest.Fixtures;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Headers;
using static HashpoolTest.Fixtures.TestTransaction;

namespace HashpoolTest;

[Collection(nameof(TestContext))]
public class SubmitTransactionTests
{
    private readonly TestContext _ctx;

    public SubmitTransactionTests(TestContext ctx)
    {
        _ctx = ctx;
    }
    [Fact(DisplayName = "Transactions: Can Submit a Simple Transfer Transaction as Bytes")]
    public async Task CanSubmitASimpleTransferTransactionAsBytes()
    {
        var tx = CreateDefaultTransactionBody(_ctx, TimeSpan.Zero);
        var bytes = CreateSignedTransactionBytes(tx);
        var content = new ByteArrayContent(bytes.ToArray());
        content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
        var response = await _ctx.HashpoolClient.PostAsync("/transactions", content);
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
    }
    [Fact(DisplayName = "Transactions: Can Submit a Simple Transfer Transaction as Base64")]
    public async Task CanSubmitASimpleTransferTransactionAsBase64()
    {
        var tx = CreateDefaultTransactionBody(_ctx, TimeSpan.Zero);
        var bytes = CreateSignedTransactionBytes(tx);
        var content = new StringContent(bytes.ToBase64());
        content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/base64");
        var response = await _ctx.HashpoolClient.PostAsync("/transactions", content);
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
    }
    [Fact(DisplayName = "Transactions: Can Submit a Simple Transfer Transaction as Hex")]
    public async Task CanSubmitASimpleTransferTransactionAsHex()
    {
        var tx = CreateDefaultTransactionBody(_ctx, TimeSpan.Zero);
        var bytes = CreateSignedTransactionBytes(tx);
        var content = new StringContent(Convert.ToHexString(bytes.ToArray()));
        content.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
        var response = await _ctx.HashpoolClient.PostAsync("/transactions", content);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
    [Fact(DisplayName = "Transactions: Submitting Base64 Transaction with Incorrect Content Type Returns Error")]
    public async Task SubmittingBase64TransactionWithIncorrectContentTypeReturnsError()
    {
        var tx = CreateDefaultTransactionBody(_ctx, TimeSpan.Zero);
        var bytes = CreateSignedTransactionBytes(tx);
        var content = new ByteArrayContent(bytes.ToArray());
        content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/base64");
        var response = await _ctx.HashpoolClient.PostAsync("/transactions", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var err = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(err);
        Assert.Equal("InvalidFormat", err.Title);
        Assert.StartsWith("The input is not a valid Base-64 string", err.Detail);
        Assert.Null(err.Type);
        Assert.Null(err.Instance);
        Assert.NotEmpty(err.Extensions);
        Assert.Equal(400, err.Status);
    }
    [Fact(DisplayName = "Transactions: Submitting Hex Transaction with Incorrect Content Type Returns Error")]
    public async Task SubmittingHexTransactionWithIncorrectContentTypeReturnsError()
    {
        var tx = CreateDefaultTransactionBody(_ctx, TimeSpan.Zero);
        var bytes = CreateSignedTransactionBytes(tx);
        var content = new StringContent(bytes.ToBase64());
        content.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
        var response = await _ctx.HashpoolClient.PostAsync("/transactions", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var err = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(err);
        Assert.Equal("InvalidFormat", err.Title);
        Assert.StartsWith("The input is not a valid hex string", err.Detail);
        Assert.Null(err.Type);
        Assert.Null(err.Instance);
        Assert.NotEmpty(err.Extensions);
        Assert.Equal(400, err.Status);
    }
    [Fact(DisplayName = "Transactions: Submitting Binary Transaction with Incorrect Content Type Returns Error")]
    public async Task SubmittingBinaryTransactionWithIncorrectContentTypeReturnsError()
    {
        var tx = CreateDefaultTransactionBody(_ctx, TimeSpan.Zero);
        var bytes = CreateSignedTransactionBytes(tx);
        var content = new StringContent(Convert.ToHexString(bytes.ToArray()));
        content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
        var response = await _ctx.HashpoolClient.PostAsync("/transactions", content);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var err = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(err);
        Assert.Equal("UnparsableTransactionProtobuf", err.Title);
        Assert.Equal("The transaction bodyBytes for the signed transaction message was not valid.", err.Detail);
        Assert.Null(err.Type);
        Assert.Null(err.Instance);
        Assert.NotEmpty(err.Extensions);
        Assert.Equal(422, err.Status);
    }

    [Fact(DisplayName = "Transactions: Submitting A Transaction Increases Transaction Count")]
    public async Task ATransactionIncreasesTransactionCount()
    {
        // OK, there could be a race condition that
        // could cause this to fail once and a while
        // due to transaction timing when this is called.
        var countBefore = (await _ctx.GetInfoAsync()).Count;
        await _ctx.PostTransactionsAsync(CreateDefaultTransactionBody(_ctx, TimeSpan.FromMinutes(1)));
        var countAfter = (await _ctx.GetInfoAsync()).Count;
        Assert.True(countBefore < countAfter);
    }

    [Fact(DisplayName = "Transactions: Submitting Expired Transaction Returns Error")]
    public async Task SubmittingExpiredTransactionReturnsError()
    {
        var tx = CreateDefaultTransactionBody(_ctx, TimeSpan.FromMinutes(-5));
        var response = await _ctx.PostTransactionsAsync(tx);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var err = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(err);
        Assert.Equal("TransactionAlreadyExpired", err.Title);
        Assert.Equal("The transaction's start time & duration are in the past, transaction would not suceed if submitted.", err.Detail);
        Assert.Null(err.Type);
        Assert.Null(err.Instance);
        Assert.NotEmpty(err.Extensions);
        Assert.Equal(400, err.Status);
    }
}