using System.Text.Json.Serialization;
using BakeryFlow.Api.Common;
using BakeryFlow.Application;
using BakeryFlow.Infrastructure;
using BakeryFlow.Infrastructure.Persistence.Seed;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;

var builder = WebApplication.CreateBuilder(args);

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:4200"];
var pathBase = builder.Configuration["App:PathBase"]?.TrimEnd('/');

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddEndpointsApiExplorer();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
    {
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BakeryFlow API",
        Version = "v1"
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Bearer token",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };

    options.AddSecurityDefinition("Bearer", securityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
});

var app = builder.Build();

app.UseForwardedHeaders();
if (!string.IsNullOrWhiteSpace(pathBase))
{
    app.UsePathBase(pathBase);
}

app.UseMiddleware<ApiExceptionMiddleware>();
app.UseSwagger(options =>
{
    options.RouteTemplate = "api/swagger/{documentName}/swagger.json";
    options.PreSerializeFilters.Add((swagger, httpRequest) =>
    {
        var forwardedProto = httpRequest.Headers["X-Forwarded-Proto"].FirstOrDefault();
        var forwardedHost = httpRequest.Headers["X-Forwarded-Host"].FirstOrDefault();
        var forwardedPrefix = httpRequest.Headers["X-Forwarded-Prefix"].FirstOrDefault();

        var scheme = string.IsNullOrWhiteSpace(forwardedProto) ? httpRequest.Scheme : forwardedProto;
        var host = string.IsNullOrWhiteSpace(forwardedHost) ? httpRequest.Host.Value : forwardedHost;
        var serverBase = string.IsNullOrWhiteSpace(forwardedPrefix)
            ? (string.IsNullOrWhiteSpace(pathBase) ? string.Empty : pathBase)
            : forwardedPrefix.TrimEnd('/');

        swagger.Servers =
        [
            new OpenApiServer
            {
                Url = $"{scheme}://{host}{serverBase}"
            }
        ];
    });
});
app.UseSwaggerUI(options =>
{
    options.RoutePrefix = "api/swagger";
    options.SwaggerEndpoint("v1/swagger.json", "BakeryFlow API v1");
});
app.UseCors("DefaultCors");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api", () => Results.Ok(new
{
    name = "BakeryFlow API",
    status = "ok",
    swagger = "swagger",
    health = "health"
}));
app.MapGet("/api/", () => Results.Ok(new
{
    name = "BakeryFlow API",
    status = "ok",
    swagger = "swagger",
    health = "health"
}));
app.MapControllers();
app.MapHealthChecks("/api/health");

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<IDatabaseSeeder>();
    await seeder.SeedAsync();
}

app.Run();
