﻿using System;
using System.Diagnostics;

using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Layout.WordDividers;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 水平方向布局的提供器
/// </summary>
class HorizontalArrangingLayoutProvider : ArrangingLayoutProvider, IInternalCharDataSizeMeasurer
{
    public HorizontalArrangingLayoutProvider(LayoutManager layoutManager) : base(layoutManager)
    {
        _divider = new DefaultWordDivider(layoutManager.TextEditor, this);
    }

    public override ArrangingType ArrangingType => ArrangingType.Horizontal;

    #region 更新非脏的段落和行

    protected override ParagraphLayoutResult UpdateParagraphStartPoint(in ParagraphLayoutArgument argument)
    {
        var paragraph = argument.ParagraphData;

        // 先设置是脏的，然后再更新，这样即可更新段落版本号
        paragraph.SetDirty();

        double paragraphBefore = argument.IsFirstParagraph ? 0 /*首段不加段前距离*/  : paragraph.ParagraphProperty.ParagraphBefore;
        var currentStartPoint = argument.CurrentStartPoint with
        {
            Y = argument.CurrentStartPoint.Y + paragraphBefore
        };
        paragraph.ParagraphLayoutData.StartPoint = currentStartPoint;

        var layoutArgument = argument with
        {
            CurrentStartPoint = currentStartPoint
        };

        var nextLineStartPoint = UpdateParagraphLineLayoutDataStartPoint(layoutArgument);
        // 设置当前段落已经布局完成
        paragraph.SetFinishLayout();

        return new ParagraphLayoutResult(nextLineStartPoint);
    }

    /// <summary>
    /// 更新段落里面的所有行的起始点
    /// </summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    private static Point UpdateParagraphLineLayoutDataStartPoint(in ParagraphLayoutArgument argument)
    {
        var currentStartPoint = argument.CurrentStartPoint;
        var paragraph = argument.ParagraphData;

        foreach (LineLayoutData lineVisualData in paragraph.LineLayoutDataList)
        {
            UpdateLineLayoutDataStartPoint(lineVisualData, currentStartPoint);

            currentStartPoint = GetNextLineStartPoint(currentStartPoint, lineVisualData);
        }

        return currentStartPoint;
    }

    /// <summary>
    /// 重新更新每一行的起始点。例如现在第一段的文本加了一行，那第二段的所有文本都需要更新每一行的起始点，而不需要重新布局第二段
    /// </summary>
    /// <param name="lineLayoutData"></param>
    /// <param name="startPoint"></param>
    static void UpdateLineLayoutDataStartPoint(LineLayoutData lineLayoutData, Point startPoint)
    {
        // 更新包括两个方面：
        // 1. 此行的起点
        // 2. 此行内的所有字符的起点坐标
        var currentStartPoint = startPoint;
        lineLayoutData.CharStartPoint = currentStartPoint;
        // 更新行内所有字符的坐标
        var lineTop = currentStartPoint.Y;
        var list = lineLayoutData.GetCharList();
        var lineHeight = lineLayoutData.LineCharSize.Height;
        for (var index = 0; index < list.Count; index++)
        {
            var charData = list[index];

            Debug.Assert(charData.CharLayoutData is not null);

            var charHeight = charData.Size!.Value.Height;

            // 保持 X 不变
            var xOffset = charData.CharLayoutData.StartPoint.X;
            // 计算 Y 方向的值
            var yOffset = CalculateCharDataTop(charHeight, lineHeight, lineTop);
            charData.CharLayoutData!.StartPoint = new Point(xOffset, yOffset);
            charData.CharLayoutData.UpdateVersion();
        }

        lineLayoutData.UpdateVersion();
    }

    /// <summary>
    /// 更新字符的坐标
    /// </summary>
    /// <param name="charHeight">charData.LineCharSize.Height</param>
    /// <param name="lineHeight">当前字符所在行的行高，包括行距在内</param>
    /// <param name="lineTop">文档布局给到行的距离文本框开头的距离</param>
    /// 只是封装算法而已
    private static double CalculateCharDataTop(double charHeight, double lineHeight, double lineTop)
    {
        // 以下的代码是简单版本的 AdaptBaseLine 方法。而正确的做法是：
        // 1. 求出最大字符的 Baseline 出来
        // 2. 求出当前字符的 Baseline 出来
        // 3. 求出 最大字符的 Baseline 和 当前字符的 Baseline 的差，此结果叠加 lineTop 就是偏移量了
        // 这里只使用简单的方法，求尺寸和行高的差，让字符底部对齐

        // 先计算出字符相对行的距离
        var charMarginTop = lineHeight - charHeight;
        // 再加上文档里面给这一行的 lineTop 距离，即可算出字符相对于文本框的距离
        var yOffset = charMarginTop + lineTop;
        return yOffset;
    }

    #endregion

    #region LayoutParagraphCore

    /// <summary>
    /// 布局段落的核心逻辑
    /// </summary>
    /// <param name="argument"></param>
    /// <param name="startParagraphOffset">开始布局的字符，刚好是一行的开始</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <remarks>
    /// 逻辑上是：
    /// 布局按照： 文本-段落-行-Run-字符
    /// 布局整个文本
    /// 布局文本的每个段落 <see cref="LayoutParagraphCore"/>
    /// 段落里面，需要对每一行进行布局 <see cref="LayoutWholeLine"/>
    /// 每一行里面，需要对每个 Char 字符进行布局 <see cref="LayoutSingleCharInLine"/>
    /// 每个字符需要调用平台的测量 <see cref="ArrangingLayoutProvider.MeasureCharInfo"/>
    /// </remarks>
    protected override ParagraphLayoutResult LayoutParagraphCore(in ParagraphLayoutArgument argument,
        in ParagraphCharOffset startParagraphOffset)
    {
        var paragraph = argument.ParagraphData;
        // 先更新非脏的行的坐标
        // 布局左上角坐标
        Point currentStartPoint;
        // 根据是否存在缓存行决定是否需要计算段前距离
        if (paragraph.LineLayoutDataList.Count == 0)
        {
            // 一行都没有的情况下，需要计算段前距离
            double paragraphBefore = argument.IsFirstParagraph ? 0 /*首段不加段前距离*/  : paragraph.ParagraphProperty.ParagraphBefore;

            currentStartPoint = argument.CurrentStartPoint with
            {
                Y = argument.CurrentStartPoint.Y + paragraphBefore
            };
        }
        else
        {
            // 有缓存的行，证明段落属性没有更改，不需要计算段前距离
            // 只需要更新缓存的行
            currentStartPoint = UpdateParagraphLineLayoutDataStartPoint(argument);
        }

        // 更新段左边距
        currentStartPoint = currentStartPoint with
        {
            X = paragraph.ParagraphProperty.LeftIndentation
        };

        // 如果是空段的话，那就进行空段布局，否则布局段落里面每一行
        bool isEmptyParagraph = paragraph.CharCount == 0;
        if (isEmptyParagraph)
        {
            // 空段布局
            currentStartPoint = LayoutEmptyParagraph(argument, currentStartPoint);
        }
        else
        {
            // 布局段落里面每一行
            currentStartPoint = LayoutParagraphLines(argument, startParagraphOffset, currentStartPoint);
        }

        //// 考虑行复用，例如刚好添加的内容是一行。或者在一行内做文本替换等
        //// 这个没有啥优先级。测试了 SublimeText 和 NotePad 工具，都没有做此复用，预计有坑
        
        // 下一段的距离需要加上段后距离
        double paragraphAfter =
            argument.IsLastParagraph ? 0 /*最后一段不加段后距离*/ : paragraph.ParagraphProperty.ParagraphAfter;
        var nextLineStartPoint = currentStartPoint with
        {
            Y = currentStartPoint.Y + paragraphAfter,
        };

        paragraph.ParagraphLayoutData.StartPoint = argument.ParagraphData.LineLayoutDataList[0].CharStartPoint;
        paragraph.ParagraphLayoutData.Size = BuildParagraphSize(argument);

        // 设置当前段落已经布局完成
        paragraph.SetFinishLayout();
       
        return new ParagraphLayoutResult(nextLineStartPoint);
    }

    /// <summary>
    /// 布局空段
    /// </summary>
    /// <param name="argument"></param>
    /// <param name="currentStartPoint"></param>
    /// <returns></returns>
    private Point LayoutEmptyParagraph(in ParagraphLayoutArgument argument, Point currentStartPoint)
    {
        var paragraph = argument.ParagraphData;
        // 如果是空段的话，如一段只是一个 \n 而已，那就需要执行空段布局逻辑
        Debug.Assert(paragraph.LineLayoutDataList.Count == 0, "空段布局时一定是一行都不存在");
        var emptyParagraphLineHeightMeasureResult = MeasureEmptyParagraphLineHeight(
            new EmptyParagraphLineHeightMeasureArgument(paragraph.ParagraphProperty, argument.ParagraphIndex));
        double lineHeight = emptyParagraphLineHeightMeasureResult.LineHeight;

        // 加上空行
        var lineLayoutData = new LineLayoutData(paragraph)
        {
            CharStartParagraphIndex = 0,
            CharEndParagraphIndex = 0,
            CharStartPoint = currentStartPoint,
            LineCharSize = new Size(0, lineHeight)
        };
        paragraph.LineLayoutDataList.Add(lineLayoutData);

        currentStartPoint = currentStartPoint with
        {
            Y = currentStartPoint.Y + lineHeight
        };
        return currentStartPoint;
    }

    /// <summary>
    /// 布局段落里面的每一行
    /// </summary>
    /// <param name="argument"></param>
    /// <param name="startParagraphOffset"></param>
    /// <param name="currentStartPoint"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="TextEditorDebugException"></exception>
    /// <exception cref="TextEditorInnerException"></exception>
    private Point LayoutParagraphLines(in ParagraphLayoutArgument argument, in ParagraphCharOffset startParagraphOffset,
        Point currentStartPoint)
    {
        ParagraphData paragraph= argument.ParagraphData;
        // 获取最大宽度信息
        double lineMaxWidth = TextEditor.SizeToContent switch
        {
            SizeToContent.Manual => TextEditor.DocumentManager.DocumentWidth,
            SizeToContent.Width => double.PositiveInfinity,
            SizeToContent.Height => TextEditor.DocumentManager.DocumentWidth,
            SizeToContent.WidthAndHeight => double.PositiveInfinity,
            _ => throw new ArgumentOutOfRangeException()
        };

        var wholeRunLineLayouter = TextEditor.PlatformProvider.GetWholeRunLineLayouter();
        for (var i = startParagraphOffset.Offset; i < paragraph.CharCount;)
        {
            // 开始行布局
            // 第一个 Run 就是行的开始
            ReadOnlyListSpan<CharData> charDataList = paragraph.ToReadOnlyListSpan(new ParagraphCharOffset(i));

            if (TextEditor.IsInDebugMode)
            {
                // 这是调试代码，判断是否在布局过程，漏掉某个字符
                foreach (var charData in charDataList)
                {
                    charData.IsSetStartPointInDebugMode = false;
                }
            }

            WholeLineLayoutResult result;
            var wholeRunLineLayoutArgument = new WholeLineLayoutArgument(argument.ParagraphIndex,
                paragraph.LineLayoutDataList.Count, paragraph.ParagraphProperty, charDataList,
                lineMaxWidth, currentStartPoint);
            if (wholeRunLineLayouter != null)
            {
                result = wholeRunLineLayouter.LayoutWholeLine(wholeRunLineLayoutArgument);
            }
            else
            {
                // 继续往下执行，如果没有注入自定义的行布局层的话
                result = LayoutWholeLine(wholeRunLineLayoutArgument);
            }

            // 当前的行布局信息
            var currentLineLayoutData = new LineLayoutData(paragraph)
            {
                CharStartParagraphIndex = i,
                CharEndParagraphIndex = i + result.CharCount,
                LineCharSize = result.Size,
                CharStartPoint = currentStartPoint,
            };
            // 更新字符信息
            Debug.Assert(result.CharCount <= charDataList.Count, "所获取的行的字符数量不能超过可提供布局的行的字符数量");
            for (var index = 0; index < result.CharCount; index++)
            {
                var charData = charDataList[index];

                if (TextEditor.IsInDebugMode)
                {
                    if (charData.IsSetStartPointInDebugMode == false)
                    {
                        throw new TextEditorDebugException($"存在某个字符没有在布局时设置坐标",
                            (charData, currentLineLayoutData, i + index));
                    }
                }

                charData.CharLayoutData!.CharIndex = new ParagraphCharOffset(i + index);
                charData.CharLayoutData.CurrentLine = currentLineLayoutData;
                charData.CharLayoutData.UpdateVersion();
            }

            paragraph.LineLayoutDataList.Add(currentLineLayoutData);

            i += result.CharCount;

            if (result.CharCount == 0)
            {
                // todo 理论上不可能，表示行布局出错了
                // 支持文本宽度小于一个字符的宽度的布局
                throw new TextEditorInnerException($"某一行在布局时，只采用了零个字符");
            }

            currentStartPoint = GetNextLineStartPoint(currentStartPoint, currentLineLayoutData);
        }

        return currentStartPoint;
    }

    #endregion

    #region LayoutWholeLine 布局一行的字符

    /// <summary>
    /// 布局一行的字符
    /// </summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    private WholeLineLayoutResult LayoutWholeLine(in WholeLineLayoutArgument argument)
    {
        var charDataList = argument.CharDataList;
        var currentStartPoint = argument.CurrentStartPoint;

        if (charDataList.Count == 0)
        {
            // 理论上不会进入这里，如果没有任何的字符，那就不需要进行行布局
            return new WholeLineLayoutResult(Size.Zero, 0);
        }

        var layoutResult = LayoutWholeLineChars(argument);
        int wholeCharCount = layoutResult.WholeCharCount;
        Size currentSize = layoutResult.CurrentLineCharSize;

        if (wholeCharCount == 0)
        {
            // 这一行一个字符都不能拿
            Debug.Assert(currentSize == Size.Zero);
            return new WholeLineLayoutResult(currentSize, wholeCharCount);
        }

        // 遍历一次，用来取出其中 FontSize 最大的字符，此字符的对应字符属性就是所期望的参与后续计算的字符属性
        // 遍历这一行的所有字符，找到最大字符的字符属性
        var charDataTakeList = charDataList.Slice(0, wholeCharCount);
        IReadOnlyRunProperty maxFontSizeCharRunProperty = GetMaxFontSizeCharRunProperty(charDataTakeList);

        // 处理行距
        var lineSpacingCalculateArgument = new LineSpacingCalculateArgument(argument.ParagraphIndex, argument.LineIndex, argument.ParagraphProperty, maxFontSizeCharRunProperty);
        LineSpacingCalculateResult lineSpacingCalculateResult = CalculateLineSpacing(lineSpacingCalculateArgument);
        double lineHeight = lineSpacingCalculateResult.TotalLineHeight;
        if (lineSpacingCalculateResult.ShouldUseCharLineHeight)
        {
            lineHeight = currentSize.Height;
        }
        
        var fixLineSpacing = lineHeight - currentSize.Height; // 行距值，现在仅调试用途
        GC.KeepAlive(fixLineSpacing);

        var lineTop = currentStartPoint.Y;
        var currentX = currentStartPoint.X;
        foreach (CharData charData in charDataTakeList)
        {
            // 计算和更新每个字符的相对文本框的坐标
            Debug.Assert(charData.Size != null, "charData.LineCharSize != null");
            var charDataSize = charData.Size!.Value;

            var yOffset = CalculateCharDataTop(charDataSize.Height, lineHeight,
                lineTop); // (lineHeight - charDataSize.Height) + lineTop;
            charData.SetStartPoint(new Point(currentX, yOffset));

            currentX += charDataSize.Width;
        }

        // 行的尺寸
        var lineSize = new Size(currentSize.Width, lineHeight);

        return new WholeLineLayoutResult(lineSize, wholeCharCount);
    }

    /// <summary>
    /// 布局一行的结果
    /// </summary>
    /// <param name="CurrentLineCharSize"></param>
    /// <param name="WholeCharCount"></param>
    readonly record struct WholeLineCharsLayoutResult(Size CurrentLineCharSize, int WholeCharCount);

    /// <summary>
    /// 布局一行里面有哪些字符
    /// </summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    private WholeLineCharsLayoutResult LayoutWholeLineChars(in WholeLineLayoutArgument argument)
    {
        var (paragraphIndex, lineIndex, paragraphProperty, charDataList, lineMaxWidth, currentStartPoint) = argument;

#if DEBUG
        // 调试下显示当前这一行的文本，方便了解当前在哪一行
        string currentLineText = argument.DebugText;
        GC.KeepAlive(currentLineText);
#endif

        var singleRunLineLayouter = TextEditor.PlatformProvider.GetSingleRunLineLayouter();

        // RunLineMeasurer
        // 行还剩余的空闲宽度
        double lineRemainingWidth = lineMaxWidth;

        // 当前相对于 charDataList 的当前序号
        int currentIndex = 0;
        // 当前的字符布局尺寸
        var currentSize = Size.Zero;

        while (currentIndex < charDataList.Count)
        {
            // 一行里面需要逐个字符进行布局
            var arguments = new SingleCharInLineLayoutArgument(charDataList, currentIndex, lineRemainingWidth,
                paragraphProperty);

            SingleCharInLineLayoutResult result;
            if (singleRunLineLayouter is not null)
            {
                result = singleRunLineLayouter.LayoutSingleCharInLine(arguments);
            }
            else
            {
                result = LayoutSingleCharInLine(arguments);
            }

            if (result.CanTake)
            {
                currentSize = currentSize.HorizontalUnion(result.TotalSize);

                currentIndex += result.TakeCount;

                // 计算此行剩余的宽度
                lineRemainingWidth -= result.TotalSize.Width;
            }

            if (result.ShouldBreakLine)
            {
                // 换行，这一行布局完成
                break;
            }
        }

        // todo 这里可以支持两端对齐
        // 整行的字符布局

        // 整个行所使用的字符数量
        var wholeCharCount = currentIndex;
        return new WholeLineCharsLayoutResult(currentSize, wholeCharCount);
    }

    /// <summary>
    /// 获取给定行的最大字号的字符属性。这个属性就是这一行的代表属性
    /// </summary>
    /// <param name="charDataList"></param>
    /// <returns></returns>
    static IReadOnlyRunProperty GetMaxFontSizeCharRunProperty(in ReadOnlyListSpan<CharData> charDataList)
    {
        var firstCharData = charDataList[0];
        IReadOnlyRunProperty maxFontSizeCharRunProperty = firstCharData.RunProperty;
        // 遍历这一行的所有字符，找到最大字符的字符属性
        for (var i = 1; i < charDataList.Count; i++)
        {
            var charData = charDataList[i];
            if (charData.RunProperty.FontSize > maxFontSizeCharRunProperty.FontSize)
            {
                maxFontSizeCharRunProperty = charData.RunProperty;
            }
        }

        return maxFontSizeCharRunProperty;
    }

    #endregion  

    /// <summary>
    /// 布局一行里面的单个字符
    /// </summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    private SingleCharInLineLayoutResult LayoutSingleCharInLine(in SingleCharInLineLayoutArgument argument)
    {
        // LayoutRule 布局规则
        // 可选无规则-直接字符布局，预计没有人使用
        // 调用分词规则-支持注入分词规则

        // 使用分词规则进行布局
        bool useWordDividerLayout = true;

        if (useWordDividerLayout)
        {
            return _divider.LayoutSingleCharInLine(argument);
        }
        else
        {
            var charData = argument.CurrentCharData;

            Size size = GetCharSize(charData);

            // 单个字符直接布局，无视语言文化。快，但是诡异
            if (argument.LineRemainingWidth > size.Width)
            {
                return new SingleCharInLineLayoutResult(takeCount: 1, size);
            }
            else
            {
                // 如果尺寸不足，也就是一个都拿不到
                return new SingleCharInLineLayoutResult(takeCount: 0, default);
            }
        }
    }

    /// <summary>
    /// 分词器
    /// </summary>
    private readonly DefaultWordDivider _divider;

    #region 辅助方法

    [DebuggerStepThrough] // 别跳太多层
    Size IInternalCharDataSizeMeasurer.GetCharSize(CharData charData) => GetCharSize(charData);

    /// <summary>
    /// 获取给定字符的尺寸
    /// </summary>
    /// <param name="charData"></param>
    /// <returns></returns>
    private Size GetCharSize(CharData charData)
    {
        // 字符可能自己缓存有了自己的尺寸，如果有缓存，那是可以重复使用
        var cacheSize = charData.Size;

        Size size;
        if (cacheSize == null)
        {
            var charInfo = new CharInfo(charData.CharObject, charData.RunProperty);
            CharInfoMeasureResult charInfoMeasureResult;
            var charInfoMeasurer = TextEditor.PlatformProvider.GetCharInfoMeasurer();
            if (charInfoMeasurer != null)
            {
                charInfoMeasureResult = charInfoMeasurer.MeasureCharInfo(charInfo);
            }
            else
            {
                charInfoMeasureResult = MeasureCharInfo(charInfo);
            }

            size = charInfoMeasureResult.Bounds.Size;
        }
        else
        {
            size = cacheSize.Value;
        }

        charData.Size ??= size;
        return size;
    }

    /// <summary>
    /// 获取下一行的起始点
    /// </summary>
    /// 对于横排布局来说，只是更新 Y 值即可
    /// <param name="currentStartPoint"></param>
    /// <param name="currentLineLayoutData"></param>
    /// <returns></returns>
    private static Point GetNextLineStartPoint(Point currentStartPoint, LineLayoutData currentLineLayoutData)
    {
        currentStartPoint = new Point(currentStartPoint.X, currentStartPoint.Y + currentLineLayoutData.LineCharSize.Height);
        return currentStartPoint;
    }

    private static Size BuildParagraphSize(in ParagraphLayoutArgument argument)
    {
        var paragraphSize = new Size(0, 0);
        foreach (var lineVisualData in argument.ParagraphData.LineLayoutDataList)
        {
            var width = Math.Max(paragraphSize.Width, lineVisualData.LineCharSize.Width);
            var height = paragraphSize.Height + lineVisualData.LineCharSize.Height;

            paragraphSize = new Size(width, height);
        }

        return paragraphSize;
    }

    #endregion

    protected override Point GetNextParagraphLineStartPoint(ParagraphData paragraphData)
    {
        const double x = 0;
        var layoutData = paragraphData.ParagraphLayoutData;
        var y = layoutData.StartPoint.Y + layoutData.Size.Height;
        return new Point(x, y);

        // 以下是通过最后一行的值进行计算的。不足的是需要判断空段，因此不如使用段落偏移加上段落高度进行计算
        //if (paragraphData.LineVisualDataList.Count == 0)
        //{
        //    // 这一段没有包含任何的行。这一段里面可能没有任何文本，只是一个 \r\n 的空段
        //    Debug.Assert(paragraphData.CharCount == 0,"只有空段才没有包含行");

        //    const double x = 0;
        //    var layoutData = paragraphData.ParagraphLayoutData;
        //    var y = layoutData.CharStartPoint.Y + layoutData.LineCharSize.Height;
        //    return new Point(x, y);
        //}
        //else
        //{
        //    var lineVisualData = paragraphData.LineVisualDataList.Last();
        //    const double x = 0;
        //    var y = lineVisualData.CharStartPoint.Y + lineVisualData.LineCharSize.Height;
        //    return new Point(x, y);
        //}
    }
}