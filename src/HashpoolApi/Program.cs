using Hellang.Middleware.ProblemDetails;
using Hashpool;
using HashpoolApi.Formatters;
using HashpoolApi.Mappers;
using Microsoft.OpenApi.Models;
using Mirror;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails(setup =>
{
    setup.IncludeExceptionDetails = (ctx, env) => builder.Environment.IsDevelopment() || builder.Environment.IsStaging();

    setup.Map<HashpoolException>(ProblemDetailsMapper.MapHashpoolException);
    setup.Map<FormatException>(ProblemDetailsMapper.MapFormatException);
});


builder.Services.AddControllers(options =>
{
    options.InputFormatters.Insert(0, new BinaryByteStringInputFormatter());
    options.InputFormatters.Insert(1, new Base64ByteStringInputFormatter());
    options.InputFormatters.Insert(2, new HexByteStringInputFormatter());
    options.OutputFormatters.Insert(0, new BinaryByteStringOutputFormatter());
    options.OutputFormatters.Insert(1, new Base64ByteStringOutputFormatter());
    options.OutputFormatters.Insert(2, new HexByteStringOutputFormatter());
});

// Add services to the container.
builder.Services.AddSingleton(_ => new MirrorRestClient(builder.Configuration["Mirror"]));
builder.Services.AddSingleton<HashpoolRegistry>();
builder.Services.AddSingleton<ExecutionEngine>();
builder.Services.AddSingleton<ChannelRegistry>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Hashpool",
        Description = "An Hedera Hashgraph Transaction Pool for temporarily caching partially signed transactions.",
        Contact = new OpenApiContact
        {
            Name = "The Creator's Galaxy",
            Url = new Uri("https://www.creatorsgalaxyfoundation.com/")
        },
        License = new OpenApiLicense
        {
            Name = "MIT",
            Url = new Uri("https://mit-license.org/")
        }
    });

    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowAnyOrigin();
    });
});

var app = builder.Build();

app.UseProblemDetails();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.Run();

// Expose program class for integration tests
// https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-6.0
public partial class Program { }