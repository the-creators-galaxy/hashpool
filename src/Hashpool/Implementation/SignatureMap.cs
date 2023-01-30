using Google.Protobuf;

namespace Proto;

public sealed partial class SignatureMap : IMessage<SignatureMap>
{
    public bool IsProperSignatureMap
    {
        get
        {
            if (_unknownFields?.CalculateSize() > 0 ||
                sigPair_ is null ||
                sigPair_.Count == 0 ||
                sigPair_.Any(s => !s.IsProperSignaturePair))
            {
                return false;
            }
            return true;
        }
    }
}
