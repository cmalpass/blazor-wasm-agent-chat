using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace BlazorAiChat;

/// <summary>
/// Standard .NET observability hooks. Configure an OpenTelemetry exporter in the host to collect them.
/// Prompts and model output are intentionally never added as telemetry attributes.
/// </summary>
internal static class ChatTelemetry
{
    public const string Name = "BlazorAiChat.Chat";

    public static readonly ActivitySource ActivitySource = new(Name);
    private static readonly Meter Meter = new(Name);

    public static readonly Counter<long> Requests = Meter.CreateCounter<long>("chat.requests");
    public static readonly Counter<long> Responses = Meter.CreateCounter<long>("chat.responses");
    public static readonly Counter<long> ResponseChunks = Meter.CreateCounter<long>("chat.response.chunks");
}
