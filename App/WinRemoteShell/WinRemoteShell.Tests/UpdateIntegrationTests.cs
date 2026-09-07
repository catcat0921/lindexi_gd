using WinRemoteShell.Client;

namespace WinRemoteShell.Tests;

[TestClass]
[DoNotParallelize]
public sealed class UpdateIntegrationTests
{
    [TestMethod]
    public async Task WhenRemoteVersionIsCurrentThenUpdateIsSkipped()
    {
        await using var host = await TestServerHost.StartAsync();
        using var output = new StringWriter();

        await UpdateClient.UpdateAsync(host.Address, false, output);

        StringAssert.Contains(output.ToString(), "The remote version is already up to date.");
    }
}
