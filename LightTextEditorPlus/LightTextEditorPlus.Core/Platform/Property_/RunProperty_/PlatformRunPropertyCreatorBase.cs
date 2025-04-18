using System;
using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Core.Platform;

/// <summary>
/// 平台相关的文本字符属性创建器基类
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class PlatformRunPropertyCreatorBase<T> : IPlatformRunPropertyCreator
    where T : IReadOnlyRunProperty
{
    ///// <inheritdoc />
    //public IReadOnlyRunProperty BuildNewProperty(Action<IReadOnlyRunProperty> config,
    //    IReadOnlyRunProperty baseRunProperty)
    //{
    //    return OnBuildNewProperty(config, (T)baseRunProperty);
    //}

    ///// <inheritdoc cref="BuildNewProperty"/>
    //protected abstract T OnBuildNewProperty(Action<IReadOnlyRunProperty> config, T baseRunProperty);

    /// <inheritdoc />
    public IReadOnlyRunProperty GetDefaultRunProperty()
    {
        return OnGetDefaultRunProperty();
    }

    /// <inheritdoc />
    public virtual IReadOnlyRunProperty ToPlatformRunProperty(ICharObject charObject, IReadOnlyRunProperty baseRunProperty)
    {
        if (baseRunProperty is not T)
        {
            throw new NotSupportedException($"传入了非当前平台所能支持的字符属性。当前平台属性类型： {typeof(T).FullName}；传入的字符属性类型：{baseRunProperty?.GetType().FullName ?? "<null>"}");
        }

        return baseRunProperty;
    }

    /// <inheritdoc cref="GetDefaultRunProperty"/>
    protected abstract T OnGetDefaultRunProperty();
}