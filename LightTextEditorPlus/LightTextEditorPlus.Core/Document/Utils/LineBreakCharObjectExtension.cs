namespace LightTextEditorPlus.Core.Document.Utils;

/// <summary>
/// �����ַ�������չ
/// </summary>
public static class LineBreakCharObjectExtension
{
    /// <summary>
    /// �ж��Ƿ��ǻ����ַ�
    /// </summary>
    /// <param name="charObject"></param>
    /// <returns></returns>
    public static bool IsLineBreak(this ICharObject charObject)
    {
        return ReferenceEquals(charObject, LineBreakCharObject.Instance);
    }
}