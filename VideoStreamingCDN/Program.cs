using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.FileProviders;
using VideoStreamingCDN.Services;
using VideoStreamingCDN.Utils;
using VideoStreamingCDN.Middleware;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

// Configure CORS - Allow all for development, customize for production
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });

    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins("https://yourdomain.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Register microservice dependencies
builder.Services.AddScoped<IVideoProcessingService, OptimizedVideoProcessingService>();
builder.Services.AddSingleton<IServerUtils, ServerUtils>();
builder.Services.AddSingleton<IBackgroundVideoProcessingService, BackgroundVideoProcessingService>();
builder.Services.AddHostedService<BackgroundVideoProcessingService>();

// Configure request limits for large file uploads with performance optimizations
var maxFileSize = builder.Configuration.GetValue<long>("VideoStorage:MaxFileSize", 52428800000);

builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = maxFileSize;
});

builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = maxFileSize;
    options.Limits.MinRequestBodyDataRate = null; // Disable minimum data rate for large files
    options.Limits.MinResponseDataRate = null;
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10); // Increase keep-alive for long uploads
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(5);
});

// Optimized form options for faster uploads
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = maxFileSize;
    options.MultipartHeadersLengthLimit = 16384; // 16KB for headers
    options.MultipartBoundaryLengthLimit = 128;
    options.ValueLengthLimit = int.MaxValue; // Allow large values
    options.KeyLengthLimit = 2048;
    options.BufferBody = true; // Enable buffering for better performance
    options.MemoryBufferThreshold = 1024 * 1024; // 1MB memory threshold
    options.BufferBodyLengthLimit = maxFileSize;
});

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
    app.UseCors("AllowAll");
}
else
{
    app.UseMiddleware<ErrorHandlingMiddleware>();
    app.UseCors("Production");
}

app.UseHttpsRedirection();

// Initialize server utilities first to ensure directories exist
var serverUtils = app.Services.GetRequiredService<IServerUtils>();
serverUtils.InitializeDirectories();

// Serve static files (videos)
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "videos")),
    RequestPath = "/videos",
    ServeUnknownFileTypes = true,
    DefaultContentType = "application/octet-stream",
    OnPrepareResponse = ctx =>
    {
        // Set CORS headers for video files
        ctx.Context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
        ctx.Context.Response.Headers.Append("Access-Control-Allow-Methods", "GET");
        ctx.Context.Response.Headers.Append("Access-Control-Allow-Headers", "Range");

        // Set appropriate content types
        var ext = Path.GetExtension(ctx.File.Name).ToLowerInvariant();
        ctx.Context.Response.Headers.ContentType = ext switch
        {
            ".m3u8" => "application/vnd.apple.mpegurl",
            ".ts" => "video/mp2t",
            _ => "application/octet-stream"
        };
    }
});

app.UseRouting();

// Add health check endpoint
app.MapHealthChecks("/health");

// Map API controllers
app.MapControllers();

// Log startup information
var logger = app.Services.GetRequiredService<ILogger<Program>>();
var serviceName = app.Configuration["Microservice:ServiceName"] ?? "VideoStreamingCDN";
var version = app.Configuration["Microservice:Version"] ?? "1.0.0";

logger.LogInformation($"Starting {serviceName} v{version}");
logger.LogInformation($"Video storage path: {serverUtils.GetVideoStoragePath()}");
logger.LogInformation($"CDN base URL: {serverUtils.GetCdnBaseUrl()}");
logger.LogInformation($"Max file size: {maxFileSize / (1024 * 1024)} MB");

app.Run();
