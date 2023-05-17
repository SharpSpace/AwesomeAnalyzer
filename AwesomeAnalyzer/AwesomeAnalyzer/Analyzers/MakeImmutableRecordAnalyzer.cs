﻿using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AwesomeAnalyzer.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MakeImmutableRecordAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            DiagnosticDescriptors.Rule0009MakeImmutableRecord
        );

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(
                c => Analyze(c),
                SyntaxKind.PropertyDeclaration
            );
        }

        private async Task Analyze(SyntaxNodeAnalysisContext context)
        {
            var propertyDeclarationSyntax = (PropertyDeclarationSyntax)context.Node;

            if (propertyDeclarationSyntax.HasParent<RecordDeclarationSyntax>() == null) return;
            if (await IsUsed(context, propertyDeclarationSyntax)) return;

            //Debug.WriteLine("propertyDeclarationSyntax: " + propertyDeclarationSyntax.ToFullString());
            context.ReportDiagnostic(
                Diagnostic.Create(
                    DiagnosticDescriptors.Rule0009MakeImmutableRecord,
                    propertyDeclarationSyntax.GetLocation(),
                    propertyDeclarationSyntax.Identifier.ValueText
                )
            );
        }

        private async Task<bool> IsUsed(SyntaxNodeAnalysisContext context, PropertyDeclarationSyntax propertyDeclaration)
        {
            var semanticModel = context.SemanticModel;
            var propertySymbol = semanticModel.GetDeclaredSymbol(propertyDeclaration);
            if (propertySymbol == null)
            {
                return true;
            }

            if (propertySymbol.IsReadOnly)
            {
                return true;
            }

            if (propertyDeclaration.ToString().Contains("private set"))
            {
                return true;
            }

            var makeImmutableRecordVisitor = new MakeImmutableRecordVisitor(semanticModel, propertySymbol);
            foreach (var syntaxTree in context.Compilation.SyntaxTrees)
            {
                makeImmutableRecordVisitor.Visit(
                    await syntaxTree.GetRootAsync(context.CancellationToken).ConfigureAwait(false)
                );
                if (makeImmutableRecordVisitor.IsFound)
                {
                    break;
                }
            }
            
            return makeImmutableRecordVisitor.IsFound;
        }
    }
}