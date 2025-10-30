﻿using Microsoft.Maui.Graphics;

namespace DotNetCampus.Inking.Utils;

static class PointExtension
{
    public static Point ToPoint(this Avalonia.Point point)
    {
        return new Point(point.X, point.Y);
    }

    public static Avalonia.Point ToAvaloniaPoint(this Point point)
    {
        return new Avalonia.Point(point.X, point.Y);
    }
}