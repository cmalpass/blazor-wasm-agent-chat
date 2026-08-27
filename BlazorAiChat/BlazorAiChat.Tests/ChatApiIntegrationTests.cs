using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BlazorAiChat.Tests;

public class ChatApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ChatApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostChat_WithValidPrompt_StreamsSuccessfulResponse()
    {
        // Arrange
        var client = _factory.CreateClient();
        var requestPayload = new { prompt = "How does Blazor WASM work?" };
        var jsonContent = new StringContent(
            JsonSerializer.Serialize(requestPayload),
            Encoding.UTF8,
            "application/json"
        );

        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/chat")
        {
            Content = jsonContent
        };
        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/plain");

        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);
        var streamedContent = await reader.ReadToEndAsync();

        streamedContent.Should().NotBeNullOrWhiteSpace();
        streamedContent.Should().Contain("How does Blazor WASM work?");
        streamedContent.Should().Contain("streams token by token");
    }
}
