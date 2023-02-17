﻿using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Windows.Media;
using LightTextEditorPlus.TextEditorPlus.Render;

namespace LightTextEditorPlus.Document;

/// <summary>
/// 用来管理本地资源，包括画刷或者是字体等
/// </summary>
class RunPropertyPlatformManager
{
    public RunPropertyPlatformManager(TextEditor textEditor)
    {
        _textEditor = textEditor;
    }

    private readonly TextEditor _textEditor;

    public (GlyphTypeface glyphTypeface, FontFamily renderingFontFamily) GetGlyphTypefaceAndRenderingFontFamily(RunProperty runProperty, char unicodeChar)
    {
        FontFamily fontFamily;
        if (runProperty.FontName.IsNotDefineFontName)
        {
            fontFamily = TextEditor.StaticConfiguration.DefaultNotDefineFontFamily;
        }
        else
        {
            fontFamily = new FontFamily(runProperty.FontName.UserFontName);
        }

        var renderingFontFamily = fontFamily;

        // 获取字体，获取方式：
        var typeface = new Typeface(fontFamily, runProperty.FontStyle, runProperty.FontWeight, runProperty.Stretch);

        bool success = typeface.TryGetGlyphTypeface(out var glyphTypeface);

        if (!success)
        {
            // 字体回滚：
            // 1. 先使用业务层传入的字体回滚策略。例如将 “方正楷体” 修改为 “楷体”
            // 2. 如果业务层的字体回滚策略不满足，那就采用 WPF 的方式回滚
            if (TryGetFallbackGlyphTypefaceByCustom(runProperty, typeface, out var fallbackGlyph,
                    out var fallbackFontFamily))
            {
                glyphTypeface = fallbackGlyph;
                renderingFontFamily = fallbackFontFamily;
            }
            // 找不到字体，需要进行回滚
            else if (TryGetFallbackGlyphTypefaceByWpf(typeface, unicodeChar, out fallbackGlyph, out fallbackFontFamily))
            {
                glyphTypeface = fallbackGlyph;
                renderingFontFamily = fallbackFontFamily;
            }
            // 理论上还失败，只能使用最终回滚字体了
            else
            {
                renderingFontFamily = new FontFamily(FontNameManager.FallbackDefaultFontName);
                var fallbackTypeface = new Typeface(renderingFontFamily, typeface.Style, typeface.Weight,
                    typeface.Stretch);
                // 理论上不会失败
                fallbackTypeface.TryGetGlyphTypeface(out glyphTypeface);
            }
        }

        return (glyphTypeface, renderingFontFamily);
    }

    private static bool TryGetFallbackGlyphTypefaceByCustom(RunProperty runProperty, Typeface typeface,
        [NotNullWhen(true)] out GlyphTypeface? glyphTypeface, [NotNullWhen(true)] out FontFamily? fallbackFontFamily)
    {
        var fallbackFontName =
            TextEditor.StaticConfiguration.FontNameManager.GetFallbackFontName(runProperty.FontName.UserFontName);
        if (string.IsNullOrEmpty(fallbackFontName))
        {
            glyphTypeface = null;
            fallbackFontFamily = null;
            return false;
        }

        fallbackFontFamily = new FontFamily(fallbackFontName);
        var fallbackTypeface = new Typeface(fallbackFontFamily, typeface.Style, typeface.Weight,
            typeface.Stretch);

        return fallbackTypeface.TryGetGlyphTypeface(out glyphTypeface);
    }

    private bool TryGetFallbackGlyphTypefaceByWpf(Typeface typeface, char unicodeChar,
        [NotNullWhen(true)] out GlyphTypeface? glyphTypeface, [NotNullWhen(true)] out FontFamily? fallbackFontFamily)
    {
        if (FallBackFontFamily.TryGetFallBackFontFamily(unicodeChar, out var familyName))
        {
            fallbackFontFamily = new FontFamily(familyName);
            var fallbackTypeface = new Typeface(fallbackFontFamily, typeface.Style, typeface.Weight,
                typeface.Stretch);

            return fallbackTypeface.TryGetGlyphTypeface(out glyphTypeface);
        }

        glyphTypeface = null;
        fallbackFontFamily = null;
        return false;
    }

    private FallBackFontFamily FallBackFontFamily { get; } = new FallBackFontFamily(CultureInfo.CurrentUICulture);
}