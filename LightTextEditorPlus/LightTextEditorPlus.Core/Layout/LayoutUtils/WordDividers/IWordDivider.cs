namespace LightTextEditorPlus.Core.Layout.LayoutUtils.WordDividers;

/// <summary>
/// ���ʷָ������ִ���
/// </summary>
public interface IWordDivider
{
    /// <summary>
    /// �ָ��
    /// </summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    DivideWordResult DivideWord(in DivideWordArgument argument);
}