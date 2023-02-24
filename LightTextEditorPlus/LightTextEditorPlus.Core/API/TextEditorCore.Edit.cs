﻿using LightTextEditorPlus.Core.Document;
using System;
using System.Collections.Generic;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Attributes;

namespace LightTextEditorPlus.Core;

public partial class TextEditorCore
{
    /// <summary>
    /// 追加一段文本，追加的文本按照段末的样式
    /// </summary>
    /// 这是对外调用的，非框架内使用
    [TextEditorPublicAPI]
    public void AppendText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        DocumentManager.AppendText(new TextRun(text));
    }

    /// <summary>
    /// 追加一段文本
    /// </summary>
    /// <param name="run"></param>
    /// 这是对外调用的，非框架内使用
    [TextEditorPublicAPI]
    public void AppendRun(IImmutableRun run)
    {
        DocumentManager.AppendText(run);
    }

    /// <summary>
    /// 在当前的文本上编辑且替换。文本没有选择时，将在当前光标后面加入文本。文本有选择时，替换选择内容为输入内容
    /// </summary>
    /// <param name="text"></param>
    /// 这是对外调用的，非框架内使用
    [TextEditorPublicAPI]
    public void EditAndReplace(string text)
    {
        TextEditorCore textEditor = this;
        DocumentManager documentManager = textEditor.DocumentManager;
        // 判断光标是否在文档末尾，且没有选择内容
        var currentSelection = CaretManager.CurrentSelection;
        var caretOffset = CaretManager.CurrentCaretOffset;
        var isEmptyText = string.IsNullOrEmpty(text);
        if (currentSelection.IsEmpty && caretOffset.Offset == documentManager.CharCount)
        {
            if (!isEmptyText)
            {
                // 在末尾，调用追加，性能更好
                documentManager.AppendText(new TextRun(text));
            }
        }
        else
        {
            if (isEmptyText)
            {
                documentManager.EditAndReplaceRun(currentSelection, null);
            }
            else
            {
                var textRun = new TextRun(text);
                documentManager.EditAndReplaceRun(currentSelection, textRun);
            }
        }
    }

    /// <summary>
    /// 编辑和替换文本
    /// </summary>
    /// <param name="selection">为空将使用当前选择内容，当前无选择则在光标之后插入</param>
    /// <param name="run"></param>
    /// 这是对外调用的，非框架内使用
    [TextEditorPublicAPI]
    public void EditAndReplaceRun(IImmutableRun? run, Selection? selection = null)
    {
        DocumentManager.EditAndReplaceRun(selection ?? CaretManager.CurrentSelection, run);
    }

    /// <summary>
    /// 添加文本
    /// </summary>
    [Obsolete("请使用" + nameof(EditAndReplace) + "代替。此方法只是用来告诉你正确的用法是调用" + nameof(EditAndReplace) + "方法", true)]
    public void AddText()
    {
    }

    /// <summary>
    /// 在当前光标后面加入纯文本
    /// </summary>
    /// <param name="text"></param>
    [Obsolete("请使用" + nameof(EditAndReplace) + "代替。此方法只是用来告诉你正确的用法是调用" + nameof(EditAndReplace) + "方法", true)]
    public void InsertTextAfterCurrentCaretOffset(string text) =>
        EditAndReplace(text);

    /// <summary>
    /// 清空文本，现在仅调试下使用
    /// </summary>
    [Obsolete("仅调试使用")]
    public void Clear()
    {
        // 调试代码，就这么强行转换
        var paragraphDataList =
            (List<ParagraphData>)DocumentManager.DocumentRunEditProvider.ParagraphManager.GetParagraphList();
        paragraphDataList.Clear();
    }

    /// <summary>
    /// 退格删除，如果没有选择，则删除光标前一个字符。如果有选择，则删除选择内容
    /// </summary>
    /// 这是对外调用的，非框架内使用
    [TextEditorPublicAPI]
    public void Backspace()
    {
        DocumentManager.Backspace();
    }

    /// <summary>
    /// 删除文本 Delete 删除光标后一个字符
    /// </summary>
    /// 这是对外调用的，非框架内使用
    [TextEditorPublicAPI]
    public void Delete()
    {
        DocumentManager.Delete();
    }

    /// <summary>
    /// 删除文本，删除给定范围内的文本
    /// </summary>
    /// 这是对外调用的，非框架内使用
    [TextEditorPublicAPI]
    public void Remove(in Selection selection)
    {
        DocumentManager.Remove(selection);
    }
}