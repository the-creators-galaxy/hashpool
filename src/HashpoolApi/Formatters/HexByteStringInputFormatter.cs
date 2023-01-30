using Google.Protobuf;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using System.Text;

namespace HashpoolApi.Formatters;

public class HexByteStringInputFormatter : TextInputFormatter
{
    public HexByteStringInputFormatter()
    {
        SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/plain"));
        SupportedEncodings.Add(Encoding.UTF8);
        SupportedEncodings.Add(Encoding.Unicode);
    }

    protected override bool CanReadType(Type type)
    {
        return typeof(ByteString).Equals(type);
    }

    public override bool CanRead(InputFormatterContext context)
    {
        return "text/plain".Equals(context.HttpContext.Request.ContentType, StringComparison.InvariantCultureIgnoreCase);
    }

    public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
    {
        var request = context.HttpContext.Request;
        var reader = context.ReaderFactory(request.Body, encoding);
        var hex = await reader.ReadToEndAsync();
        var bytes = Convert.FromHexString(hex);
        var data = ByteString.CopyFrom(bytes);
        return await InputFormatterResult.SuccessAsync(data);
    }
}
