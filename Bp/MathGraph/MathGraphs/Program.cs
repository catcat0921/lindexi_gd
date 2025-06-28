﻿using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using MathGraphs.Serialization;

namespace MathGraphs;

internal class Program
{
    private static void Main(string[] args)
    {
        AddSuperPoint();

        SerializeEdge();

        SerializeLink();

        AddBidirectionalEdge();

        AddEdge();

        Serialize();
    }

    /// <summary>
    /// 添加超点
    /// </summary>
    private static void AddSuperPoint()
    {
        // 对于超点来说，就是点里面包含其他的点，点就是图
        var mathGraph = new MathGraph<MathGraph<string, string>, int>();
        var ap = new MathGraph<string, string>();
        ap.CreateAndAddElement("aaa");
        var bp = new MathGraph<string, string>();
        bp.CreateAndAddElement("bbb");
        mathGraph.CreateAndAddElement(ap);
        mathGraph.CreateAndAddElement(bp);

        var mathGraphSerializer = mathGraph.GetSerializer();
        var json = mathGraphSerializer.Serialize();
        Equals(mathGraph, Deserialize<MathGraph<string, string>, int>(json, new SuperPointDeserializationContext()));
    }

    class SuperPointDeserializationContext : IDeserializationContext
    {
        public bool TryDeserialize(string value, string? type, [NotNullWhen(true)] out object? result)
        {
            result = null;

            if (type == typeof(MathGraph<string, string>).FullName)
            {
                var mathGraph = new MathGraph<string, string>();
                var mathGraphSerializer = mathGraph.GetSerializer(this);
                mathGraphSerializer.Deserialize(value);
                result = mathGraph;
                return true;
            }

            return false;
        }
    }

    private static void SerializeEdge()
    {
        var mathGraph = new MathGraph<string, string>();
        var a = mathGraph.CreateAndAddElement("a");
        var b = mathGraph.CreateAndAddElement("b");
        var c = mathGraph.CreateAndAddElement("c");

        mathGraph.AddEdge(a, b, "ab");
        mathGraph.AddEdge(b, c, "bc");

        Debug.Assert(a.OutElementList[0] == b);
        Debug.Assert(b.OutElementList[0] == c);
        Debug.Assert(c.InElementList[0] == b);
        Debug.Assert(b.InElementList[0] == a);

        SerializeDeserialize(mathGraph);
    }

    private static void SerializeLink()
    {
        var mathGraph = new MathGraph<string, string>();
        var a = mathGraph.CreateAndAddElement("a");
        var b = mathGraph.CreateAndAddElement("b");
        var c = mathGraph.CreateAndAddElement("c");

        mathGraph.AddEdge(a, b);
        mathGraph.AddEdge(b, c);
        Debug.Assert(a.OutElementList[0] == b);
        Debug.Assert(b.OutElementList[0] == c);
        Debug.Assert(c.InElementList[0] == b);
        Debug.Assert(b.InElementList[0] == a);
        SerializeDeserialize(mathGraph);
    }

    private static void AddBidirectionalEdge()
    {
        var mathGraph = new MathGraph<string, string>();
        var a = mathGraph.CreateAndAddElement("a");
        var b = mathGraph.CreateAndAddElement("b");

        mathGraph.AddBidirectionalEdge(a, b);

        Debug.Assert(a.OutElementList.Contains(b));
        Debug.Assert(b.InElementList.Contains(a));
        Debug.Assert(a.InElementList.Contains(b));
        Debug.Assert(b.OutElementList.Contains(a));
        SerializeDeserialize(mathGraph);
    }

    private static void AddEdge()
    {
        var mathGraph = new MathGraph<string, string>();
        var a = mathGraph.CreateAndAddElement("a");
        var b = mathGraph.CreateAndAddElement("b");

        mathGraph.AddEdge(a, b);

        Debug.Assert(a.OutElementList.Contains(b));
        Debug.Assert(b.InElementList.Contains(a));

        SerializeDeserialize(mathGraph);
    }

    private static void Serialize()
    {
        var mathGraph = new MathGraph<int, string>();

        List<MathGraphElement<int, string>> elementList = [];

        for (int i = 0; i < 10; i++)
        {
            var mathGraphElement = mathGraph.CreateAndAddElement(i);
            elementList.Add(mathGraphElement);
        }

        for (int i = 0; i < 10; i++)
        {
            var mathGraphElement = elementList[i];
            for (int j = 0; j < 10; j++)
            {
                if (i != j)
                {
                    mathGraphElement.AddInElement(elementList[j]);
                }
            }
        }

        SerializeDeserialize(mathGraph);
    }

    private static void SerializeDeserialize<TElementInfo, TEdgeInfo>(MathGraph<TElementInfo, TEdgeInfo> mathGraph)
    {
        var mathGraphSerializer = mathGraph.GetSerializer();
        var json = mathGraphSerializer.Serialize();
        Equals(mathGraph, Deserialize<TElementInfo, TEdgeInfo>(json));
    }

    private static MathGraph<TElementInfo, TEdgeInfo> Deserialize<TElementInfo, TEdgeInfo>(string json, IDeserializationContext? deserializationContext = null)
    {
        var mathGraph = new MathGraph<TElementInfo, TEdgeInfo>();
        var mathGraphSerializer = mathGraph.GetSerializer(deserializationContext);
        mathGraphSerializer.Deserialize(json);
        return mathGraph;
    }

    private static void Equals<TElementInfo, TEdgeInfo>(MathGraph<TElementInfo, TEdgeInfo> a, MathGraph<TElementInfo, TEdgeInfo> b)
    {
        Debug.Assert(a.ElementList.Count == b.ElementList.Count);
        for (var i = 0; i < a.ElementList.Count; i++)
        {
            var elementA = a.ElementList[i];
            var elementB = b.ElementList[i];

            ElementEquals(elementA, elementB);

            ElementListEquals(elementA.InElementList, elementB.InElementList);
            ElementListEquals(elementA.OutElementList, elementB.OutElementList);

            EdgeListEquals(elementA.EdgeList, elementB.EdgeList);
        }

        static void ElementListEquals(IReadOnlyList<MathGraphElement<TElementInfo, TEdgeInfo>> a, IReadOnlyList<MathGraphElement<TElementInfo, TEdgeInfo>> b)
        {
            Debug.Assert(a.Count == b.Count);
            for (var i = 0; i < a.Count; i++)
            {
                ElementEquals(a[i], b[i]);
            }
        }

        static void ElementEquals(MathGraphElement<TElementInfo, TEdgeInfo> a, MathGraphElement<TElementInfo, TEdgeInfo> b)
        {
            if(a.Value is MathGraph<string, string> aMathGraph && b.Value is MathGraph<string, string> bMathGraph)
            {
                Equals(aMathGraph, bMathGraph);
                return;
            }

            Debug.Assert(EqualityComparer<TElementInfo>.Default.Equals(a.Value, b.Value));
            Debug.Assert(a.Id == b.Id);
        }

        static void EdgeListEquals(IReadOnlyList<MathGraphEdge<TElementInfo, TEdgeInfo>> aEdgeList,
            IReadOnlyList<MathGraphEdge<TElementInfo, TEdgeInfo>> bEdgeList)
        {
            Debug.Assert(aEdgeList.Count == bEdgeList.Count);

            for (var i = 0; i < aEdgeList.Count; i++)
            {
                EdgeEquals(aEdgeList[i], bEdgeList[i]);
            }
        }

        static void EdgeEquals(MathGraphEdge<TElementInfo, TEdgeInfo> a, MathGraphEdge<TElementInfo, TEdgeInfo> b)
        {
            Debug.Assert(EqualityComparer<TEdgeInfo>.Default.Equals(a.EdgeInfo, b.EdgeInfo));

            Debug.Assert(a.GetType() == b.GetType());

            if (a is MathGraphBidirectionalEdge<TElementInfo, TEdgeInfo> aBidirectionalEdge &&
                b is MathGraphBidirectionalEdge<TElementInfo, TEdgeInfo> bBidirectionalEdge)
            {
                ElementEquals(aBidirectionalEdge.AElement, bBidirectionalEdge.AElement);
                ElementEquals(aBidirectionalEdge.BElement, bBidirectionalEdge.BElement);
            }
            else if (a is MathGraphUnidirectionalEdge<TElementInfo, TEdgeInfo> aUnidirectionalEdge &&
                     b is MathGraphUnidirectionalEdge<TElementInfo, TEdgeInfo> bUnidirectionalEdge)
            {
                ElementEquals(aUnidirectionalEdge.From, bUnidirectionalEdge.From);
                ElementEquals(aUnidirectionalEdge.To, bUnidirectionalEdge.To);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}