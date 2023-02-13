﻿using System;
using System.Windows;
using System.Windows.Media;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Rendering;

namespace LightTextEditorPlus.Layout;

/// <summary>
/// 视觉呈现容器
/// </summary>
class TextView : UIElement, IRenderManager
{
    public TextView(TextEditor textEditor)
    {
        _textEditor = textEditor;

        // 因为此类型永远不可被命中，所以直接重写并不再处理基类的命中测试改变方法。
        IsHitTestVisibleProperty.OverrideMetadata(typeof(TextView), new UIPropertyMetadata(false));

        _visualCollection = new VisualCollection(this);
    }

    private readonly TextEditor _textEditor;
    private readonly VisualCollection _visualCollection;

    private TextRenderBase? _textRenderBase;

    public void Render(RenderInfoProvider renderInfoProvider)
    {
        var textRender = GetTextRenderBase();

        var drawingVisual = textRender.Render(renderInfoProvider, _textEditor);

        // 需要加入逻辑树，且需要将旧的从逻辑树移除。否则将看不到文本
        if (_textDrawingVisual is not null)
        {
            _visualCollection.Remove(_textDrawingVisual);
        }

        _textDrawingVisual = drawingVisual;
        _visualCollection.Add(drawingVisual);

        InvalidateVisual();
    }

    private TextRenderBase GetTextRenderBase()
    {
        switch (_textEditor.TextEditorCore.ArrangingType)
        {
            case ArrangingType.Horizontal:
                if (_textRenderBase is HorizontalTextRender)
                {
                    return _textRenderBase;
                }
                else
                {
                    _textRenderBase = new HorizontalTextRender();
                    return _textRenderBase;
                }
            case ArrangingType.Vertical:
            case ArrangingType.Mongolian:
                // 还没有支持竖排渲染
                throw new NotSupportedException();
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private DrawingVisual? _textDrawingVisual;

    protected override Visual GetVisualChild(int index) => _visualCollection[index];

    protected override int VisualChildrenCount => _visualCollection.Count;

    #region 禁用命中测试

    // 只是用来呈现，不进行交互，关闭命中测试可以提升很多性能

    protected override HitTestResult? HitTestCore(PointHitTestParameters hitTestParameters) => null;
    protected override GeometryHitTestResult? HitTestCore(GeometryHitTestParameters hitTestParameters) => null;

    #endregion
}