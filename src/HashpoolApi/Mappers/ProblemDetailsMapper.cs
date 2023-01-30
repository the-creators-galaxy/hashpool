using Hashpool;
using Microsoft.AspNetCore.Mvc;

namespace HashpoolApi.Mappers;

public static class ProblemDetailsMapper
{
    public static ProblemDetails MapHashpoolException(HashpoolException hashpoolException)
    {
        return new ProblemDetails()
        {
            Title = hashpoolException.Code.ToString(),
            Detail = hashpoolException.Message,
            Status = hashpoolException.Code switch
            {
                HashpoolCode.GossipNodeNotAvailable => StatusCodes.Status400BadRequest,
                HashpoolCode.TransactionAlreadyExpired => StatusCodes.Status400BadRequest,
                HashpoolCode.UnsupportedTransactionType => StatusCodes.Status400BadRequest,
                HashpoolCode.TransactionNotFound => StatusCodes.Status404NotFound,
                HashpoolCode.DuplicateTransactionId => StatusCodes.Status409Conflict,
                HashpoolCode.UnparsableTransactionProtobuf => StatusCodes.Status422UnprocessableEntity,
                HashpoolCode.UnparsableSignatureMapProtobuf => StatusCodes.Status422UnprocessableEntity,
                HashpoolCode.TooLateToSignTransaction => StatusCodes.Status400BadRequest,
                HashpoolCode.TransactionReceiptNotFound => StatusCodes.Status404NotFound,
                HashpoolCode.TransactionNotSubmitted => StatusCodes.Status400BadRequest,
                HashpoolCode.NetworkTooBusyToGetReceipt => StatusCodes.Status503ServiceUnavailable,
                HashpoolCode.TransactionFailedPrecheck => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status400BadRequest,
            }
        };
    }

    public static ProblemDetails MapFormatException(FormatException formatException)
    {
        return new ProblemDetails()
        {
            Title = "InvalidFormat",
            Detail = formatException.Message,
            Status = StatusCodes.Status400BadRequest
        };
    }
}
