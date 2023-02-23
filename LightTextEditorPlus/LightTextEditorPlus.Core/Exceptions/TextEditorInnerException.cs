﻿using System;
using System.Runtime.Serialization;

namespace LightTextEditorPlus.Core.Exceptions;

/// <summary>
/// 文本框架内异常。如果你看到此异常，那就意味着文本框架内存在异常。快来将异常和步骤告诉文本开发者
/// </summary>
public class TextEditorInnerException : TextEditorException
{
    public TextEditorInnerException()
    {
    }

    public TextEditorInnerException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public TextEditorInnerException(string? message) : base(message)
    {
    }

    public TextEditorInnerException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}