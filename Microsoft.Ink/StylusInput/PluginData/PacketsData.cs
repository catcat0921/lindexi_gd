﻿// Decompiled with JetBrains decompiler
// Type: Microsoft.StylusInput.PluginData.PacketsData
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

namespace Microsoft.StylusInput.PluginData
{
  public sealed class PacketsData : StylusDataBase
  {
    private PacketsData()
    {
    }

    public PacketsData(Stylus stylus, int packetPropertyCount, int[] packetData) => this.Initialize(stylus, packetPropertyCount, packetData);
  }
}
