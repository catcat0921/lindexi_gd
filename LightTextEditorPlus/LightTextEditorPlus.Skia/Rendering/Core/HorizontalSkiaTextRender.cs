﻿using System;
using System.Diagnostics;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Core.Utils;
using LightTextEditorPlus.Diagnostics;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Utils;
using SkiaSharp;

namespace LightTextEditorPlus.Rendering.Core;

/// <summary>
/// 水平横排的文本渲染器
/// </summary>
class HorizontalSkiaTextRender : BaseSkiaTextRender
{
    public HorizontalSkiaTextRender(RenderManager renderManager): base(renderManager.TextEditor)
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
                if (lineInfo.IsIncludeMarker)
                {
                    TextReadOnlyListSpan<CharData> markerCharDataList = lineInfo.GetMarkerCharDataList();
                    RenderCharList(markerCharDataList, lineInfo);
                }

                // 先不考虑缓存
                LineDrawingArgument argument = lineInfo.Argument;

                foreach (TextReadOnlyListSpan<CharData> charList in argument.CharList.GetCharSpanContinuous())
                {
                    RenderCharList(charList, lineInfo);
                }

                if (argument.CharList.Count == 0)
                {
                    // 空行
                    // 绘制四线三格的调试信息
                    if (TextEditor.DebugConfiguration.ShowHandwritingPaperDebugInfo)
                    {
                        CharHandwritingPaperInfo charHandwritingPaperInfo =
                            renderInfoProvider.GetHandwritingPaperInfo(in lineInfo);
                        DrawDebugHandwritingPaper(canvas, new TextRect(argument.StartPoint, argument.LineContentSize with
                        {
                            // 空行是 0 宽度，需要将其设置为整个文本的宽度才好计算
                            Width = renderInfoProvider.TextEditor.DocumentManager.DocumentWidth,
                        }).ToSKRect(), charHandwritingPaperInfo);
                    }
                }

                DrawDebugBounds(new TextRect(argument.StartPoint, argument.LineContentSize).ToSKRect(), Config.DebugDrawLineContentBoundsInfo);
            }
        }

        return new SkiaTextRenderResult()
        {
            RenderBounds = renderBounds
        };

        void DrawDebugBounds(SKRect bounds, TextEditorDebugBoundsDrawInfo? drawInfo)
        {
            DrawDebugBoundsInfo(canvas, bounds, drawInfo);
        }

        void RenderCharList(TextReadOnlyListSpan<CharData> charList, ParagraphLineRenderInfo lineInfo)
        {
            CharData firstCharData = charList[0];

            SkiaTextRunProperty skiaTextRunProperty = firstCharData.RunProperty.AsSkiaRunProperty();

            // 不需要在这里处理字体回滚，在输入的过程中已经处理过了
            ////  考虑字体回滚问题
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
                DrawDebugBounds(charData.GetBounds().ToSKRect(), Config.DebugDrawCharBoundsInfo);

                width += (float) charData.Size!.Value.Width;
            }

            SKRect charSpanBounds = SKRect.Create(x, y, width, height);
            DrawDebugBounds(charSpanBounds, Config.DebugDrawCharSpanBoundsInfo);
            renderBounds = renderBounds.Union(charSpanBounds.ToTextRect());

            if (!skFont.ContainsGlyphs(charSpan))
            {
                // 预计不会出现这样的问题，在渲染之前已经处理过了
                throw new TextEditorInnerException($"文本框架内应该确保进入渲染层时，不会出现字体不能包含字符的情况");
            }

            // 绘制四线三格的调试信息
            if (TextEditor.DebugConfiguration.ShowHandwritingPaperDebugInfo)
            {
                CharHandwritingPaperInfo charHandwritingPaperInfo =
                    renderInfoProvider.GetHandwritingPaperInfo(in lineInfo, firstCharData);
                DrawDebugHandwritingPaper(canvas, charSpanBounds, charHandwritingPaperInfo);
            }

            //float x = skiaTextRenderInfo.X;
            //float y = skiaTextRenderInfo.Y;

            var baselineY = /*skFont.Metrics.Leading +*/ (-skFont.Metrics.Ascent);

            // 由于 Skia 的 DrawText 传入的 Point 是文本的基线，因此需要调整 Y 值
            y += baselineY;
            using SKTextBlob skTextBlob = SKTextBlob.Create(charSpan, skFont);
            canvas.DrawText(skTextBlob, x, y, textRenderSKPaint);
        }
    }
}