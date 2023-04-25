﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Primitive;

using TextEditor = LightTextEditorPlus.Core.TextEditorCore;

namespace LightTextEditorPlus.Core.Rendering;

/// <summary>
/// 提供用于渲染时用到的信息
/// </summary>
public class RenderInfoProvider
{
    internal RenderInfoProvider(TextEditor textEditor)
    {
        TextEditor = textEditor;
    }

    internal TextEditor TextEditor { get; }

    /// <summary>
    /// 当前渲染信息是否脏的。如果是脏的就不能使用
    /// </summary>
    public bool IsDirty { internal set; get; }

    /// <summary>
    /// 获取选择对应的范围。一般是一行一个 Rect 对象
    /// </summary>
    /// <param name="selection"></param>
    /// <returns></returns>
    public IList<Rect> GetSelectionBoundsList(in Selection selection)
    {
        if (selection.IsEmpty)
        {
            return new Rect[0];
        }

        var result = new List<Rect>();
        LineLayoutData? currentLineLayoutData = null;
        Rect currentBounds = default;
        CharData? lastCharData = null;
        foreach (var charData in TextEditor.DocumentManager.GetCharDataRange(selection))
        {
            if (ReferenceEquals(charData.CharObject, LineBreakCharObject.Instance))
            {
                // 这是换段了
                continue;
            }

            var sameLine = currentLineLayoutData is not null && ReferenceEquals(charData.CharLayoutData?.CurrentLine, currentLineLayoutData);

            if (sameLine)
            {
                // 在相同的一行里面，先不加入计算
            }
            else
            {
                if (currentLineLayoutData is not null)
                {
                    var lastBounds = lastCharData!.GetBounds();
                    result.Add(currentBounds.Union(lastBounds));
                }

                currentLineLayoutData = charData.CharLayoutData?.CurrentLine;
                currentBounds = charData.GetBounds();
            }

            lastCharData = charData;
        }

        if (lastCharData is not null)
        {
            var lastBounds = lastCharData!.GetBounds();
            result.Add(currentBounds.Union(lastBounds));
        }

        return result;
    }

    /// <summary>
    /// 获取给定光标坐标的光标渲染信息
    /// </summary>
    /// <param name="caretOffset"></param>
    /// <returns></returns>
    /// <exception cref="HitCaretOffsetOutOfRangeException"></exception>
    /// <exception cref="TextEditorInnerException"></exception>
    public CaretRenderInfo GetCaretRenderInfo(CaretOffset caretOffset)
    {
        var textEditor = TextEditor;
        var documentCharCount = textEditor.DocumentManager.CharCount;
        if (caretOffset.Offset > documentCharCount)
        {
            // 超过文档的字符数量
            throw new HitCaretOffsetOutOfRangeException(textEditor, caretOffset, documentCharCount,
                nameof(caretOffset));
        }

        var paragraphManager = textEditor.DocumentManager.ParagraphManager;
        var hitParagraphDataResult = paragraphManager.GetHitParagraphData(caretOffset);
        var paragraphData = hitParagraphDataResult.ParagraphData;
        ParagraphCaretOffset hitParagraphCaretOffset = hitParagraphDataResult.HitOffset;
        // 是否段首，如果是段首，那一定就是行首
        var isParagraphStart = hitParagraphCaretOffset.Offset == 0;
        bool isAtLineStart = caretOffset.IsAtLineStart || isParagraphStart;

        if (isParagraphStart)
        {
            // 段首，短路代码，特殊执行，不需要走循环
            var lineIndex = 0;
            var lineLayoutData = paragraphData.LineLayoutDataList[0];
            // 段落起始下，取段落首个字符，如可能取到 \n 将拿不到 CharData 对象
            // 如“|abc”的情况，应该取字符 a 作为命中的字符
            var hitLineCharOffset = new LineCharOffset(0);
            var hitLineCaretOffset = new LineCaretOffset(0);
            return new CaretRenderInfo(TextEditor, lineIndex, hitLineCharOffset, hitLineCaretOffset, hitParagraphCaretOffset, caretOffset, lineLayoutData);
        }
        else if (hitParagraphCaretOffset.Offset == paragraphData.CharCount)
        {
            // 短路代码，如果命中到段末。这个逻辑可以快速判断，不需要走循环
            var lineIndex = paragraphData.LineLayoutDataList.Count - 1;
            var lineLayoutData = paragraphData.LineLayoutDataList[lineIndex];

            // 以下代码的 -1 的逻辑是：
            // 段落起始下，取段落首个字符，如可能取到 \n 将拿不到 CharData 对象
            // 非段落起始，取光标前一个字符
            // 如“abc|”的情况，应该取字符 c 作为命中的字符
            var hitCharOffset = hitParagraphCaretOffset.Offset - 1;

            var hitLineCharOffset = new LineCharOffset(hitCharOffset - lineLayoutData.CharStartParagraphIndex);
            var hitLineCaretOffset =
                new LineCaretOffset(hitParagraphCaretOffset.Offset - lineLayoutData.CharStartParagraphIndex);

            return new CaretRenderInfo(TextEditor, lineIndex, hitLineCharOffset, hitLineCaretOffset, hitParagraphCaretOffset, caretOffset, lineLayoutData);
        }

        for (var lineIndex = 0; lineIndex < paragraphData.LineLayoutDataList.Count; lineIndex++)
        {
            var lineLayoutData = paragraphData.LineLayoutDataList[lineIndex];

            if (lineLayoutData.CharEndParagraphIndex == hitParagraphCaretOffset.Offset && !caretOffset.IsAtLineStart)
            {
                // 命中到行末，但是此时光标设置非行首情况
                var hitLineCaretOffset =
                    new LineCaretOffset(hitParagraphCaretOffset.Offset - lineLayoutData.CharStartParagraphIndex);
                Debug.Assert(hitLineCaretOffset.Offset != 0, "不会出现空行命中情况");
                // 取光标前一个字符
                // 如“a|bc”的情况，应该取字符 a 作为命中的字符
                var hitLineCharOffset = new LineCharOffset(Math.Max(0, hitLineCaretOffset.Offset - 1));

                return new CaretRenderInfo(TextEditor, lineIndex, hitLineCharOffset, hitLineCaretOffset, hitParagraphCaretOffset, caretOffset, lineLayoutData);
            }
            else if (lineLayoutData.CharEndParagraphIndex > hitParagraphCaretOffset.Offset)
            {
                var hitLineCaretOffset =
                    new LineCaretOffset(hitParagraphCaretOffset.Offset - lineLayoutData.CharStartParagraphIndex);
                Debug.Assert(hitLineCaretOffset.Offset != 0, "不会出现空行命中情况");
                // 取光标前一个字符
                // 如“a|bc”的情况，应该取字符 a 作为命中的字符
                var hitLineCharOffset = new LineCharOffset(Math.Max(0, hitLineCaretOffset.Offset - 1));

                return new CaretRenderInfo(TextEditor, lineIndex, hitLineCharOffset, hitLineCaretOffset, hitParagraphCaretOffset, caretOffset, lineLayoutData);
            }
        }

        //int hitCharOffset;
        //if (isAtLineStart)
        //{
        //    // 行首情况下，取后一个字符。从光标转换为字符坐标是根据业务决定
        //    hitCharOffset = hitParagraphCaretOffset.Offset;
        //}
        //else
        //{
        //    // 非行首情况下，一律取前一个字符
        //    hitCharOffset = hitParagraphCaretOffset.Offset - 1;
        //}

        //if (hitCharOffset == paragraphData.CharCount)
        //{
        //    // 短路代码，如果命中到段末。这个逻辑可以快速判断，不需要走循环
        //    var lineIndex = paragraphData.LineLayoutDataList.Count - 1;
        //    var lineLayoutData = paragraphData.LineLayoutDataList[lineIndex];

        //    var hitLineCharOffset = new LineCharOffset(hitCharOffset - lineLayoutData.CharStartParagraphIndex);
        //    var hitLineCaretOffset =
        //        new LineCaretOffset(hitParagraphCaretOffset.Offset - lineLayoutData.CharStartParagraphIndex);

        //    return new CaretRenderInfo(TextEditor, lineIndex, hitLineCharOffset, hitLineCaretOffset, hitParagraphCaretOffset, caretOffset, lineLayoutData);
        //}

        //for (var lineIndex = 0; lineIndex < paragraphData.LineLayoutDataList.Count; lineIndex++)
        //{
        //    var lineLayoutData = paragraphData.LineLayoutDataList[lineIndex];

        //    if (lineLayoutData.CharEndParagraphIndex > hitCharOffset)
        //    {
        //        var hitLineCharOffset = new LineCharOffset(hitCharOffset - lineLayoutData.CharStartParagraphIndex);
        //        var hitLineCaretOffset =
        //            new LineCaretOffset(hitParagraphCaretOffset.Offset - lineLayoutData.CharStartParagraphIndex);

        //        return new CaretRenderInfo(TextEditor, lineIndex, hitLineCharOffset, hitLineCaretOffset, hitParagraphCaretOffset, caretOffset, lineLayoutData);
        //    }
        //}

        // 理论上不可能进入此分支
        throw new TextEditorInnerException("无法命中光标对应的字符");
    }

    //public Rect GetCharLayoutInfo(DocumentOffset documentOffset)
    //{
    //}

    /// <summary>
    /// 获取段落的渲染信息
    /// </summary>
    /// <returns></returns>
    public IEnumerable<ParagraphRenderInfo> GetParagraphRenderInfoList()
    {
        var paragraphManager = TextEditor.DocumentManager.ParagraphManager;
        var list = paragraphManager.GetParagraphList();
        for (var index = 0; index < list.Count; index++)
        {
            VerifyNotDirty();
            var paragraphData = list[index];
            yield return new ParagraphRenderInfo(index, paragraphData, this);
        }
    }

    internal void VerifyNotDirty()
    {
        if (IsDirty)
        {
            throw new TextEditorRenderInfoDirtyException(TextEditor);
        }
    }
}