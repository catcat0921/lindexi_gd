﻿using System;
using System.Diagnostics;
using LightTextEditorPlus.Core.Document.Segments;

namespace LightTextEditorPlus.Core.Carets;

/// <summary>
/// 文本光标偏移量，从 0 开始，定义如下
/// <para>0 1 2</para>
/// <para>|a|b|</para>
/// <para>如上面例子，在文档开头的光标是 0 在 "ab" 字符串最后的光标是 2 的便宜</para>
/// </summary>
/// <remarks>
/// 光标偏移量和 <see cref="DocumentOffset"/> 是不相同的，光标是在某个字符的前和后，但 <see cref="DocumentOffset"/> 就是某个字符。光标偏移量的最大值，是等于字符数量而 <see cref="DocumentOffset"/> 的最大值是字符数量 -1 的值
/// </remarks>
public readonly struct CaretOffset : IEquatable<CaretOffset>
{
    /// <summary>
    /// 创建文本光标偏移量
    /// </summary>
    /// <param name="offset"></param>
    /// <param name="isAtLineStart">设置当前Offset是否处于当前行的起始位置</param>
    [DebuggerStepThrough]
    public CaretOffset(int offset, bool isAtLineStart=false)
    {
        if (offset < 0)
        {
            throw new ArgumentException($"Offset must greater or equals than 0");
        }

        Offset = offset;
        IsAtLineStart = isAtLineStart;
    }

    /// <summary>
    /// 光标偏移量
    /// </summary>
    public int Offset { get; }

    /// <summary>
    /// 获取或设置当前 <see cref="Offset"/> 是否处于当前行的起始位置（在段落起始位置时也属于在行首）
    /// </summary>
    public bool IsAtLineStart { get; }

    public override string ToString() => Offset.ToString();

    public bool Equals(CaretOffset other)
    {
        return Offset == other.Offset;
    }

    public override bool Equals(object? obj)
    {
        return obj is CaretOffset other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Offset;
    }

    public static bool operator ==(CaretOffset left, CaretOffset right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(CaretOffset left, CaretOffset right)
    {
        return !left.Equals(right);
    }
}