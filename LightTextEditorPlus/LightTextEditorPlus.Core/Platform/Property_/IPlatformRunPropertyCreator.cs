﻿using LightTextEditorPlus.Core.Document;

using System;

namespace LightTextEditorPlus.Core.Platform;

/// <summary>
/// 平台相关的字符属性创建器
/// </summary>
public interface IPlatformRunPropertyCreator
{
    /// <summary>
    /// 基于原有的只读属性创建出新的字符属性
    /// </summary>
    /// <returns></returns>
    IReadOnlyRunProperty BuildNewProperty(Action<IReadOnlyRunProperty> config,IReadOnlyRunProperty baseRunProperty);

    /// <summary>
    /// 获取默认的字符属性
    /// </summary>
    /// <returns></returns>
    /// 允许跨多个 TextEditor 使用
    IReadOnlyRunProperty GetDefaultRunProperty();
}

/// <summary>
/// 平台相关的文本字符属性创建器基类
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class PlatformRunPropertyCreatorBase<T> : IPlatformRunPropertyCreator
    where T : IReadOnlyRunProperty
{
    /// <inheritdoc />
    public IReadOnlyRunProperty BuildNewProperty(Action<IReadOnlyRunProperty> config, IReadOnlyRunProperty baseRunProperty)
    {
        return OnBuildNewProperty(config, (T) baseRunProperty);
    }

    /// <inheritdoc cref="BuildNewProperty"/>

    protected abstract T OnBuildNewProperty(Action<IReadOnlyRunProperty> config, T baseRunProperty);

    /// <inheritdoc />
    public IReadOnlyRunProperty GetDefaultRunProperty()
    {
        return OnGetDefaultRunProperty();
    }

    /// <inheritdoc cref="GetDefaultRunProperty"/>
    protected abstract T OnGetDefaultRunProperty();
}

/// <summary>
/// 默认的平台相关文本字符属性创建器
/// </summary>
public class DefaultPlatformRunPropertyCreator : PlatformRunPropertyCreatorBase<LayoutOnlyRunProperty>
{
    /// <inheritdoc />
    protected override LayoutOnlyRunProperty OnBuildNewProperty(Action<IReadOnlyRunProperty> config, LayoutOnlyRunProperty baseRunProperty)
    {
        var runProperty = new LayoutOnlyRunProperty(baseRunProperty);
        config(runProperty);
        return runProperty;
    }

    /// <inheritdoc />
    protected override LayoutOnlyRunProperty OnGetDefaultRunProperty()
    {
        return new LayoutOnlyRunProperty();
    }
}