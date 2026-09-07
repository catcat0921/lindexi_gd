using DotNetCampus.Cli;
using DotNetCampus.Cli.Compiler;
using WinRemoteShell.Client;

namespace WinRemoteShell.Commands;

[Command("update")]
internal sealed class UpdateCommand : ICommandHandler
{
    [Option("server")]
    public string? Server { get; init; }

    [Option("force")]
    public bool Force { get; init; }

    public async Task<int> RunAsync()
    {
        await UpdateClient.UpdateAsync(ServerAddressResolver.Resolve(Server), Force, Console.Out);
        return 0;
    }
}
