namespace SddOrchestrator.Models;

public sealed class OrchestratorOptions
{
    public string TargetRepoPath { get; set; } = "";
    public string ReportsDir { get; set; } = "";
    public string CopilotExecutable { get; set; } = "copilot";
    public List<AgentSpec> Agents { get; set; } = new();
}

public sealed class AgentSpec
{
    public string Name { get; set; } = "";
    public string Prompt { get; set; } = "";
}

public sealed record AgentInvocationResult(
    string AgentName,
    bool Success,
    string? RawOutput,
    string? JsonPayload,
    string? Error,
    TimeSpan Duration);
