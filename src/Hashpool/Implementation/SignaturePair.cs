using Google.Protobuf;

namespace Proto;

public sealed partial class SignaturePair : IMessage<SignaturePair>
{
    public bool IsProperSignaturePair
    {
        get
        {
            if (_unknownFields?.CalculateSize() > 0 ||
                signatureCase_ == SignatureOneofCase.None ||
                pubKeyPrefix_ is null ||
                pubKeyPrefix_.IsEmpty ||
                signature_ is null ||
                (signature_ is ByteString signatureBytes && signatureBytes.IsEmpty))
            {
                return false;
            }
            return true;
        }
    }
}
