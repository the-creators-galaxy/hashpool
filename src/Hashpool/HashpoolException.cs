namespace Hashpool;

public class HashpoolException : Exception
{
    public HashpoolCode Code { get; private init; }
    public HashpoolException(HashpoolCode code, string description) : base(description)
    {
        Code = code;
    }
}