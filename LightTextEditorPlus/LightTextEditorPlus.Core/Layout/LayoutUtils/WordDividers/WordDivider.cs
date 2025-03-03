using System.Globalization;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Layout.LayoutUtils.WordDividers;

internal class WordDivider : IWordDivider
{
    public DivideWordResult DivideWord(in DivideWordArgument argument)
    {
        int currentIndex = 0;

        var currentCharData = argument.CurrentRunList[currentIndex];

        if (IsPunctuationNotInLineEnd(currentCharData.CharObject.CodePoint))
        {
            currentIndex++;
        }

        var totalCount = currentIndex;
        var charCount = WordCharHelper.ReadWordCharCount(argument.CurrentRunList, currentIndex);
        totalCount += charCount;

        // ���ܷ������׵ķ���
        var wordNextCharIndex = currentIndex + charCount;
        if (wordNextCharIndex < argument.CurrentRunList.Count)
        {
            CharData charDataInNextWord = argument.CurrentRunList[wordNextCharIndex];
            Utf32CodePoint codePoint = charDataInNextWord.CharObject.CodePoint;

            if (IsPunctuationNotInLineStart(codePoint))
            {
                // �����һ���ַ��Ǳ����ţ��Ҳ��ܷ������ף��ǾͲ�Ҫ������һ������
                totalCount += 1;
            }
        }

        return new DivideWordResult(totalCount);
    }

    private static bool IsPunctuationNotInLineEnd(Utf32CodePoint codePoint)
    {
        UnicodeCategory unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(codePoint.Value);

        return unicodeCategory is UnicodeCategory.OpenPunctuation
            or UnicodeCategory.InitialQuotePunctuation;
    }

    /// <summary>
    /// ͨ�������Ļ��жϵ�ǰ����ı������Ƿ��ܷ������ס������Ļ�����ֻ�������жϷ��ţ��Ƿ��ܷ����������ı�����ж�
    /// </summary>
    /// <param name="codePoint">�������֮ǰ��ȷ��ֻ��һ���ַ�</param>
    /// <returns></returns>
    private static bool IsPunctuationNotInLineStart(Utf32CodePoint codePoint)
    {
        // ֻ���жϱ����Ŷ���
        // �����жϣ�ͨ���������жϡ�ֻҪ�Ǳ����ţ��Ҳ��ǿ��������׵ģ��Ǿͷ��� true ֵ
        UnicodeCategory unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(codePoint.Value);
        return unicodeCategory is UnicodeCategory.OtherPunctuation
            //or UnicodeCategory.OpenPunctuation �� ����
            or UnicodeCategory.ClosePunctuation
            or UnicodeCategory.ConnectorPunctuation
            or UnicodeCategory.DashPunctuation
            //or UnicodeCategory.InitialQuotePunctuation �� ��
            or UnicodeCategory.FinalQuotePunctuation;

        //if (RegexPatterns.LeftSurroundInterpunction.Contains(charInNextWord))
        //{
        //    // ���ж��Ƿ������ף�����ж������Ƚ�С���ٶȿ�
        //    return false;
        //}

        //return Regex.IsMatch(text, RegexPatterns.Interpunction);

        //Span<char> punctuationNotInLineStartList = stackalloc char[]
        //{
        //    // Ӣ��ϵ��
        //    '.',
        //    ',',
        //    ':',
        //    ';',
        //    '?',
        //    '!',
        //    '\'',
        //    '"',
        //    ')',

        //    // ����ϵ�� GB/T 15834 �淶
        //    '��',
        //    '��',
        //    '��',
        //    '��',
        //    '��',
        //    '��',
        //    '��',
        //    '��',
        //    '��',
        //    '��',
        //    '��', // ����� 5.1.7 ����ű겻�ܳ�����һ��֮��
        //    '/', // 5.1.9 ����������Ҳ��������ĩ

        //    // �������Եģ�����
        //};

        //return punctuationNotInLineStartList.Contains(charInNextWord);
    }
}