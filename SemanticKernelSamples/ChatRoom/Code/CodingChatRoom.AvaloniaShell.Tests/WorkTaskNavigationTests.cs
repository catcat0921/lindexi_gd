using CodingChatRoom.AvaloniaShell.ViewModels;

namespace CodingChatRoom.AvaloniaShell.Tests;

[TestClass]
public sealed class WorkTaskNavigationTests
{
    [TestMethod]
    public void SwitchingTasksShouldPreserveTheOriginalDraft()
    {
        var shell = new MainViewModel();
        var original = shell.ActiveWorkTask;
        var other = new MainViewModel().ActiveWorkTask;
        shell.WorkTasks.Add(other);
        original.Chat.InputText = "original draft";

        shell.ActivateWorkTaskCommand.Execute(other);
        shell.ChatViewModel.InputText = "other draft";
        shell.ActivateWorkTaskCommand.Execute(original);

        Assert.AreEqual("original draft", shell.ChatViewModel.InputText);
    }

    [TestMethod]
    public void RenamingTaskShouldNotRenameItsSession()
    {
        var shell = new MainViewModel();
        string title = shell.ChatViewModel.CurrentSessionTitle;

        shell.ActiveWorkTask.DisplayName = "Independent task name";

        Assert.AreEqual(title, shell.ChatViewModel.CurrentSessionTitle);
    }

    [TestMethod]
    public void SwitchingTasksShouldProjectTheSelectedChatInstance()
    {
        var shell = new MainViewModel();
        var other = new MainViewModel().ActiveWorkTask;
        shell.WorkTasks.Add(other);

        shell.ActivateWorkTaskCommand.Execute(other);

        Assert.AreSame(other.Chat, shell.ChatViewModel);
    }
}
