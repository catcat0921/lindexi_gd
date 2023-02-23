﻿using LightTextEditorPlus.Core.Document;
using System;
using System.Collections.Generic;
using LightTextEditorPlus.Core.Carets;

namespace LightTextEditorPlus.Core;

public partial class TextEditorCore
{
    /// <summary>
    /// 追加一段文本，追加的文本按照段末的样式
    /// </summary>
    public void AppendText(string text)
    {
        DocumentManager.AppendText(text);
    }

    /// <summary>
    /// 在当前的文本上编辑且替换。文本没有选择时，将在当前光标后面加入文本。文本有选择时，替换选择内容为输入内容
    /// </summary>
    /// <param name="text"></param>
    public void EditAndReplace(string text)
    {
        TextEditorCore textEditor = this;
        DocumentManager documentManager = textEditor.DocumentManager;
        // 判断光标是否在文档末尾，且没有选择内容
        var currentSelection = CaretManager.CurrentSelection;
        var caretOffset = CaretManager.CurrentCaretOffset;
        if (currentSelection.IsEmpty && caretOffset.Offset == documentManager.CharCount)
        {
            // 在末尾，调用追加，性能更好
            documentManager.AppendText(text);
        }
        else
        {
            var textRun = new TextRun(text);
            documentManager.EditAndReplaceRun(currentSelection, textRun);
        }
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
        var paragraphDataList = (List<ParagraphData>) DocumentManager.DocumentRunEditProvider.ParagraphManager.GetParagraphList();
        paragraphDataList.Clear();
    }

    /// <summary>
    /// 退格删除，如果没有选择，则删除光标前一个字符。如果有选择，则删除选择内容
    /// </summary>
    public void Backspace()
    {
        DocumentManager.Backspace();
    }

    /// <summary>
    /// 删除给定范围内的文本
    /// </summary>
    /// <param name="selection"></param>
    public void Delete(Selection selection)
    {
        if (selection.IsEmpty)
        {
            return;
        }

        // 删除范围内的文本，等价于将范围内的文本替换为空
        DocumentManager.EditAndReplaceRun(selection, null);
    }

    [Obsolete("只是用来告诉你，应该调用" + nameof(Delete) + "删除文本", true)]
    public void Remove()
    {
    }
}