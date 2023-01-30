using HashpoolTest.Fixtures;

namespace HashpoolTest;

[Collection(nameof(TestContext))]
public class PrerequsiteTests
{
    private readonly TestContext _networkCredentials;

    public PrerequsiteTests(TestContext networkCredentials)
    {
        _networkCredentials = networkCredentials;
    }
    [Fact(DisplayName = "Test Prerequisites: Network Credentials For Tests Exist")]
    public void NetworkCredentialsExist()
    {
        Assert.NotNull(_networkCredentials);
    }
    [Fact(DisplayName = "Test Prerequisites: Test Account Shard is Non-Negative")]
    public void AccountShardIsNonNegative()
    {
        Assert.True(_networkCredentials.AccountShard >= 0, "Test Account Shard should be greater than or equal to zero.");
    }
    [Fact(DisplayName = "Test Prerequisites: Test Account Realm is Non-Negative")]
    public void AccountRealmIsNonNegative()
    {
        Assert.True(_networkCredentials.AccountRealm >= 0, "Test Account Realm should be greater than or equal to zero.");
    }
    [Fact(DisplayName = "Test Prerequisites: Test Account Account Number is Non-Negative")]
    public void AccountAccountNumberIsNonNegative()
    {
        Assert.True(_networkCredentials.AccountNumber >= 0, "Test Account Number should be greater than or equal to zero.");
    }
    [Fact(DisplayName = "Test Prerequisites: Test Account Private Key is not Empty")]
    public void AccountAccountPrivateKeyIsNotEmpty()
    {
        Assert.False(_networkCredentials.PrivateKey.IsEmpty);
    }
    [Fact(DisplayName = "Test Prerequisites: Test Account Public Key is not Empty")]
    public void AccountAccountPublicKeyIsNotEmpty()
    {
        Assert.False(_networkCredentials.PublicKey.IsEmpty);
    }
    [Fact(DisplayName = "Test Prerequisites: Test Account Public and Private Keys Match")]
    public void PublicAndPrivateKeysMatch()
    {
        var privateKey = Ed25519Util.PrivateParamsFromDerOrRaw(_networkCredentials.PrivateKey);
        var generatedPublicKey = privateKey.GeneratePublicKey();
        var publicKey = Ed25519Util.PublicParamsFromDerOrRaw(_networkCredentials.PublicKey);
        Assert.Equal(generatedPublicKey.GetEncoded(), publicKey.GetEncoded());
    }
    [Fact(DisplayName = "Test Prerequisites: Can Generate Key Pair from Bouncy Castle")]
    public void CanGenerateKeyPairFromBouncyCastle()
    {
        var (publicKey, privateKey) = Ed25519Util.GenerateKeyPair();

        var checkPrivateKey = Ed25519Util.PrivateParamsFromDerOrRaw(Convert.FromHexString(privateKey));
        var checkPublicKey = Ed25519Util.ToDerBytes(checkPrivateKey.GeneratePublicKey());
        var checkPublicHex = Convert.ToHexString(checkPublicKey.Span);

        Assert.Equal(publicKey, checkPublicHex);
    }
    [Fact(DisplayName = "Test Prerequisites: Target Gossip Node Has Been Selected")]
    public void TargetGossipNodeHasBeenSelected()
    {
        var node = _networkCredentials.GossipNode;
        Assert.NotNull(node);
        Assert.Matches(@"^\d\.\d\.\d$", node.Account);
        Assert.NotNull(node.Endpoints);
        Assert.NotEmpty(node.Endpoints);
        foreach (var endpoint in node.Endpoints!)
        {
            Assert.NotNull(endpoint.Address);
            Assert.NotEmpty(endpoint.Address);
            Assert.True(endpoint.Port > 0);
        }
    }
    [Fact(DisplayName = "Test Prerequisites: Hashpool Client Has Been Created")]
    public void HashpoolClientHasBeenCreated()
    {
        var client = _networkCredentials.HashpoolClient;
        Assert.NotNull(client);
    }
}