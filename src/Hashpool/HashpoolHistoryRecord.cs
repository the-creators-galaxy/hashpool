namespace Hashpool
{
    public record HashpoolHistoryRecord
    {
        public DateTime Timestamp { get; private init; }
        public String Description { get; private init; }
        public HashpoolHistoryRecord(DateTime timestamp, string description)
        {
            Timestamp = timestamp;
            Description = description;
        }
    }
}
