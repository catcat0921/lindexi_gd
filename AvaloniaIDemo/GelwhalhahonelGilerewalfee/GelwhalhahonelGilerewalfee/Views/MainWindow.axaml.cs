using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;
using System.Runtime.Versioning;

namespace GelwhalhahonelGilerewalfee.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(5, 0))
        {
            return;
        }

        if (TryGetPlatformHandle() is {} handle)
        {
            // һ����˵���� SetWindowsHookEx �Ǹ�ȫ�ֵģ��Լ�Ӧ���ڿ��Ը��Ӽ�
            //SetWindowsHookEx()
            Debug.Assert(Environment.Is64BitProcess);

            // [ʵս���飺SetWindowLongPtr�ڿ���64λ�����ʹ�÷��� | �ٷ����� | ����÷���ǻ۰칫ƽ̨ | TopomelBox �ٷ�վ��](https://www.topomel.com/archives/245.html )

            _newWndProc = CustomWndProc;
            var functionPointer = Marshal.GetFunctionPointerForDelegate(_newWndProc);
            _oldWndProc = SetWindowLongPtrW(handle.Handle, (int) WINDOW_LONG_PTR_INDEX.GWLP_WNDPROC, functionPointer);
        }
    }

    /*
     *LONG_PTR SetWindowLongPtrW(
         [in] HWND     hWnd,
         [in] int      nIndex,
         [in] LONG_PTR dwNewLong
       );
     */
    [LibraryImport("User32.dll")]
    private static partial IntPtr SetWindowLongPtrW(
        IntPtr hWnd,
        int nIndex,
        IntPtr dwNewLong);

    // cswin32 ���ɵ��� [MarshalAs(UnmanagedType.FunctionPtr)] winmdroot.UI.WindowsAndMessaging.WNDPROC lpPrevWndFunc �Ĳ���
    [DllImport("USER32.dll", ExactSpelling = true, EntryPoint = "CallWindowProcW"), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [SupportedOSPlatform("windows5.0")]
    private static extern LRESULT CallWindowProc(nint lpPrevWndFunc, HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam);

    private delegate LRESULT WndProcDelegate(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam);
    private WndProcDelegate? _newWndProc;
    private IntPtr _oldWndProc;

    [SupportedOSPlatform("windows5.0")]
    private LRESULT CustomWndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        if (msg == WM_POINTERUPDATE)
        {
            TouchInfoTextBlock.Text = $"{DateTime.Now} Pointer Update";
        }

        return CallWindowProc(_oldWndProc, hwnd, msg, wParam, lParam);
    }
}
