using Grpc.Core;
using Grpc.Net.Client;
using Mirror;
using Proto;
using System.Collections.Concurrent;

namespace Hashpool
{
    public class ChannelRegistry
    {
        private ConcurrentDictionary<AccountID, ChannelList>? _channels;
        private readonly MirrorRestClient _mirror;

        public ChannelRegistry(MirrorRestClient mirror)
        {
            _mirror = mirror;
            _channels = null;
        }

        public async ValueTask<GrpcChannel?> GetChannel(AccountID nodeAddress)
        {
            var channels = await GetOrLoadChannels();
            if (channels!.TryGetValue(nodeAddress, out ChannelList? list))
            {
                return list!.GetNextChannel();
            }
            return null;
        }

        public async ValueTask<IEnumerable<KeyValuePair<string, GrpcChannel[]>>> GetAllChannels()
        {
            var channels = await GetOrLoadChannels();
            return channels.Select(pair => KeyValuePair.Create(pair.Key.ToKeyString(), pair.Value.GetAllChannels()));
        }

        private async Task<ConcurrentDictionary<AccountID, ChannelList>> GetOrLoadChannels()
        {
            if (_channels is null)
            {
                // A Burst of requests could cause multiple calls to
                // the mirror node in a degenerate edge case.
                var channels = new ConcurrentDictionary<AccountID, ChannelList>();
                await foreach (var node in _mirror.GetGossipNodesAsync())
                {
                    if (AccountID.TryParseFromKeyString(node.Account, out AccountID? accountId) && node.Endpoints is not null)
                    {
                        var entrypoints = node.Endpoints
                            .Where(e => e.Port == 50211 && !string.IsNullOrWhiteSpace(e.Address))
                            .Select(e => GrpcChannel.ForAddress($"http://{e.Address}:50211"))
                            .ToArray();
                        if (entrypoints.Length > 0)
                        {
                            channels[accountId] = new ChannelList(entrypoints);
                        }
                    }
                }
                _channels = channels;
            }
            return _channels;
        }

        internal class ChannelList
        {
            private int _index = 0;
            private readonly GrpcChannel[] _list;
            public ChannelList(GrpcChannel[] channelList)
            {
                _list = channelList;
            }

            public GrpcChannel GetNextChannel()
            {
                // Close enough to thread safe round-robin.
                var index = Interlocked.Increment(ref _index) % _list.Length;
                Interlocked.Exchange(ref _index, index);
                return _list[index];
            }

            public GrpcChannel[] GetAllChannels()
            {
                return _list;
            }
        }
    }
}
