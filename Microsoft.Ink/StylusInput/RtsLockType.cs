﻿// Decompiled with JetBrains decompiler
// Type: Microsoft.StylusInput.RtsLockType
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

namespace Microsoft.StylusInput
{
  internal enum RtsLockType : uint
  {
    RtsObjLock = 1,
    RtsSyncEventLock = 2,
    RtsAsyncEventLock = 4,
    RtsExcludeCallback = 8,
    RtsSyncObjLock = 11, // 0x0000000B
    RtsAsyncObjLock = 13, // 0x0000000D
  }
}
