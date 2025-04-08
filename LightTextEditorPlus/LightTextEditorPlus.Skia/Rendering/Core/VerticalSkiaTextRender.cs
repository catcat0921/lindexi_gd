using System;
using System.Diagnostics;

using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Core.Utils;
using LightTextEditorPlus.Document;
using SkiaSharp;

namespace LightTextEditorPlus.Rendering.Core;

/// <summary>
/// �����ı���Ⱦ��
/// </summary>
class VerticalSkiaTextRender : BaseSkiaTextRender
{
    public VerticalSkiaTextRender(RenderManager renderManager) : base(renderManager.TextEditor)
    {
        RenderManager = renderManager;
    }

    public RenderManager RenderManager { get; }

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
                    RenderingRunPropertyInfo renderingRunPropertyInfo = skiaTextRunProperty.GetRenderingRunPropertyInfo(firstCharData.CharObject.CodePoint);
                    SKFont skFont = renderingRunPropertyInfo.Font;
                    SKPaint textRenderSKPaint = renderingRunPropertyInfo.Paint;

                    using CharDataListToCharSpanResult charSpanResult = charList.ToCharSpan();
                    ReadOnlySpan<char> charSpan = charSpanResult.CharSpan;

                    SKPoint[] positionList = new SKPoint[charList.Count];
                    for (int i = 0; i < charList.Count; i++)
                    {
                        (double x, double y) = charList[i].GetStartPoint();
                        y += charList[i].Baseline;
                        positionList[i] = new SKPoint((float) x, (float) y);
                    }

                    using SKTextBlob skTextBlob = SKTextBlob.CreatePositioned(charSpan,skFont, positionList.AsSpan());
                    canvas.DrawText(skTextBlob, 0, 0, textRenderSKPaint);
                }
            }
        }

        return new SkiaTextRenderResult()
        {
            // todo ������Ҫ�޸�Ϊ��ȷ�ķ�Χ
            RenderBounds = renderBounds
        };
    }
}