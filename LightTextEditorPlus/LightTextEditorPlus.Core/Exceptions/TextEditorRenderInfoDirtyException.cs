﻿using System;

namespace LightTextEditorPlus.Core.Exceptions;

/// <summary>
/// 在文本是脏的获取了渲染信息
/// </summary>
public class TextEditorRenderInfoDirtyException : TextEditorException
{
    /// <inheritdoc />
    public override string Message => "文本布局已更新，此渲染信息是脏的。请不要缓存 RenderInfoProvider 对象";
}