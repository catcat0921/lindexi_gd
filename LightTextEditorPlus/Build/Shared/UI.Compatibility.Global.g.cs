﻿#nullable enable

/*******************************************************************************
 *
 * 本文件用于在多个不同的 UI 框架间提供兼容性类型支持。
 *
 ******************************************************************************/

#if USE_AVALONIA

global using FontStyle = Avalonia.Media.FontStyle;
global using FontWeight = Avalonia.Media.FontWeight;
global using FontStretch = Avalonia.Media.FontStretch;

namespace LightTextEditorPlus;

#elif USE_WPF
global using FontStyle = System.Windows.FontStyle;
global using FontWeight = System.Windows.FontWeight;
global using FontStretch = System.Windows.FontStretch;

using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

namespace LightTextEditorPlus;

internal static class UICompatibility
{
    public static Point TopLeft { get; } = new(0, 0);

    public static Window? GetTopLevel(DependencyObject dependencyObject)
    {
        return Window.GetWindow(dependencyObject);
    }

    public static void Show(this Window window, Window owner)
    {
        window.Owner = owner;
        window.Show();
    }

    public static void ShowAt(this Popup popup, FrameworkElement frameworkElement)
    {
        popup.IsOpen = true;
    }

    public static void Hide(this Popup popup)
    {
        popup.IsOpen = false;
    }

    public static T? FindAncestorOfType<T>(this Visual dependencyObject)
        where T : Visual
    {
        while (true)
        {
            var parent = VisualTreeHelper.GetParent(dependencyObject) as Visual;
            if (parent is T t)
            {
                return t;
            }
            if (parent is null)
            {
                break;
            }
        }
        return null;
    }
}

internal sealed class UICompatibilityDispatcher(System.Windows.Threading.Dispatcher dispatcher)
{
    public static UICompatibilityDispatcher UIThread { get; } = new UICompatibilityDispatcher(Application.Current.Dispatcher);

    internal System.Windows.Threading.Dispatcher Dispatcher { get; } = dispatcher;

    public bool CheckAccess()
    {
        return Dispatcher.CheckAccess();
    }

    public DispatcherOperation InvokeAsync(Action action)
    {
        return Dispatcher.InvokeAsync(action);
    }

    public DispatcherOperation<TResult> InvokeAsync<TResult>(Func<TResult> action)
    {
        return Dispatcher.InvokeAsync(action);
    }

    public async Task InvokeAsync(Func<Task> action)
    {
        await await Dispatcher.InvokeAsync(action);
    }

    public async Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> action)
    {
        return await await Dispatcher.InvokeAsync(action);
    }

    public async Task DirectRunOrInvokeAsync(Action action)
    {
        if (Dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            await Dispatcher.InvokeAsync(action);
        }
    }

    public async Task<TResult> DirectRunOrInvokeAsync<TResult>(Func<TResult> func)
    {
        if (Dispatcher.CheckAccess())
        {
            return func();
        }
        else
        {
            return await Dispatcher.InvokeAsync(func);
        }
    }

    public async Task DirectRunOrInvokeAsync(Func<Task> action)
    {
        if (Dispatcher.CheckAccess())
        {
            await action();
        }
        else
        {
            await await Dispatcher.InvokeAsync(action);
        }
    }

    public async Task<TResult> DirectRunOrInvokeAsync<TResult>(Func<Task<TResult>> func)
    {
        if (Dispatcher.CheckAccess())
        {
            return await func();
        }
        else
        {
            return await await Dispatcher.InvokeAsync(func);
        }
    }
}

/// <summary>
/// 像素大小
/// </summary>
/// <param name="Width">宽度</param>
/// <param name="Height">高度</param>
internal readonly record struct PixelSize(int Width, int Height);

#endif
