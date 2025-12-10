using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AwesomeAnalyzer.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ClassAsStructAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(DiagnosticDescriptors.Rule0200ClassAsStruct);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            using (_ = new MeasureTime())
            {
                if (context.IsDisabledEditorConfig(DiagnosticDescriptors.Rule0200ClassAsStruct.Id))
                {
                    return;
                }

                var classDeclaration = (ClassDeclarationSyntax)context.Node;

                // Skip if already a struct, interface, or has modifiers that prevent conversion
                if (classDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword)) return;
                if (classDeclaration.Modifiers.Any(SyntaxKind.AbstractKeyword)) return;

                // Skip if class has a base class (other than object)
                if (classDeclaration.BaseList != null)
                {
                    var semanticModel = context.SemanticModel;
                    var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration, context.CancellationToken);
                    if (classSymbol?.BaseType != null && classSymbol.BaseType.SpecialType != SpecialType.System_Object)
                    {
                        return;
                    }

                    // Skip if class implements interfaces
                    // While structs CAN implement interfaces, this is a more complex performance decision
                    // and is outside the scope of this analyzer's conservative recommendations
                    if (classDeclaration.BaseList.Types.Count > 0)
                    {
                        return;
                    }
                }

                // Check if all fields are readonly
                var fields = classDeclaration.Members.OfType<FieldDeclarationSyntax>();
                foreach (var field in fields)
                {
                    if (!field.Modifiers.Any(SyntaxKind.ReadOnlyKeyword) &&
                        !field.Modifiers.Any(SyntaxKind.ConstKeyword))
                    {
                        return;
                    }
                }

                // Check if all properties are immutable (no setters or init-only setters)
                var properties = classDeclaration.Members.OfType<PropertyDeclarationSyntax>();
                foreach (var property in properties)
                {
                    if (property.AccessorList != null)
                    {
                        var setter = property.AccessorList.Accessors
                            .FirstOrDefault(a => a.IsKind(SyntaxKind.SetAccessorDeclaration) ||
                                               a.IsKind(SyntaxKind.InitAccessorDeclaration));

                        if (setter != null && setter.IsKind(SyntaxKind.SetAccessorDeclaration))
                        {
                            return; // Has mutable setter
                        }
                    }
                    else if (property.ExpressionBody == null)
                    {
                        return; // Auto-property without accessor list and no expression body
                    }
                }

                // Skip if class has virtual members
                var members = classDeclaration.GetMembers(context.Compilation);
                if (members.Any(x => x.IsVirtual)) return;

                // Check if class is small enough (heuristic: <= 3 fields/properties)
                var memberCount = fields.Count() + properties.Count();
                if (memberCount > 3)
                {
                    return; // Class is too large to be a good candidate for struct
                }

                // Skip if class has no members at all (empty class)
                if (memberCount == 0)
                {
                    return;
                }

                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.Rule0200ClassAsStruct,
                        classDeclaration.Identifier.GetLocation(),
                        classDeclaration.Identifier.ValueText
                    )
                );
            }
        }
    }
}
