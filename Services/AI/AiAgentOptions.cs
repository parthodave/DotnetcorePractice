namespace DotNet8WebAPI.Services.AI;

public sealed class AiAgentOptions
{
    public string Provider { get; set; } = "OpenAI";
    public int MaxMessageLength { get; set; } = 4000;
    public int MaxToolIterations { get; set; } = 6;
    public int MaxToolCalls { get; set; } = 10;
    public int TimeoutSeconds { get; set; } = 60;
}
