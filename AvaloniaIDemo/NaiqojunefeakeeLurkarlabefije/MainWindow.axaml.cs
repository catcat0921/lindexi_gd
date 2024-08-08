using System.Diagnostics;
using System.Reflection;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;

namespace NaiqojunefeakeeLurkarlabefije;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this)!;

        // ͨ�����ڻ�ȡ���������Ӽ򵥣�
        var handle = topLevel.TryGetPlatformHandle()!;
        Console.WriteLine($"X11 xid {handle.Handle}");

        // ���¾��Ƿ���ķ�����
        var platformImpl = topLevel.PlatformImpl;

        var type = platformImpl.GetType();

        var propertyInfo = type.GetProperty("Handle", BindingFlags.Instance | BindingFlags.Public);

        var value = propertyInfo.GetValue(platformImpl);

        Debug.Assert(value is IPlatformHandle);

        if (value is PlatformHandle platformHandle)
        {
            var x11Handler = platformHandle.Handle;
            Console.WriteLine(x11Handler);
        }
        else if(value is IPlatformHandle platformHandle2)
        {
            // ��ǰ�� Windows ��û����ȷ�����ͣ���һ������ WindowImpl ���е� WindowImplPlatformHandle �ڲ���
            var hwnd = platformHandle2.Handle;
            Console.WriteLine(hwnd);
        }
    }
}