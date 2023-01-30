namespace Mirror;

public class GossipNodeList : PagedList<GossipNode>
{
    public GossipNode[]? Nodes { get; set; }
    public override IEnumerable<GossipNode> GetItems()
    {
        return Nodes ?? Array.Empty<GossipNode>();
    }
}