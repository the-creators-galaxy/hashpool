using Google.Protobuf;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using System.Text;

namespace HashpoolApi.Formatters;

public class Base64ByteStringOutputFormatter : TextOutputFormatter
{
    public Base64ByteStringOutputFormatter()
    {
        SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/base64"));
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
        await context.HttpContext.Response.WriteAsync(bytes.ToBase64(), selectedEncoding);
    }
}
