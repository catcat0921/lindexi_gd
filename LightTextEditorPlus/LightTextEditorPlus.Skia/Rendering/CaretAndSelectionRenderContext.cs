﻿namespace LightTextEditorPlus.Rendering;

/// <summary>
/// 光标和选择范围的渲染上下文
/// </summary>
/// <param name="IsOvertypeModeCaret">是否替换覆盖模式</param>
public readonly record struct CaretAndSelectionRenderContext(bool IsOvertypeModeCaret)
{
}