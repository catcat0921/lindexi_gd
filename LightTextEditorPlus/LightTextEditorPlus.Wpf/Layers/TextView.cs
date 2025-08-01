using System;
using System.Windows;
using System.Windows.Media;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Layers.Caret_;
using LightTextEditorPlus.Rendering;
using Rect = System.Windows.Rect;

namespace LightTextEditorPlus.Layers;

/// <summary>
/// 视觉呈现容器
/// </summary>
/// 包括了文本本身和叠加的选择和光标等。由于文本字符渲染本身属于重点核心逻辑，就放在这个类型里面，减少 Visual 数量
class TextView : UIElement, IRenderManager
{
    static TextView()
    {
        // 因为此类型永远不可被命中，所以直接重写并不再处理基类的命中测试改变方法。
        IsHitTestVisibleProperty.OverrideMetadata(typeof(TextView), new UIPropertyMetadata(false));
    }

    public TextView(TextEditor textEditor)
    {
        _textEditor = textEditor;

        _selectionAndCaretLayer = new SelectionAndCaretLayer(textEditor);

        _visualCollection = new VisualCollection(this)
        {
            _selectionAndCaretLayer
        };
        IsHitTestVisible = true;
        textEditor.Loaded += TextEditor_Loaded;
    }

    private void TextEditor_Loaded(object sender, RoutedEventArgs e)
    {
        _textEditor.Loaded -= TextEditor_Loaded;

        if (!_textEditor.TextEditorCore.IsDirty)
        {
            var renderInfoProvider = _textEditor.TextEditorCore.GetRenderInfo();
            _selectionAndCaretLayer.UpdateSelectionAndCaret(renderInfoProvider);
        }
    }

    private readonly TextEditor _textEditor;
    private readonly VisualCollection _visualCollection;

    private TextRendererBase? _textRenderBase;

    public void Render(RenderInfoProvider renderInfoProvider)
    {
        var textRender = GetTextRenderBase();

        var drawingVisual = textRender.Render(renderInfoProvider, _textEditor);

        //SnapsToDevicePixels = true;
        RenderOptions.SetClearTypeHint(drawingVisual, ClearTypeHint.Enabled);
        RenderOptions.SetEdgeMode(drawingVisual, EdgeMode.Aliased);

        // 需要加入逻辑树，且需要将旧的从逻辑树移除。否则将看不到文本
        if (_textDrawingVisual is not null)
        {
            _visualCollection.Remove(_textDrawingVisual);
        }

        _textDrawingVisual = drawingVisual;
        _visualCollection.Insert(0, drawingVisual);

        _selectionAndCaretLayer.UpdateSelectionAndCaret(renderInfoProvider);
        //InvalidateVisual();
    }

    private TextRendererBase GetTextRenderBase()
    {
        if (_textEditor.TextEditorCore.ArrangingType == ArrangingType.Horizontal)
        {
            if (_textRenderBase is HorizontalTextRenderer)
            {
                return _textRenderBase;
            }
            else
            {
                _textRenderBase = new HorizontalTextRenderer();
                return _textRenderBase;
            }
        }

        if (_textEditor.TextEditorCore.ArrangingType == ArrangingType.Vertical || _textEditor.TextEditorCore.ArrangingType == ArrangingType.Mongolian)
        {
            // 还没有支持竖排渲染
            throw new NotSupportedException();
        }

        throw new ArgumentOutOfRangeException();
    }

    private DrawingVisual? _textDrawingVisual;
    private readonly SelectionAndCaretLayer _selectionAndCaretLayer;

    protected override Visual GetVisualChild(int index) => _visualCollection[index];

    protected override int VisualChildrenCount => _visualCollection.Count;

    #region 禁用命中测试

    protected override System.Windows.Size MeasureCore(System.Windows.Size availableSize)
    {
        return availableSize;
    }

    protected override void ArrangeCore(Rect finalRect)
    {
        // 走默认布局即可
        base.ArrangeCore(finalRect);
    }

    #region 命中测试

    // 只是用来呈现，不进行交互，关闭命中测试可以提升很多性能

    protected override HitTestResult? HitTestCore(PointHitTestParameters hitTestParameters) => null;
    protected override GeometryHitTestResult? HitTestCore(GeometryHitTestParameters hitTestParameters) => null;
    #endregion

    #endregion
}