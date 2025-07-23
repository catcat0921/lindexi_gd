﻿using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using SkiaSharp;

namespace KerwurajairqeageJihaidealecoher;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        foreach (var fontFamilyName in SKFontManager.Default.GetFontFamilies())
        {
            var fontFamily = new FontFamily(fontFamilyName);
            var lineSpacing = fontFamily.LineSpacing;

            SKTypeface? skTypeface = SKFontManager.Default.MatchFamily(fontFamilyName);
            var skFont = new SKFont(skTypeface, 100);
            var h = (-skFont.Metrics.Ascent + skFont.Metrics.Descent) / skFont.Size;
            var h2 = (-skFont.Metrics.Ascent + skFont.Metrics.Descent + skFont.Metrics.Leading) / skFont.Size;

            var d = Math.Abs(lineSpacing - h2);

            bool isNearlyEqual = d < 0.01;
            Debug.WriteLine($"{fontFamilyName} 是否相近 {isNearlyEqual} {d:0.00000}");

            if (!isNearlyEqual)
            {

            }
        }
    }
}