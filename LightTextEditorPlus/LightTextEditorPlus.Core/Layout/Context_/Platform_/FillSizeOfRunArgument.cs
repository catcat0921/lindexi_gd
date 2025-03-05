using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive.Collections;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// ��� Run �ĳߴ����
/// </summary>
/// <param name="RunList"></param>
/// <param name="UpdateLayoutContext"></param>
public readonly record struct FillSizeOfRunArgument(TextReadOnlyListSpan<CharData> RunList, UpdateLayoutContext UpdateLayoutContext)
{
    /// <summary>
    /// ��ǰ���ַ�
    /// </summary>
    public CharData CurrentCharData => RunList[0];

    /// <summary>
    /// �����ַ�������Ϣ��������
    /// </summary>
    public ICharDataLayoutInfoSetter CharDataLayoutInfoSetter => UpdateLayoutContext;

    /// <summary>
    /// ���õ�ǰ�ַ��Ĳ��������������� <see cref="CharDataLayoutInfoSetter"/> ���ø��� <see cref="CurrentCharData"/> �ĳߴ���Ϣ
    /// </summary>
    /// <param name="result"></param>
    public void SetCurrentCharDataMeasureResult(in CharInfoMeasureResult result)
    {
        CharDataLayoutInfoSetter.SetCharDataInfo(CurrentCharData, result.Bounds.TextSize, result.Baseline);
    }
};