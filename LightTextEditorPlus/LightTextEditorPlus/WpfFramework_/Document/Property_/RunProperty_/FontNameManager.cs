﻿using System;
using System.Collections.Generic;
using System.Drawing.Text; // todo 后续干掉 WinForms 的获取字体
using System.Globalization;
using System.Linq;
using System.Windows.Media;

namespace LightTextEditorPlus.Document;

public class FontNameManager
{
    public static List<string> InstalledFontFamiliesEx =>
        _installedFontFamiliesEx ??= GetInstalledFamiliesEx();

    /// <summary>
    /// 判断字体是否已经安装，里面使用 HASH 方法，性能比较好
    /// </summary>
    /// <param name="fontFamilySource"></param>
    /// <returns></returns>
    public static bool CheckFontFamilyInstalled(string fontFamilySource)
    {
        return InstalledFontFamiliesHashSet.Contains(fontFamilySource);
    }

    private static List<string>? _installedFontFamiliesEx;

    private static HashSet<string> InstalledFontFamiliesHashSet => _installedFontFamiliesHashSet ??= new HashSet<string>(InstalledFontFamiliesEx);

    private static HashSet<string>? _installedFontFamiliesHashSet;

    #region 静态

    /// <summary>
    /// 获取当前机器上的字体名称，会返回所有Culture下的名称
    /// </summary>
    /// <returns></returns>
    private static List<string> GetInstalledFamiliesEx()
    {
        //zh - CN 0x0804 中文 - 中国
        //zh - CHS 0x0004 中文（简体）
        //en - GB 0x0809 英语 - 英国
        //en - US 0x0409 英语 - 美国
        const int USCode = 0x0409;
        //ja - JP 0x0411日语 - 日本
        var fontCollection = GetInstalledFontFamilies();

        var familyNames = fontCollection.Select(x => x.Name).Where(x => !string.IsNullOrEmpty(x)).ToList();
        var englishNames = fontCollection.Select(x => x.GetName(USCode))
            .Where(x => !string.IsNullOrEmpty(x));

        foreach (var englishName in englishNames)
        {
            if (!familyNames.Contains(englishName))
            {
                familyNames.Add(englishName);
            }
        }

        //通过所有Culture下的字体名称比对，来处理不同系统下打开后依然能够正常判断
        //但是，有的字体在设计名称时存在问题，比如“微软雅黑 light”
        //中文情况下，它的字体名称是“微软雅黑 Light”，中文和英文名称分别是“微软雅黑”“Microsoft YaHei”
        //英文情况下，它的字体名称是“Microsoft YaHei Light”，中文和英文名称分别是“微软雅黑”“Microsoft YaHei”
        //这就导致在中文环境下设置的字体在英文环境下无法识别，将被回退为Arail
        //如果要从根本上解决此问题，需要在序列化的时候考虑Culture
        var familyNamesWithAllCulture = new List<string>();
        familyNamesWithAllCulture.AddRange(familyNames);
        foreach (var familyName in familyNames)
        {
            try
            {
                var f = new FontFamily(familyName);
                if (f.FamilyNames.Values is not null)
                {
                    familyNamesWithAllCulture.AddRange(f.FamilyNames.Values.ToList());
                }
                else
                {
                    familyNamesWithAllCulture.Add(familyName);
                }
            }
            catch (Exception e)
            {
                // 获取当前机器上的字体名称失败
            }
        }

        familyNamesWithAllCulture.AddRange(GetSystemFontFamilies());
        return familyNamesWithAllCulture.Distinct().ToList();
    }

    private static List<string> GetSystemFontFamilies()
    {
        try
        {
            //WPF拿取区域字体
            CultureInfo currentCulture = CultureInfo.CurrentUICulture;
            var defaultCulture = new CultureInfo("en-US");

            //添加本地区域字体
            var familiesNames = Fonts.SystemFontFamilies.Select(x => x.FamilyNames).Select(x => x.FirstOrDefault(k => Equals(k.Key.GetSpecificCulture(), currentCulture))).Where(x => x.Key != null).Select(x => x.Value).ToList();
            familiesNames.Sort();

            //添加默认区域字体
            if (!Equals(currentCulture, defaultCulture))
            {
                var defaultFonts = Fonts.SystemFontFamilies.Select(x => x.FamilyNames).Where(x => x.Any(k => Equals(k.Key.GetSpecificCulture(), defaultCulture)) && x.All(k => !Equals(k.Key.GetSpecificCulture(), currentCulture))).Select(x => x.FirstOrDefault(k => Equals(k.Key.GetSpecificCulture(), defaultCulture))).Where(x => x.Key != null).Select(x => x.Value).ToList();
                defaultFonts.Sort();
                familiesNames.AddRange(defaultFonts);
            }
            return familiesNames.ToList();
        }
        //Fonts.SystemFontFamilies可能会抛出异常
        catch (Exception e)
        {
            return new List<string>(0);
        }
    }

    private static List<System.Drawing.FontFamily> GetInstalledFontFamilies()
    {
        try
        {
            var fc = new InstalledFontCollection();
            return fc.Families.ToList();
        }
        catch (Exception e)
        {
            return new List<System.Drawing.FontFamily>(0);
        }
    }

    #endregion
}