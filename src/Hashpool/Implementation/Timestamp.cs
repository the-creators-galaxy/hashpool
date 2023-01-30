using Google.Protobuf;
using Hashpool;

namespace Proto;

public sealed partial class Timestamp : IMessage<Timestamp>
{
    public string ToKeyString()
    {
        return $"{seconds_}.{nanos_.ToString("D8")}";
    }

    public static Timestamp FromKeyString(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            Timestamp timestamp = new Timestamp();
            var parts = value.Split('.');
            if (parts.Length == 1 && long.TryParse(parts[0], out timestamp.seconds_))
            {
                return timestamp;
            }
            else if (parts.Length == 2 && long.TryParse(parts[0], out timestamp.seconds_) && int.TryParse(parts[1], out timestamp.nanos_))
            {
                return timestamp;
            }
        }
        throw new HashpoolException(HashpoolCode.InvalidTransactionId, "Unable to parse timestamp from transaction id string.");
    }
}
