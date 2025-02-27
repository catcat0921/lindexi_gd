namespace LightTextEditorPlus.Core.Primitive;

/// <summary>
/// �ı����ʽ����
/// </summary>
public static class TextPointFormatter
{
    /// <summary>
    /// ת��Ϊ��ѧ���ʽ���� (1.23,4.56)
    /// </summary>
    /// <param name="textPoint"></param>
    /// <returns></returns>
    public static string ToMathPointFormat(this TextPoint textPoint)
        => $"({textPoint.X:#.##},{textPoint.Y:#.##})";
}