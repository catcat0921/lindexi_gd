﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Text;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Threading;

using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Core.Utils;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.TextEditorPlus.Render;
using LightTextEditorPlus.Utils.Threading;

using Microsoft.Win32;

using Point = LightTextEditorPlus.Core.Primitive.Point;
using Rect = LightTextEditorPlus.Core.Primitive.Rect;
using Size = LightTextEditorPlus.Core.Primitive.Size;

namespace LightTextEditorPlus;

public partial class TextEditor : FrameworkElement, IRenderManager
{
    public TextEditor()
    {
        var textEditorPlatformProvider = new TextEditorPlatformProvider(this);
        TextEditorCore = new TextEditorCore(textEditorPlatformProvider);
        SetDefaultTextRunProperty(property =>
        {
            property.FontSize = 30;
        });

        TextEditorPlatformProvider = textEditorPlatformProvider;

        SnapsToDevicePixels = true;
        RenderOptions.SetClearTypeHint(this, ClearTypeHint.Enabled);
        RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);

        Loaded += TextEditor_Loaded;
    }

    private void TextEditor_Loaded(object sender, RoutedEventArgs e)
    {
    }

    #region 公开属性

    public TextEditorCore TextEditorCore { get; }

    #endregion

    #region 公开方法

    /// <summary>
    /// 设置当前文本的默认字符属性
    /// </summary>
    public void SetDefaultTextRunProperty(Action<RunProperty> config)
    {
        TextEditorCore.DocumentManager.SetDefaultTextRunProperty<RunProperty>(config);
    }

    /// <summary>
    /// 设置当前光标的字符属性。在光标切走之后，自动失效
    /// </summary>
    public void SetCurrentCaretRunProperty(Action<RunProperty> config)
        => TextEditorCore.DocumentManager.SetCurrentCaretRunProperty<RunProperty>(config);

    public void SetRunProperty(Action<RunProperty> config, Selection? selection = null)
        => TextEditorCore.DocumentManager.SetRunProperty(config, selection);

    #endregion


    internal TextEditorPlatformProvider TextEditorPlatformProvider { get; }

    private readonly DrawingGroup _drawingGroup = new DrawingGroup();

    protected override void OnRender(DrawingContext drawingContext)
    {
        drawingContext.DrawDrawing(_drawingGroup);
    }

    void IRenderManager.Render(RenderInfoProvider renderInfoProvider)
    {
        var pixelsPerDip = (float) VisualTreeHelper.GetDpi(this).PixelsPerDip;

        using (var drawingContext = _drawingGroup.Open())
        {
            foreach (var paragraphRenderInfo in renderInfoProvider.GetParagraphRenderInfoList())
            {
                foreach (var lineRenderInfo in paragraphRenderInfo.GetLineRenderInfoList())
                {
                    var argument = lineRenderInfo.Argument;
                    drawingContext.PushTransform(new TranslateTransform(0, argument.StartPoint.Y));

                    try
                    {
                        if (lineRenderInfo.Argument.IsDrawn)
                        {
                            // 如果行已经绘制过，那就尝试复用
                            if (lineRenderInfo.Argument.LineAssociatedRenderData is DrawingVisual cacheLineVisual)
                            {
                                drawingContext.DrawDrawing(cacheLineVisual.Drawing);
                                continue;
                            }
                        }

                        DrawingVisual lineVisual = DrawLine(argument, pixelsPerDip);
                        drawingContext.DrawDrawing(lineVisual.Drawing);
                        lineRenderInfo.SetDrawnResult(new LineDrawnResult(lineVisual));
                    }
                    finally
                    {
                        drawingContext.Pop();
                    }
                }
            }
        }

        InvalidateVisual();
    }

    private DrawingVisual DrawLine(in LineDrawingArgument argument, float pixelsPerDip)
    {
        //var drawingGroup = new DrawingGroup();

        var drawingVisual = new DrawingVisual();
        using var drawingContext = drawingVisual.RenderOpen();

        var splitList = argument.CharList.SplitContinuousCharData((last, current) => last.RunProperty.Equals(current.RunProperty));

        foreach (var charList in splitList)
        {
            var runProperty = charList[0].RunProperty;
            // 获取到字体信息
            var currentRunProperty = runProperty.AsRunProperty();
            var glyphTypeface = currentRunProperty.GetGlyphTypeface();
            var fontSize = runProperty.FontSize;

            var glyphIndices = new List<ushort>(charList.Count);
            var advanceWidths = new List<double>(charList.Count);
            var characters = new List<char>(charList.Count);

            var startPoint = charList[0].GetStartPoint();
            // 为了尽可能的进行复用，尝试减去行的偏移，如此行绘制信息可以重复使用。只需要上层使用重新设置行的偏移量
            startPoint = startPoint with { Y = startPoint.Y - argument.StartPoint.Y };

            // 行渲染高度
            var height = 0d;

            foreach (var charData in charList)
            {
                var text = charData.CharObject.ToText();

                for (var i = 0; i < text.Length; i++)
                {
                    var c = text[i];
                    var glyphIndex = glyphTypeface.CharacterToGlyphMap[c];
                    glyphIndices.Add(glyphIndex);

                    var width = glyphTypeface.AdvanceWidths[glyphIndex] * fontSize;
                    width = GlyphExtension.RefineValue(width);
                    advanceWidths.Add(width);

                    height = Math.Max(height, glyphTypeface.AdvanceHeights[glyphIndex] * fontSize);

                    characters.Add(c);
                }
            }

            var location = new System.Windows.Point(startPoint.X, startPoint.Y + height);

            var glyphRun = new GlyphRun
            (
                glyphTypeface,
                bidiLevel: 0,
                isSideways: false,
                renderingEmSize: fontSize,
                pixelsPerDip: pixelsPerDip,
                glyphIndices: glyphIndices,
                baselineOrigin: location, // 设置文本的偏移量
                advanceWidths: advanceWidths, // 设置每个字符的字宽，也就是字号
                glyphOffsets: null, // 设置每个字符的偏移量，可以为空
                characters: characters,
                deviceFontName: null,
                clusterMap: null,
                caretStops: null,
                language: _defaultXmlLanguage
            );

            Brush brush = currentRunProperty.Foreground.Value;
            drawingContext.DrawGlyphRun(brush, glyphRun);
        }

        return drawingVisual;
    }

    private readonly XmlLanguage _defaultXmlLanguage =
        XmlLanguage.GetLanguage(CultureInfo.CurrentUICulture.IetfLanguageTag);
}

internal class TextEditorPlatformProvider : PlatformProvider
{
    public TextEditorPlatformProvider(TextEditor textEditor)
    {
        TextEditor = textEditor;

        _textLayoutDispatcherRequiring = new DispatcherRequiring(UpdateLayout, DispatcherPriority.Render);
        _charInfoMeasurer = new CharInfoMeasurer(textEditor);
        _runPropertyCreator = new RunPropertyCreator();
    }

    private void UpdateLayout()
    {
        Debug.Assert(_lastTextLayout is not null);
        _lastTextLayout?.Invoke();
    }

    private TextEditor TextEditor { get; }
    private readonly DispatcherRequiring _textLayoutDispatcherRequiring;
    private Action? _lastTextLayout;

    public override void RequireDispatchUpdateLayout(Action textLayout)
    {
        _lastTextLayout = textLayout;
        _textLayoutDispatcherRequiring.Require();
    }

    public override ICharInfoMeasurer? GetCharInfoMeasurer()
    {
        return _charInfoMeasurer;
    }

    private readonly CharInfoMeasurer _charInfoMeasurer;

    public override IRenderManager? GetRenderManager()
    {
        return TextEditor;
    }

    public override IPlatformRunPropertyCreator GetPlatformRunPropertyCreator() => _runPropertyCreator;

    private readonly RunPropertyCreator _runPropertyCreator; //= new RunPropertyCreator();
}

class CharInfoMeasurer : ICharInfoMeasurer
{
    public CharInfoMeasurer(TextEditor textEditor)
    {
        _textEditor = textEditor;
    }
    private readonly TextEditor _textEditor;

    // todo 允许开发者设置默认字体
    private readonly FontFamily _defaultFontFamily = new FontFamily($"微软雅黑");

    public CharInfoMeasureResult MeasureCharInfo(in CharInfo charInfo)
    {
        // todo 属性系统需要加上字体管理模块
        // todo 处理字体回滚
        var runPropertyFontFamily = charInfo.RunProperty.AsRunProperty().FontName;
        var fontFamily = ToFontFamily(runPropertyFontFamily);
        var collection = fontFamily.GetTypefaces();
        Typeface typeface = collection.First();
        foreach (var t in collection)
        {
            if (t.Stretch == FontStretches.Normal && t.Weight == FontWeights.Normal)
            {
                typeface = t;
                break;
            }
        }

        bool success = typeface.TryGetGlyphTypeface(out GlyphTypeface glyphTypeface);

        if (!success)
        {
            // 处理字体回滚
        }

        // todo 对于字符来说，反复在字符串和字符转换，需要优化
        var text = charInfo.CharObject.ToText();

        var size = Size.Zero;

        if (_textEditor.TextEditorCore.ArrangingType == ArrangingType.Horizontal)
        {
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];

                var glyphIndex = glyphTypeface.CharacterToGlyphMap[c];

                var fontSize = charInfo.RunProperty.FontSize;

                var width = glyphTypeface.AdvanceWidths[glyphIndex] * fontSize;
                width = GlyphExtension.RefineValue(width);
                var height = glyphTypeface.AdvanceHeights[glyphIndex] * fontSize;

                size = size.HorizontalUnion(width, height);
            }
        }
        else
        {
            throw new NotImplementedException("还没有实现竖排的文本测量");
        }

        return new CharInfoMeasureResult(new Rect(new Point(), size));
    }

    private FontFamily ToFontFamily(FontName runPropertyFontFamily)
    {
        if (runPropertyFontFamily.IsNotDefineFontName)
        {
            // 如果采用没有定义的字体名，那就返回默认的字体
            return _defaultFontFamily;
        }

        var fontFamily = new FontFamily(runPropertyFontFamily.UserFontName);
        return fontFamily;
    }
}