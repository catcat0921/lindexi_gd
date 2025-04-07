using System;
using System.Diagnostics;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Core.Utils;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Utils;
using SkiaSharp;

namespace LightTextEditorPlus.Rendering.Core;

class HorizontalSkiaTextRender : BaseSkiaTextRender
{
    public RenderManager RenderManager { get; }

    public HorizontalSkiaTextRender(RenderManager renderManager): base(renderManager.TextEditor)
    {
        RenderManager = renderManager;
    }

    private SkiaTextEditor _textEditor => base.TextEditor;

    public override SkiaTextRenderResult Render(in SkiaTextRenderArgument renderArgument)
    {
        SKCanvas canvas = renderArgument.Canvas;

        RenderInfoProvider renderInfoProvider = renderArgument.RenderInfoProvider;
        TextRect renderBounds = renderArgument.RenderBounds;

        Debug.Assert(!renderInfoProvider.IsDirty);

        foreach (ParagraphRenderInfo paragraphRenderInfo in renderInfoProvider.GetParagraphRenderInfoList())
        {
            foreach (ParagraphLineRenderInfo lineInfo in paragraphRenderInfo.GetLineRenderInfoList())
            {
                // �Ȳ����ǻ���
                LineDrawingArgument argument = lineInfo.Argument;
                foreach (TextReadOnlyListSpan<CharData> charList in argument.CharList.GetCharSpanContinuous())
                {
                    CharData firstCharData = charList[0];

                    SkiaTextRunProperty skiaTextRunProperty = firstCharData.RunProperty.AsSkiaRunProperty();

                    // ����Ҫ�����ﴦ������ع���������Ĺ������Ѿ��������
                    ////  ��������ع�����
                    RenderingRunPropertyInfo renderingRunPropertyInfo = skiaTextRunProperty.GetRenderingRunPropertyInfo(firstCharData.CharObject.CodePoint);

                    SKFont skFont = renderingRunPropertyInfo.Font;

                    SKPaint textRenderSKPaint = renderingRunPropertyInfo.Paint;

                    var runBounds = firstCharData.GetBounds();
                    var startPoint = runBounds.LeftTop;

                    float x = (float) startPoint.X;
                    float y = (float) startPoint.Y;
                    float width = 0;
                    float height = (float) runBounds.Height;

                    using CharDataListToCharSpanResult charSpanResult = charList.ToCharSpan();
                    ReadOnlySpan<char> charSpan = charSpanResult.CharSpan;

                    foreach (CharData charData in charList)
                    {
                        DrawDebugBounds(charData.GetBounds().ToSKRect(), _debugDrawCharBoundsColor);

                        width += (float) charData.Size!.Value.Width;
                    }

                    SKRect charSpanBounds = SKRect.Create(x, y, width, height);
                    DrawDebugBounds(charSpanBounds, _debugDrawCharSpanBoundsColor);
                    renderBounds = renderBounds.Union(charSpanBounds.ToTextRect());

                    if (!skFont.ContainsGlyphs(charSpan))
                    {
                        // Ԥ�Ʋ���������������⣬����Ⱦ֮ǰ�Ѿ��������
                        throw new TextEditorInnerException($"�ı������Ӧ��ȷ��������Ⱦ��ʱ������������岻�ܰ����ַ������");
                    }

                    // ������������ĵ�����Ϣ
                    if (_textEditor.DebugConfiguration.ShowHandwritingPaperDebugInfo)
                    {
                        CharHandwritingPaperInfo charHandwritingPaperInfo =
                            renderInfoProvider.GetHandwritingPaperInfo(in lineInfo, firstCharData);
                        DrawDebugHandwritingPaper(canvas, charSpanBounds, charHandwritingPaperInfo);
                    }

                    //float x = skiaTextRenderInfo.X;
                    //float y = skiaTextRenderInfo.Y;

                    var baselineY = /*skFont.Metrics.Leading +*/ (-skFont.Metrics.Ascent);

                    // ���� Skia �� DrawText ����� Point ���ı��Ļ��ߣ������Ҫ���� Y ֵ
                    y += baselineY;
                    using SKTextBlob skTextBlob = SKTextBlob.Create(charSpan, skFont);
                    canvas.DrawText(skTextBlob, x, y, textRenderSKPaint);
                }

                if (argument.CharList.Count == 0)
                {
                    // ����
                    // ������������ĵ�����Ϣ
                    if (_textEditor.DebugConfiguration.ShowHandwritingPaperDebugInfo)
                    {
                        CharHandwritingPaperInfo charHandwritingPaperInfo =
                            renderInfoProvider.GetHandwritingPaperInfo(in lineInfo);
                        DrawDebugHandwritingPaper(canvas, new TextRect(argument.StartPoint, argument.LineSize with
                        {
                            // ������ 0 ��ȣ���Ҫ��������Ϊ�����ı��Ŀ�Ȳźü���
                            Width = renderInfoProvider.TextEditor.DocumentManager.DocumentWidth,
                        }).ToSKRect(), charHandwritingPaperInfo);
                    }
                }

                DrawDebugBounds(new TextRect(argument.StartPoint, argument.LineSize).ToSKRect(), _debugDrawLineBoundsColor);
            }
        }

        return new SkiaTextRenderResult()
        {
            RenderBounds = renderBounds
        };

        void DrawDebugBounds(SKRect bounds, SKColor? color)
        {
            if (color is null)
            {
                return;
            }

            SKPaint debugPaint = GetDebugPaint(color.Value);
            canvas.DrawRect(bounds, debugPaint);
        }
    }
}