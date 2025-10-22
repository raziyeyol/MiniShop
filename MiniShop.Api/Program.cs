using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using FluentValidation;
using FluentValidation.AspNetCore;
using Hellang.Middleware.ProblemDetails;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using MiniShop.Api;
using MiniShop.Api.Data;
using MiniShop.Api.Validation;

var builder = WebApplication.CreateBuilder(args);

// --- EF Core to MySQL ---
var cs = builder.Configuration.GetConnectionString("mysql")
         ?? "Server=localhost;Port=3306;Database=minishop;User=root;Password=Password123!";
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseMySql(cs, ServerVersion.AutoDetect(cs)));

// --- Versioning ---
builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";          // e.g. v1, v2
        options.SubstituteApiVersionInUrl = true;
    });

// --- ProblemDetails ---
builder.Services.AddProblemDetails(opts =>
{
    opts.IncludeExceptionDetails = (ctx, ex) => builder.Environment.IsDevelopment();
});

// --- Validation (FluentValidation) ---
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<ProductCreateValidator>();

// --- Caching (Redis or in-memory fallback) ---
if (!string.IsNullOrWhiteSpace(builder.Configuration["Redis:ConnectionString"]))
{
    builder.Services.AddStackExchangeRedisCache(o =>
        o.Configuration = builder.Configuration["Redis:ConnectionString"]);
}
else
{
    builder.Services.AddDistributedMemoryCache();
}

// --- Swagger ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Hook Swagger to API versioning (register docs per discovered version)
builder.Services.AddTransient<IConfigureOptions<Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions>, ConfigureSwaggerOptions>();

var app = builder.Build();

app.UseProblemDetails();

// Swagger
app.UseSwagger();
app.UseSwaggerUI(o =>
{
    var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
    foreach (var desc in provider.ApiVersionDescriptions)
    {
        o.SwaggerEndpoint($"/swagger/{desc.GroupName}/swagger.json", desc.GroupName.ToUpperInvariant());
    }
});

// Map endpoints with versioning
var api = app.NewVersionedApi();
var v1 = api.MapGroup("/api/v{version:apiVersion}").HasApiVersion(1.0);
var v2 = api.MapGroup("/api/v{version:apiVersion}").HasApiVersion(2.0);

v1.MapProductEndpoints();
v2.MapProductEndpointsV2();

app.Run();

// Needed for WebApplicationFactory in tests
public partial class Program { }

// -----------------------
// Helper to register Swagger docs per API version
// -----------------------
file sealed class ConfigureSwaggerOptions : IConfigureOptions<Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _provider;

    public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider) => _provider = provider;

    public void Configure(Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions options)
    {
        foreach (var desc in _provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(desc.GroupName, new OpenApiInfo
            {
                Title = "MiniShop API",
                Version = desc.ApiVersion.ToString(),
                Description = "Versioned minimal API with EF Core (MySQL), validation, and ProblemDetails."
            });
        }
    }
}
