﻿// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink._IInkPictureEvents_NewInAirPacketsEventHandler
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.InteropServices;

namespace Microsoft.Ink
{
  [TypeLibType(16)]
  [ComVisible(false)]
  internal delegate void _IInkPictureEvents_NewInAirPacketsEventHandler(
    [MarshalAs(UnmanagedType.Interface), In] IInkCursor Cursor,
    [In] int lPacketCount,
    [MarshalAs(UnmanagedType.Struct), In, Out] ref object PacketData);
}
