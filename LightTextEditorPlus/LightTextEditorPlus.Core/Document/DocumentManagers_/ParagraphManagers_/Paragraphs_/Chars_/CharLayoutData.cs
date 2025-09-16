﻿using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Document;

class LayoutCharData : IParagraphCache
{
    public LayoutCharData(CharData charData, ParagraphData paragraph)
    {
        CharData = charData;
        Paragraph = paragraph;
        paragraph.InitVersion(this);
    }

    public CharData CharData { get; }

    internal ParagraphData Paragraph { get; }

    public uint CurrentParagraphVersion { get; set; }

    /// <summary>
    /// 字符数据是否失效
    /// </summary>
    /// <remarks>如果字符数据版本和段落版本不同步，则字符数据没有被布局更新，证明数据失效</remarks>
    /// <returns></returns>
    public bool IsInvalidVersion() => Paragraph.IsInvalidVersion(this);

    /// <summary>
    /// 从段落更新缓存版本信息
    /// </summary>
    public void UpdateVersion() => Paragraph.UpdateVersion(this);
}

/// <summary>
/// 字符的布局信息，包括字符所在的段落和所在的行，字符所在的相对于文本框的坐标
/// </summary>
class CharLayoutData : IParagraphCache
{
    public CharLayoutData(CharData charData, ParagraphData paragraph)
    {
        CharData = charData;
        Paragraph = paragraph;
        paragraph.InitVersion(this);
    }

    public CharData CharData { get; }

    internal ParagraphData Paragraph { get; }

    public uint CurrentParagraphVersion { get; set; }

    /// <summary>
    /// 字符数据是否失效
    /// </summary>
    /// <remarks>如果字符数据版本和段落版本不同步，则字符数据没有被布局更新，证明数据失效</remarks>
    /// <returns></returns>
    public bool IsInvalidVersion() => Paragraph.IsInvalidVersion(this);

    /// <summary>
    /// 从段落更新缓存版本信息
    /// </summary>
    public void UpdateVersion() => Paragraph.UpdateVersion(this);

    /// <summary>
    /// 字符在行内的起始点，坐标相对于行
    /// </summary>
    /// 可用来辅助布局上下标
    public TextPointInLineCoordinateSystem CharLineStartPoint { set; get; }

    //public TextPoint BaselineStartPoint { set; get; }

    /// <summary>
    /// 字符是当前段落 <see cref="Paragraph"/> 的第几个字符
    /// </summary>
    /// 调试作用
    public ParagraphCharOffset CharIndex { set; get; }

    /// <summary>
    /// 字符是当前行的第几个字
    /// </summary>
    public int CharIndexInLine
    {
        get
        {
            if (CurrentLine is null)
            {
                return -1;
            }

            return CharIndex.Offset - CurrentLine.CharStartParagraphIndex;
        }
    }

    /// <summary>
    /// 当前所在的行
    /// </summary>
    public LineLayoutData? CurrentLine { set; get; }

    public override string ToString()
    {
        return $"'{CharData.CharObject}' 第{Paragraph.Index}段，第{CurrentLine?.LineInParagraphIndex}行，段内第{CharIndex.Offset}个字符，行内第{CharIndexInLine}个字符";
    }
}
