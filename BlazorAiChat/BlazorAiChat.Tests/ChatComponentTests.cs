using System.Net;
using System.Text;
using Bunit;
using BlazorAiChat.Client.Pages;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BlazorAiChat.Tests;

public class ChatComponentTests : BunitContext
{
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }

    [Fact]
    public void ChatPage_InitialRender_RendersTitleAndDisabledSendButton()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://localhost") };
        Services.AddSingleton(httpClient);

        // Act
        var cut = Render<Chat>();

        // Assert
        cut.Find("h1").TextContent.Should().Be("AI Agent Chat");
        var button = cut.Find("button.btn.btn-primary");
        button.HasAttribute("disabled").Should().BeTrue();
        cut.Find("input.form-control").GetAttribute("placeholder").Should().Be("Type a message...");
    }

    [Fact]
    public async Task ChatPage_SendingMessage_StreamsContentIntoMessageList()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler(req =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("Hello! I am an AI response.", Encoding.UTF8, "text/plain")
            };
            return response;
        });

        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://localhost") };
        Services.AddSingleton(httpClient);

        var cut = Render<Chat>();

        // Act - Type a message into the input box
        var input = cut.Find("input.form-control");
        input.Input("Tell me a joke");

        // Act - Click send button
        var button = cut.Find("button.btn.btn-primary");
        button.HasAttribute("disabled").Should().BeFalse();
        await cut.InvokeAsync(() => button.Click());

        // Assert - Wait for async updates to finish
        cut.WaitForState(() => cut.FindAll(".message").Count >= 2);

        var messages = cut.FindAll(".message");
        messages[0].TextContent.Should().Contain("User:");
        messages[0].TextContent.Should().Contain("Tell me a joke");

        messages[1].TextContent.Should().Contain("Assistant:");
        messages[1].TextContent.Should().Contain("Hello! I am an AI response.");
    }

    [Fact]
    public async Task ChatPage_NetworkFailure_RendersErrorMessage()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://localhost") };
        Services.AddSingleton(httpClient);

        var cut = Render<Chat>();

        // Act
        var input = cut.Find("input.form-control");
        input.Input("Trigger error");
        var button = cut.Find("button.btn.btn-primary");
        await cut.InvokeAsync(() => button.Click());

        // Assert
        cut.WaitForState(() => cut.FindAll(".message").Count >= 2);
        var assistantMsg = cut.FindAll(".message")[1];
        assistantMsg.TextContent.Should().Contain("[Error:");
    }
}
