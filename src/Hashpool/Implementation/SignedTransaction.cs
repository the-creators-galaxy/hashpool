using Google.Protobuf;

namespace Proto;

public sealed partial class SignedTransaction : IMessage<SignedTransaction>
{
    public bool IsProperSignedTransaction
    {
        get
        {
            if (_unknownFields?.CalculateSize() > 0)
            {
                return false;
            }
            if (bodyBytes_ is null)
            {
                return false;
            }
            return true;
        }

    }
}
