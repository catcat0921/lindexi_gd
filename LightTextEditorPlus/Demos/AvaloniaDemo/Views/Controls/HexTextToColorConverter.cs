using System.Diagnostics;
using Avalonia.Media;

namespace LightTextEditorPlus.AvaloniaDemo.Views.Controls;

public static class HexTextToColorConverter
{
    public static Color? ParseHexString(string input)
    {
        (bool success, byte a, byte r, byte g, byte b) = ConvertToColor(input);
        if (success)
        {
            return Color.FromArgb(a, r, g, b);
        }
        else
        {
            return null;
        }
    }

   public static (bool success, byte a, byte r, byte g, byte b) ConvertToColor(string input)
    {
        bool startWithPoundSign = input.StartsWith('#');
        var colorStringLength = input.Length;
        if (startWithPoundSign) colorStringLength -= 1;
        int currentOffset = startWithPoundSign ? 1 : 0;
        // ���Բ��õĸ�ʽ����
        // #FFDFD991   8 ���ַ� ���� Alpha ͨ��
        // #DFD991     6 ���ַ�
        // #FD92       4 ���ַ� ���� Alpha ͨ��
        // #DAC        3 ���ַ�
        if (colorStringLength == 8
            || colorStringLength == 6
            || colorStringLength == 4
            || colorStringLength == 3)
        {
            bool success;
            byte result;
            byte a;

            int readCount;
            // #DFD991     6 ���ַ�
            // #FFDFD991   8 ���ַ� ���� Alpha ͨ��
            //if (colorStringLength == 8 || colorStringLength == 6)
            if (colorStringLength > 5)
            {
                readCount = 2;
            }
            else
            {
                readCount = 1;
            }

            bool includeAlphaChannel = colorStringLength == 8 || colorStringLength == 4;

            if (includeAlphaChannel)
            {
                (success, result) = HexCharToNumber(input, currentOffset, readCount);
                if (!success) return default;
                a = result;
                currentOffset += readCount;
            }
            else
            {
                a = 0xFF;
            }

            (success, result) = HexCharToNumber(input, currentOffset, readCount);
            if (!success) return default;
            byte r = result;
            currentOffset += readCount;

            (success, result) = HexCharToNumber(input, currentOffset, readCount);
            if (!success) return default;
            byte g = result;
            currentOffset += readCount;

            (success, result) = HexCharToNumber(input, currentOffset, readCount);
            if (!success) return default;
            byte b = result;

            return (true, a, r, g, b);
        }

        return default;
    }

    static (bool success, byte result) HexCharToNumber(string input, int offset, int readCount)
    {
        Debug.Assert(readCount == 1 || readCount == 2, "Ҫ�� readCount ֻ���� 1 ���� 2 ��ֵ�����ǿ�����ƣ���˲����ж�");

        byte result = 0;

        for (int i = 0; i < readCount; i++, offset++)
        {
            var c = input[offset];
            byte n;
            if (c >= '0' && c <= '9')
            {
                n = (byte) (c - '0');
            }
            else if (c >= 'a' && c <= 'f')
            {
                n = (byte) (c - 'a' + 10);
            }
            else if (c >= 'A' && c <= 'F')
            {
                n = (byte) (c - 'A' + 10);
            }
            else
            {
                return default;
            }

            result *= 16;
            result += n;
        }

        if (readCount == 1)
        {
            result = (byte) (result * 16 + result);
        }

        return (true, result);
    }
}