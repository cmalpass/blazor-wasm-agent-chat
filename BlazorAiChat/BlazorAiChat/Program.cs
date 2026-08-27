using BlazorAiChat;
using BlazorAiChat.Client.Pages;
using BlazorAiChat.Components;
using BlazorAiChat.Contracts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.AI;
using System.Diagnostics;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// Chat.razor is prerendered by the server before the WebAssembly runtime takes
// over, so it needs an HttpClient service during that initial render as well.
builder.Services.AddHttpClient();

builder.Services.AddSingleton<IChatClient, SimulatedChatClient>();

// The endpoint remains frictionless when running the simulated local demo. In every
// other environment it requires a validated JWT bearer token.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();
builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("chat", context =>
    {
        var partitionKey = context.User.Identity?.Name
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";

        return RateLimitPartition.GetTokenBucketLimiter(partitionKey, _ => new TokenBucketRateLimiterOptions
        {
            TokenLimit = 10,
            TokensPerPeriod = 10,
            ReplenishmentPeriod = TimeSpan.FromMinutes(1),
            AutoReplenishment = true,
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        });
    });
});

builder.Services.AddRequestTimeouts(options =>
{
    options.AddPolicy("chat", TimeSpan.FromSeconds(90));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
// Keep browser-friendly status pages away from API responses so a 401/429 remains
// a machine-readable HTTP status for the WebAssembly client.
app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/api"),
    branch => branch.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true));
app.UseHttpsRedirection();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseRequestTimeouts();
app.MapStaticAssets();

var chatEndpoint = app.MapPost("/api/chat", (IChatClient chatClient, ChatRequest request, HttpContext httpContext, ILogger<Program> logger) =>
{
    const int maximumMessages = 20;
    const int maximumMessageLength = 4_000;
    const int maximumConversationLength = 8_000;

    if (request.Messages.Count is 0 or > maximumMessages)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["messages"] = ["Provide between one and 20 messages."]
        });
    }

    var totalLength = 0;
    var messages = new List<ChatMessage>(request.Messages.Count);

    foreach (var message in request.Messages)
    {
        var content = message.Content?.Trim();
        if (string.IsNullOrWhiteSpace(content) || content.Length > maximumMessageLength)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["messages"] = [$"Each message must contain between one and {maximumMessageLength} characters."]
            });
        }

        totalLength += content.Length;
        if (totalLength > maximumConversationLength)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["messages"] = [$"The conversation must not exceed {maximumConversationLength} characters."]
            });
        }

        ChatRole? role = message.Role?.ToLowerInvariant() switch
        {
            "user" => ChatRole.User,
            "assistant" => ChatRole.Assistant,
            _ => null
        };

        if (role is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["messages"] = ["Only user and assistant messages are accepted."]
            });
        }

        messages.Add(new ChatMessage(role.Value, content));
    }

    return Results.Stream(async (stream) =>
    {
        using var activity = ChatTelemetry.ActivitySource.StartActivity("chat.completion");
        activity?.SetTag("ai.chat.message_count", messages.Count);
        ChatTelemetry.Requests.Add(1);

        var chunksWritten = 0;
        try
        {
            await foreach (var chunk in chatClient.GetStreamingResponseAsync(messages, cancellationToken: httpContext.RequestAborted)
                .WithCancellation(httpContext.RequestAborted))
            {
                if (chunk.Text is { Length: > 0 } text)
                {
                    var bytes = System.Text.Encoding.UTF8.GetBytes(text);
                    await stream.WriteAsync(bytes, httpContext.RequestAborted);
                    await stream.FlushAsync(httpContext.RequestAborted);
                    chunksWritten++;
                }
            }

            ChatTelemetry.Responses.Add(1, new KeyValuePair<string, object?>("outcome", "success"));
            logger.LogInformation("Streamed {ChunkCount} chat response chunks.", chunksWritten);
        }
        catch (OperationCanceledException) when (httpContext.RequestAborted.IsCancellationRequested)
        {
            ChatTelemetry.Responses.Add(1, new KeyValuePair<string, object?>("outcome", "cancelled"));
            logger.LogInformation("Chat response was cancelled after {ChunkCount} chunks.", chunksWritten);
        }
        catch (Exception exception)
        {
            ChatTelemetry.Responses.Add(1, new KeyValuePair<string, object?>("outcome", "failed"));
            activity?.SetStatus(ActivityStatusCode.Error);
            logger.LogError(exception, "Chat provider failed after {ChunkCount} chunks.", chunksWritten);
        }
        finally
        {
            ChatTelemetry.ResponseChunks.Add(chunksWritten);
        }
    }, "text/plain; charset=utf-8");
});

chatEndpoint.RequireRateLimiting("chat").WithRequestTimeout("chat");
if (!app.Environment.IsDevelopment())
{
    chatEndpoint.RequireAuthorization();
}

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(BlazorAiChat.Client._Imports).Assembly);

app.Run();

public partial class Program { }
