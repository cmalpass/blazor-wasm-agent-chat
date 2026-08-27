using BlazorAiChat;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Xunit;

namespace BlazorAiChat.Tests;

public class SimulatedChatClientTests
{
    [Fact]
    public async Task GetResponseAsync_ReturnsValidCompletionMessage()
    {
        // Arrange
        using var client = new SimulatedChatClient();
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello world")
        };

        // Act
        var response = await client.GetResponseAsync(messages);

        // Assert
        response.Should().NotBeNull();
        response.Messages.Should().NotBeEmpty();
        response.Messages.First().Role.Should().Be(ChatRole.Assistant);
        response.Messages.First().Text.Should().Contain("Hello world");
    }

    [Fact]
    public async Task GetStreamingResponseAsync_StreamsSequentialChunks()
    {
        // Arrange
        using var client = new SimulatedChatClient();
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Testing streaming")
        };

        // Act
        var chunks = new List<ChatResponseUpdate>();
        await foreach (var chunk in client.GetStreamingResponseAsync(messages))
        {
            chunks.Add(chunk);
        }

        // Assert
        chunks.Should().NotBeEmpty();
        chunks.All(c => c.Role == ChatRole.Assistant).Should().BeTrue();
        var combinedText = string.Concat(chunks.Select(c => c.Text));
        combinedText.Should().Contain("Testing streaming");
        combinedText.Should().Contain("streams token by token");
    }
}
