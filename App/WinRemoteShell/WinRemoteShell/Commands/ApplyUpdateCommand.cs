using DotNetCampus.Cli;
using DotNetCampus.Cli.Compiler;
using WinRemoteShell.Server;

namespace WinRemoteShell.Commands;

[Command("apply-update")]
internal sealed class ApplyUpdateCommand : ICommandHandler
{
    [Option("source")]
    public required string Source { get; init; }

    [Option("target")]
    public required string Target { get; init; }

    [Option("process-id")]
    public int ProcessId { get; init; }

    [Option("port")]
    public int Port { get; init; }

    [Option("service")]
    public bool Service { get; init; }

    public Task<int> RunAsync() =>
        UpdateApplier.ApplyAsync(Source, Target, ProcessId, Port, Service);
}
