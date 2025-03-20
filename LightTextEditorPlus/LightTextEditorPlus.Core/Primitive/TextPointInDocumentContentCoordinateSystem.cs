using System;
using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Utils;
using LightTextEditorPlus.Core.Utils.Maths;

namespace LightTextEditorPlus.Core.Primitive;

/// <summary>
/// 相对于文档内容坐标系的点
/// </summary>
public readonly struct TextPointInDocumentContentCoordinateSystem
    : IEquatable<TextPointInDocumentContentCoordinateSystem>
{
    internal TextPointInDocumentContentCoordinateSystem(double x, double y, LayoutManager layoutManager)
    {
        _x = x;
        _y = y;
        _layoutManager = layoutManager;
    }

    private readonly double _x;

    private readonly double _y;

    private readonly LayoutManager _layoutManager;

    /// <summary>
    /// 是否为零点坐标
    /// </summary>
    public bool IsZero => _x == 0 && _y == 0;

    /// <summary>
    /// 是否为无效坐标
    /// </summary>
    public bool IsInvalid
        // 只需判断一个条件就好了，不用判断 X 和 Y 的值
        => ReferenceEquals(_layoutManager, null);

    /// <summary>
    /// 无效的起点坐标
    /// </summary>
    public static TextPointInDocumentContentCoordinateSystem InvalidStartPoint
    {
        get
        {
            TextPoint invalidStartPoint = TextContext.InvalidStartPoint;

            return new TextPointInDocumentContentCoordinateSystem(invalidStartPoint.X, invalidStartPoint.Y, layoutManager: null!);
        }
    }

    /// <inheritdoc />
    public bool Equals(TextPointInDocumentContentCoordinateSystem other)
    {
        return _x.Equals(other._x) && _y.Equals(other._y) && ReferenceEquals(_layoutManager, other._layoutManager);
    }


    /// <inheritdoc cref="Equals(TextPointInDocumentContentCoordinateSystem)"/>
    public bool NearlyEquals(in TextPointInDocumentContentCoordinateSystem other)
    {
        return ReferenceEquals(_layoutManager, other._layoutManager)
               && Nearly.Equals(_x, other._x)
               && Nearly.Equals(_y, other._y);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is TextPointInDocumentContentCoordinateSystem other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(_x, _y);
    }

    /// <summary>
    /// 转换为文档坐标系的点
    /// </summary>
    /// <returns></returns>
    public TextPoint ToTextPoint()
    {
        _layoutManager.TextEditor.VerifyNotDirty(autoLayoutEmptyTextEditor: false);
        TextPoint documentContentStartPoint = _layoutManager.DocumentLayoutBounds.DocumentContentBounds.Location;
        return new TextPoint(_x + documentContentStartPoint.X, _y + documentContentStartPoint.Y);
    }

    //internal bool NearlyEqualsX(double x) => Nearly.Equals(_x, x);
    //internal bool NearlyEqualsY(double y) => Nearly.Equals(_y, y);

    /// <summary>
    /// 偏移
    /// </summary>
    /// <param name="offsetX"></param>
    /// <param name="offsetY"></param>
    /// <returns></returns>
    public TextPointInDocumentContentCoordinateSystem Offset(double offsetX, double offsetY)
    {
        return new TextPointInDocumentContentCoordinateSystem(_x + offsetX, _y + offsetY, _layoutManager);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"DocumentContentCoordinate:({_x:0.###},{_y:0.###})";
    }
}