﻿using LightTextEditorPlus.Core.Layout;

namespace LightTextEditorPlus.Core.Platform;

/// <summary>
/// 整行的布局器，用来布局一整行，包括处理行距等信息
/// </summary>
public interface IWholeLineLayouter
{
    /// <summary>
    /// 布局整行
    /// </summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    WholeLineLayoutResult UpdateLayoutWholeLine(in WholeLineLayoutArgument argument);
}