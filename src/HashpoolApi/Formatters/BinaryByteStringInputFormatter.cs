using Google.Protobuf;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

namespace HashpoolApi.Formatters;

public class BinaryByteStringInputFormatter : InputFormatter
{
    public BinaryByteStringInputFormatter()
    {
        SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/octet-stream"));
    }

    protected override bool CanReadType(Type type)
    {
        return typeof(ByteString).Equals(type);
    }

    public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
    {
        var request = context.HttpContext.Request;
        var data = await ByteString.FromStreamAsync(request.Body);
        return await InputFormatterResult.SuccessAsync(data);
    }
}
