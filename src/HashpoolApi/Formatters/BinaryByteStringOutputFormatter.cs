using Google.Protobuf;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

namespace HashpoolApi.Formatters;

public class BinaryByteStringOutputFormatter : OutputFormatter
{
    public BinaryByteStringOutputFormatter()
    {
        SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/octet-stream"));
    }
    public override bool CanWriteResult(OutputFormatterCanWriteContext context)
    {
        return
            "application/octet-stream".Equals(context.ContentType.Value, StringComparison.InvariantCultureIgnoreCase) &&
            typeof(ByteString).Equals(context.ObjectType);
    }

    protected override bool CanWriteType(Type? type)
    {
        return typeof(ByteString).Equals(type);
    }

    public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
    {
        var bytes = context.Object as ByteString;
        if (bytes is not null)
        {
            await context.HttpContext.Response.BodyWriter.WriteAsync(bytes.Memory);
            await context.HttpContext.Response.BodyWriter.FlushAsync();
        }
    }
}
