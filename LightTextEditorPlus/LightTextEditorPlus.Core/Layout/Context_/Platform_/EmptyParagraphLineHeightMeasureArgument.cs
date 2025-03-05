using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 空段的行高测量参数
/// </summary>
/// <param name="ParagraphProperty">段落属性</param>
/// <param name="ParagraphIndex">段落序号</param>
/// <param name="ParagraphStartRunProperty"></param>
/// <param name="UpdateLayoutContext">布局上下文信息</param>
public readonly record struct EmptyParagraphLineHeightMeasureArgument(
    ParagraphProperty ParagraphProperty,
    ParagraphIndex ParagraphIndex,
    IReadOnlyRunProperty ParagraphStartRunProperty,
    UpdateLayoutContext UpdateLayoutContext)
{
}
