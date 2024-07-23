﻿using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Windows;

namespace KurwalljurchawrallKakeawelba;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        var stringBuilder = new StringBuilder();

        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < 1000_0000; i++)
        {
            stringBuilder.Clear();
            //stringBuilder.Append(i);
            stringBuilder.Append(i.ToString(CultureInfo.InvariantCulture));
        }
        stopwatch.Stop();
    }
}

