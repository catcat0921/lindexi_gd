﻿using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Layout.LayoutUtils;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Core.Utils;
using LightTextEditorPlus.Core.Utils.Maths;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 实际的布局提供器
/// </summary>
/// 段落排版关系
/// ------------------------------------------
/// |段起始点
/// | // 段前间距 ParagraphBefore
/// | --------------------------------------
/// | |行起始点  
/// | |    // 行
/// | |         
/// | --------------------------------------
/// |
/// | // 段后间距 ParagraphAfter
/// -------------------------------------------
abstract class ArrangingLayoutProvider
{
    protected ArrangingLayoutProvider(LayoutManager layoutManager)
    {
        LayoutManager = layoutManager;
    }

    /// <summary>
    /// 布局方式
    /// </summary>
    public abstract ArrangingType ArrangingType { get; }

    /// <summary>
    /// 布局管理器
    /// </summary>
    public LayoutManager LayoutManager { get; }

    public TextEditorCore TextEditor => LayoutManager.TextEditor;

    /// <summary>
    /// 是否进入调试模式
    /// </summary>
    protected bool IsInDebugMode => TextEditor.IsInDebugMode;

    /// <summary>
    /// 更新布局
    /// </summary>
    /// <returns></returns>
    /// 入口方法
    public DocumentLayoutResult UpdateLayout(UpdateLayoutContext updateLayoutContext)
    {
        // 布局逻辑：
        // - 01 获取需要更新布局段落的逻辑
        // - 02 预布局阶段
        //   - 进入段落布局
        //     - 进入行布局
        // - 03 回溯最终布局阶段
        //   - 获取文档整个的布局信息
        //     - 获取文档的布局尺寸
        //   - 段落回溯布局
        //     - 行回溯布局
        //     - 水平对齐的右对齐、居中对齐等
        //     - 垂直对齐的下对齐、居中对齐等

        IReadOnlyList<ParagraphData> paragraphList = updateLayoutContext.InternalParagraphList;
        DebugAssert(paragraphList.Count > 0, "获取到的段落只有 0 段，即使空文本也会存在一段");

        bool shouldClearCharSize = updateLayoutContext.CurrentConfiguration.ShouldClearCharSizeForArrangingTypeChanged;
        if (shouldClearCharSize)
        {
            CharDataLayoutHelper.ClearAllCharDataInfo(paragraphList, updateLayoutContext);
        }

        // 01 获取需要更新布局段落的逻辑
        FirstDirtyParagraphInfo firstDirtyParagraphInfo = GetFirstDirtyParagraph(paragraphList, updateLayoutContext);

        // 02 预布局阶段
        PreUpdateDocumentLayoutResult preUpdateDocumentLayoutResult =
            PreUpdateDocumentLayout(paragraphList, updateLayoutContext, firstDirtyParagraphInfo);

        // 03 回溯最终布局阶段
        FinalUpdateDocumentLayoutResult finalUpdateDocumentLayoutResult = FinalUpdateDocumentLayout(preUpdateDocumentLayoutResult, updateLayoutContext);

        if (IsInDebugMode)
        {
            // 进入一些校验逻辑
            if (shouldClearCharSize && firstDirtyParagraphInfo.FirstDirtyParagraphIndex.Index != 0)
            {
                throw new TextEditorInnerDebugException($"要求清理全部字符信息时，必定是全量更新");
            }

            EnsureFinishLayoutCompletedInDebugMode();
        }

        // 这是多余的判断，仅仅用在 DEBUG 过程中，没开 IsInDebugMode 的逻辑
        Debug.Assert(TextEditor.DocumentManager.ParagraphManager.GetParagraphList()
            .All(t => t.IsDirty() == false));

        return new DocumentLayoutResult(finalUpdateDocumentLayoutResult.LayoutBounds, updateLayoutContext);
    }

    /// <summary>
    /// 调试模式下确保布局完成
    /// </summary>
    /// <exception cref="TextEditorInnerDebugException"></exception>
    private void EnsureFinishLayoutCompletedInDebugMode()
    {
        foreach (ParagraphData paragraphData in TextEditor.DocumentManager.ParagraphManager.GetParagraphList())
        {
            if (paragraphData.IsDirty())
            {
                throw new TextEditorInnerDebugException($"完成布局之后，段落还是脏的");
            }

            IParagraphLayoutData paragraphLayoutData = paragraphData.ParagraphLayoutData;
            if (paragraphLayoutData.StartPointInDocumentContentCoordinateSystem.IsInvalid)
            {
                throw new TextEditorInnerDebugException($"完成布局之后，没有更新段落的文本起始点布局信息");
            }
            if (paragraphLayoutData.TextSize == TextSize.Invalid)
            {
                throw new TextEditorInnerDebugException($"完成布局之后，没有更新段落的文本尺寸布局信息");
            }
            if (paragraphLayoutData.OutlineSize == TextSize.Invalid)
            {
                throw new TextEditorInnerDebugException($"完成布局之后，没有更新段落的文本外接尺寸布局信息");
            }
        }
    }

    #region 01 获取需要更新布局段落的逻辑

    /// <summary>
    /// 获取首个变脏的段落
    /// </summary>
    /// <param name="paragraphList"></param>
    /// <param name="updateLayoutContext"></param>
    private FirstDirtyParagraphInfo GetFirstDirtyParagraph(IReadOnlyList<ParagraphData> paragraphList,
        UpdateLayoutContext updateLayoutContext)
    {
        // todo 项目符号的段落，如果在段落上方新建段落，那需要项目符号更新
        // 这个逻辑准备给项目符号逻辑更新，逻辑是，假如现在有两段，分别采用 `1. 2.` 作为项目符号
        // 在 `1.` 后面新建一个段落，需要自动将原本的 `2.` 修改为 `3.` 的内容，这个逻辑准备交给项目符号模块自己编辑实现

        updateLayoutContext.RecordDebugLayoutInfo($"开始寻找首个变脏段落序号", LayoutDebugCategory.FindDirty);

        // 首行出现变脏的序号
        var firstDirtyParagraphIndex = -1;
        // 首个脏段的起始 也就是横排左上角的点。等于非脏段的下一个行起点
        var currentStartPoint = new TextPointInDocumentContentCoordinateSystem(0, 0, LayoutManager);
        for (var index = 0; index < paragraphList.Count; index++)
        {
            ParagraphData paragraphData = paragraphList[index];
            if (paragraphData.IsDirty())
            {
                firstDirtyParagraphIndex = index;
                break;
            }
            else
            {
                var paragraph = paragraphList[index];

                // 需要进行不同的排版计算。例如横排和竖排的规则不相同
                currentStartPoint = GetNextParagraphLineStartPoint(in currentStartPoint, paragraph);
            }
        }

        TextPointInDocumentContentCoordinateSystem firstStartPoint = currentStartPoint;
        if (firstDirtyParagraphIndex == -1)
        {
            throw new TextEditorInnerException($"进入布局时，没有任何一段需要布局");
        }

        DebugAssert(!firstStartPoint.IsInvalid, "必定能获取到起始点");

        updateLayoutContext.RecordDebugLayoutInfo(
            $"完成寻找首个变脏段落序号。首个变脏的段落序号是： {firstDirtyParagraphIndex}；首个脏段的起始点：{firstStartPoint}", LayoutDebugCategory.FindDirty);

        return new FirstDirtyParagraphInfo(new ParagraphIndex(firstDirtyParagraphIndex), firstStartPoint);
    }

    /// <summary>
    /// 获取首个变脏的段落的序号和起始点
    /// </summary>
    /// <param name="FirstDirtyParagraphIndex">首个出现变脏的序号</param>
    /// <param name="FirstStartPoint">首个脏段的起始 也就是横排左上角的点。等于非脏段的下一个行起点</param>
    private readonly record struct FirstDirtyParagraphInfo(
        ParagraphIndex FirstDirtyParagraphIndex,
        TextPointInDocumentContentCoordinateSystem FirstStartPoint);

    #endregion 获取需要更新布局段落的逻辑

    #region 02 预布局阶段

    /// <summary>
    /// 预布局文档
    /// </summary>
    /// <param name="paragraphList"></param>
    /// <param name="updateLayoutContext"></param>
    /// <param name="firstDirtyParagraphInfo"></param>
    /// <returns></returns>
    /// 等价于 BuildRenderData 阶段。只是文本库将布局和渲染分开了，所以这里只是获取布局信息
    private PreUpdateDocumentLayoutResult PreUpdateDocumentLayout(IReadOnlyList<ParagraphData> paragraphList,
        UpdateLayoutContext updateLayoutContext, in FirstDirtyParagraphInfo firstDirtyParagraphInfo)
    {
        updateLayoutContext.RecordDebugLayoutInfo($"PreUpdateDocumentLayout 进入预布局阶段", LayoutDebugCategory.PreDocument);

        // firstDirtyParagraphIndex - 首行出现变脏的序号
        // firstStartPoint - 首个脏段的起始 也就是横排左上角的点。等于非脏段的下一个行起点
        (ParagraphIndex firstDirtyParagraphIndex, TextPointInDocumentContentCoordinateSystem firstStartPoint) = firstDirtyParagraphInfo;

        // 进入段落内布局
        var currentStartPoint = firstStartPoint;
        for (var paragraphIndex = firstDirtyParagraphIndex.Index; paragraphIndex < paragraphList.Count; paragraphIndex++)
        {
            updateLayoutContext.RecordDebugLayoutInfo($"开始预布局第 {paragraphIndex} 段", LayoutDebugCategory.PreParagraphStart);
            ParagraphData paragraphData = paragraphList[paragraphIndex];

            ParagraphIndex index = new ParagraphIndex(paragraphIndex);

            // 在这里进行项目符号和缩进的计算
            ParagraphLayoutIndentInfo indentInfo = CalculateParagraphIndentAndMarker(new CalculateParagraphIndentArgument(paragraphData, index, paragraphList, updateLayoutContext));
            if (paragraphData.ParagraphLayoutData.IndentInfo != indentInfo)
            {
                // 如果设置前后存在差异，则表示当前段落应该重新布局，比如自动编号项目符号，从原本的 9. 变更为 10.，那么就需要重新布局
                paragraphData.SetDirtyByIndentChanged();
            }
            paragraphData.SetParagraphLayoutIndentInfo(in indentInfo);

            updateLayoutContext.RecordDebugLayoutInfo($"完成缩进计算", LayoutDebugCategory.PreIndent);

            var argument = new ParagraphLayoutArgument(index, currentStartPoint, paragraphData,
                paragraphList, indentInfo, updateLayoutContext);

            ParagraphLayoutResult result = UpdateParagraphLayout(argument);
            var nextParagraphStartPoint = result.NextParagraphStartPoint;
            // 预布局过程中，没有获取其 Outline 的值。于是将在无缓存时调用 OutlineBounds={paragraphData.ParagraphLayoutData.OutlineBounds}; 为 {X=0 Y=0 Width=0 Height=0} 的值
            updateLayoutContext.RecordDebugLayoutInfo($"完成预布局第 {paragraphIndex} 段，共 {paragraphData.LineLayoutDataList.Count} 行 StartPoint={paragraphData.ParagraphLayoutData.StartPointInDocumentContentCoordinateSystem};Size={paragraphData.ParagraphLayoutData.TextSize}; NextParagraphStartPoint={nextParagraphStartPoint}", LayoutDebugCategory.PreParagraph);

            currentStartPoint = nextParagraphStartPoint;

            // 以下是调试辅助代码，用于确保布局计算正确
            if (IsInDebugMode)
            {
                // 预期此时完成了起始点和文本尺寸的布局了，即已经有值了
                if (paragraphData.ParagraphLayoutData.TextSize == TextSize.Invalid)
                {
                    throw new TextEditorInnerDebugException($"完成预布局第 {paragraphIndex} 段之后，没有更新段落的文本尺寸布局信息");
                }

                if (paragraphData.ParagraphLayoutData.StartPointInDocumentContentCoordinateSystem.IsInvalid)
                {
                    throw new TextEditorInnerDebugException($"完成预布局第 {paragraphIndex} 段之后，没有更新段落的文本起始点布局信息");
                }

                var exceptedOffsetY = argument.GetParagraphBefore() +
                                      paragraphData.ParagraphLayoutData.TextSize.Height + argument.GetParagraphAfter();
                var excepted = argument.CurrentStartPoint.Offset(0, exceptedOffsetY);
                if (!nextParagraphStartPoint.NearlyEquals(excepted))
                {
                    // 预期下一个段落的起始点是当前段落的起始点 + 当前段落的段前间距 + 当前段落的文本高度 + 当前段落的段后间距
                    throw new TextEditorInnerDebugException($"预期下一个段落的起始点是 {excepted}，实际是 {nextParagraphStartPoint}");
                }
            }
        }

        //TextRect documentBounds = TextRect.Empty;
        //for (int i = 0; i < paragraphList.Count; i++)
        //{
        //    ParagraphData paragraphData = paragraphList[i];
        //    IParagraphLayoutData layoutData = paragraphData.ParagraphLayoutData;
        //    var bounds = layoutData.TextContentBounds;
        //    documentBounds = documentBounds.Union(bounds);

        //    if (IsInDebugMode && i == 0)
        //    {
        //        if (!layoutData.StartPointInDocumentContentCoordinateSystem.IsZero)
        //        {
        //            throw new TextEditorInnerDebugException("首段的坐标必然是 0,0 点");
        //        }
        //        else
        //        {
        //            // 正好首段是 0,0 点，因此最终计算出来的文档范围应该是内容范围，只需取尺寸即可
        //        }
        //    }
        //}

        TextSize documentContentSize = CalculateDocumentContentSize(paragraphList, updateLayoutContext);

        //Debug.Assert(documentBounds.Location == TextPoint.Zero);
        updateLayoutContext.RecordDebugLayoutInfo($"PreUpdateDocumentLayout 完成预布局阶段。段落数量： {paragraphList.Count}，文档尺寸：{documentContentSize.ToCommaSplitWidthAndHeight()}", LayoutDebugCategory.PreDocument);

        return new PreUpdateDocumentLayoutResult(documentContentSize);
    }

    #region 缩进和项目符号

    /// <summary>
    /// 计算缩进和项目符号
    /// </summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    /// UpdateMarker
    /// 项目符号计算分为两个部分：
    /// 1. 在 <see cref="ArrangingLayoutProvider.CalculateParagraphIndentAndMarker"/> 计算左右方向的缩进影响
    /// 2. 在 <see cref="HorizontalArrangingLayoutProvider.PreUpdateMarker"/> 计算左上角的起始点坐标
    protected ParagraphLayoutIndentInfo CalculateParagraphIndentAndMarker(CalculateParagraphIndentArgument argument)
    {
        argument.UpdateLayoutContext.RecordDebugLayoutInfo($"开始缩进计算", LayoutDebugCategory.PreIndent);
        ParagraphData paragraphData = argument.CurrentParagraphData;
        ParagraphProperty paragraphProperty = paragraphData.ParagraphProperty;

        double markerIndentation = 0;

        MarkerRuntimeInfo? info = paragraphData.MarkerRuntimeInfo;

        if (info != null)
        {
            argument.UpdateLayoutContext.RecordDebugLayoutInfo($"段落存在项目符号，测量项目符号缩进", LayoutDebugCategory.PreMarkerIndent);
            markerIndentation = MeasureMarkerIndentation(info);
        }

        double lineMaxWidth = GetLineMaxWidth();

        var indentInfo = new ParagraphLayoutIndentInfo
        {
            LineMaxWidth = lineMaxWidth,
            Indent = paragraphProperty.Indent,
            IndentType = paragraphProperty.IndentType,
            LeftIndentation = paragraphProperty.LeftIndentation,
            RightIndentation = paragraphProperty.RightIndentation,
            MarkerIndentation = markerIndentation,
        };

        return indentInfo;

        double MeasureMarkerIndentation(MarkerRuntimeInfo markerRuntimeInfo)
        {
            if (!paragraphData.IsDirty())
            {
                argument.UpdateLayoutContext.RecordDebugLayoutInfo($"段落非脏，返回缓存值", LayoutDebugCategory.PreMarkerIndent);

                return markerRuntimeInfo.MarkerIndentation;
            }

            TextReadOnlyListSpan<CharData> charDataList = markerRuntimeInfo.CharDataList;
            DebugAssert(charDataList.Count > 0, "能够有项目符号运行时数据时，必定存在字符列表");

            for (var i = 0; i < charDataList.Count; i++)
            {
                // 循环进行字符测量。在 MeasureAndFillSizeOfRun 方法里面，不会测量整个字符列表，只会测量连续的部分
                CharData charData = charDataList[i];
                if (charData.IsInvalidCharDataInfo)
                {
                    TextReadOnlyListSpan<CharData> toMeasureCharDataList = charDataList.Slice(i);

                    var fillSizeOfRunArgument = new FillSizeOfRunArgument(toMeasureCharDataList, argument.UpdateLayoutContext);
                    MeasureAndFillSizeOfRun(fillSizeOfRunArgument);
                }
            }

            foreach (CharData charData in charDataList)
            {
                markerIndentation += charData.Size.Width;

                if (IsInDebugMode)
                {
                    charData.IsSetStartPointInDebugMode = false;
                }
            }

            if (markerRuntimeInfo.TextMarker.MinimumIndent != null)
            {
                markerIndentation = Math.Max(markerIndentation, markerRuntimeInfo.TextMarker.MinimumIndent.Value);
            }

            markerRuntimeInfo.MarkerIndentation = markerIndentation;
            return markerIndentation;
        }
    }

    #endregion

    /// <summary>
    /// 计算文档的内容尺寸
    /// </summary>
    /// <param name="paragraphList"></param>
    /// <param name="updateLayoutContext"></param>
    /// <returns></returns>
    protected abstract TextSize CalculateDocumentContentSize(IReadOnlyList<ParagraphData> paragraphList,
        UpdateLayoutContext updateLayoutContext);

    /// <summary>
    /// 预布局文档的结果
    /// </summary>
    protected readonly record struct PreUpdateDocumentLayoutResult(TextSize DocumentContentSize);

    /// <summary>
    /// 段落内布局
    /// </summary>
    private ParagraphLayoutResult UpdateParagraphLayout(in ParagraphLayoutArgument argument)
    {
        // 如果段落本身是没有脏的，可能是当前段落的前面段落变更，导致需要更新段落的左上角坐标点而已
        // 这里执行快速的短路代码，提升性能
        UpdateLayoutContext context = argument.UpdateLayoutContext;
        if (!argument.ParagraphData.IsDirty())
        {
            context.RecordDebugLayoutInfo($"段落本身没有脏，进入快速分支，只需更新段落起始点坐标", LayoutDebugCategory.PreParagraph);
            return UpdateNotDirtyParagraphStartPoint(argument);
        }
        else
        {
            // 继续执行段落内布局
        }

        context.RecordDebugLayoutInfo($"第 {argument.ParagraphIndex.Index} 段，段落是脏的，执行段落内布局", LayoutDebugCategory.PreParagraph);
        argument.ParagraphData.SetLayoutDirty(exceptTextSize: false/*应该是连文本尺寸都是脏的*/);

        // 先找到首个需要更新的坐标点，这里的坐标是段坐标
        var dirtyParagraphOffset = 0;
        // 首个是脏的行的序号
        var lastIndex = -1;
        var paragraph = argument.ParagraphData;
        for (var index = 0; index < paragraph.LineLayoutDataList.Count; index++)
        {
            LineLayoutData lineLayoutData = paragraph.LineLayoutDataList[index];
            if (lineLayoutData.IsDirty == false)
            {
                dirtyParagraphOffset += lineLayoutData.CharCount;
            }
            else
            {
                lastIndex = index;
                break;
            }
        }

        context.RecordDebugLayoutInfo($"段内第 {lastIndex} 行是脏的，从此行开始布局", LayoutDebugCategory.PreParagraph);

        // 将脏的行移除掉，然后重新添加新的行
        // 例如在一段里面，首行就是脏的，那么此时应该就是从 0 开始，将后续所有行都移除掉
        // 例如在一段里面，有三行，首行不是脏的，第一行是脏的，那就需要删除第一行和第二行
        if (lastIndex == 0)
        {
            // 一段的首行是脏的，将后续全部删掉
            foreach (var lineLayoutData in paragraph.LineLayoutDataList)
            {
                lineLayoutData.Dispose();
            }

            paragraph.ClearLineLayoutDataList();
        }
        else if (lastIndex > 0)
        {
            for (var i = lastIndex; i < paragraph.LineLayoutDataList.Count; i++)
            {
                var lineVisualData = paragraph.LineLayoutDataList[i];
                lineVisualData.Dispose();
            }

            paragraph.RemoveLineLayoutDataRange(lastIndex, paragraph.LineLayoutDataList.Count - lastIndex);
        }
        else
        {
            // 这一段一个脏的行都没有。那可能是直接在空段追加，或者是首次布局
            Debug.Assert(paragraph.LineLayoutDataList.Count == 0);
        }

        // 不需要通过如此复杂的逻辑获取有哪些，因为存在的坑在于后续分拆 IImmutableRun 逻辑将会复杂
        //paragraph.GetRunRange(dirtyParagraphOffset);

        var startParagraphOffset = new ParagraphCharOffset(dirtyParagraphOffset);

        var result = UpdateParagraphLayoutCore(argument, startParagraphOffset);

#if DEBUG
        // 排版的结果如何？通过段落里面的每一行的信息，可以了解
        var lineVisualDataList = paragraph.LineLayoutDataList;
        foreach (var lineLayoutData in lineVisualDataList)
        {
            // 每一行有多少个字符，字符的坐标
            var charList = lineLayoutData.GetCharList();
            foreach (var charData in charList)
            {
                // 字符的坐标是多少
                var startPoint = charData.CharLayoutData?.CharLineStartPoint;
                _ = startPoint;
            }
        }
#endif

        return result;
    }

    /// <summary>
    /// 排版和测量布局段落，处理段落内布局
    /// </summary>
    /// 这是一段一段进行排版和测量布局
    /// <param name="paragraph"></param>
    /// <param name="startParagraphOffset"></param>
    /// <returns></returns>
    protected abstract ParagraphLayoutResult UpdateParagraphLayoutCore(in ParagraphLayoutArgument paragraph,
        in ParagraphCharOffset startParagraphOffset);

    /// <summary>
    /// 更新段落的左上角坐标
    /// </summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    protected abstract ParagraphLayoutResult UpdateNotDirtyParagraphStartPoint(in ParagraphLayoutArgument argument);

    /// <summary>
    /// 测量空段高度。空段的文本行高度包括行距，不包括段前和段后间距
    /// </summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    protected EmptyParagraphLineHeightMeasureResult MeasureEmptyParagraphLineHeight(
        in EmptyParagraphLineHeightMeasureArgument argument)
    {
        argument.UpdateLayoutContext.RecordDebugLayoutInfo($"第 {argument.ParagraphIndex.Index} 段，空段布局",
            LayoutDebugCategory.PreParagraph);

        var emptyParagraphLineHeightMeasurer = TextEditor.PlatformProvider.GetEmptyParagraphLineHeightMeasurer();
        if (emptyParagraphLineHeightMeasurer != null)
        {
            return emptyParagraphLineHeightMeasurer.MeasureEmptyParagraphLineHeight(argument);
        }
        else
        {
            var paragraphProperty = argument.ParagraphProperty;

            var runProperty = argument.ParagraphStartRunProperty;

            var lineSpacingCalculateArgument =
                new LineSpacingCalculateArgument(argument.ParagraphIndex, 0, paragraphProperty, runProperty);
            var lineSpacingCalculateResult = CalculateLineSpacing(lineSpacingCalculateArgument);
            double lineHeight = lineSpacingCalculateResult.TotalLineHeight;
            if (lineSpacingCalculateResult.ShouldUseCharLineHeight)
            {
                // 如果需要使用文本高度，那么进行测量空行文本
                var size = MeasureEmptyParagraphLineSize(runProperty, argument.UpdateLayoutContext);

                lineHeight = size.Height;
            }

            return new EmptyParagraphLineHeightMeasureResult(lineHeight);
        }
    }

    /// <summary>
    /// 测量使用 <paramref name="runProperty"/> 的空段文本行的字符高度
    /// </summary>
    /// <param name="runProperty"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    private TextSize MeasureEmptyParagraphLineSize(IReadOnlyRunProperty runProperty, UpdateLayoutContext context)
    {
        context.RecordDebugLayoutInfo($"空行布局", LayoutDebugCategory.PreWholeLine);
        CharDataInfo charDataInfo = MeasureEmptyParagraphCharDataInfo(runProperty, context);
        return charDataInfo.FrameSize;
    }

    /// <summary>
    /// 测量空段的字符数据
    /// </summary>
    /// <returns></returns>
    protected CharDataInfo MeasureEmptyParagraphCharDataInfo(IReadOnlyRunProperty paragraphStartRunProperty, UpdateLayoutContext context)
    {
        SingleObjectList<CharData> virtualCharDataList = context.GetEmptyParagraphSingleVirtualCharDataList(paragraphStartRunProperty);
        var listSpan = virtualCharDataList.ToListSpan();
        CharData virtualCharData = virtualCharDataList.CurrentObject;

        MeasureAndFillSizeOfRun(new FillSizeOfRunArgument(listSpan, context));
        Debug.Assert(!virtualCharData.IsInvalidCharDataInfo);
        // 尽管 virtualCharData 是瞬时的，但是从中取出 CharDataInfo 结构体是安全的
        return virtualCharData.CharDataInfo;
    }

    /// <summary>
    /// 获取下一段的首行起始点
    /// </summary>
    /// <param name="currentPoint"></param>
    /// <param name="paragraphData"></param>
    /// <returns></returns>
    /// 对于横排来说，是往下排。对于竖排来说，也许是往左也许是往右排
    protected abstract TextPointInDocumentContentCoordinateSystem GetNextParagraphLineStartPoint(in TextPointInDocumentContentCoordinateSystem currentPoint, ParagraphData paragraphData);

    #region 行距

    /// <summary>
    /// 计算行距
    /// </summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    protected LineSpacingCalculateResult CalculateLineSpacing(in LineSpacingCalculateArgument argument)
    {
        ILineSpacingCalculator? lineSpacingCalculator = TextEditor.PlatformProvider.GetLineSpacingCalculator();

        if (lineSpacingCalculator != null)
        {
            return lineSpacingCalculator.CalculateLineSpacing(argument);
        }

        // 没有注入平台相关的行距计算器的情况下，以下是默认逻辑

        ParagraphProperty paragraphProperty = argument.ParagraphProperty;
        ITextLineSpacing textLineSpacing = paragraphProperty.LineSpacing;

        double lineHeight;
        if (textLineSpacing is MultipleTextLineSpace multipleTextLineSpace)
        {
            // 倍数行距逻辑
            var lineSpacing = multipleTextLineSpace.LineSpacing;

            var needNotCalculateLineSpacing =
                // 处理首行不展开，文档的首段首行不加上行距
                // 也就是不需要处理 lineHeight 的值
                TextEditor.LineSpacingStrategy == LineSpacingStrategy.FirstLineShrink
                && argument.ParagraphIndex == 0
                && argument.LineIndex == 0;

            if (needNotCalculateLineSpacing)
            {
                // 如果不需要计算行距，那就随意了
                return new LineSpacingCalculateResult(true, double.NaN, LineSpacing: 0);
            }
            else
            {
                lineHeight =
                    LineSpacingCalculator.CalculateLineHeightWithLineSpacing(TextEditor,
                        argument.MaxFontSizeCharRunProperty,
                        lineSpacing);
            }
        }
        else if (textLineSpacing is ExactlyTextLineSpace exactlyTextLineSpace)
        {
            // 如果定义了固定行距，那就使用固定行距
            lineHeight = exactlyTextLineSpace.ExactlyLineHeight;
        }
        else
        {
            throw new NotSupportedException(
                $"传入的行距为 {textLineSpacing?.GetType()} 类型，无法在文本框框架内处理。可重写 {nameof(ILineSpacingCalculator)} 处理器自行处理此行距类型");
        }

        return new LineSpacingCalculateResult(ShouldUseCharLineHeight: false, lineHeight, lineHeight);
    }

    #endregion

    #endregion 02 预布局阶段

    #region 03 回溯最终布局阶段

    /// <summary>
    /// 回溯文档布局排版。例如右对齐、居中对齐等
    /// </summary>
    /// Rewind Polished Backtrack Document Layout 回溯也是抛光的过程，抛光是指对文档的最后一次布局调整
    protected abstract FinalUpdateDocumentLayoutResult FinalUpdateDocumentLayout(PreUpdateDocumentLayoutResult preUpdateDocumentLayoutResult,
        UpdateLayoutContext updateLayoutContext);

    /// <summary>
    /// 回溯文档布局排版结果
    /// </summary>
    /// <param name="LayoutBounds"></param>
    protected readonly record struct FinalUpdateDocumentLayoutResult(DocumentLayoutBoundsInHorizontalArrangingCoordinateSystem LayoutBounds);

    #endregion 03 回溯最终布局阶段

    #region 通用辅助方法

    /// <summary>
    /// 测量字符信息。确保 argument.CurrentCharData.Size 一定不为空
    /// </summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    protected void MeasureAndFillSizeOfRun(in FillSizeOfRunArgument argument)
    {
        // 通过平台提供者获取字符信息测量器
        ICharInfoMeasurer? charInfoMeasurer = TextEditor.PlatformProvider.GetCharInfoMeasurer();
        if (charInfoMeasurer != null)
        {
            charInfoMeasurer.MeasureAndFillSizeOfRun(argument);

            if (IsInDebugMode)
            {
                if (argument.CurrentCharData.IsInvalidCharDataInfo)
                {
                    throw new TextEditorDebugException($"测量布局之后，当前字符依然没有尺寸");
                }
            }
        }
        else
        {
            if (!argument.CurrentCharData.IsInvalidCharDataInfo)
            {
                // 如果字符信息有效，则无须继续测量
                return;
            }

            // 默认的字符信息测量器
            MeasureAndFillCharInfo(argument.CurrentCharData, argument.CharDataLayoutInfoSetter);
        }
    }

    /// <summary>
    /// 通用的测量字符信息的方法，直接就是设置宽度高度为字号大小
    /// </summary>
    /// <param name="charData"></param>
    /// <param name="setter"></param>
    /// <returns></returns>
    private static void MeasureAndFillCharInfo(CharData charData, ICharDataLayoutInfoSetter setter)
    {
        double fontSize = charData.RunProperty.FontSize;
        // 默认是方块字符
        var size = new TextSize(fontSize, fontSize);
        // 设置基线为字号大小的向上一点点
        const double
            testBaselineRatio = 4d / 5; // 这是一个测试值，确保无 UI 框架下，都采用相同的基线值，方便调试计算。这个值是如何获取的？通过在 PPT 里面进行测量微软雅黑字体的基线的
        double baseline = fontSize * testBaselineRatio;

        // 字外框。文字外框，字外框尺寸
        TextSize textFrameSize = size;
        // 字面尺寸，字墨尺寸，字墨大小。文字的字身框中，字图实际分布的空间的尺寸
        TextSize textFaceSize = size;

        setter.SetCharDataInfo(charData, new CharDataInfo(textFrameSize, textFaceSize, baseline));
    }

    ///// <summary>
    ///// 获取文档的命中范围
    ///// </summary>
    ///// <returns></returns>
    //public TextRect GetDocumentHitBounds()
    //{
    //    var documentBounds = LayoutManager.DocumentLayoutBounds.DocumentBounds;
    //    return CalculateHitBounds(in documentBounds);
    //}

    ///// <summary>
    ///// 根据传入的文档范围计算命中范围
    ///// </summary>
    ///// <param name="documentBounds"></param>
    ///// <returns></returns>
    //protected abstract TextRect CalculateHitBounds(in TextRect documentBounds);

    protected abstract double GetLineMaxWidth();

    /// <summary>
    /// 调试断言，只有在调试模式下才会执行。只有一些不影响性能的简单判断才能调用此方法。其他可能有性能问题的，依然使用 <see cref="Debug.Assert(bool,string)"/> 方法
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="message"></param>
    protected void DebugAssert(bool condition, string message)
    {
        if (IsInDebugMode)
        {
            TextEditorInnerDebugAsset.Assert(condition, message);
        }
    }

    #endregion 通用辅助方法
}
