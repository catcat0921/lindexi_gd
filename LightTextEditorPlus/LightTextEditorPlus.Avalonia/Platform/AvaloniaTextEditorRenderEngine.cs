using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;

using LightTextEditorPlus.Utils;
using System.Collections.Generic;

using System.Diagnostics;
using System.Linq;

namespace LightTextEditorPlus;

partial class TextEditor
{
    private readonly AvaloniaTextEditorRenderEngine _renderEngine;

    private class AvaloniaTextEditorRenderEngine
    {
        public AvaloniaTextEditorRenderEngine(TextEditor textEditor)
        {
            TextEditor = textEditor;
        }

        public TextEditor TextEditor { get; }
        private Rect _lastRenderBounds = new Rect();

        /// <summary>
        /// ���ڽ���ͷ�����
        /// </summary>
        private readonly HashSet<ITextEditorContentSkiaRender> _contentSkiaRenderCache = new HashSet<ITextEditorContentSkiaRender>(ReferenceEqualityComparer.Instance);

        /// <summary>
        /// ����һ�����ڵ���ʹ�õ��ֶ�
        /// </summary>
        private List<ITextEditorContentSkiaRender>? _debugAllContentSkiaRenderList;

        public void Render(DrawingContext context)
        {
            TextEditor textEditor = TextEditor;
            if (textEditor.IsDirty)
            {
                // ׼��Ҫ��Ⱦ�ˣ�����ı�������ģ��Ǿ�ǿ�Ʋ���
                textEditor.ForceRedraw();
            }

            SkiaTextEditor skiaTextEditor = textEditor.SkiaTextEditor;
            ITextEditorContentSkiaRender textEditorSkiaRender = skiaTextEditor.GetCurrentTextRender();

            var currentBounds = new Rect(textEditor.DesiredSize);

            #region �����Ⱦ��Դ�ͷ�����
            // ���Խ���ͷ����⡣��Ϊ������ UI �̣߳�����֪����Ⱦ�߳��Ƿ���ʹ�ã����Ǿ������ͷ��߼�������Ⱦ�߳�
            List<ITextEditorContentSkiaRender>? toDisposedList = null;
            _contentSkiaRenderCache.RemoveWhere(t => t.IsDisposed);
            foreach (var textEditorContentSkiaRender in _contentSkiaRenderCache.Where(textEditorContentSkiaRender => textEditorContentSkiaRender.IsObsoleted))
            {
                toDisposedList ??= new List<ITextEditorContentSkiaRender>();
                toDisposedList.Add(textEditorContentSkiaRender);
            }
            if (_contentSkiaRenderCache.Add(textEditorSkiaRender))
            {
                Debug.Assert(_contentSkiaRenderCache.Count < 3, "Ԥ�ڲ��ᳬ��������һ������ UI �߳�׼���ȴ���Ⱦ����һ��������Ⱦ�߳̽�����Ⱦ����");
#if DEBUG
                _debugAllContentSkiaRenderList ??= new List<ITextEditorContentSkiaRender>();
                _debugAllContentSkiaRenderList.Add(textEditorSkiaRender);
#endif
            }

            var count = _debugAllContentSkiaRenderList?.Count(t => !t.IsDisposed);
            _ = count;
            Debug.WriteLine($"��ǰδ�ͷ������� {count}/{_debugAllContentSkiaRenderList?.Count}");
            #endregion

            currentBounds = currentBounds.Union(textEditorSkiaRender.RenderBounds.ToAvaloniaRect());

            var renderBounds = currentBounds;

            if (_lastRenderBounds.Width > 0 || _lastRenderBounds.Height > 0)
            {
                // ֮ǰ����Ⱦ�����Ǿ�Ҫ�ػ�֮ǰ���������� Avalonia �����⣬���ǰһ�η�Χ�Ƚϴ󣬱��αȽ�С�������Ȼ���ձ��εķ�Χ�������ǰһ�ε���Ⱦ���ݲ����
                renderBounds = renderBounds.Union(_lastRenderBounds);
            }
            _lastRenderBounds = currentBounds;

            context.Custom(new TextEditorCustomDrawOperation(renderBounds, textEditorSkiaRender, toDisposedList));

            if (textEditor.IsInEditingInputMode
                // ���������ѡ�������ڷǱ༭ģʽ��Ҳ����ƣ����ڷǱ༭ģʽ��Ҳ�����ѡ������
                || textEditor.CaretConfiguration.ShowSelectionWhenNotInEditingInputMode)
            {
                // ֻ�б༭ģʽ�²Ż���ƹ���ѡ������
                context.Custom(new TextEditorCustomDrawOperation(renderBounds,
                    skiaTextEditor.GetCurrentCaretAndSelectionRender(), toDisposedList: null));
            }
        }
    }
}

file class TextEditorCustomDrawOperation : ICustomDrawOperation
{
    public TextEditorCustomDrawOperation(Rect bounds, ITextEditorSkiaRender render, List<ITextEditorContentSkiaRender>? toDisposedList)
    {
        _render = render;
        _toDisposedList = toDisposedList;
        Bounds = bounds;

        render.AddReference();
    }

    private readonly ITextEditorSkiaRender _render;
    private readonly List<ITextEditorContentSkiaRender>? _toDisposedList;

    public void Dispose()
    {
        _render.ReleaseReference();
    }

    public bool Equals(ICustomDrawOperation? other)
    {
        return ReferenceEquals(_render, (other as TextEditorCustomDrawOperation)?._render);
    }

    public bool HitTest(Point p)
    {
        return Bounds.Contains(p);
    }

    public void Render(ImmediateDrawingContext context)
    {
        ISkiaSharpApiLeaseFeature? skiaSharpApiLeaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
        if (skiaSharpApiLeaseFeature != null)
        {
            using ISkiaSharpApiLease skiaSharpApiLease = skiaSharpApiLeaseFeature.Lease();
            _render.Render(skiaSharpApiLease.SkCanvas);
        }
        else
        {
            // ��֧�� Skia ����
        }
    }

    public Rect Bounds { get; }
}