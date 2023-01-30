using HashpoolTest.Fixtures;

namespace HashpoolTest;

[Collection(nameof(TestContext))]
public class InfoTests
{
    private readonly TestContext _context;

    public InfoTests(TestContext context)
    {
        _context = context;
    }
    [Fact(DisplayName = "Info: Can Get Info")]
    public async Task CanGetInfo()
    {
        var info = await _context.HashpoolClient.GetStringAsync("/info");
        Assert.NotNull(info);
    }
    [Fact(DisplayName = "Info: Mirror Node URL Matches Configuration")]
    public async Task TaskMirrorNodeURLMatchesConfiguration()
    {
        var info = await _context.GetInfoAsync();
        Assert.Equal(_context.MirrorEndpoint, info!.MirrorNode);
    }
    [Fact(DisplayName = "Info: Contains list of Gossip Node Channels")]
    public async Task ContainslistOfGossipNodeChannels()
    {
        var info = await _context.GetInfoAsync();
        Assert.NotNull(info!.Channels);
        Assert.NotEmpty(info.Channels);
        foreach (var channel in info.Channels)
        {
            Assert.NotNull(channel);
            Assert.NotNull(channel.Account);
            Assert.NotNull(channel.Endpoints);
            Assert.NotEmpty(channel.Endpoints);
            foreach (var endpoint in channel.Endpoints)
            {
                Assert.NotNull(endpoint);
                Assert.NotEmpty(endpoint);
            }
        }
    }
    [Fact(DisplayName = "Info: Contains A Timestamp")]
    public async Task ContainsATimestamp()
    {
        var floorDate = DateTime.UtcNow;
        await Task.Delay(100);
        var info = await _context.GetInfoAsync();
        Assert.NotNull(info!.Timestamp);
        Assert.True(floorDate < TestTransaction.DateFromTimeStampKey(info.Timestamp));
    }

    [Fact(DisplayName = "Info: Count Is Non Negative")]
    public async Task CountIsNonNegative()
    {
        var info = await _context.GetInfoAsync();
        Assert.True(info!.Count >= 0);
    }
}