﻿using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Core.Tests;

public static class TextEditorExtension
{
    public static string GetText(this TextEditorCore textEditor) =>
        textEditor.DocumentManager.ParagraphManager.GetText();

    public static string ConvertToString(this IEnumerable<CharData> charDataList)
    {
        return string.Join("", charDataList.Select(t => t.CharObject.ToText()));
    }
}