using System;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive.Collections;

namespace LightTextEditorPlus.Platform;

internal class SkiaWholeLineCharsLayouter : IWholeLineCharsLayouter
{
    public WholeLineCharsLayoutResult UpdateWholeLineCharsLayout(in WholeLineLayoutArgument argument)
    {
        //TextSize CurrentLineCharTextSize, int WholeTakeCount
        // ȡ�������ַ����������ַ�ָ����������ͬ���ַ������Բ���ͬ�ģ��ָ�Ϊ��ε��ò���
        var wholeTakeCount = 0;

        while (wholeTakeCount < argument.CharDataList.Count)
        {
            var currentIndex = wholeTakeCount;
            TextReadOnlyListSpan<CharData> runList = argument.CharDataList.Slice(currentIndex).GetFirstCharSpanContinuous();

        }

        throw new NotImplementedException();
    }

    private SingleCharInLineLayoutResult MeasureSingleRunLayout()
    {
        throw new NotImplementedException();
    }
}