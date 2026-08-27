namespace BlazorAiChat.Contracts;

/// <summary>
/// The deliberately small wire contract shared by the WebAssembly client and the server gateway.
/// Provider-specific Microsoft.Extensions.AI types stay on the trusted server.
/// </summary>
public sealed class ChatRequest
{
    public IReadOnlyList<ChatMessageDto> Messages { get; init; } = [];
}

public sealed class ChatMessageDto
{
    public string Role { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;
}
