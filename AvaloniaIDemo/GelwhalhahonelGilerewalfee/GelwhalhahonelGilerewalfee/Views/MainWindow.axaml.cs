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
using System.Text;
using Windows.Win32.UI.Controls;
using Windows.Win32.UI.Input.Pointer;
using Avalonia.Controls.Documents;
using Avalonia.Input;

namespace GelwhalhahonelGilerewalfee.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Loaded += MainWindow_Loaded;

        this.PointerMoved += MainWindow_PointerMoved;
    }

    private void MainWindow_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (e.Pointer.Type == PointerType.Mouse)
        {
            return;
        }

        var (x, y) = e.GetPosition(this);
        TouchInfoTextBlock.Text += $"\r\n[Avalonia PointerMoved] Id={e.Pointer.Id} XY={x:0.00},{y:0.00}";
    }

    private unsafe void MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0))
        {
            return;
        }

        uint deviceCount = 0;
        GetPointerDevices(&deviceCount, null);
        var pointerDeviceInfoArray = stackalloc  POINTER_DEVICE_INFO[(int) deviceCount];
        var span = new Span<POINTER_DEVICE_INFO>(pointerDeviceInfoArray, (int) deviceCount);
        GetPointerDevices(&deviceCount, pointerDeviceInfoArray);
        var info = new StringBuilder();
        foreach (POINTER_DEVICE_INFO pointerDeviceInfo in span)
        {
            info.AppendLine($"Device={pointerDeviceInfo.device} DisplayOrientation={pointerDeviceInfo.displayOrientation} MaxActiveContacts={pointerDeviceInfo.maxActiveContacts} Monitor={pointerDeviceInfo.monitor} PointerDeviceType={pointerDeviceInfo.pointerDeviceType} StartingCursorId={pointerDeviceInfo.startingCursorId} ProductString={pointerDeviceInfo.productString.ToString()}");
        }

        TouchInfoTextBlock.Text = info.ToString();

        if (TryGetPlatformHandle() is {} handle)
        {
            // һ����˵���� SetWindowsHookEx �Ǹ�ȫ�ֵģ��Լ�Ӧ���ڿ��Ը��Ӽ�
            //SetWindowsHookEx()
            Debug.Assert(Environment.Is64BitProcess);

            // ������ SetWindowLongPtrW ��ԭ���ǣ�64λ�ĳ������ 32λ�� SetWindowLongW �ᵼ���쳣������λ������ƥ�䷽��ָ�룬��ϸ�뿴
            // [ʵս���飺SetWindowLongPtr�ڿ���64λ�����ʹ�÷��� | �ٷ����� | ����÷���ǻ۰칫ƽ̨ | TopomelBox �ٷ�վ��](https://www.topomel.com/archives/245.html )

            _newWndProc = Hook;
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
    private unsafe LRESULT Hook(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        if (msg == WM_POINTERUPDATE/*Pointer Update*/)
        {
           Debug.Assert(OperatingSystem.IsWindowsVersionAtLeast(10, 0),"�ܹ��յ� WM_Pointer ��Ϣ���ض�ϵͳ�汾�Ų����");
           
            var pointerId = (uint) (ToInt32(wParam) & 0xFFFF);
            GetPointerTouchInfo(pointerId, out POINTER_TOUCH_INFO info);
            POINTER_INFO pointerInfo = info.pointerInfo;

            global::Windows.Win32.Foundation.RECT pointerDeviceRect = default;
            global::Windows.Win32.Foundation.RECT displayRect = default;

            GetPointerDeviceRects(pointerInfo.sourceDevice, &pointerDeviceRect, &displayRect);

            uint propertyCount = 0;
            GetPointerDeviceProperties(pointerInfo.sourceDevice, &propertyCount, null);
            POINTER_DEVICE_PROPERTY* pointerDevicePropertyArray = stackalloc POINTER_DEVICE_PROPERTY[(int)propertyCount];
            GetPointerDeviceProperties(pointerInfo.sourceDevice, &propertyCount, pointerDevicePropertyArray);
            var pointerDevicePropertySpan =
                new Span<POINTER_DEVICE_PROPERTY>(pointerDevicePropertyArray, (int)propertyCount);

            var touchInfo = new StringBuilder();
            touchInfo.Append($"[{DateTime.Now}] ");
            touchInfo.AppendLine($"Id={pointerId} PointerDeviceRect={RectToWHString(pointerDeviceRect)} RectToWHString={RectToWHString(displayRect)} PropertyCount={propertyCount} SourceDevice={pointerInfo.sourceDevice}");
            foreach (var pointerDeviceProperty in pointerDevicePropertySpan)
            {
            }

            //TouchInfoTextBlock.Text = $"[{DateTime.Now}] Id={pointerId} PointerDeviceRect={RectToString(pointerDeviceRect)} DisplayRect={RectToString(displayRect)}";

            TouchInfoTextBlock.Text = touchInfo.ToString();
        }

        return CallWindowProc(_oldWndProc, hwnd, msg, wParam, lParam);

        static string RectToWHString(global::Windows.Win32.Foundation.RECT rect)
        {
            return $"[WH:{rect.Width},{rect.Height}]";
        }

        static string RectToString(global::Windows.Win32.Foundation.RECT rect)
        {
            return $"[XY:{rect.left},{rect.top};WH:{rect.Width},{rect.Height}]";
        }
    }

    private static int ToInt32(WPARAM wParam) => ToInt32((IntPtr)wParam.Value);
    private static int ToInt32(IntPtr ptr) => IntPtr.Size == 4 ? ptr.ToInt32() : (int) (ptr.ToInt64() & 0xffffffff);
}
