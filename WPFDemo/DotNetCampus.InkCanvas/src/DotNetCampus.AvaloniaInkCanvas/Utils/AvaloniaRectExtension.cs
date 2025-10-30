﻿using DotNetCampus.Numerics.Geometry;
using SkiaSharp;

namespace DotNetCampus.Inking.Utils;

static class AvaloniaRectExtension
{
    //public static Rect Union(this Rect rect, Point point)
    //{
    //    return Rect.FromLTRB
    //    (
    //        Math.Min(rect.Left, point.X),
    //        Math.Min(rect.Top, point.Y),
    //        Math.Max(rect.Right, point.X),
    //        Math.Max(rect.Bottom, point.Y)
    //    );
    //}

    //public static Rect ToMauiRect(this Avalonia.Rect rect)
    //{
    //    return new Rect(rect.X, rect.Y, rect.Width, rect.Height);
    //}

    //public static Rect ToMauiRect(this SKRect rect)
    //{
    //    return new Rect(rect.Left, rect.Top, rect.Width, rect.Height);
    //}

    public static Rect2D ToRect2D(this SKRect rect)
    {
        return new Rect2D(rect.Left, rect.Top, rect.Width, rect.Height);
    }

    public static Rect2D ToRect2D(this Avalonia.Rect rect)
    {
        return new Rect2D(rect.Left, rect.Top, rect.Width, rect.Height);
    }

    //public static Avalonia.Rect ToAvaloniaRect(this Rect rect)
    //{
    //    return new Avalonia.Rect(rect.X, rect.Y, rect.Width, rect.Height);
    //}
}