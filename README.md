# Blazor WASM AI Chat with Microsoft.Extensions.AI

A companion code repository for the blog post: **[Building a Real-Time Blazor WASM AI Chat App with Microsoft.Extensions.AI](https://chrismalpass.com/posts/blazor-wasm-ai-chat-app-with-microsoft-agent-framework/)**.

This repository demonstrates how to architect a secure, high-performance streaming AI application using **Blazor WebAssembly** and **Microsoft.Extensions.AI** on .NET 9.

---

## 🚀 Architecture Highlights

- **Agent Gateway Pattern**: The Blazor WASM client communicates with an ASP.NET Core backend Minimal API, ensuring LLM API keys and credentials are never exposed in the browser.
- **Provider-Agnostic AI**: Implements Microsoft's new `IChatClient` abstraction, allowing seamless switching between OpenAI, Azure OpenAI, Anthropic, and local models (via Ollama) without changing business logic.
- **True Token Streaming**: Streams token-by-token responses from backend to WASM using `IAsyncEnumerable<ChatResponseUpdate>` and HTTP response streaming (`HttpCompletionOption.ResponseHeadersRead`).
- **UI Thread Safety**: Debounces DOM re-renders (`StateHasChanged()`) during high-frequency token bursts to prevent browser UI freezing.

---

## 📁 Solution Structure

- `BlazorAiChat/BlazorAiChat`: The ASP.NET Core backend server hosting the `/api/chat` streaming Minimal API endpoint and registering `IChatClient`.
- `BlazorAiChat/BlazorAiChat.Client`: The Blazor WebAssembly frontend containing the interactive chat UI (`Chat.razor`).

---

## 🛠️ Getting Started

### Prerequisites
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (or .NET 10 preview)

### Running Locally
1. Clone the repository:
   ```bash
   git clone https://github.com/cmalpass/blazor-wasm-agent-chat.git
   cd blazor-wasm-agent-chat/BlazorAiChat
   ```

2. Run the application:
   ```bash
   dotnet run --project BlazorAiChat
   ```

3. Navigate to `https://localhost:7150/chat` (or the HTTP/HTTPS port shown in your terminal) and start chatting!

By default, the application runs with an in-memory `SimulatedChatClient` so you can test token streaming immediately without configuring external API keys.

---

## 🔄 Switching to Ollama (Local AI)

To use a local Ollama model instead of the simulated client:

1. Install [OllamaSharp](https://github.com/awaescher/OllamaSharp):
   ```bash
   dotnet add BlazorAiChat/BlazorAiChat.csproj package OllamaSharp
   ```

2. Replace the `IChatClient` registration in `BlazorAiChat/Program.cs`:
   ```csharp
   using OllamaSharp;

   var ollama = new OllamaApiClient(new Uri("http://localhost:11434"));
   ollama.SelectedModel = "phi3"; // or llama3.1, qwen2.5-coder

   builder.Services.AddSingleton<IChatClient>(ollama);
   ```

The Blazor WASM client requires zero code changes.

---

## 📜 License

This project is licensed under the [MIT License](LICENSE).
