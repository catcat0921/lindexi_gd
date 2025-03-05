using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive.Collections;

namespace LightTextEditorPlus.Core.Layout.LayoutUtils.WordDividers;

/// <summary>
/// �ָ�ʵĲ���
/// </summary>
/// <param name="CurrentRunList"></param>
/// <param name="UpdateLayoutContext"></param>
public readonly record struct DivideWordArgument(TextReadOnlyListSpan<CharData> CurrentRunList, UpdateLayoutContext UpdateLayoutContext);