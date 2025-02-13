using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace NinahajawhuLairfoheahurcee.Analyzer;

[Generator(LanguageNames.CSharp)]
public class IncrementalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // ��ע��һ�����Ը���ҵ��ʹ��
        context.RegisterPostInitializationOutput(initializationContext =>
        {
            initializationContext.AddSource("FooAttribute.cs",
                """
                namespace Lindexi;

                public class FooAttribute : Attribute
                {
                }
                """);
        });

        IncrementalValueProvider<ImmutableArray<string>> targetClassNameArrayProvider = context.SyntaxProvider.ForAttributeWithMetadataName("Lindexi.FooAttribute",
            // ��һ���ж�
            (node, _) => node.IsKind(SyntaxKind.ClassDeclaration),
            (syntaxContext, _) => syntaxContext.TargetSymbol.Name)
            .Collect();

        context.RegisterSourceOutput(targetClassNameArrayProvider, (productionContext, classNameArray) =>
        {
            productionContext.AddSource("GeneratedCode.cs",
                $$"""
                using System;
                namespace NinahajawhuLairfoheahurcee
                {
                    public static class GeneratedCode
                    {
                        public static void Print()
                        {
                            Console.WriteLine("����� Foo ���Ե������У� {{string.Join(",", classNameArray)}}");
                        }
                    }
                }
                """);
        });
    }
}

