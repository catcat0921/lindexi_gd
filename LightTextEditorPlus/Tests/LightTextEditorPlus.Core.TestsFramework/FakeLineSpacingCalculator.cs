using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Platform;

namespace LightTextEditorPlus.Core.TestsFramework;

/// <summary>
/// ���иߺ��ֺ�һ�����������
/// </summary>
public class FakeLineSpacingCalculator : ILineSpacingCalculator
{
    public LineSpacingCalculateResult CalculateLineSpacing(in LineSpacingCalculateArgument argument)
    {
        return new LineSpacingCalculateResult(false, argument.MaxFontSizeCharRunProperty.FontSize);
    }
}