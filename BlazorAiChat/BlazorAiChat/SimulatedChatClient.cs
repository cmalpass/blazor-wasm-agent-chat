using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace BlazorAiChat;

public class SimulatedChatClient : IChatClient
{
    public void Dispose() { }

    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        await Task.Delay(500, cancellationToken);
        var lastMessage = chatMessages.LastOrDefault()?.Text ?? "";
        return new ChatResponse(new ChatMessage(ChatRole.Assistant, $"I am a simulated AI. You said: {lastMessage}"));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var lastMessage = chatMessages.LastOrDefault()?.Text ?? "";
        var response = $"I am a simulated AI agent.\nYou said: {lastMessage}\n\nThis streams token by token!";
        
        var words = response.Split(' ');
        foreach (var word in words)
        {
            await Task.Delay(50, cancellationToken);
            yield return new ChatResponseUpdate
            {
                Role = ChatRole.Assistant,
                Contents = new List<AIContent> { new TextContent(word + " ") }
            };
        }
    }
    
    public object? GetService(Type serviceType, object? serviceKey = null) => null;
}
