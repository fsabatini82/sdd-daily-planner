using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using SddOrchestrator.Models;

namespace SddOrchestrator.Services;

/// <summary>
/// Wraps `copilot --agent &lt;name&gt; -p "..."` in a Process and returns the JSON output.
/// Production-ready (no TODO markers). For the didactic semi-worked version see lab-repo/.
/// </summary>
public sealed class CopilotCliRunner(ILogger<CopilotCliRunner> logger)
{
    public async Task<AgentInvocationResult> RunAsync(
        string executable,
        string agentName,
        string prompt,
        string workingDirectory,
        CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();

        var pat = Environment.GetEnvironmentVariable("COPILOT_GITHUB_TOKEN");
        if (string.IsNullOrEmpty(pat))
        {
            return new AgentInvocationResult(
                agentName, false, null, null,
                "COPILOT_GITHUB_TOKEN env var is not set. Configure a fine-grained PAT with 'Copilot Requests' permission.",
                stopwatch.Elapsed);
        }

        var psi = new ProcessStartInfo
        {
            FileName = executable,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        psi.ArgumentList.Add("--agent");
        psi.ArgumentList.Add(agentName);
        psi.ArgumentList.Add("-p");
        psi.ArgumentList.Add(prompt);
        psi.ArgumentList.Add("--allow-tool");
        psi.ArgumentList.Add("read");
        psi.ArgumentList.Add("--allow-tool");
        psi.ArgumentList.Add("search");
        psi.ArgumentList.Add("--no-color");
        psi.EnvironmentVariables["COPILOT_GITHUB_TOKEN"] = pat;

        logger.LogInformation("Invoking copilot --agent {Agent} in {Cwd}", agentName, workingDirectory);

        try
        {
            using var process = Process.Start(psi)
                ?? throw new InvalidOperationException("Failed to start copilot process");

            var stdoutBuilder = new StringBuilder();
            var stderrBuilder = new StringBuilder();

            var stdoutTask = ReadStreamAsync(process.StandardOutput, stdoutBuilder, ct);
            var stderrTask = ReadStreamAsync(process.StandardError, stderrBuilder, ct);

            await process.WaitForExitAsync(ct);
            await Task.WhenAll(stdoutTask, stderrTask);
            stopwatch.Stop();

            var stdout = stdoutBuilder.ToString();
            var stderr = stderrBuilder.ToString();

            if (process.ExitCode != 0)
            {
                return new AgentInvocationResult(
                    agentName, false, stdout, null,
                    $"copilot exited with code {process.ExitCode}. Stderr: {stderr}",
                    stopwatch.Elapsed);
            }

            var json = JsonExtractor.ExtractFencedJson(stdout);
            return new AgentInvocationResult(agentName, json is not null, stdout, json,
                json is null ? "Could not extract JSON block from output" : null,
                stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new AgentInvocationResult(agentName, false, null, null, ex.Message, stopwatch.Elapsed);
        }
    }

    private static async Task ReadStreamAsync(StreamReader reader, StringBuilder sink, CancellationToken ct)
    {
        var buffer = new char[4096];
        int read;
        while ((read = await reader.ReadAsync(buffer, ct)) > 0)
            sink.Append(buffer, 0, read);
    }
}
