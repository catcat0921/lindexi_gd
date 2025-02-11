using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Document;

internal static class ParagraphPropertyExtension
{
    /// <summary>
    /// ��ȡ�����������ֵ����Ϊ���������ֵ�Ǵ�����������������������������ģ�������Ҫ�����Ƿ����������ж�
    /// </summary>
    /// <param name="property"></param>
    /// <param name="isFirstLine">�Ƿ�����</param>
    /// <returns></returns>
    public static double GetLeftIndentationValue(this ParagraphProperty property, bool isFirstLine)
    {
        return property.IndentType switch
        {
            IndentType.Hanging => property.LeftIndentation,
            IndentType.FirstLine => isFirstLine ? property.LeftIndentation : 0,
            _ => 0,
        };
    }
}