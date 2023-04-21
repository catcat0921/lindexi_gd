﻿# 文本库

富文本布局库

支持替换平台渲染和平台测量层，可采用 WPF 或 MAUI 或其他基础框架作为基础平台层。可直接对接 WPF 或 MAUI 等 UI 框架。通过对接不同的基础平台可实现跨平台功能

进度：可当成简单的 TextBlock 使用

## 功能

- 平文本
- 加粗、斜体、下划线、上下标
- 字体、字号
- 文本颜色
- 左对齐、右对齐、分散对齐、两端对齐
- 分词换行
- 项目符号
- 左右缩进
- 段前段后距离
- 倍数行距、固定倍数行距
- 遵循中文符号换行规则
- 命中测试，属性和光标系统
- 文本公式混排，图文混排

## 架构

从整体的角度：

![](http://image.acmx.xyz/lindexi%2F202211916957655.jpg)

分层的角度：

![](http://image.acmx.xyz/lindexi%2F20221191610494994.jpg)

调用关系：

![](http://image.acmx.xyz/lindexi%2F20221191611114337.jpg)

依赖关系：

![](http://image.acmx.xyz/lindexi%2F20221191611321914.jpg)

数据走向：

![](http://image.acmx.xyz/lindexi%2F20221192012258129.jpg)

## 各个项目的作用

### LightTextEditorPlus.Core

文本库的平台无关实现，实现了文本的基础排版布局功能。提供给具体平台框架对接的接口，可以在不同的平台框架上，使用具体平台框架的文本渲染引擎提供具体的文本排版布局信息，以及在排版布局完成之后，对接具体平台的渲染

入口类型：TextEditorCore

### LightTextEditorPlus.Wpf

使用 WPF 框架承载的文本库，平台相关具体实现。底层使用 `LightTextEditorPlus.Core` 进行驱动，渲染层和 IME 输入法等使用 WPF 提供

### LightTextEditorPlus.MauiGraphics

使用 MAUI 框架承载的文本库，使用到 MAUI 的渲染层。仅提供渲染输出功能，不提供编辑功能。支持多平台渲染。底层核心对接是 SKIA 技术

## 文本状态

### 文本是脏的

默认创建出来的文本是脏的，需要布局完成之后，才不是脏的。在文本是脏的状态下，禁止获取文本布局相关信息

在文本进行任何编辑动作之后，文本也会标记为是脏的。等待文本布局完成之后，才不是脏的

## 调试机制

### 设置调试模式

可以调用 TextEditorCore 的 SetInDebugMode 方法，让单个文本对象进入调试模式。进入调试模式之后，将会有更多的输出信息，和可能抛出 TextEditorDebugException 调试异常

如期望对所有的文本都进入调试模式，可以调用 `TextEditorCore.SetAllInDebugMode` 静态方法

请不要在发布版本开启调试模式，开启调试模式之后，将会影响文本的性能

### 布局原因

通过 TextEditorCore 的 `_layoutUpdateReasonManager` 字段即可了解到框架内记录的触发布局的原因


## 行为定义

由于文本库在实现的时候，许多功能都需要选择其中某个方式，有些选择是冲突的，而且选择的方向本身将会影响整体的框架和具体的实现。关于文本库所选择的行为，详细请参阅 [行为定义.md](./行为定义.md)

## 相似的项目

https://github.com/toptensoftware/RichTextKit : Rich text rendering for SkiaSharp