﻿using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Core.TestsFramework;

public static class TestHelper
{
    public const string PlainNumberText = "123";
    public const string PlainLongNumberText = "1231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231"; // 200 个字符

    /// <summary>
    /// 默认的固定字符布局的字号
    /// </summary>
    public const double DefaultFixCharFontSize = 15;

    /// <summary>
    /// 默认固定字符的默认字号的字宽
    /// </summary>
    public const double DefaultFixCharWidth = DefaultFixCharFontSize;

    public static char[] PunctuationNotInLineStartCharList => new[]
    {
        ',', '.', ';', '!', '，', '。', '！', '：', '；', '、', ')', '）' 
    };

    public static TextEditorCore GetTextEditorCore(TestPlatformProvider? testPlatformProvider = null)
    {
        testPlatformProvider ??= new TestPlatformProvider();
        var textEditorCore = new TextEditorCore(testPlatformProvider);

        if (testPlatformProvider.CharInfoMeasurer is FixedCharSizeCharInfoMeasurer)
        {
            // 如果是固定字符尺寸测量的，那就设置默认字体就是 15 号
            textEditorCore.DocumentManager.SetDefaultTextRunProperty<LayoutOnlyRunProperty>(runProperty => runProperty.FontSize = DefaultFixCharFontSize);
        }

        return textEditorCore;
    }
}