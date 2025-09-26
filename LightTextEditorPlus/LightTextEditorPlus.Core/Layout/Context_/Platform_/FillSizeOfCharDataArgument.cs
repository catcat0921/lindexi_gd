﻿using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Layout.LayoutUtils;
using LightTextEditorPlus.Core.Primitive.Collections;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 填充 字符 的尺寸参数
/// </summary>
/// <param name="ToMeasureCharDataList"></param>
/// <param name="UpdateLayoutContext"></param>
public readonly record struct FillSizeOfCharDataArgument(TextReadOnlyListSpan<CharData> ToMeasureCharDataList, UpdateLayoutContext UpdateLayoutContext)
{
    /// <summary>
    /// 当前的字符
    /// </summary>
    public CharData CurrentCharData => ToMeasureCharDataList[0];

    /// <summary>
    /// 设置字符布局信息辅助工具
    /// </summary>
    public ICharDataLayoutInfoSetter CharDataLayoutInfoSetter => UpdateLayoutContext;

    ///// <summary>
    ///// 设置当前字符的测量结果。将会调用 <see cref="CharDataLayoutInfoSetter"/> 设置给到 <see cref="CurrentCharData"/> 的尺寸信息
    ///// </summary>
    ///// <param name="result"></param>
    //public void SetCurrentCharDataMeasureResult(in CharInfoMeasureResult result)
    //{
    //    CharDataLayoutInfoSetter.SetCharDataInfo(CurrentCharData, result.Bounds.TextSize, result.Baseline);
    //}
};