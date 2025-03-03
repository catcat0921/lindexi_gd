using System;
using System.Collections.Generic;
using System.Text;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive.Collections;

using SkiaSharp;

using HarfBuzzSharp;
using Buffer = HarfBuzzSharp.Buffer;
using System.Runtime.InteropServices;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Layout.LayoutUtils.WordDividers;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Document;

namespace LightTextEditorPlus.Platform;

// ��ʵ�����ǲ���Ҫ�ģ�ֻҪ���ַ����ּ���
public class SkiaWholeLineCharsLayouter : IWholeLineCharsLayouter
{
    public WholeLineCharsLayoutResult UpdateWholeLineCharsLayout(in WholeLineLayoutArgument argument)
    {
        ParagraphProperty paragraphProperty = argument.ParagraphProperty;
        TextReadOnlyListSpan<CharData> charDataList = argument.CharDataList;
        double lineMaxWidth = argument.LineMaxWidth;
        UpdateLayoutContext context = argument.UpdateLayoutContext;

        IWordDivider wordDivider = context.PlatformProvider.GetWordDivider();

        // �л�ʣ��Ŀ��п��
        double lineRemainingWidth = lineMaxWidth;

        // ��ǰ����� charDataList �ĵ�ǰ���
        int currentIndex = 0;
        // ��ǰ���ַ����ֳߴ�
        var currentSize = TextSize.Zero;

        while (currentIndex < charDataList.Count)
        {
            TextReadOnlyListSpan<CharData> runList = charDataList.Slice(currentIndex).GetFirstCharSpanContinuous();
            var arguments = new SingleCharInLineLayoutArgument(charDataList, currentIndex, lineRemainingWidth,
                argument.Paragraph, context);

            FillSizeOfCharData(runList, in arguments);

            // ʹ�ö����㷨�����Ƿ���Ҫ����
            var currentRunList = charDataList.Slice(currentIndex);

            // �ӵ�ǰ���ַ���ʼ�����Ի�ȡһ������
            DivideWordResult divideWordResult = wordDivider.DivideWord(new DivideWordArgument(currentRunList, context));
            int takeCount = divideWordResult.TakeCount;

            // ���㵱ǰ���ܿ��
            var takeSize = TextSize.Zero;
            for (int i = 0; i < takeCount; i++)
            {
                CharData charData = currentRunList[i];
                TextSize size = charData.Size ?? throw new InvalidOperationException("CharData �� Size ����Ϊ��");
                takeSize = takeSize.HorizontalUnion(size);
            }

            if (lineRemainingWidth > takeSize.Width)
            {
                // ��һ�л��пռ䣬���Լ������뵥��
                lineRemainingWidth -= takeSize.Width;

                currentIndex += takeCount;

                currentSize = currentSize.HorizontalUnion(takeSize);
            }
            else
            {
                // ��һ��û�пռ��ˣ���Ҫ����
                if (currentIndex == 0)
                {
                    // ��һ��һ�����ʶ��Ų��£��Ǿ�ǿ�з���һ�����ַ�
                    for (; currentIndex < currentRunList.Count; currentIndex++)
                    {
                        var charData = currentRunList[currentIndex];
                        TextSize textSize = charData.Size ?? throw new InvalidOperationException("CharData �� Size ����Ϊ��");

                        if (lineRemainingWidth > textSize.Width)
                        {
                            lineRemainingWidth -= textSize.Width;
                            currentSize = currentSize.HorizontalUnion(textSize);
                            // currentIndex++
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                break;
            }
        }

        int totalTakeCount = currentIndex;

        return new WholeLineCharsLayoutResult(currentSize, totalTakeCount);
    }

    public SingleCharInLineLayoutResult LayoutSingleCharInLine(in SingleCharInLineLayoutArgument argument)
    {
        return MeasureSingleRunLayout(in argument);
    }

    private SingleCharInLineLayoutResult MeasureSingleRunLayout(in SingleCharInLineLayoutArgument argument)
    {
        // ��ȡ�������ַ������������ַ�Ҳ���ܽ��뵽���ﲼ�֡����Բ���ͬ�ģ��ȴ��´ν���˷�������
        TextReadOnlyListSpan<CharData> runList = argument.RunList.Slice(argument.CurrentIndex).GetFirstCharSpanContinuous();

        FillSizeOfCharData(runList, in argument);

        throw new NotImplementedException();
    }

    private static void FillSizeOfCharData(TextReadOnlyListSpan<CharData> runList, in SingleCharInLineLayoutArgument argument)
    {
        UpdateLayoutContext updateLayoutContext = argument.UpdateLayoutContext;

        CharData currentCharData = argument.CurrentCharData;
        var runProperty = currentCharData.RunProperty.AsSkiaRunProperty();

        RenderingRunPropertyInfo renderingRunPropertyInfo = runProperty.GetRenderingRunPropertyInfo(runList[0].CharObject.CodePoint);
        SKTypeface skTypeface = renderingRunPropertyInfo.Typeface;
        SKFont skFont = renderingRunPropertyInfo.Font;
        SKPaint skPaint = renderingRunPropertyInfo.Paint;

        // ȷ���������ַ��ĳߴ�
        var charCount = 0;
        // Ϊʲô�� 0 ��ʼ�������� argument.CurrentIndex ��ʼ��ԭ������ runList �����Ѿ�ʹ�� Slice �ü���
        StringBuilder stringBuilder = new StringBuilder(runList.Count);
        for (var i = 0; i < runList.Count; i++)
        {
            // ���������ǿ�����һ�� CharObject ������� Char �����
            CharData charData = runList[i];
            string text = charData.CharObject.ToText();
            charCount += text.Length;
            stringBuilder.Append(text);
        }

        // Copy from https://github.com/AvaloniaUI/Avalonia
        // src\Skia\Avalonia.Skia\TextShaperImpl.cs
        // src\Skia\Avalonia.Skia\GlyphRunImpl.cs

        var glyphIndices = new ushort[charCount];
        var glyphBounds = new SKRect[charCount];

        var glyphInfoList = new List<TextGlyphInfo>();
        using (var buffer = new Buffer())
        {
            var text = stringBuilder.ToString();
            buffer.AddUtf16(text);
            buffer.GuessSegmentProperties();

            buffer.Language = new Language(updateLayoutContext.TextEditor.CurrentCulture);

            var face = new HarfBuzzSharp.Face(GetTable);

            Blob? GetTable(Face f, Tag tag)
            {
                var size = skTypeface.GetTableSize(tag);
                var data = Marshal.AllocCoTaskMem(size);
                if (skTypeface.TryGetTableData(tag, 0, size, data))
                {
                    return new Blob(data, size, MemoryMode.ReadOnly, () => Marshal.FreeCoTaskMem(data));
                }
                else
                {
                    return null;
                }
            }

            var font = new HarfBuzzSharp.Font(face);
            font.SetFunctionsOpenType();

            font.Shape(buffer);

            font.GetScale(out var scaleX, out _);

            var fontRenderingEmSize = skPaint.TextSize;
            var textScale = fontRenderingEmSize / (float) scaleX;

            var bufferLength = buffer.Length;

            var glyphInfos = buffer.GetGlyphInfoSpan();

            var glyphPositions = buffer.GetGlyphPositionSpan();

            for (var i = 0; i < bufferLength; i++)
            {
                var sourceInfo = glyphInfos[i];

                var glyphIndex = (ushort) sourceInfo.Codepoint;

                var glyphCluster = (int) sourceInfo.Cluster;

                var position = glyphPositions[i];

                var glyphAdvance = position.XAdvance * textScale;

                var offsetX = position.XOffset * textScale;

                var offsetY = -position.YOffset * textScale;

                glyphInfoList.Add(new TextGlyphInfo(glyphIndex, glyphCluster, glyphAdvance, (offsetX, offsetY)));
            }
        }

        var count = glyphInfoList.Count;
        var renderGlyphPositions = new SKPoint[count]; // û������õ�
        var currentX = 0.0;
        for (int i = 0; i < count; i++)
        {
            var glyphInfo = glyphInfoList[i];
            var offset = glyphInfo.GlyphOffset;

            glyphIndices[i] = glyphInfo.GlyphIndex;

            renderGlyphPositions[i] = new SKPoint((float) (currentX + offset.OffsetX), (float) offset.OffsetY);

            currentX += glyphInfoList[i].GlyphAdvance;
        }

        // ���´������ֻ��Ϊ�˵��Զ��ѣ����ȶ��˾Ϳ���ɾ��
        _ = renderGlyphPositions;

        var runBounds = new SKRect();
        var glyphRunBounds = new SKRect[count];
        skFont.GetGlyphWidths(glyphIndices.AsSpan(0, charCount), null, glyphBounds.AsSpan(0, charCount));

        var baselineY = -skFont.Metrics.Ascent;

        var baselineOrigin = new SKPoint(0, baselineY);
        currentX = 0.0;

        float charHeight = renderingRunPropertyInfo.GetLayoutCharHeight();

        // ʵ��ʹ�����棬���Ժ��� GetGlyphWidths ��Ӱ�죬��Ϊʵ����û���õ�
        for (var i = 0; i < count; i++)
        {
            var renderBounds = glyphBounds[i];
            var glyphInfo = glyphInfoList[i];
            var advance = glyphInfo.GlyphAdvance;

            // ˮƽ�����£���Ӧ�÷����ַ�����Ⱦ�߶ȣ�����Ӧ�÷����ַ��߶ȡ��������Ա�֤�ַ��Ļ��߶��롣�� a �� f �� g �ĸ߶Ȳ���ͬ�������������Ⱦ�߶ȷ��أ��ᵼ�»��߲����룬��ɵײ�����
            // ���Ӧ���� advance ��������Ⱦ��ȣ���Ⱦ���̫խ

            var width = (float) advance;// renderBounds.Width;
            float height = charHeight;// = renderBounds.Height; //skPaint.TextSize; //(float) skFont.Metrics.Ascent + (float) skFont.Metrics.Descent;
            //height = (float) LineSpacingCalculator.CalculateLineHeightWithPPTLineSpacingAlgorithm(1, skPaint.TextSize);
            //var enhance = 0f;
            //// ��Щ����� Top ���ǳ������ӣ���Ҫ�������绪�ķ�������
            ////if (baselineY < Math.Abs(skFont.Metrics.Top))
            ////{
            ////    enhance = Math.Abs(skFont.Metrics.Top) - baselineY;
            ////}

            //height = /*skFont.Metrics.Leading + ��Щ����� Leading �ǲ������Ű�ģ�Խ���ģ������ϼӡ����ܽ��������� */ baselineY + skFont.Metrics.Descent + enhance;
            //// ͬ�� skFont.Metrics.Bottom Ҳ�ǲ�Ӧ��ʹ�õģ������¼��ǳ������ӵ�

            glyphRunBounds[i] = SKRect.Create((float) (currentX + renderBounds.Left), baselineOrigin.Y + renderBounds.Top, width,
                height);

            runBounds.Union(glyphRunBounds[i]);

            currentX += advance;
        }

        if (runBounds.Left < 0)
        {
            runBounds.Offset(-runBounds.Left, 0);
        }

        runBounds.Offset(baselineOrigin.X, 0);

        // ��ֵ��ÿ���ַ��ĳߴ�
        var glyphRunBoundsIndex = 0;
        // Ϊʲô�� 0 ��ʼ�������� argument.CurrentIndex ��ʼ��ԭ������ runList �����Ѿ�ʹ�� Slice �ü���
        for (var i = 0; i < runList.Count; i++)
        {
            CharData charData = runList[i];
            if (charData.Size == null)
            {
                SKRect glyphRunBound = glyphRunBounds[glyphRunBoundsIndex];

                argument.CharDataLayoutInfoSetter.SetCharDataInfo(charData, new TextSize(glyphRunBound.Width, glyphRunBound.Height), baselineY);
            }

            // ��� CharData ���ַ���һһ��Ӧ�����⣬����һ�� CharData ��Ӧ����ַ�
            glyphRunBoundsIndex += charData.CharObject.ToText().Length;
            // Ԥ�ڲ�����ֳ��������
            if (glyphRunBoundsIndex >= glyphRunBounds.Length)
            {
                if (i == runList.Count - 1 && glyphRunBoundsIndex == glyphRunBounds.Length)
                {
                    // �����һ�������һ��Ԥ������ȵ�
                    // ���������֧�Ƿ���Ԥ�ڵġ��ոպ����һ�� CharData ��Ӧ���ַ��պ������һ���ַ�
                    break;
                }

                if (updateLayoutContext.IsInDebugMode)
                {
                    throw new TextEditorInnerDebugException(Message);
                }
                else
                {
                    updateLayoutContext. Logger.LogWarning(Message);
                    // ���ܼ���ѭ������������Խ��
                    break;
                }
            }
        }
    }

    private const string Message = "���ֹ����з��� CharData �� Text ������ƥ�䣬Ԥ���ǿ����ʵ�ֵ�����";
    readonly record struct TextGlyphInfo(ushort GlyphIndex, int GlyphCluster, double GlyphAdvance, (float OffsetX, float OffsetY) GlyphOffset = default);

   
}