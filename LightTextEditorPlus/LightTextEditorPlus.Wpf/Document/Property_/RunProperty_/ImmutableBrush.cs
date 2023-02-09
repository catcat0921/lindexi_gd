﻿using System;
using System.Windows.Media;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Utils;

namespace LightTextEditorPlus.Document;

/// <summary>
/// 表示一个不可变的画刷
/// </summary>
/// 要是还有人去拿属性去改，那我也救不了了
public class ImmutableBrush : ImmutableRunPropertyValue<Brush>, IEquatable<ImmutableBrush>
{
    public ImmutableBrush(Brush value) : base(value)
    {
    }

    public bool Equals(ImmutableBrush? other)
    {
        if (other is null)
        {
            return false;
        }

        return Converter.AreEquals(this.Value, other.Value);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((ImmutableBrush) obj);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }
}