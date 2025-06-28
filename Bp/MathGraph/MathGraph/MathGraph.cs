﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MathGraph.Serialization;

namespace MathGraph;

public class MathGraph<TElementInfo, TEdgeInfo> : ISerializableElement
{
    public MathGraph()
    {
        _elementList = [];
    }

    private readonly List<MathGraphElement<TElementInfo, TEdgeInfo>> _elementList;

    public IReadOnlyList<MathGraphElement<TElementInfo, TEdgeInfo>> ElementList => _elementList;

    public MathGraphElement<TElementInfo, TEdgeInfo> CreateAndAddElement(TElementInfo value, string? id = null)
    {
        var element = new MathGraphElement<TElementInfo, TEdgeInfo>(this, value, id);
        _elementList.Add(element);
        return element;
    }

    public MathGraphSerializer<TElementInfo, TEdgeInfo> GetSerializer(IDeserializationContext? deserializationContext = null) =>
        new MathGraphSerializer<TElementInfo, TEdgeInfo>(this, deserializationContext);

    /// <summary>
    /// 添加单向边，添加边的同时，会添加元素关系
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <param name="edgeInfo"></param>
    public void AddEdge(MathGraphElement<TElementInfo, TEdgeInfo> from, MathGraphElement<TElementInfo, TEdgeInfo> to,
        TEdgeInfo? edgeInfo = default)
    {
        from.AddOutElement(to);
        Debug.Assert(to.InElementList.Contains(from));

        if (edgeInfo != null)
        {
            var edge = new MathGraphUnidirectionalEdge<TElementInfo, TEdgeInfo>(from, to)
            {
                EdgeInfo = edgeInfo
            };
            from.AddEdge(edge);
        }
    }

    /// <summary>
    /// 添加双向边
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="edgeInfo"></param>
    public void AddBidirectionalEdge(MathGraphElement<TElementInfo, TEdgeInfo> a,
        MathGraphElement<TElementInfo, TEdgeInfo> b, TEdgeInfo? edgeInfo = default)
    {
        AddEdge(a, b);
        AddEdge(b, a);

        if (edgeInfo != null)
        {
            var edge = new MathGraphBidirectionalEdge<TElementInfo, TEdgeInfo>(a, b)
            {
                EdgeInfo = edgeInfo
            };
            a.AddEdge(edge);
            //b.AddEdge(edge);
        }
    }

    string ISerializableElement.Serialize()
    {
        var mathGraphSerializer = GetSerializer();
        return mathGraphSerializer.Serialize();
    }

    internal void StartDeserialize(int elementCount)
    {
        _elementList.Clear();
        EnsureElementCapacity(elementCount);
    }

    /// <summary>
    /// 序列化使用设置元素的大小
    /// </summary>
    /// <param name="capacity"></param>
    private void EnsureElementCapacity(int capacity)
    {
        _elementList.EnsureCapacity(capacity);
    }
}

static class MathGraphElementIdGenerator
{
    private static ulong _idCounter = 0;

    public static string GenerateId()
    {
        return Interlocked.Increment(ref _idCounter).ToString();
    }
}

/// <summary>
/// 表示图里的一个元素
/// </summary>
/// <typeparam name="TElementInfo">元素本身的类型</typeparam>
/// <typeparam name="TEdgeInfo">边的类型</typeparam>
public class MathGraphElement<TElementInfo, TEdgeInfo>
{
    /// <summary>
    /// 创建图里的一个元素
    /// </summary>
    /// <param name="mathGraph"></param>
    /// <param name="value"></param>
    /// <param name="id"></param>
    public MathGraphElement(MathGraph<TElementInfo, TEdgeInfo> mathGraph, TElementInfo value, string? id = null)
    {
        Value = value;
        MathGraph = mathGraph;

        if (id is null)
        {
            id = MathGraphElementIdGenerator.GenerateId();
        }

        Id = id;

        if (Value is IMathGraphElementSensitive<TElementInfo, TEdgeInfo> sensitive)
        {
            Debug.Assert(sensitive.MathGraphElement is null);
            sensitive.MathGraphElement = this;
        }
    }

    /// <summary>
    /// 元素所在的图
    /// </summary>
    public MathGraph<TElementInfo, TEdgeInfo> MathGraph { get; }

    public string Id { get; }

    /// <summary>
    /// 元素自己的数据
    /// </summary>
    public TElementInfo Value { get; }

    /// <summary>
    /// 出度的元素列表，包含所有从当前元素出发的边指向的元素。对于带双向边的元素来说，出度与入度是相同的
    /// </summary>
    public IReadOnlyList<MathGraphElement<TElementInfo, TEdgeInfo>> OutElementList => _outElementList;

    /// <summary>
    /// 入度的元素列表，包含所有指向当前元素的边的起点元素。对于带双向边的元素来说，出度与入度是相同的
    /// </summary>
    public IReadOnlyList<MathGraphElement<TElementInfo, TEdgeInfo>> InElementList => _inElementList;

    public IReadOnlyList<MathGraphEdge<TElementInfo, TEdgeInfo>> EdgeList => _edgeList;

    private readonly List<MathGraphElement<TElementInfo, TEdgeInfo>> _outElementList = [];
    private readonly List<MathGraphElement<TElementInfo, TEdgeInfo>> _inElementList = [];

    private readonly List<MathGraphEdge<TElementInfo, TEdgeInfo>> _edgeList = [];

    /// <summary>
    /// 添加边的关系，只加边关系，不加元素关系。如需加元素关系，调用 <see cref="MathGraph{TElementInfo, TEdgeInfo}.AddEdge"/> 方法，或调用 <see cref="AddInElement"/> 或 <see cref="AddOutElement"/> 方法
    /// </summary>
    /// <param name="edge"></param>
    public void AddEdge(MathGraphEdge<TElementInfo, TEdgeInfo> edge)
    {
        foreach (var mathGraphEdge in _edgeList)
        {
            if (ReferenceEquals(mathGraphEdge, edge))
            {
                return;
            }
        }

        edge.EnsureContain(this);

        _edgeList.Add(edge);

        var otherElement = edge.GetOtherElement(this);
#if DEBUG
        foreach (var mathGraphEdge in otherElement._edgeList)
        {
            if (ReferenceEquals(mathGraphEdge, edge))
            {
                Debug.Fail("边都是成对出现，在当前元素不存在的边，在边的对应元素必定也不存在");
            }
        }
#endif
        otherElement._edgeList.Add(edge);
    }

    public void AddOutElement(MathGraphElement<TElementInfo, TEdgeInfo> element)
    {
        EnsureSameMathGraph(element);
        if (_outElementList.Contains(element))
        {
            return;
        }

        _outElementList.Add(element);
        Debug.Assert(!element._inElementList.Contains(this));
        element._inElementList.Add(this);
    }

    public void RemoveOutElement(MathGraphElement<TElementInfo, TEdgeInfo> element)
    {
        EnsureSameMathGraph(element);
        if (!_outElementList.Contains(element))
        {
            return;
        }

        _outElementList.Remove(element);
        Debug.Assert(element._inElementList.Contains(this));
        element._inElementList.Remove(this);
    }

    public void AddInElement(MathGraphElement<TElementInfo, TEdgeInfo> element)
    {
        EnsureSameMathGraph(element);
        if (_inElementList.Contains(element))
        {
            return;
        }

        _inElementList.Add(element);
        Debug.Assert(!element._outElementList.Contains(this));
        element._outElementList.Add(this);
    }

    public void RemoveInElement(MathGraphElement<TElementInfo, TEdgeInfo> element)
    {
        EnsureSameMathGraph(element);
        if (!_inElementList.Contains(element))
        {
            return;
        }

        _inElementList.Remove(element);
        Debug.Assert(element._outElementList.Contains(this));
        element._outElementList.Remove(this);
    }

    public override string ToString()
    {
        // Out={string.Join(',', OutElementList.Select(t => $"(Value={t.Value};Id={t.Id})"))};
        // In={string.Join(',', InElementList.Select(t => $"(Value={t.Value};Id={t.Id})"))}
        return $"Value={Value} ; Id={Id};";
    }

    /// <summary>
    /// 确保两个元素在相同的一个图里面
    /// </summary>
    /// <param name="element"></param>
    /// <exception cref="InvalidOperationException"></exception>
    private void EnsureSameMathGraph(MathGraphElement<TElementInfo, TEdgeInfo> element)
    {
        if (!ReferenceEquals(MathGraph, element.MathGraph))
        {
            throw new InvalidOperationException();
        }
    }
}

/// <summary>
/// 单向边
/// </summary>
/// <typeparam name="TElementInfo"></typeparam>
/// <typeparam name="TEdgeInfo"></typeparam>
public class MathGraphUnidirectionalEdge<TElementInfo, TEdgeInfo> : MathGraphEdge<TElementInfo, TEdgeInfo>
{
    /// <summary>
    /// 创建单向边
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    public MathGraphUnidirectionalEdge(MathGraphElement<TElementInfo, TEdgeInfo> from,
        MathGraphElement<TElementInfo, TEdgeInfo> to) : base(from, to)
    {
    }

    /// <summary>
    /// 单向边的起点元素
    /// </summary>
    public MathGraphElement<TElementInfo, TEdgeInfo> From => base.A;

    /// <summary>
    /// 单向边的终点元素
    /// </summary>
    public MathGraphElement<TElementInfo, TEdgeInfo> To => base.B;

    public override string ToString()
    {
        return $"[{From}] -> [{To}]";
    }
}

/// <summary>
/// 双向边
/// </summary>
/// <typeparam name="TElementInfo"></typeparam>
/// <typeparam name="TEdgeInfo"></typeparam>
public class MathGraphBidirectionalEdge<TElementInfo, TEdgeInfo> : MathGraphEdge<TElementInfo, TEdgeInfo>
{
    public MathGraphBidirectionalEdge(MathGraphElement<TElementInfo, TEdgeInfo> a,
        MathGraphElement<TElementInfo, TEdgeInfo> b) : base(a, b)
    {
    }

    /// <summary>
    /// 双向边中的一个元素
    /// </summary>
    public MathGraphElement<TElementInfo, TEdgeInfo> AElement => base.A;

    /// <summary>
    /// 双向边中的另一个元素
    /// </summary>
    public MathGraphElement<TElementInfo, TEdgeInfo> BElement => base.B;

    public override string ToString()
    {
        return $"[{AElement}] <-> [{BElement}]";
    }
}

/// <summary>
/// 表示图的边，包含两个元素，A和B。A和B可以是同一个元素，但不能是null。
/// </summary>
/// <typeparam name="TElementInfo"></typeparam>
/// <typeparam name="TEdgeInfo"></typeparam>
public abstract class MathGraphEdge<TElementInfo, TEdgeInfo>
{
    protected MathGraphEdge(MathGraphElement<TElementInfo, TEdgeInfo> a, MathGraphElement<TElementInfo, TEdgeInfo> b)
    {
        A = a;
        B = b;
    }

    public TEdgeInfo? EdgeInfo { get; set; }

    /// <summary>
    /// 边的两端中的一个元素
    /// </summary>
    protected MathGraphElement<TElementInfo, TEdgeInfo> A { get; }

    /// <summary>
    /// 边的两端中的另一个元素
    /// </summary>
    protected MathGraphElement<TElementInfo, TEdgeInfo> B { get; }

    /// <summary>
    /// 确保元素在边的两端之一
    /// </summary>
    /// <param name="element"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public void EnsureContain(MathGraphElement<TElementInfo, TEdgeInfo> element)
    {
        if (!ReferenceEquals(element, A) && !ReferenceEquals(element, B))
        {
            throw new InvalidOperationException();
        }
    }

    /// <summary>
    /// 传入边的这一个元素，返回边的另一个元素
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException">如果传入的元素不属于边的两端两个元素中的一个</exception>
    public MathGraphElement<TElementInfo, TEdgeInfo> GetOtherElement(MathGraphElement<TElementInfo, TEdgeInfo> element)
    {
        if (ReferenceEquals(element, A))
        {
            return B;
        }

        if (ReferenceEquals(element, B))
        {
            return A;
        }

        throw new InvalidOperationException();
    }
}