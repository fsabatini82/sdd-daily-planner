using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SddOrchestrator.Models;

namespace SddOrchestrator.Services;

/// <summary>
/// Orchestrates one Copilot CLI invocation per specialized agent.
/// Strategy: separation of concerns + targeted prompts + per-agent model.
/// Each agent reads only the files it needs, on the right-sized model.
/// </summary>
public sealed class OrchestratorService(
    CopilotCliRunner runner,
    ReportRenderer renderer,
    IOptions<OrchestratorOptions> options,
    ILogger<OrchestratorService> logger)
{
    public async Task<int> RunAsync(CancellationToken ct)
    {
        var opts = options.Value;
        // Resolve relative paths against the launch working directory (cwd),
        // not AppContext.BaseDirectory (= bin/Debug/netX.Y). This way running
        // `dotnet run` from `lab-repo2/` makes "library-system" point to lab-repo2/library-system.
        var baseDir = Directory.GetCurrentDirectory();
        var repoPath = Path.GetFullPath(opts.TargetRepoPath, baseDir);
        var reportsDir = Path.GetFullPath(opts.ReportsDir, baseDir);
        Directory.CreateDirectory(reportsDir);

        if (!Directory.Exists(repoPath))
        {
            logger.LogError("Target repo path does not exist: {Path}", repoPath);
            return 1;
        }

        logger.LogInformation("Target repo: {Repo}", repoPath);
        logger.LogInformation("Reports dir: {Reports}", reportsDir);

        var results = new List<AgentInvocationResult>();
        foreach (var agent in opts.Agents)
        {
            logger.LogInformation("--- Invoking agent '{Agent}' ---", agent.Name);
            var result = await runner.RunAsync(opts.CopilotExecutable, agent.Name, agent.Prompt, repoPath, ct);
            results.Add(result);
            logger.LogInformation("Agent '{Agent}' done in {Duration} — success: {Success}",
                agent.Name, result.Duration, result.Success);
        }

        var report = renderer.Render(results);
        var reportPath = Path.Combine(reportsDir, $"morning-report-{DateTime.Now:yyyyMMdd-HHmm}.md");
        await File.WriteAllTextAsync(reportPath, report, ct);

        logger.LogInformation("Report written to {Path}", reportPath);
        Console.WriteLine($"\n✓ Report: {reportPath}");

        var allOk = results.All(r => r.Success);
        if (allOk)
        {
            Console.WriteLine($"✅ Esecuzione terminata con successo — {results.Count} agenti, tutti OK.");
        }
        else
        {
            var failed = results.Where(r => !r.Success).Select(r => r.AgentName);
            Console.WriteLine($"⚠️ Esecuzione terminata con errori — agenti falliti: {string.Join(", ", failed)}");
        }
        return allOk ? 0 : 2;
    }
}
