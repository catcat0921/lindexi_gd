﻿using System;

namespace LightTextEditorPlus.Core.Primitive;

/// <summary>
/// 表示一个抽象的字体名称，整个文本库都将使用此类型来表示文本所使用的字体。
/// 它所指代的字体甚至可以是未安装的，会协助找到最接近的可用来真实渲染的字体。
/// </summary>
public readonly struct FontName:IEquatable<FontName>
{
    /// <summary>
    /// 初始化抽象字体名称的新实例。
    /// </summary>
    /// <param name="userFontName">
    /// 用户所指定的字体名称。<para/>
    /// 如果用户从未指定，则应设为 <see cref="string.Empty"/>；
    /// 但是为了在文本能在所有用户的设备上获得尽可能一致的体验，应该在用户未指定但又必须使用时初始化一个非空有效值。
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="userFontName"/> 为 null。</exception>
    public FontName(string userFontName)
    {
        UserFontName = userFontName ?? throw new ArgumentNullException(nameof(userFontName));
    }

    /// <summary>
    /// 用户所指定的字体名称。
    /// </summary>
    public string UserFontName { get; }

    internal static FontName DefaultNotDefineFontName => new FontName("_NotDefine_");

    public bool Equals(FontName other)
    {
        return UserFontName == other.UserFontName;
    }

    public override bool Equals(object? obj)
    {
        return obj is FontName other && Equals(other);
    }

    public override int GetHashCode()
    {
        return UserFontName.GetHashCode();
    }

    public static bool operator ==(FontName left, FontName right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(FontName left, FontName right)
    {
        return !left.Equals(right);
    }
}