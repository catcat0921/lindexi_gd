using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using CodingChatRoom.AvaloniaShell.ViewModels;
using CodingChatRoom.AvaloniaShell.Views;

namespace CodingChatRoom.AvaloniaShell.Tests;

[TestClass]
public sealed class ShellControlStyleTests
{
    [TestMethod]
    [DataRow("Primary", "#405FBD", "#FFFFFF")]
    [DataRow("Secondary", "#EDF2FC", "#294990")]
    [DataRow("Flat", "#EDF2FC", "#294990")]
    [DataRow("Icon", "#EDF2FC", "#294990")]
    [DataRow("Navigation", "#EDF2FC", "#294990")]
    [DataRow("TaskSelect", "#EDF2FC", "#294990")]
    [DataRow("Danger", "#FCE1E7", "#9A2440")]
    public void HoverShouldApplyTheMatchingSurfaceAndTextColors(string style, string background, string foreground)
    {
        var button = new StateButton { Content = "Action", Classes = { style } };
        var window = new Window { Content = button };
        try
        {
            window.Show();
            button.SetState(":pointerover", true);
            var surface = button.GetVisualDescendants().OfType<Border>().Single(item => item.Name == "ButtonSurface");

            Assert.AreEqual((Color.Parse(background), Color.Parse(foreground)),
                (((ISolidColorBrush)surface.Background!).Color, ((ISolidColorBrush)button.Foreground!).Color));
        }
        finally { window.Close(); }
    }

    [TestMethod]
    [DataRow("Primary", "#334FA6", "#FFFFFF")]
    [DataRow("Secondary", "#DEE7F9", "#233E7B")]
    [DataRow("Danger", "#F4CDD7", "#82203A")]
    public void PressedShouldOverrideHoverColors(string style, string background, string foreground)
    {
        var button = new StateButton { Content = "Action", Classes = { style } };
        var window = new Window { Content = button };
        try
        {
            window.Show();
            button.SetState(":pointerover", true);
            button.SetState(":pressed", true);
            Assert.AreEqual((Color.Parse(background), Color.Parse(foreground)),
                (((ISolidColorBrush)button.Background!).Color, ((ISolidColorBrush)button.Foreground!).Color));
        }
        finally { window.Close(); }
    }

    [TestMethod]
    public void DisabledPrimaryShouldNotRetainHoverColors()
    {
        var button = new StateButton { Content = "Send", Classes = { "Primary" } };
        var window = new Window { Content = button };
        try
        {
            window.Show();
            button.SetState(":pointerover", true);
            button.IsEnabled = false;
            Assert.AreEqual(Color.Parse("#E3EAF9"), ((ISolidColorBrush)button.Background!).Color);
        }
        finally { window.Close(); }
    }

    [TestMethod]
    public void ComposerActionsShouldFitAtMinimumWindowWidth()
    {
        var view = new ChatView { DataContext = new ChatViewModel() };
        var window = new Window { Width = 1016, Height = 620, Content = view };
        try
        {
            window.Show();
            window.UpdateLayout();
            var checkbox = view.FindControl<CheckBox>("EnableDotNetRunCheckBox")!;
            var button = view.FindControl<Button>("CompressConversationButton")!;
            var checkboxOrigin = checkbox.TranslatePoint(default, view)!.Value;
            var buttonOrigin = button.TranslatePoint(default, view)!.Value;
            Assert.IsTrue(checkboxOrigin.X + checkbox.Bounds.Width <= buttonOrigin.X);
        }
        finally { window.Close(); }
    }

    [TestMethod]
    public void HoveredIconShouldInheritItsButtonForeground()
    {
        var icon = new PathIcon();
        var button = new StateButton { Content = icon, Classes = { "Primary" } };
        var window = new Window { Content = button };
        try
        {
            window.Show();
            button.SetState(":pointerover", true);
            Assert.AreEqual(Colors.White, ((ISolidColorBrush)icon.Foreground!).Color);
        }
        finally { window.Close(); }
    }

    [TestMethod]
    public void KeyboardFocusShouldHaveAVisibleRing()
    {
        var button = new StateButton { Content = "Action" };
        var window = new Window { Content = button };
        try
        {
            window.Show();
            button.SetState(":focus-visible", true);
            var surface = button.GetVisualDescendants().OfType<Border>().Single(item => item.Name == "ButtonSurface");
            Assert.AreEqual(BoxShadows.Parse("0 0 0 2 #829CDD"), surface.BoxShadow);
        }
        finally { window.Close(); }
    }

    [TestMethod]
    public void ComposerPanelShouldRemainCompactWithoutAttachments()
    {
        var view = new ChatView { DataContext = new ChatViewModel() };
        var window = new Window { Width = 1016, Height = 620, Content = view };
        try
        {
            window.Show();
            window.UpdateLayout();
            var panel = view.FindControl<Border>("ComposerPanel")!;
            Assert.IsTrue(panel.Bounds.Height <= 150, $"Composer height: {panel.Bounds.Height}");
        }
        finally { window.Close(); }
    }

    [TestMethod]
    public void PresetGreetingShouldUseWelcomeLayout()
    {
        using var chat = new ChatViewModel();
        chat.Messages.Add(new MessageItemViewModel(AgentLib.Model.CopilotChatMessage.CreateAssistant("Greeting", isPresetInfo: true)));
        Assert.IsTrue(chat.ShowWelcome);
    }

    [TestMethod]
    public void RealAssistantMessageShouldNotBeHiddenByWelcomeLayout()
    {
        using var chat = new ChatViewModel();
        chat.Messages.Add(new MessageItemViewModel(AgentLib.Model.CopilotChatMessage.CreateAssistant("Answer", isPresetInfo: false)));
        Assert.IsFalse(chat.ShowWelcome);
    }

    private sealed class StateButton : Button
    {
        protected override Type StyleKeyOverride => typeof(Button);
        internal void SetState(string state, bool value) => PseudoClasses.Set(state, value);
    }
}
