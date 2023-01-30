using Google.Protobuf;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using System.Text;

namespace HashpoolApi.Formatters;

public class Base64ByteStringInputFormatter : TextInputFormatter
{
    public Base64ByteStringInputFormatter()
    {
        SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/base64"));
        SupportedEncodings.Add(Encoding.UTF8);
        SupportedEncodings.Add(Encoding.Unicode);
    }

    protected override bool CanReadType(Type type)
    {
        return typeof(ByteString).Equals(type);
    }

    public override bool CanRead(InputFormatterContext context)
    {
        return "application/base64".Equals(context.HttpContext.Request.ContentType, StringComparison.InvariantCultureIgnoreCase);
    }

    public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
    {
        var request = context.HttpContext.Request;
        var reader = context.ReaderFactory(request.Body, encoding);
        var base64 = await reader.ReadToEndAsync();
        var data = ByteString.FromBase64(base64);
        return await InputFormatterResult.SuccessAsync(data);
    }
}
