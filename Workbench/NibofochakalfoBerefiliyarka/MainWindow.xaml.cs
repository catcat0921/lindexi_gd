using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace NibofochakalfoBerefiliyarka;

public partial class MainWindow : Window
{
    private const ushort PageDownVirtualKey = 0x22;
    private const uint InputKeyboard = 1;
    private const uint KeyEventExtendedKey = 0x0001;
    private const uint KeyEventKeyUp = 0x0002;
    private const uint NextWindow = 2;
    private readonly CancellationTokenSource _closingCancellationTokenSource = new();
    private readonly DispatcherTimer _targetWindowTimer;
    private bool _isRunning;

    public MainWindow()
    {
        InitializeComponent();

        _targetWindowTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100),
        };
        _targetWindowTimer.Tick += TargetWindowTimer_OnTick;
        Loaded += (_, _) =>
        {
            UpdateTargetWindowPreview();
            _targetWindowTimer.Start();
        };
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        _targetWindowTimer.Stop();
        _closingCancellationTokenSource.Cancel();
        base.OnClosing(e);
    }

    private void TargetWindowTimer_OnTick(object? sender, EventArgs e)
    {
        UpdateTargetWindowPreview();
    }

    private void UpdateTargetWindowPreview()
    {
        if (_isRunning)
        {
            return;
        }

        var applicationWindow = new WindowInteropHelper(this).Handle;
        var targetWindow = TryFindTargetWindow(applicationWindow);
        TargetWindowTextBlock.Text = targetWindow == IntPtr.Zero
            ? "未找到目标窗口"
            : GetWindowDescription(targetWindow);
    }

    private async void StartButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_isRunning)
        {
            return;
        }

        if (!int.TryParse(RepeatCountTextBox.Text, NumberStyles.None, CultureInfo.CurrentCulture, out var repeatCount)
            || repeatCount is < 1 or > 10000)
        {
            System.Windows.MessageBox.Show(this, "请输入 1 到 10000 之间的截图次数。", "输入无效", MessageBoxButton.OK, MessageBoxImage.Warning);
            RepeatCountTextBox.Focus();
            RepeatCountTextBox.SelectAll();
            return;
        }

        _isRunning = true;
        StartButton.IsEnabled = false;
        RepeatCountTextBox.IsEnabled = false;

        try
        {
            var outputDirectory = CreateOutputDirectory();
            var targetWindow = FindTargetWindow(new WindowInteropHelper(this).Handle);
            TargetWindowTextBlock.Text = GetWindowDescription(targetWindow);

            WindowState = WindowState.Minimized;
            await Task.Delay(TimeSpan.FromMilliseconds(100), _closingCancellationTokenSource.Token);
            ActivateTargetWindow(targetWindow);
            await Task.Delay(TimeSpan.FromMilliseconds(900), _closingCancellationTokenSource.Token);

            for (var index = 1; index <= repeatCount; index++)
            {
                _closingCancellationTokenSource.Token.ThrowIfCancellationRequested();
                CaptureVirtualScreen(Path.Combine(outputDirectory, $"截图_{index:D4}.png"));

                if (index == repeatCount)
                {
                    break;
                }

                ActivateTargetWindow(targetWindow);
                SendPageDown(targetWindow);
                await Task.Delay(TimeSpan.FromSeconds(1), _closingCancellationTokenSource.Token);
            }

            WindowState = WindowState.Normal;
            Activate();
            System.Windows.MessageBox.Show(this, $"截图完成，共保存 {repeatCount} 张图片。\n\n保存位置：\n{outputDirectory}", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (OperationCanceledException) when (_closingCancellationTokenSource.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (exception is ExternalException or IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            WindowState = WindowState.Normal;
            Activate();
            System.Windows.MessageBox.Show(this, $"截图失败：{exception.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _isRunning = false;
            StartButton.IsEnabled = true;
            RepeatCountTextBox.IsEnabled = true;
        }
    }

    private static string CreateOutputDirectory()
    {
        var outputDirectory = Path.Combine(AppContext.BaseDirectory, "图片", DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture));
        Directory.CreateDirectory(outputDirectory);
        return outputDirectory;
    }

    private static void CaptureVirtualScreen(string filePath)
    {
        var bounds = Forms.SystemInformation.VirtualScreen;
        using var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
        bitmap.Save(filePath, ImageFormat.Png);
    }

    private static IntPtr FindTargetWindow(IntPtr applicationWindow)
    {
        var targetWindow = TryFindTargetWindow(applicationWindow);
        return targetWindow != IntPtr.Zero
            ? targetWindow
            : throw new InvalidOperationException("没有找到需要翻页的目标窗口，请先打开目标程序并将其放在本程序后方。");
    }

    private static IntPtr TryFindTargetWindow(IntPtr applicationWindow)
    {
        var candidate = GetWindow(applicationWindow, NextWindow);
        while (candidate != IntPtr.Zero)
        {
            if (IsWindowVisible(candidate) && GetWindowTextLength(candidate) > 0)
            {
                return candidate;
            }

            candidate = GetWindow(candidate, NextWindow);
        }

        return IntPtr.Zero;
    }

    private static string GetWindowDescription(IntPtr windowHandle)
    {
        var titleLength = GetWindowTextLength(windowHandle);
        var titleBuilder = new StringBuilder(titleLength + 1);
        GetWindowText(windowHandle, titleBuilder, titleBuilder.Capacity);

        GetWindowThreadProcessId(windowHandle, out var processId);
        try
        {
            using var process = Process.GetProcessById((int)processId);
            return $"{process.ProcessName} — {titleBuilder}";
        }
        catch (ArgumentException)
        {
            return titleBuilder.ToString();
        }
    }

    private static void ActivateTargetWindow(IntPtr targetWindow)
    {
        if (GetForegroundWindow() == targetWindow)
        {
            return;
        }

        SetForegroundWindow(targetWindow);
        if (GetForegroundWindow() != targetWindow)
        {
            throw new InvalidOperationException("无法激活目标窗口，请点击目标窗口后再重新开始。");
        }
    }

    private static void SendPageDown(IntPtr targetWindow)
    {
        if (GetForegroundWindow() != targetWindow)
        {
            throw new InvalidOperationException("目标窗口失去输入焦点，已停止发送翻页键。");
        }

        var inputs = new[]
        {
            CreateKeyboardInput(KeyEventExtendedKey),
            CreateKeyboardInput(KeyEventExtendedKey | KeyEventKeyUp),
        };

        var sentInputCount = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>());
        if (sentInputCount != inputs.Length)
        {
            var errorCode = Marshal.GetLastWin32Error();
            throw errorCode == 0
                ? new InvalidOperationException($"翻页键发送失败，只发送了 {sentInputCount}/{inputs.Length} 个键盘事件。")
                : new Win32Exception(errorCode, $"翻页键发送失败，只发送了 {sentInputCount}/{inputs.Length} 个键盘事件。");
        }
    }

    private static Input CreateKeyboardInput(uint flags)
    {
        return new Input
        {
            Type = InputKeyboard,
            Data = new InputUnion
            {
                Keyboard = new KeyboardInput
                {
                    VirtualKey = PageDownVirtualKey,
                    Flags = flags,
                },
            },
        };
    }

    private void RepeatCountTextBox_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = e.Text.Any(character => !char.IsDigit(character));
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        internal uint Type;
        internal InputUnion Data;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        internal MouseInput Mouse;

        [FieldOffset(0)]
        internal KeyboardInput Keyboard;

        [FieldOffset(0)]
        internal HardwareInput Hardware;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput
    {
        internal int X;
        internal int Y;
        internal uint MouseData;
        internal uint Flags;
        internal uint Time;
        internal UIntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        internal ushort VirtualKey;
        internal ushort ScanCode;
        internal uint Flags;
        internal uint Time;
        internal UIntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HardwareInput
    {
        internal uint Message;
        internal ushort ParameterLow;
        internal ushort ParameterHigh;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindow(IntPtr windowHandle, uint command);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr windowHandle);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr windowHandle);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr windowHandle, StringBuilder text, int maximumCount);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr windowHandle, out uint processId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr windowHandle);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint inputCount, Input[] inputs, int inputSize);
}
