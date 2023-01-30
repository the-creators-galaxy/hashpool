using Google.Protobuf;
using HashpoolTest.Fixtures;
using Proto;
using System.Net;
using static HashpoolTest.Fixtures.TestTransaction;

namespace HashpoolTest;

[Collection(nameof(TestContext))]
public class SignTransactionTests
{
    private readonly TestContext _ctx;

    public SignTransactionTests(TestContext ctx)
    {
        _ctx = ctx;
    }

    [Fact(DisplayName = "Sign Transactions: Can Sign a Transaction")]
    public async Task CanSignATransaction()
    {
        var tx = CreateDefaultTransactionBody(_ctx, TimeSpan.FromMinutes(5));
        var response = await _ctx.PostTransactionsAsync(tx);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var signedTransactionBytes = await response.Content.ReadAsByteArrayAsync();
        var transactionBytes = SignedTransaction.Parser.ParseFrom(signedTransactionBytes).BodyBytes.ToArray();
        var privateKey = Ed25519Util.PrivateParamsFromDerOrRaw(_ctx.PrivateKey);
        var (publicKey, signature) = Ed25519Util.Sign(transactionBytes, privateKey);
        var originalSig = new SignaturePair
        {
            Ed25519 = ByteString.CopyFrom(signature),
            PubKeyPrefix = ByteString.CopyFrom(publicKey)
        };
        var sigMap = new SignatureMap();
        sigMap.SigPair.Add(originalSig);

        response = await _ctx.PostTransactionsSignaturesAsync(tx.TransactionID.ToKeyString(), sigMap);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updatedSignedTransaction = SignedTransaction.Parser.ParseFrom(await response.Content.ReadAsByteArrayAsync());

        Assert.Single(updatedSignedTransaction!.SigMap!.SigPair);

        var echoedSig = updatedSignedTransaction.SigMap!.SigPair[0];
        Assert.Equal(originalSig, echoedSig);

        var echoedTx = TransactionBody.Parser.ParseFrom(updatedSignedTransaction.BodyBytes);
        Assert.Equal(tx, echoedTx);
    }

}