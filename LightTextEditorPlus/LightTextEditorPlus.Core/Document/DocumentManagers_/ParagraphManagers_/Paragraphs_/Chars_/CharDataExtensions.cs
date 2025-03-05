namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// �� <see cref="CharData"/> ����չ
/// </summary>
public static class CharDataExtensions
{
    /// <summary>
    /// ת��Ϊ <see cref="CharInfo"/> �ṹ��
    /// </summary>
    /// <param name="charData"></param>
    /// <returns></returns>
    public static CharInfo ToCharInfo(this CharData charData) => new CharInfo(charData.CharObject, charData.RunProperty);
}