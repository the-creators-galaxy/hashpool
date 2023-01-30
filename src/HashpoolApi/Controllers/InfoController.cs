using Grpc.Core;
using Grpc.Net.Client;
using Hashpool;
using HashpoolApi.Models;
using Microsoft.AspNetCore.Mvc;
using Mirror;

namespace HashpoolApi.Controllers;
/// <summary>
/// Provides information regarding the general state 
/// and configuration of the cache.
/// </summary>
[ApiController]
[Route("[controller]")]
public partial class InfoController : ControllerBase
{
    /// <summary>
    /// System logger for this controller.
    /// </summary>
    private readonly ILogger<TransactionsController> _logger;
    /// <summary>
    /// Mirror node client for retrieving pertinent 
    /// configuration information.
    /// </summary>
    private readonly MirrorRestClient _mirror;
    /// <summary>
    /// The system channel registry that maps node wallet 
    /// IDs to physical GRPC channel endpoints.
    /// </summary>
    private readonly ChannelRegistry _channels;
    /// <summary>
    /// The memory pool of cached transactions.
    /// </summary>
    private readonly HashpoolRegistry _hashpool;
    /// <summary>
    /// Controller constructor.
    /// </summary>
    /// <param name="mirror">
    /// Mirror node client for retrieving pertinent 
    /// configuration information.
    /// </param>
    /// <param name="channels">
    /// The system channel registry that maps node wallet 
    /// IDs to physical GRPC channel endpoints.
    /// </param>
    /// <param name="hashpool">
    /// The memory pool of cached transactions.
    /// </param>
    /// <param name="logger">
    /// System logger for this controller.
    /// </param>
    public InfoController(MirrorRestClient mirror, ChannelRegistry channels, HashpoolRegistry hashpool, ILogger<TransactionsController> logger)
    {
        _mirror = mirror;
        _channels = channels;
        _hashpool = hashpool;
        _logger = logger;
    }
    /// <summary>
    /// Returns general information about this hashpool instance.
    /// </summary>
    /// <response code="200">
    /// A JSON structure enumerating the current state of the cache 
    /// and known gossip node endpoints.
    /// </response>
    [HttpGet]
    [Produces("application/json")]
    public async Task<ActionResult<HashpoolInfo>> Get()
    {
        var timestamp = (DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds.ToString();
        var channels = await _channels.GetAllChannels();
        return Ok(new HashpoolInfo
        {
            MirrorNode = _mirror.EndpointUrl,
            Channels = channels.Select(RegistryToChannelInfo).ToArray(),
            Timestamp = timestamp,
            Count = _hashpool.Count
        });
    }
    /// <summary>
    /// Helper function to create a channel info object
    /// from the channel dictionary.
    /// </summary>
    private static ChannelInfo RegistryToChannelInfo(KeyValuePair<string, GrpcChannel[]> item)
    {
        return new ChannelInfo
        {
            Account = item.Key,
            Endpoints = item.Value.Select(c => c.Target).ToArray()
        };
    }
}