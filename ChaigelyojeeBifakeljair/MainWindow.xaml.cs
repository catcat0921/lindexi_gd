#nullable enable
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ChaigelyojeeBifakeljair;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();
    }

    private void Canvas_OnCreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
    {
        var imageFile = @"C:\lindexi\Image\1.png";// ͼƬ��ַ����Լ��滻
        if (!File.Exists(imageFile))
        {
            // �Լ������Լ���ͼƬ
            Debugger.Break();
        }

        var task = LoadImageAsync();
        args.TrackAsyncAction(task.AsAsyncAction());

        async Task LoadImageAsync()
        {
            CanvasBitmap canvasBitmap = await CanvasBitmap.LoadAsync(sender, imageFile);
            _canvasBitmap = canvasBitmap;
        }
    }

    private void Canvas_OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        if (_canvasBitmap is { } canvasBitmap)
        {
            var centerX = canvasBitmap.Bounds._width / 2;
            var centerY = canvasBitmap.Bounds._height / 2;

            var transform2DEffect = new Transform2DEffect();
            transform2DEffect.Source = canvasBitmap;
            var matrix3X2 = Matrix3x2.CreateScale(-1, 1, new Vector2(centerX, centerY));
            transform2DEffect.TransformMatrix = matrix3X2;
            args.DrawingSession.DrawImage(transform2DEffect);
        }
    }

    private CanvasBitmap? _canvasBitmap;

    private void MainWindow_OnClosed(object sender, WindowEventArgs args)
    {
        Canvas.RemoveFromVisualTree();
    }
}
