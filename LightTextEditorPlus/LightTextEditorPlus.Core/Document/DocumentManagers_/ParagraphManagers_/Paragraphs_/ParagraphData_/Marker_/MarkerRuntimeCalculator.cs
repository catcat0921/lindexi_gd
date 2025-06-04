﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Platform;

namespace LightTextEditorPlus.Core.Document;

static class MarkerRuntimeCalculator
{
    /// <summary>
    /// 更新段落的项目符号信息
    /// </summary>
    /// <param name="paragraphList"></param>
    /// <param name="textEditor"></param>
    public static void UpdateParagraphMarkerRuntimeInfo(IReadOnlyList<ParagraphData> paragraphList, TextEditorCore textEditor)
    {
        textEditor.Logger.LogDebug($"[TextEditorCore][MarkerRuntimeCalculator] 开始更新段落的项目符号信息");
        if (paragraphList.All(t => t.ParagraphProperty.Marker is null))
        {
            textEditor.Logger.LogDebug($"[TextEditorCore][MarkerRuntimeCalculator] 完成更新段落的项目符号信息。未找到任何一段设置了项目符号，直接跳过");

            // 短路代码，没有任何一个项目符号的情况
            return;
        }

        IPlatformRunPropertyCreator platformRunPropertyCreator = textEditor.PlatformProvider.GetPlatformRunPropertyCreator();

        foreach (MarkerTextInfo markerTextInfo in GetMarkerTextInfoList(paragraphList))
        {
            ParagraphData paragraphData = markerTextInfo.ParagraphData;
            if (markerTextInfo.MarkerText is null)
            {
                continue;
            }

            ParagraphProperty paragraphProperty = paragraphData.ParagraphProperty;

            if (paragraphProperty.Marker is null)
            {
                Debug.Fail("如果能拿到 MarkerText 则 ParagraphProperty.Marker 必定存在");
                continue;
            }

            var marker = paragraphProperty.Marker;

            IReadOnlyRunProperty? markerRunProperty = marker.RunProperty;

            if (markerRunProperty is null || marker.ShouldFollowParagraphFirstCharRunProperty)
            {
                // 没有字符属性、或需要跟随段落首个字符的字符属性，则取段落首个字符的属性作为样式进行更新

                IReadOnlyRunProperty styleRunProperty;
                if (paragraphData.IsEmptyParagraph)
                {
                    styleRunProperty = paragraphData.ParagraphStartRunProperty;
                }
                else
                {
                    Debug.Assert(paragraphData.CharCount > 0);
                    CharData firstCharData = paragraphData.GetCharData(new ParagraphCharOffset(0));
                    styleRunProperty = firstCharData.RunProperty;
                }

                markerRunProperty = platformRunPropertyCreator.UpdateMarkerRunProperty(markerRunProperty, styleRunProperty);
            }

            if (paragraphData.MarkerRuntimeInfo is { } markerRuntimeInfo)
            {
                if (string.Equals(markerRuntimeInfo.Text, markerTextInfo.MarkerText, StringComparison.InvariantCulture) && markerRuntimeInfo.RunProperty.Equals(markerRunProperty))
                {
                    // 没有改变
                    continue;
                }
                else
                {
                    // 改变了，继续更新
                }
            }

            paragraphData.MarkerRuntimeInfo = new MarkerRuntimeInfo(markerTextInfo.MarkerText, markerRunProperty, marker, paragraphData);
        }

        textEditor.Logger.LogDebug($"[TextEditorCore][MarkerRuntimeCalculator] 完成更新段落的项目符号信息");
    }

    public readonly record struct MarkerTextInfo(string? MarkerText, ParagraphData ParagraphData);

    public static List<MarkerTextInfo> GetMarkerTextInfoList(IReadOnlyList<ParagraphData> paragraphList)
    {
        var list = new List<MarkerTextInfo>(paragraphList.Count);

        var dictionary = new Dictionary<NumberMarkerGroupId, uint>();

        for (var i = 0; i < paragraphList.Count; i++)
        {
            ParagraphData paragraphData = paragraphList[i];
            ParagraphProperty paragraphProperty = paragraphData.ParagraphProperty;
            string? markerText = null;
            if (paragraphProperty.Marker is { } marker)
            {
                if (marker is BulletMarker bulletMarker)
                {
                    // 无序项目符号
                    markerText = bulletMarker.MarkerText;
                }
                else if (marker is NumberMarker numberMarker)
                {
                    // 有序项目符号
                    if (!dictionary.TryGetValue(numberMarker.GroupId,out var currentIndex))
                    {
                        currentIndex = numberMarker.StartAt;
                    }
                    else
                    {
                        // 如果上一段是空段，则不增加编号，保持 currentIndex 不变
                        var isLastParagraphEmpty = i>0 && paragraphList[i - 1].IsEmptyParagraph;

                        if (isLastParagraphEmpty)
                        {
                            // 如果是空段落，则不增加编号，保持 currentIndex 不变
                            // 忽略 CS1717 建议，忽略自己等于自己的警告，这里就是要明确这么写
#pragma warning disable CS1717 // Assignment made to same variable
                            currentIndex = currentIndex;
#pragma warning restore CS1717 // Assignment made to same variable
                        }
                        else
                        {
                            currentIndex++;
                        }
                    }

                    dictionary[numberMarker.GroupId] = currentIndex;

                    markerText = numberMarker.GetMarkerText(currentIndex);
                }
            }

            list.Add(new MarkerTextInfo(markerText, paragraphData));
        }

        return list;
    }
}