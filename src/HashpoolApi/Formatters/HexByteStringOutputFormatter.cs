using Google.Protobuf;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using System.Text;

namespace HashpoolApi.Formatters;

public class HexByteStringOutputFormatter : TextOutputFormatter
{
    public HexByteStringOutputFormatter()
    {
        SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/plain"));
        SupportedEncodings.Add(Encoding.UTF8);
        SupportedEncodings.Add(Encoding.Unicode);
    }

    protected override bool CanWriteType(Type? type)
    {
        return typeof(ByteString).Equals(type);
    }

    public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
    {
        var bytes = (ByteString)context.Object!;
        var hex = Convert.ToHexString(bytes.ToByteArray());
        await context.HttpContext.Response.WriteAsync(hex, selectedEncoding);
    }
}
