using Google.Protobuf;
using Proto;

namespace HashpoolTest.Fixtures;

public static class TestContextSignExtensions
{
    public static SignatureMap SignWithPayer(this TestContext context, byte[] bodyBytes)
    {
        var sigMap = new SignatureMap();
        var privateKey = Ed25519Util.PrivateParamsFromDerOrRaw(context.PrivateKey);
        var (publicKey, signature) = Ed25519Util.Sign(bodyBytes, privateKey);
        var originalSig = new SignaturePair
        {
            Ed25519 = ByteString.CopyFrom(signature),
            PubKeyPrefix = ByteString.CopyFrom(publicKey)
        };
        sigMap.SigPair.Add(originalSig);
        return sigMap;
    }

}
