using System;
using System.Collections.Generic;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Utils;
using SkiaSharp;

namespace LightTextEditorPlus.Rendering;

class TextEditorSkiaRender : ITextEditorContentSkiaRender
{
    public TextEditorSkiaRender(SkiaTextEditor textEditor, SKPicture picture, TextRect renderBounds)
    {
        _picture = picture;
        RenderBounds = renderBounds;
        _textEditor = textEditor;
        IsInDebugMode = _textEditor.TextEditorCore.IsInDebugMode;
    }

    /// <summary>
    /// �������ı��༭�������ֶν����ڵ��ԣ��в�Ҫ�ڴ��ֶ��ϵ����κη�������Ϊ����������Ⱦ�߳��ϵ��ã������� UI �߳��ϵ���
    /// </summary>
    private readonly SkiaTextEditor _textEditor;

    public TextRect RenderBounds { get; }

    internal bool IsUsed { get; set; }

    private readonly SKPicture _picture;

    public bool IsDisposed { get; private set; }
    public bool IsInDebugMode { get; }

    public string? DisposeReason { get; private set; }

    public void Dispose(string disposeReason)
    {
        DisposeReason = disposeReason;
        IDisposable disposable = this;
        disposable.Dispose();
    }

    void IDisposable.Dispose()
    {
        _picture.Dispose();
        IsDisposed = true;
    }

    public void Render(SKCanvas canvas)
    {
        if (IsDisposed)
        {
#if DEBUG
            var name = _textEditor.DebugName;
            string? disposeReason = DisposeReason;
            GC.KeepAlive(name);
            GC.KeepAlive(disposeReason);
#endif
        }

        canvas.DrawPicture(_picture);
    }

    private int _count;

    public void AddReference()
    {
        if (IsDisposed)
        {

        }

        _count++;
    }

    public void ReleaseReference()
    {
        _count--;
        if (_count == 0)
        {
            Dispose("ReleaseReference to 0");
        }
    }
}

record TextEditorSelectionSkiaRender(IReadOnlyList<TextRect> SelectionBoundsList, SKColor SelectionColor) : ITextEditorCaretAndSelectionRenderSkiaRender
{
    public void Render(SKCanvas canvas)
    {
        using SKPaint skPaint = new SKPaint();
        skPaint.Style = SKPaintStyle.Fill;
        skPaint.Color = SelectionColor;
        foreach (TextRect selectionBounds in SelectionBoundsList)
        {
            canvas.DrawRect(selectionBounds.ToSKRect(), skPaint);
        }
    }

    public void AddReference()
    {
    }

    public void ReleaseReference()
    {
    }

    public void Dispose()
    {
    }
}

record TextEditorCaretSkiaRender(SKRect CaretBounds, SKColor CaretColor) : ITextEditorCaretAndSelectionRenderSkiaRender
{
    public void Render(SKCanvas canvas)
    {
        using SKPaint skPaint = new SKPaint();
        skPaint.Style = SKPaintStyle.Fill;
        skPaint.Color = CaretColor;
        canvas.DrawRect(CaretBounds, skPaint);
    }

    public void AddReference()
    {
    }

    public void ReleaseReference()
    {
    }

    public void Dispose()
    {
    }
}

