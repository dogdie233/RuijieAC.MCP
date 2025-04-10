using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;


namespace RuijieAC.MCP.SourceGenerator;

/// <summary>
/// A sample source generator that creates a custom report based on class properties. The target class should be annotated with the 'Generators.ReportAttribute' attribute.
/// When using the source code as a baseline, an incremental source generator is preferable because it reduces the performance overhead.
/// </summary>
[Generator]
public class McpToolsSourceGenerator : IIncrementalGenerator
{
    private const string ExtClassName = "SGMcpServerBuilderExtensions";
    private const string MethodName = "WithToolsFromAssemblySourceGen";
    private const string DeclarationScript = $$"""
                                             // <auto-generated/>
                                             using Microsoft.Extensions.DependencyInjection;
                                             using System.Diagnostics.CodeAnalysis;
                                             
                                             namespace ModelContextProtocol.Server;

                                             internal static partial class {{ExtClassName}} {
                                                 internal static partial IMcpServerBuilder {{MethodName}}(this IMcpServerBuilder builder); 
                                                 
                                                 [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
                                                 private static void WithTools2(IMcpServerBuilder builder,
                                                    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods |
                                                        DynamicallyAccessedMemberTypes.NonPublicMethods |
                                                        DynamicallyAccessedMemberTypes.PublicConstructors)] Type t) {
                                                     builder.WithTools([t]);
                                                 }
                                             }
                                             """;
    
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(initializationContext =>
        {
            initializationContext.AddSource("SGMcpServerBuilderExtensions.Declaration.g.cs", DeclarationScript);
        });
        
        var tools = context.SyntaxProvider.ForAttributeWithMetadataName("ModelContextProtocol.Server.McpServerToolTypeAttribute",
            (node, _) => node.IsKind(SyntaxKind.ClassDeclaration) || node.IsKind(SyntaxKind.RecordDeclaration),
            (syntaxContext, _) =>
            {
                var ns = syntaxContext.TargetSymbol.ContainingNamespace;
                var name = syntaxContext.TargetSymbol.Name;
                return $"global::{(ns?.IsGlobalNamespace ?? true ? "" : ns.Name + ".")}{name}";
            })
            .Collect();
        context.RegisterImplementationSourceOutput(tools, (productionContext, arr) =>
        {
            var body = string.Join("\n", 
                arr.Select(type => $"WithTools2(builder, typeof({type}));")
            );
            productionContext.AddSource("SGMcpServerBuilderExtensions.Implementation.g.cs",
                $$"""
                // <auto-generated/>
                using Microsoft.Extensions.DependencyInjection;
                using System.Diagnostics.CodeAnalysis;
                
                namespace ModelContextProtocol.Server;

                internal static partial class {{ExtClassName}} {
                    internal static partial IMcpServerBuilder {{MethodName}}(this IMcpServerBuilder builder) {
                        {{body}}
                        return builder;
                    }
                }
                """);
        });
    }
}