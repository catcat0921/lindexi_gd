using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JuqawhicaqarLairciwholeni.Analyzer;

[Generator(LanguageNames.CSharp)]
public class FooIncrementalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<GeneratedCodeInfo> sourceProvider = context.SyntaxProvider.CreateSyntaxProvider(
            (node, _) =>
            {
                if (node is InvocationExpressionSyntax invocationExpressionSyntax)
                {
                    if (invocationExpressionSyntax.Expression is MemberAccessExpressionSyntax memberAccessExpressionSyntax)
                    {
                        // ����һ��������Ϊ WriteLine �ķ������룬���Ͳ�֪��������˭�� WriteLine �ˡ��﷨���������޷�֪��������������ĸ���
                        // ���� Foo a = ...; a.WriteLine(...);
                        // �� Foo b = ...; b.WriteLine(...);
                        // ��ʱ������﷨����ֻ�жϳ��� WriteLine ��������һ���жϾͽ������������
                        return memberAccessExpressionSyntax.Name.Identifier.Text == "WriteLine";
                    }
                }

                return false;
            },
            (syntaxContext, _) =>
            {
                var symbolInfo = syntaxContext.SemanticModel.GetSymbolInfo(syntaxContext.Node);

                if (symbolInfo.Symbol is not IMethodSymbol methodSymbol
                    // ��������жϴ������࣬��Ϊ�﷨�������Ѿ��ж����� WriteLine ����
                    || methodSymbol.Name != "WriteLine")
                {
                    return default(GeneratedCodeInfo);
                }

                // ������̼����жϾ����Ƿ� Foo ���͵� WriteLine ����
                var className = methodSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                if (className != "global::JuqawhicaqarLairciwholeni.Foo")
                {
                    return default(GeneratedCodeInfo);
                }

                /*
                   class Foo
                   {
                       public void WriteLine(int message)
                       {
                           Console.WriteLine($"Foo: {message}");
                       }
                   }
                 */

                var invocationExpressionSyntax = (InvocationExpressionSyntax) syntaxContext.Node;
                ArgumentSyntax argumentSyntax = invocationExpressionSyntax.ArgumentList.Arguments.First();
                var argument = (int)syntaxContext.SemanticModel.GetConstantValue(argumentSyntax.Expression).Value!;

#pragma warning disable RSEXPERIMENTAL002 // ʵ���Ծ��棬���Լ���
                var interceptableLocation = syntaxContext.SemanticModel.GetInterceptableLocation(invocationExpressionSyntax)!;

                var displayLocation = interceptableLocation.GetDisplayLocation();

                var generatedCode =
                    $$"""
                      using System.Runtime.CompilerServices;
                      
                      namespace Foo_JuqawhicaqarLairciwholeni
                      {
                          static partial class FooInterceptor
                          {
                              // {{displayLocation}}
                              [InterceptsLocation(version: {{interceptableLocation.Version}}, data: "{{interceptableLocation.Data}}")]
                              public static void InterceptorMethod{{argument}}(this {{className}} foo, int param)
                              {
                                  Console.WriteLine($"Interceptor{{argument}}: lindexi is doubi");
                              }
                          }
                      }

                      namespace System.Runtime.CompilerServices
                      {
                          [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
                          file sealed class InterceptsLocationAttribute : Attribute
                          {
                              public InterceptsLocationAttribute(int version, string data)
                              {
                                  _ = version;
                                  _ = data;
                              }
                          }
                      }
                      """;

                return new GeneratedCodeInfo(generatedCode, $"FooInterceptor{argument}.cs");
            })
            .Where(t => t != default);

        context.RegisterImplementationSourceOutput(sourceProvider,
           (productionContext, provider) =>
           {
               productionContext.AddSource(provider.Name, provider.GeneratedCode);
           });
    }
}

readonly record struct GeneratedCodeInfo(string GeneratedCode, string Name);
