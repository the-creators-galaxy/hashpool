using Microsoft.AspNetCore.Mvc.Testing;
using Mirror;

namespace HashpoolTest.Fixtures;

public class TestContext
{
    private readonly IConfiguration _configuration;
    private readonly MirrorRestClient _mirror;
    private readonly GossipNode _gossipNode;
    private readonly HttpClient _hashpoolClient;
    private readonly WebApplicationFactory<Program> _hashpoolApp;
    private readonly string _payer;

    public string MirrorEndpoint { get { return _configuration["mirror"]; } }
    public long AccountShard { get { return GetAsInt("account:shard"); } }
    public long AccountRealm { get { return GetAsInt("account:realm"); } }
    public long AccountNumber { get { return GetAsInt("account:number"); } }
    public ReadOnlyMemory<byte> PrivateKey { get { return Convert.FromHexString(_configuration["account:privateKey"]); } }
    public ReadOnlyMemory<byte> PublicKey { get { return Convert.FromHexString(_configuration["account:publicKey"]); } }
    public MirrorRestClient Mirror { get { return _mirror; } }
    public HttpClient HashpoolClient { get { return _hashpoolClient; } }

    public GossipNode GossipNode { get { return _gossipNode; } }
    public string Payer { get { return _payer; } }

    public TestContext()
    {
        _configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", true)
            .AddEnvironmentVariables()
            .AddUserSecrets<TestContext>(true)
            .Build();
        _mirror = new MirrorRestClient(MirrorEndpoint);
        _gossipNode = PickGossipNode(_mirror);
        _hashpoolApp = new WebApplicationFactory<Program>();
        _hashpoolClient = _hashpoolApp.CreateClient();
        _payer = $"{AccountShard}.{AccountRealm}.{AccountNumber}";
    }
    private int GetAsInt(string key)
    {
        var valueAsString = _configuration[key];
        if (int.TryParse(valueAsString, out int value))
        {
            return value;
        }
        throw new InvalidProgramException($"Unable to convert configuration '{key}' of '{valueAsString}' into an integer value.");
    }

    private static GossipNode PickGossipNode(MirrorRestClient mirror)
    {
        try
        {
            var task = GetGosspNodeListAsync();
            task.Wait();
            var list = task.Result;
            return list[new Random().Next(0, list.Count)];
        }
        catch (Exception ex)
        {
            throw new Exception("Unable to find a target gossip node.", ex);
        }

        async Task<List<GossipNode>> GetGosspNodeListAsync()
        {
            var list = new List<GossipNode>();
            await foreach (var node in mirror.GetGossipNodesAsync())
            {
                list.Add(node);
            }
            return list;
        }
    }

    [CollectionDefinition(nameof(TestContext))]
    public class FixtureCollection : ICollectionFixture<TestContext>
    {
    }
}