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

        // Resolve `copilot` against PATH (with PATHEXT on Windows) so the user
        // can keep "copilot" in appsettings.json instead of an absolute path.
        var resolvedExe = ResolveExecutable(executable);
        bool isCmdWrapper = resolvedExe.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase)
            || resolvedExe.EndsWith(".bat", StringComparison.OrdinalIgnoreCase);
        var psi = new ProcessStartInfo
        {
            FileName = isCmdWrapper ? "cmd.exe" : resolvedExe,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        if (isCmdWrapper)
        {
            psi.ArgumentList.Add("/c");
            psi.ArgumentList.Add(resolvedExe);
        }
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

    private static string ResolveExecutable(string nameOrPath)
    {
        // Already a path: caller asked for an explicit binary, use it as-is.
        if (Path.IsPathRooted(nameOrPath) || nameOrPath.Contains('/') || nameOrPath.Contains('\\'))
            return nameOrPath;

        // Unix: Process.Start resolves PATH natively, no manual lookup needed.
        if (!OperatingSystem.IsWindows())
            return nameOrPath;

        // Windows: Process.Start with UseShellExecute=false does NOT consult PATHEXT,
        // so iterate PATH manually trying each PATHEXT extension (npm globals
        // typically install .cmd shims on Windows).
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        var pathExt = (Environment.GetEnvironmentVariable("PATHEXT") ?? ".COM;.EXE;.BAT;.CMD")
            .Split(';', StringSplitOptions.RemoveEmptyEntries);

        foreach (var dir in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var ext in pathExt)
            {
                var candidate = Path.Combine(dir, nameOrPath + ext);
                if (File.Exists(candidate)) return candidate;
            }
            var asIs = Path.Combine(dir, nameOrPath);
            if (File.Exists(asIs)) return asIs;
        }

        // Not found — let Process.Start fail with its native diagnostic.
        return nameOrPath;
    }
}
