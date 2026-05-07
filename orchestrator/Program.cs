using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SddOrchestrator.Models;
using SddOrchestrator.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables();

builder.Services.Configure<OrchestratorOptions>(builder.Configuration.GetSection("Orchestrator"));
builder.Services.AddSingleton<CopilotCliRunner>();
builder.Services.AddSingleton<ReportRenderer>();
builder.Services.AddSingleton<OrchestratorService>();

builder.Logging.AddSimpleConsole(o =>
{
    o.SingleLine = true;
    o.TimestampFormat = "HH:mm:ss ";
});

var host = builder.Build();

using var lifetime = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; lifetime.Cancel(); };

var orchestrator = host.Services.GetRequiredService<OrchestratorService>();
return await orchestrator.RunAsync(lifetime.Token);
