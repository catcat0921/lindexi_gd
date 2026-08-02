using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using XiaoXiIme.ImeInterop;

namespace XiaoXiIme.ImeModule;

internal static unsafe partial class ImeUiWindowClass
{
    private const uint CsIme = 0x00010000;
    private const uint GetModuleHandleExFlagUnchangedRefCount = 0x00000002;
    private const uint GetModuleHandleExFlagFromAddress = 0x00000004;
    private const int ErrorClassAlreadyExists = 1410;
    private const string ClassName = ImeExportsContract.ImeUiClassName;

    private static int s_registrationAttempted;
    private static int s_registrationSucceeded;
    private static int s_registrationErrorCode;

    internal static bool RegistrationAttempted => Volatile.Read(ref s_registrationAttempted) != 0;

    internal static bool RegistrationSucceeded => Volatile.Read(ref s_registrationSucceeded) != 0;

    internal static int RegistrationErrorCode => Volatile.Read(ref s_registrationErrorCode);

#pragma warning disable CA2255
    [ModuleInitializer]
    internal static void Initialize()
#pragma warning restore CA2255
    {
        TryRegister();
    }

    internal static bool TryRegister()
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        if (RegistrationSucceeded)
        {
            return true;
        }

        Volatile.Write(ref s_registrationAttempted, 1);

        var windowProcedure = (delegate* unmanaged[Stdcall]<nint, uint, nuint, nint, nint>)&WindowProcedure;
        if (!GetModuleHandleEx(
                GetModuleHandleExFlagFromAddress | GetModuleHandleExFlagUnchangedRefCount,
                (nint)windowProcedure,
                out var module))
        {
            Volatile.Write(ref s_registrationErrorCode, Marshal.GetLastPInvokeError());
            return false;
        }

        fixed (char* className = ClassName)
        {
            var windowClass = new WindowClassEx
            {
                Size = (uint)sizeof(WindowClassEx),
                Style = CsIme,
                WindowProcedure = windowProcedure,
                Instance = module,
                ClassName = className,
            };

            if (RegisterClassEx(&windowClass) != 0)
            {
                Volatile.Write(ref s_registrationErrorCode, 0);
                Volatile.Write(ref s_registrationSucceeded, 1);
                return true;
            }
        }

        var errorCode = Marshal.GetLastPInvokeError();
        Volatile.Write(ref s_registrationErrorCode, errorCode);
        if (errorCode == ErrorClassAlreadyExists)
        {
            Volatile.Write(ref s_registrationSucceeded, 1);
            return true;
        }

        return false;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static nint WindowProcedure(nint window, uint message, nuint wParam, nint lParam)
    {
        return DefWindowProc(window, message, wParam, lParam);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowClassEx
    {
        public uint Size;
        public uint Style;
        public delegate* unmanaged[Stdcall]<nint, uint, nuint, nint, nint> WindowProcedure;
        public int ClassExtraBytes;
        public int WindowExtraBytes;
        public nint Instance;
        public nint Icon;
        public nint Cursor;
        public nint BackgroundBrush;
        public char* MenuName;
        public char* ClassName;
        public nint SmallIcon;
    }

    [LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleExW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetModuleHandleEx(uint flags, nint moduleNameOrAddress, out nint module);

    [LibraryImport("user32.dll", EntryPoint = "RegisterClassExW", SetLastError = true)]
    private static partial ushort RegisterClassEx(WindowClassEx* windowClass);

    [LibraryImport("user32.dll", EntryPoint = "DefWindowProcW")]
    private static partial nint DefWindowProc(nint window, uint message, nuint wParam, nint lParam);
}
