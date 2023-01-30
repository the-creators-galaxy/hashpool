using HashpoolApi.Models;

namespace HashpoolTest.Fixtures;

public static class TestContextGetExtensions
{
    public static Task<HashpoolInfo> GetInfoAsync(this TestContext context)
    {
        return context.HashpoolClient.GetFromJsonAsync<HashpoolInfo>("/info")!;
    }

    public static Task<TransactionInfo> GetTransactionInfoAsync(this TestContext context, string transactionId)
    {
        return context.HashpoolClient.GetFromJsonAsync<TransactionInfo>($"/transactions/{transactionId}")!;
    }
}
