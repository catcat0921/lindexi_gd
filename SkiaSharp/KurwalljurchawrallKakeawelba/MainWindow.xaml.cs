﻿using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

using ReewheaberekaiNayweelehe;
using SkiaInkCore.Diagnostics;
using SkiaInkCore.Interactives;
using SkiaInkCore.Settings;

using SkiaSharp;

using Point = dotnetCampus.Mathematics.SpatialGeometry.Point2D;

namespace KurwalljurchawrallKakeawelba;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        SkiaCanvas.RequireDraw(context =>
        {
        });
    }

    private void InkModeButton_OnClick(object sender, RoutedEventArgs e)
    {
        SkiaCanvas.GotoInk();
    }

    private void ManipulationModeButton_OnClick(object sender, RoutedEventArgs e)
    {
        SkiaCanvas.GotoManipulation();
    }
}

public class SkiaCanvas : FrameworkElement
{
    public SkiaCanvas()
    {
        Loaded += SkiaCanvas_Loaded;
    }

    public event EventHandler<SkiaCanvasContext>? Draw;

    public void RequireDraw()
    {
        if (_isRequireDraw)
        {
            return;
        }

        _isRequireDraw = true;

        Dispatcher.InvokeAsync(DrawInner, DispatcherPriority.Render);
    }

    public void RequireDraw(Action<SkiaCanvasContext> draw)
    {
        Draw += InvokeDraw;
        RequireDraw();

        void InvokeDraw(object? sender, SkiaCanvasContext context)
        {
            Draw -= InvokeDraw;
            draw(context);
        }
    }

    private bool _isRequireDraw = false;

    /// <summary>
    ///     切换到笔模式
    /// </summary>
    public void GotoInk()
    {
        RequireDraw(context =>
        {
            Init(context);

            _inkCanvas.SkInkCanvasManipulationManager.Enable = false;
        });
    }

    public void GotoManipulation()
    {
        RequireDraw(context =>
        {
            Init(context);

            _inkCanvas.SkInkCanvasManipulationManager.Enable = true;
        });
    }

    private void SkiaCanvas_Loaded(object sender, RoutedEventArgs e)
    {
        // ReSharper disable once AsyncVoidLambda 虽然 async void 有异常会炸，但这里是测试代码，就这样
        RequireDraw(async context =>
        {
            Init(context);

            await Task.Delay(100);
        });
    }

    private void DrawInner()
    {
        if (_writeableBitmap is null)
        {
            var writeableBitmap = new WriteableBitmap((int) ActualWidth, (int) ActualHeight, 96, 96, PixelFormats.Bgra32,
                null);
            _writeableBitmap = writeableBitmap;
        }

        if (_context is null)
        {
            var skImageInfo = new SKImageInfo()
            {
                Width = _writeableBitmap.PixelWidth,
                Height = _writeableBitmap.PixelHeight,
                ColorType = SKColorType.Bgra8888,
                AlphaType = SKAlphaType.Premul,
                ColorSpace = SKColorSpace.CreateSrgb()
            };

            SkBitmap = new SKBitmap(skImageInfo);
            SkCanvas = new SKCanvas(SkBitmap);

            _context = new SkiaCanvasContext(SkCanvas, SkBitmap);
        }

        Draw?.Invoke(this, _context);

        _writeableBitmap.Lock();
        // 由于 Skia 不支持范围读取，因此这里需要全部刷新
        var dirtyRect = new Int32Rect(0, 0, _writeableBitmap.PixelWidth, _writeableBitmap.PixelHeight);
        var pixels = _context.SKBitmap.GetPixels(out var length);
        var stride = 4/*RGBA共4个*/ * _writeableBitmap.PixelWidth;
        _writeableBitmap.WritePixels(dirtyRect, pixels, (int) _writeableBitmap.PixelWidth * _writeableBitmap.PixelHeight * 4/*RGBA共4个*/, stride);
        _writeableBitmap.AddDirtyRect(dirtyRect);
        _writeableBitmap.Unlock();

        InvalidateVisual();

        _isRequireDraw = false;
    }

    public SKBitmap? SkBitmap { get; private set; }
    public SKCanvas? SkCanvas { get; private set; }

    protected override void OnRender(DrawingContext drawingContext)
    {
        if (_writeableBitmap is not null)
        {
            drawingContext.DrawImage(_writeableBitmap, new Rect(0, 0, ActualWidth, ActualHeight));
        }
    }

    private SkiaCanvasContext? _context;
    private WriteableBitmap? _writeableBitmap;

    #region 输入层

    private bool _isMouseDown = false;

    protected override void OnStylusDown(StylusDownEventArgs e)
    {
        RequireDraw(context =>
        {
            Init(context);

            var position = e.GetPosition(this);
            var inkingInputInfo = new InkingModeInputArgs(e.StylusDevice.Id, new Point(position.X, position.Y), (ulong) e.Timestamp);

            _inkingInputManager.Down(inkingInputInfo);
        });
    }

    protected override void OnStylusMove(StylusEventArgs e)
    {
        RequireDraw(context =>
        {
            Init(context);

            var position = e.GetPosition(this);
            var inkingInputInfo = new InkingModeInputArgs(e.StylusDevice.Id, new Point(position.X, position.Y), (ulong) e.Timestamp);

            _inkingInputManager.Move(inkingInputInfo);
        });
    }

    protected override void OnStylusUp(StylusEventArgs e)
    {
        RequireDraw(context =>
        {
            _isMouseDown = false;
            Init(context);
            var position = e.GetPosition(this);
            var inkingInputInfo = new InkingModeInputArgs(e.StylusDevice.Id, new Point(position.X, position.Y), (ulong) e.Timestamp);

            _inkingInputManager.Up(inkingInputInfo);
        });
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        if (e.StylusDevice != null)
        {
            return;
        }

        RequireDraw(context =>
        {
            _isMouseDown = true;
            Init(context);

            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                _inkCanvas.EnterEraserMode();
            }

            var position = e.GetPosition(this);
            var inkingInputInfo = new InkingModeInputArgs(0, new Point(position.X, position.Y), (ulong) e.Timestamp);

            _inkingInputManager.Down(inkingInputInfo);
        });
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (e.StylusDevice != null)
        {
            return;
        }

        RequireDraw(context =>
        {
            if (!_isMouseDown)
            {
                return;
            }

            Init(context);

            var position = e.GetPosition(this);
            var inkingInputInfo = new InkingModeInputArgs(0, new Point(position.X, position.Y), (ulong) e.Timestamp);

            _inkingInputManager.Move(inkingInputInfo);
        });
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        if (e.StylusDevice != null)
        {
            return;
        }

        RequireDraw(context =>
        {
            _isMouseDown = false;
            Init(context);
            var position = e.GetPosition(this);
            var inkingInputInfo = new InkingModeInputArgs(0, new Point(position.X, position.Y), (ulong) e.Timestamp);

            _inkingInputManager.Up(inkingInputInfo);

            _inkCanvas.EnterPenMode();
        });
    }

    private SkInkCanvas? _inkCanvas;
    private InkingModeInputDispatcher? _inkingInputManager;

    [MemberNotNull(nameof(_inkCanvas), nameof(_inkingInputManager))]
    private void Init(SkiaCanvasContext context)
    {
        _inkCanvas ??= new SkInkCanvas(context.SKCanvas, context.SKBitmap);
        _inkCanvas.Settings = SkInkCanvasSettings.DebugSettings(_inkCanvas.Settings);
        _inkingInputManager ??= new InkingModeInputDispatcher(_inkCanvas);

        if (!_isInit)
        {
            _inkingInputManager.AddInputProcessor(_inkCanvas.SkInkCanvasManipulationManager);

            _isInit = true;
            var testInput = new TestInput(_inkCanvas);
            testInput.RenderSplashScreen();

            _inkCanvas.RequestDispatcher += (_, action) =>
            {
                Dispatcher.InvokeAsync(() =>
                {
                    RequireDraw(_ => action());
                });
            };
        }
    }

    private bool _isInit = false;

    #endregion
}

public record SkiaCanvasContext(SKCanvas SKCanvas, SKBitmap SKBitmap);
