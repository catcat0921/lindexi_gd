﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Platform;

public interface IPlatformProvider
{
    // 获取默认字体

    /// <summary>
    /// 加入调度更新布局请求
    /// </summary>
    /// 推荐处理：快速多次触发时，只触发一次，以及调度到合适的时机去执行
    /// <param name="textLayout"></param>
    void RequireDispatchUpdateLayout(Action textLayout);

    /// <summary>
    /// 创建文本日志
    /// </summary>
    /// <returns>可为空，为空采用空白日志</returns>
    ITextLogger? BuildTextLogger();
}

public abstract class PlatformProvider : IPlatformProvider
{
    public virtual void RequireDispatchUpdateLayout(Action textLayout)
    {
        textLayout();
    }

    public virtual ITextLogger? BuildTextLogger()
    {
        return null;
    }
}