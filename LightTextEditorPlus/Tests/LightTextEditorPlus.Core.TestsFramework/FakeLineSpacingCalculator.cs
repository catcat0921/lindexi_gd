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
        if (double.IsNaN(argument.ParagraphProperty.FixedLineSpacing))
        {
            return new LineSpacingCalculateResult(false, argument.MaxFontSizeCharRunProperty.FontSize * argument.ParagraphProperty.LineSpacing);
        }
        else
        {
            return new LineSpacingCalculateResult(true, argument.ParagraphProperty.FixedLineSpacing);
        }
    }
}