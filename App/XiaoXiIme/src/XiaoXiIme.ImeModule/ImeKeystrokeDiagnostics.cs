using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using XiaoXiIme.ImeInterop;

namespace XiaoXiIme.ImeModule;

[StructLayout(LayoutKind.Sequential)]
public struct ImeKeystrokeDiagnosticSnapshot
{
    public const uint CurrentVersion = 4;

    public uint Version;
    public uint ImeProcessKeyCallCount;
    public uint ImeToAsciiExCallCount;
    public uint LastProcessVirtualKey;
    public uint LastProcessHandled;
    public uint LastToAsciiVirtualKey;
    public uint LastToAsciiHandled;
    public uint LastCompositionWriteSucceeded;
    public uint LastMessageCount;
    public uint LastReturnValue;
    public uint UiClassRegistrationAttempted;
    public uint UiClassRegistrationSucceeded;
    public uint UiClassRegistrationErrorCode;
    public uint ImeInquireCallCount;
    public uint LastImeInquireSystemInfoFlags;
    public uint ImeSelectCallCount;
    public uint LastImeSelectValue;
    public uint ImeSetActiveContextCallCount;
    public uint LastImeSetActiveContextValue;
    public uint LastImeInquireReturnValue;
    public uint LastImeInquirePrivateDataSize;
    public uint LastImeInquireProperty;
    public uint LastImeInquireConversionCaps;
    public uint LastImeInquireSentenceCaps;
    public uint LastImeInquireUiCaps;
    public uint LastImeInquireSetCompositionStringCaps;
    public uint LastImeInquireSelectCaps;
}

public static unsafe class ImeKeystrokeDiagnostics
{
    private static int s_imeProcessKeyCallCount;
    private static int s_imeToAsciiExCallCount;
    private static int s_lastProcessVirtualKey;
    private static int s_lastProcessHandled;
    private static int s_lastToAsciiVirtualKey;
    private static int s_lastToAsciiHandled;
    private static int s_lastCompositionWriteSucceeded;
    private static int s_lastMessageCount;
    private static int s_lastReturnValue;
    private static int s_imeInquireCallCount;
    private static int s_lastImeInquireSystemInfoFlags;
    private static int s_lastImeInquireReturnValue;
    private static int s_lastImeInquirePrivateDataSize;
    private static int s_lastImeInquireProperty;
    private static int s_lastImeInquireConversionCaps;
    private static int s_lastImeInquireSentenceCaps;
    private static int s_lastImeInquireUiCaps;
    private static int s_lastImeInquireSetCompositionStringCaps;
    private static int s_lastImeInquireSelectCaps;
    private static int s_imeSelectCallCount;
    private static int s_lastImeSelectValue;
    private static int s_imeSetActiveContextCallCount;
    private static int s_lastImeSetActiveContextValue;

    [UnmanagedCallersOnly(EntryPoint = "XiaoXiImeResetKeystrokeDiagnostics", CallConvs = [typeof(CallConvStdcall)])]
    public static void ResetExport()
    {
        Reset();
    }

    [UnmanagedCallersOnly(EntryPoint = "XiaoXiImeGetKeystrokeDiagnostics", CallConvs = [typeof(CallConvStdcall)])]
    public static int GetSnapshotExport(ImeKeystrokeDiagnosticSnapshot* snapshot, uint snapshotSize)
    {
        if (snapshot is null || snapshotSize < sizeof(ImeKeystrokeDiagnosticSnapshot))
        {
            return 0;
        }

        *snapshot = GetSnapshot();
        return 1;
    }

    internal static void RecordImeInquire(uint systemInfoFlags, int returnValue, ImeInquireInfo info)
    {
        Interlocked.Increment(ref s_imeInquireCallCount);
        Volatile.Write(ref s_lastImeInquireSystemInfoFlags, unchecked((int)systemInfoFlags));
        Volatile.Write(ref s_lastImeInquireReturnValue, returnValue);
        Volatile.Write(ref s_lastImeInquirePrivateDataSize, unchecked((int)info.PrivateDataSize));
        Volatile.Write(ref s_lastImeInquireProperty, unchecked((int)info.Property));
        Volatile.Write(ref s_lastImeInquireConversionCaps, unchecked((int)info.ConversionCaps));
        Volatile.Write(ref s_lastImeInquireSentenceCaps, unchecked((int)info.SentenceCaps));
        Volatile.Write(ref s_lastImeInquireUiCaps, unchecked((int)info.UiCaps));
        Volatile.Write(ref s_lastImeInquireSetCompositionStringCaps, unchecked((int)info.SetCompositionStringCaps));
        Volatile.Write(ref s_lastImeInquireSelectCaps, unchecked((int)info.SelectCaps));
    }

    internal static void RecordImeSelect(bool select)
    {
        Interlocked.Increment(ref s_imeSelectCallCount);
        Volatile.Write(ref s_lastImeSelectValue, select ? 1 : 0);
    }

    internal static void RecordImeSetActiveContext(bool active)
    {
        Interlocked.Increment(ref s_imeSetActiveContextCallCount);
        Volatile.Write(ref s_lastImeSetActiveContextValue, active ? 1 : 0);
    }

    internal static void RecordImeProcessKey(uint virtualKey, bool handled)
    {
        Interlocked.Increment(ref s_imeProcessKeyCallCount);
        Volatile.Write(ref s_lastProcessVirtualKey, unchecked((int)virtualKey));
        Volatile.Write(ref s_lastProcessHandled, handled ? 1 : 0);
    }

    internal static void RecordImeToAsciiEx(uint virtualKey, bool handled, bool compositionWriteSucceeded, uint messageCount, uint returnValue)
    {
        Interlocked.Increment(ref s_imeToAsciiExCallCount);
        Volatile.Write(ref s_lastToAsciiVirtualKey, unchecked((int)virtualKey));
        Volatile.Write(ref s_lastToAsciiHandled, handled ? 1 : 0);
        Volatile.Write(ref s_lastCompositionWriteSucceeded, compositionWriteSucceeded ? 1 : 0);
        Volatile.Write(ref s_lastMessageCount, unchecked((int)messageCount));
        Volatile.Write(ref s_lastReturnValue, unchecked((int)returnValue));
    }

    internal static void Reset()
    {
        Volatile.Write(ref s_imeProcessKeyCallCount, 0);
        Volatile.Write(ref s_imeToAsciiExCallCount, 0);
        Volatile.Write(ref s_lastProcessVirtualKey, 0);
        Volatile.Write(ref s_lastProcessHandled, 0);
        Volatile.Write(ref s_lastToAsciiVirtualKey, 0);
        Volatile.Write(ref s_lastToAsciiHandled, 0);
        Volatile.Write(ref s_lastCompositionWriteSucceeded, 0);
        Volatile.Write(ref s_lastMessageCount, 0);
        Volatile.Write(ref s_lastReturnValue, 0);
        Volatile.Write(ref s_imeInquireCallCount, 0);
        Volatile.Write(ref s_lastImeInquireSystemInfoFlags, 0);
        Volatile.Write(ref s_lastImeInquireReturnValue, 0);
        Volatile.Write(ref s_lastImeInquirePrivateDataSize, 0);
        Volatile.Write(ref s_lastImeInquireProperty, 0);
        Volatile.Write(ref s_lastImeInquireConversionCaps, 0);
        Volatile.Write(ref s_lastImeInquireSentenceCaps, 0);
        Volatile.Write(ref s_lastImeInquireUiCaps, 0);
        Volatile.Write(ref s_lastImeInquireSetCompositionStringCaps, 0);
        Volatile.Write(ref s_lastImeInquireSelectCaps, 0);
        Volatile.Write(ref s_imeSelectCallCount, 0);
        Volatile.Write(ref s_lastImeSelectValue, 0);
        Volatile.Write(ref s_imeSetActiveContextCallCount, 0);
        Volatile.Write(ref s_lastImeSetActiveContextValue, 0);
    }

    internal static ImeKeystrokeDiagnosticSnapshot GetSnapshot()
    {
        return new ImeKeystrokeDiagnosticSnapshot
        {
            Version = ImeKeystrokeDiagnosticSnapshot.CurrentVersion,
            ImeProcessKeyCallCount = unchecked((uint)Volatile.Read(ref s_imeProcessKeyCallCount)),
            ImeToAsciiExCallCount = unchecked((uint)Volatile.Read(ref s_imeToAsciiExCallCount)),
            LastProcessVirtualKey = unchecked((uint)Volatile.Read(ref s_lastProcessVirtualKey)),
            LastProcessHandled = unchecked((uint)Volatile.Read(ref s_lastProcessHandled)),
            LastToAsciiVirtualKey = unchecked((uint)Volatile.Read(ref s_lastToAsciiVirtualKey)),
            LastToAsciiHandled = unchecked((uint)Volatile.Read(ref s_lastToAsciiHandled)),
            LastCompositionWriteSucceeded = unchecked((uint)Volatile.Read(ref s_lastCompositionWriteSucceeded)),
            LastMessageCount = unchecked((uint)Volatile.Read(ref s_lastMessageCount)),
            LastReturnValue = unchecked((uint)Volatile.Read(ref s_lastReturnValue)),
            UiClassRegistrationAttempted = ImeUiWindowClass.RegistrationAttempted ? 1u : 0u,
            UiClassRegistrationSucceeded = ImeUiWindowClass.RegistrationSucceeded ? 1u : 0u,
            UiClassRegistrationErrorCode = unchecked((uint)ImeUiWindowClass.RegistrationErrorCode),
            ImeInquireCallCount = unchecked((uint)Volatile.Read(ref s_imeInquireCallCount)),
            LastImeInquireSystemInfoFlags = unchecked((uint)Volatile.Read(ref s_lastImeInquireSystemInfoFlags)),
            ImeSelectCallCount = unchecked((uint)Volatile.Read(ref s_imeSelectCallCount)),
            LastImeSelectValue = unchecked((uint)Volatile.Read(ref s_lastImeSelectValue)),
            ImeSetActiveContextCallCount = unchecked((uint)Volatile.Read(ref s_imeSetActiveContextCallCount)),
            LastImeSetActiveContextValue = unchecked((uint)Volatile.Read(ref s_lastImeSetActiveContextValue)),
            LastImeInquireReturnValue = unchecked((uint)Volatile.Read(ref s_lastImeInquireReturnValue)),
            LastImeInquirePrivateDataSize = unchecked((uint)Volatile.Read(ref s_lastImeInquirePrivateDataSize)),
            LastImeInquireProperty = unchecked((uint)Volatile.Read(ref s_lastImeInquireProperty)),
            LastImeInquireConversionCaps = unchecked((uint)Volatile.Read(ref s_lastImeInquireConversionCaps)),
            LastImeInquireSentenceCaps = unchecked((uint)Volatile.Read(ref s_lastImeInquireSentenceCaps)),
            LastImeInquireUiCaps = unchecked((uint)Volatile.Read(ref s_lastImeInquireUiCaps)),
            LastImeInquireSetCompositionStringCaps = unchecked((uint)Volatile.Read(ref s_lastImeInquireSetCompositionStringCaps)),
            LastImeInquireSelectCaps = unchecked((uint)Volatile.Read(ref s_lastImeInquireSelectCaps)),
        };
    }
}
