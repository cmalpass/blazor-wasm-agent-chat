using BlazorAiChat;
using BlazorAiChat.Client.Pages;
using BlazorAiChat.Components;
using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddSingleton<IChatClient, SimulatedChatClient>();

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
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();

app.MapPost("/api/chat", async (IChatClient chatClient, ChatRequest request, CancellationToken cancellationToken) =>
{
    var messages = new List<ChatMessage> { new ChatMessage(ChatRole.User, request.Prompt) };
    var responseStream = chatClient.GetStreamingResponseAsync(messages, cancellationToken: cancellationToken);
    
    return Results.Stream(async (stream) =>
    {
        await foreach (var chunk in responseStream.WithCancellation(cancellationToken))
        {
            if (chunk.Text != null)
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(chunk.Text);
                await stream.WriteAsync(bytes, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }
        }
    }, "text/plain");
});

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(BlazorAiChat.Client._Imports).Assembly);

app.Run();

public class ChatRequest
{
    public string Prompt { get; set; } = "";
}
