﻿using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Document;

internal static class ParagraphPropertyExtension
{
    /// <summary>
    /// 获取一行最大可用宽度。即 <paramref name="totalWidth"/> 减去左右边距和缩进
    /// </summary>
    /// <param name="paragraphProperty"></param>
    /// <param name="totalWidth">行的最大空间</param>
    /// <param name="isFirstLine"></param>
    /// <returns></returns>
    public static double GetUsableLineMaxWidth(this ParagraphProperty paragraphProperty, double totalWidth, bool isFirstLine)
    {
        double indent = paragraphProperty.GetIndent(isFirstLine);

        return totalWidth - paragraphProperty.LeftIndentation - paragraphProperty.RightIndentation - indent;
    }

    /// <summary>
    /// 获取缩进
    /// </summary>
    /// <param name="paragraphProperty"></param>
    /// <param name="isFirstLine">是否首行</param>
    /// <returns></returns>
    public static double GetIndent(this ParagraphProperty paragraphProperty, bool isFirstLine)
    {
        double indent = paragraphProperty.IndentType switch
        {
            // 首行缩进
            IndentType.FirstLine => isFirstLine ? paragraphProperty.Indent : 0,
            // 悬挂缩进，首行不缩进
            IndentType.Hanging => isFirstLine ? 0 : paragraphProperty.Indent,
            _ => 0
        };
        return indent;
    }
}