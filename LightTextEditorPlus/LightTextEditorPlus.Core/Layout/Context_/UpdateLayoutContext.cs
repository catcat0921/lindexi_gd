﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

using LightTextEditorPlus.Core.Diagnostics;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Layout.LayoutUtils;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Core.Utils;
using LightTextEditorPlus.Core.Utils.TextArrayPools;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 更新布局的上下文
/// </summary>
public class UpdateLayoutContext : ICharDataLayoutInfoSetter
{
    internal UpdateLayoutContext(LayoutManager layoutManager, ArrangingLayoutProvider arrangingLayoutProvider,
        UpdateLayoutConfiguration currentConfiguration)
    {
        LayoutManager = layoutManager;
        ArrangingLayoutProvider = arrangingLayoutProvider;
        CurrentConfiguration = currentConfiguration;

        IReadOnlyList<ParagraphData> paragraphList = TextEditor.DocumentManager.ParagraphManager.GetParagraphList();
        InternalParagraphList = paragraphList;
    }

    /// <summary>
    /// 段落列表
    /// </summary>
    public ReadOnlyParagraphList ParagraphList => TextEditor.ParagraphList;

    /// <summary>
    /// 内部使用的段落列表
    /// </summary>
    internal IReadOnlyList<ParagraphData> InternalParagraphList { get; }

    internal LayoutManager LayoutManager { get; }

    internal ArrangingLayoutProvider ArrangingLayoutProvider { get; }

    /// <summary>
    /// 当前的更新布局配置
    /// </summary>
    public UpdateLayoutConfiguration CurrentConfiguration { get; }

    /// <summary>
    /// 文本编辑器
    /// </summary>
    public TextEditorCore TextEditor => LayoutManager.TextEditor;

    /// <summary>
    /// 平台提供者
    /// </summary>
    public ITextEditorPlatformProvider PlatformProvider => TextEditor.PlatformProvider;

    /// <summary>
    /// 调试配置
    /// </summary>
    public TextEditorDebugConfiguration DebugConfiguration => TextEditor.DebugConfiguration;

    /// <summary>
    /// 是否在调试模式
    /// </summary>
    public bool IsInDebugMode => DebugConfiguration.IsInDebugMode;

    /// <summary>
    /// 日志
    /// </summary>
    public ITextLogger Logger => TextEditor.Logger;

    /// <summary>
    /// 记录布局过程的调试信息
    /// </summary>
    /// <param name="message"></param>
    /// <param name="category"></param>
    public void RecordDebugLayoutInfo([InterpolatedStringHandlerArgument("")] ref LayoutDebugMessageInterpolatedStringHandler message, LayoutDebugCategory category)
    {
        if (!message.IsInDebugMode)
        {
            return;
        }
        string formattedText = message.GetFormattedText();
        var layoutDebugMessage = new LayoutDebugMessage(category, formattedText);
#if DEBUG
        var padCount = category.ToLogPadCount();
        // 两个两个的空格
        padCount *= 2;

        var debugMessage = $"{"".PadLeft(padCount)}[{category}] {formattedText}";
        Debug.WriteLine(debugMessage);
#endif

        LayoutDebugMessageList ??= [];
        LayoutDebugMessageList.Add(layoutDebugMessage);
    }

    /// <summary>
    /// 获取布局调试信息列表
    /// </summary>
    /// <returns></returns>
    public IReadOnlyList<LayoutDebugMessage> GetLayoutDebugMessageList()
    {
        if (LayoutDebugMessageList is { } list)
        {
            return list;
        }

        return Array.Empty<LayoutDebugMessage>();
    }

    private List<LayoutDebugMessage>? LayoutDebugMessageList { get; set; }

    /// <summary>
    /// 当前布局是否已经完成
    /// </summary>
    public bool IsCurrentLayoutCompleted { get; private set; }

    internal void SetLayoutCompleted()
    {
        IsCurrentLayoutCompleted = true;
    }

    #region ICharDataLayoutInfoSetter

    /// <inheritdoc />
    public void SetLayoutStartPoint(CharData charData, TextPointInLineCoordinateSystem point)
    {
        if (IsCurrentLayoutCompleted)
        {
            throw new InvalidOperationException($"只有在布局过程才能设置 {nameof(charData)} 的布局属性");
        }

        charData.SetLayoutCharLineStartPoint(point);
    }

    /// <inheritdoc />
    public void SetCharDataInfo(CharData charData, in CharDataInfo charDataInfo)
    {
        if (IsCurrentLayoutCompleted)
        {
            throw new InvalidOperationException($"只有在布局过程才能设置 {nameof(charData)} 的布局属性");
        }

        charData.SetCharDataInfo(charDataInfo);
    }

    #endregion

    #region InterpolatedStringHandler

    /// <summary>
    /// 用于记录布局调试信息的字符串处理器
    /// </summary>
    [InterpolatedStringHandler]
    public readonly ref struct LayoutDebugMessageInterpolatedStringHandler
    {
        /// <summary>
        /// 创建用于记录布局调试信息的字符串处理器
        /// </summary>
        /// <param name="literalLength"></param>
        /// <param name="formattedCount"></param>
        /// <param name="context"></param>
        /// <param name="isEnable"></param>
        public LayoutDebugMessageInterpolatedStringHandler(int literalLength, int formattedCount, UpdateLayoutContext context, out bool isEnable)
        {
            bool isInDebugMode = context.IsInDebugMode;
            isEnable = isInDebugMode;
            if (isInDebugMode)
            {
                _stringBuilder = new StringBuilder();
            }
            IsInDebugMode = isInDebugMode;
        }

        internal bool IsInDebugMode { get; }

        /// <summary>
        /// 添加字面量
        /// </summary>
        /// <param name="s"></param>
        public void AppendLiteral(string s)
        {
            _stringBuilder?.Append(s);
        }

        /// <summary>
        /// 添加格式化字符串
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        public void AppendFormatted<T>(T value)
        {
            _stringBuilder?.Append(value);
        }

        /// <summary>
        /// 添加格式化字符串
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="format"></param>
        public void AppendFormatted<T>(T value, string format)
        {
            if (_stringBuilder is not null)
            {
                if (value is double d)
                {
                    _stringBuilder.Append(d.ToString(format));
                }
                else
                {
                    _stringBuilder.AppendFormat(format, value);
                }
            }
        }

        private readonly StringBuilder? _stringBuilder;

        internal string GetFormattedText() => _stringBuilder?.ToString() ?? string.Empty;
    }

    #endregion

    /// <inheritdoc />
    public override string? ToString()
    {
        if (LayoutDebugMessageList != null)
        {
            return LayoutDebugMessageList.Last().ToString();
        }

        return base.ToString();
    }

    #region 缓存

    // 这里存放一些布局过程中的缓存，用于提升性能，在布局完成后自动 GC 释放。减少太多的静态对象

    /// <summary>
    /// 布局过程的默认 CharObject 对象
    /// </summary>
    public ICharObject LayoutDefaultCharData { get => field ??= new SingleCharObject(TextContext.DefaultChar); private set; }

    #region 框架内的不安全的缓存对象

    /// <summary>
    /// 瞬时使用，在出外面的时候必当确保不再被外部引用，随时都被更改对象本身。字符使用的是 <see cref="LayoutDefaultCharData"/> 字符
    /// </summary>
    /// 由于这个机制本身就不安全，所以不会对外暴露，只在内部使用
    internal CharData GetTransientMeasureCharData(IReadOnlyRunProperty runProperty)
    {
        if (_transientCharData is null)
        {
            _transientCharData = new CharData(LayoutDefaultCharData, runProperty);
        }
        else
        {
            _transientCharData.DangerousChangeRunProperty(runProperty);
        }

        return _transientCharData;
    }

    private CharData? _transientCharData;

    /// <summary>
    /// 获取一个瞬时的字符数据列表。这个列表只包含一个元素，且元素是 <see cref="CharData"/> 对象，只有瞬时作用，不得存放起来
    /// </summary>
    /// <param name="charData"></param>
    /// <returns></returns>
    internal SingleObjectList<CharData> GetTransientSingleCharDataList(CharData charData)
    {
        if (_transientCharDataList is null)
        {
            _transientCharDataList = new SingleObjectList<CharData>(charData);
        }
        else
        {
            _transientCharDataList.CurrentObject = charData;
        }

        return _transientCharDataList;
    }

    private SingleObjectList<CharData>? _transientCharDataList;

    /// <summary>
    /// 获取一个瞬时的字符数据列表。这个列表只包含一个元素，且元素是使用了 <paramref name="runProperty"/> 属性的瞬时的 <see cref="CharData"/> 对象，只有瞬时作用，不得存放起来
    /// </summary>
    /// <returns></returns>
    internal SingleObjectList<CharData> GetTransientSingleCharDataList(IReadOnlyRunProperty runProperty)
    {
        CharData charData = GetTransientMeasureCharData(runProperty);
        return GetTransientSingleCharDataList(charData);
    }

    /// <summary>
    /// 获取空段的虚拟的字符数据列表。这个列表只包含一个元素，且元素是使用了 <paramref name="paragraphStartRunProperty"/> 属性的瞬时的 <see cref="CharData"/> 对象，只有瞬时作用，不得存放起来
    /// </summary>
    /// <param name="paragraphStartRunProperty"></param>
    /// <returns></returns>
    /// 只是为了让空段也假装有文本内容，方便进行测量动作
    internal SingleObjectList<CharData> GetEmptyParagraphSingleVirtualCharDataList(IReadOnlyRunProperty paragraphStartRunProperty) =>
        GetTransientSingleCharDataList(paragraphStartRunProperty);

    #endregion

    #endregion

    /// <summary>
    /// 租用一个数组空间。记得使用 using 释放哦。将从数组池进行租借数组空间
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="minimumLength"></param>
    /// <returns></returns>
    public TextPoolArrayContext<T> Rent<T>(int minimumLength) => TextArrayPool.Rent<T>(minimumLength);
}
