using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Core.TestsFramework;

public static class TestHelper
{
    public const string PlainNumberText = "123";
    public const string PlainLongNumberText = "1231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231"; // 200 ���ַ�

    /// <summary>
    /// Ĭ�ϵĹ̶��ַ����ֵ��ֺ�
    /// </summary>
    public const double DefaultFixCharFontSize = 15;

    /// <summary>
    /// Ĭ�Ϲ̶��ַ���Ĭ���ֺŵ��ֿ�
    /// </summary>
    public const double DefaultFixCharWidth = DefaultFixCharFontSize;

    public static TextEditorCore GetTextEditorCore(TestPlatformProvider? testPlatformProvider = null)
    {
        testPlatformProvider ??= new TestPlatformProvider();
        var textEditorCore = new TextEditorCore(testPlatformProvider);

        if (testPlatformProvider.CharInfoMeasurer is FixedCharSizeCharInfoMeasurer)
        {
            // ����ǹ̶��ַ��ߴ�����ģ��Ǿ�����Ĭ��������� 15 ��
            textEditorCore.DocumentManager.SetDefaultTextRunProperty<LayoutOnlyRunProperty>(runProperty => runProperty.FontSize = DefaultFixCharFontSize);
        }

        return textEditorCore;
    }
}