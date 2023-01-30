using Google.Protobuf;
using Proto;
using System.Net.Http.Headers;
using static HashpoolTest.Fixtures.TestTransaction;

namespace HashpoolTest.Fixtures;

public static class TestContextPostExtensions
{
    public static async Task<HttpResponseMessage> PostTransactionsAsync(this TestContext context, TransactionBody transactionBody, SignatureMap? sigMap = null)
    {
        var bytes = CreateSignedTransactionBytes(transactionBody, sigMap);
        var content = new ByteArrayContent(bytes.ToArray());
        content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
        var request = new HttpRequestMessage
        {
            Content = content,
            Method = HttpMethod.Post,
            RequestUri = new Uri(context.HashpoolClient.BaseAddress!, "/transactions")
        };
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/octet-stream"));
        return await context.HashpoolClient.SendAsync(request);
    }

    public static async Task<HttpResponseMessage> PostTransactionsSignaturesAsync(this TestContext context, string transactionId, SignatureMap signatures)
    {
        var bytes = signatures.ToByteString();
        var content = new ByteArrayContent(bytes.ToArray());
        content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
        var request = new HttpRequestMessage
        {
            Content = content,
            Method = HttpMethod.Post,
            RequestUri = new Uri(context.HashpoolClient.BaseAddress!, $"/transactions/{transactionId}/signatures")
        };
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/octet-stream"));
        return await context.HashpoolClient.SendAsync(request);
    }

}
