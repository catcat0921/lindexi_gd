﻿using System;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;

namespace LightTextEditorPlus.Core.Rendering;

/// <summary>
/// 光标下的渲染信息
/// </summary>
public readonly struct CaretRenderInfo
{
    internal CaretRenderInfo(TextEditorCore textEditor,int lineIndex, int hitLineOffset, ParagraphCaretOffset hitOffset, CaretOffset caretOffset, LineLayoutData lineLayoutData)
    {
        TextEditor = textEditor;
        LineIndex = lineIndex;
        HitLineOffset = hitLineOffset;
        HitOffset = hitOffset;
        CaretOffset = caretOffset;
        LineLayoutData = lineLayoutData;
    }

    internal TextEditorCore TextEditor { get; }

    /// <summary>
    /// 行在段落里的序号
    /// </summary>
    public int LineIndex { get; }

    /// <summary>
    /// 段落在文档里属于第几段
    /// </summary>
    public int ParagraphIndex => ParagraphData.Index;

    /// <summary>
    /// 这一行的字符列表
    /// </summary>
    public ReadOnlyListSpan<CharData> LineCharDataList => LineLayoutData.GetCharList();

    /// <summary>
    /// 行的范围
    /// </summary>
    public Rect LineBounds => LineLayoutData.GetLineBounds();

    /// <summary>
    /// 命中到行的哪个字符
    /// </summary>
    public int HitLineOffset { get; }

    /// <summary>
    /// 是否命中到行的起点
    /// </summary>
    public bool IsLineStart => CaretOffset.IsAtLineStart;

    /// <summary>
    /// 命中的字符。如果是空段，那将没有命中哪个字符。对于在行首或段首的，那将命中在光标前面的字符
    /// </summary>
    public CharData? CharData
    {
        get
        {
            if (LineLayoutData.CharCount == 0)
            {
                return null;
            }
            else
            {
                return LineLayoutData.GetCharList()[HitLineOffset];
            }
        }
    }

    /// <summary>
    /// 获取在光标之后的字符。如果是空段，那就空
    /// </summary>
    /// <returns></returns>
    public CharData? GetCharDataAfterCaretOffset()
    {
        if (CaretOffset.IsAtLineStart)
        {
            return CharData;
        }

        var hitCharOffset = new ParagraphCharOffset(HitOffset.Offset + 1);
        var hitCharData = ParagraphData.GetCharData(hitCharOffset);
        return hitCharData;
    }

    /// <summary>
    /// 是否一个空段
    /// </summary>
    public bool IsEmptyParagraph => ParagraphData.CharCount == 0;

    internal ParagraphData ParagraphData => LineLayoutData.CurrentParagraph;
    internal ParagraphCaretOffset HitOffset { get; }

    internal LineLayoutData LineLayoutData { get; }

    /// <summary>
    /// 光标偏移量
    /// </summary>
    public CaretOffset CaretOffset { get; }

    /// <summary>
    /// 获取光标的范围
    /// </summary>
    /// <param name="caretWidth"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="NotImplementedException"></exception>
    public Rect GetCaretBounds(double caretWidth)
    {
        var charData = CharData;
        var startPoint = charData?.GetStartPoint() ?? LineBounds.LeftTop;
        Size size;
        if (charData?.Size is not null)
        {
            size = charData.Size.Value;
        }
        else
        {
            size = LineBounds.Size;
        }

        switch (TextEditor.ArrangingType)
        {
            case ArrangingType.Horizontal:
                var (x, y) = startPoint;
                // 可以获取到起始点，那肯定存在尺寸
                if (IsLineStart)
                {
                    // 如果命中到行的开始，那就是首个字符之前，不能加上字符的尺寸
                }
                else
                {
                    x += size.Width;
                }
                var width = caretWidth;
                var height =
                    LineSpacingCalculator.CalculateLineHeightWithLineSpacing(TextEditor,
                        TextEditor.DocumentManager.CurrentCaretRunProperty, 1);
                y += size.Height - height;
                var rectangle = new Rect(x, y, width, height);
                return rectangle;
                break;
            case ArrangingType.Vertical:
                break;
            case ArrangingType.Mongolian:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        throw new NotImplementedException();
    }
}