using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Formatting;

namespace TwStyleGuide
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class TwStyleGuideAnalyzer : DiagnosticAnalyzer
	{
		private static DiagnosticDescriptor Rule1 = new DiagnosticDescriptor("TW0001",
																								  "Checks for LineBreak and Indentation",
																								  "The Statement \"{0}\" must be placed in a new Line.",
																								  "Layout",
																								  DiagnosticSeverity.Warning,
																								  isEnabledByDefault: true,
																								  description: "A Statement should be on a new line and indented");  // DiagnosticSeverity.Error will not compile if existent
		private static DiagnosticDescriptor Rule2 = new DiagnosticDescriptor("TW0002",
																						  "Checks for string initialisation",
																						  "The string \"{0}\" should not be inialized by \"{1}\" but with \"String.Empty\".",
																						  "Variables",
																						  DiagnosticSeverity.Warning, // Squiggles only on Warning and Error, Info has none
																						  isEnabledByDefault: true);  // DiagnosticSeverity.Error will not compile if existent
		private static DiagnosticDescriptor Rule3 = new DiagnosticDescriptor("TW0003",
																						  "Checks for Comment formatting",
																						  "There should be a whitespace between the slashes and the comment.",
																						  "Layout",
																						  DiagnosticSeverity.Warning, // Squiggles only on Warning and Error, Info has none
																						  isEnabledByDefault: true);  // DiagnosticSeverity.Error will not compile if existent
		private static DiagnosticDescriptor Rule4 = new DiagnosticDescriptor("TW0004",
																						  "Checks for mandatory Comments",
																						  "The public {1} \"{0}\" has no comment.",
																						  "Layout",
																						  DiagnosticSeverity.Warning, // Squiggles only on Warning and Error, Info has none
																						  isEnabledByDefault: true);  // DiagnosticSeverity.Error will not compile if existent
		private static DiagnosticDescriptor Rule5 = new DiagnosticDescriptor("TW0005",
																						  "Implicit declarations.",
																						  "The variable \"{0}\" is declared explicit while being initialized.",
																						  "Variables",
																						  DiagnosticSeverity.Warning, // Squiggles only on Warning and Error, Info has none
																						  isEnabledByDefault: true);  // DiagnosticSeverity.Error will not compile if existent

		/// <summary>
		/// The array publishes the Check-Rules
		/// </summary>
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule1, Rule2, Rule3, Rule4, Rule5); } }

		/// <summary>
		/// In the Initialize Method one have to Register all Check-Functions
		/// </summary>
		/// <param name="context">The context (the Solution or Project)</param>
		public override void Initialize(AnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(AnalyzeIndentationOfSyntaxNode, SyntaxKind.IfStatement, SyntaxKind.ForStatement, SyntaxKind.WhileStatement, SyntaxKind.ForEachStatement);
			context.RegisterSyntaxNodeAction(AnalyzeVariableDeclarationSyntaxNode, SyntaxKind.LocalDeclarationStatement);
			context.RegisterSyntaxTreeAction(AnalyzeSingleLineCommentSyntaxNode);   // SyntaxKind.SingleLineComment does not work/is not fired
			context.RegisterSyntaxNodeAction(AnalyzePublicMethodComment, SyntaxKind.MethodDeclaration);
			context.RegisterSyntaxNodeAction(AnalyzePublicVarComment, SyntaxKind.FieldDeclaration);
			context.RegisterSyntaxNodeAction(AnalyzePublicEnumComment, SyntaxKind.EnumDeclaration);
			context.RegisterSyntaxNodeAction(AnalyzePublicStructComment, SyntaxKind.StructDeclaration);
			context.RegisterSyntaxNodeAction(AnalyzePublicPropertyComment, SyntaxKind.PropertyDeclaration);
		}

		private void AnalyzePublicPropertyComment(SyntaxNodeAnalysisContext context)
		{

			if (!((PropertyDeclarationSyntax)context.Node).Modifiers.Any(SyntaxKind.PublicKeyword)) return;
			var singleLineComments = from comment in context.Node.DescendantTrivia() where comment.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) select comment;

			if (singleLineComments.Count() == 0)
			{
				var diagnostic = Diagnostic.Create(Rule4,
															  ((PropertyDeclarationSyntax)context.Node).Identifier.GetLocation(),
															  ((PropertyDeclarationSyntax)context.Node).Identifier.ToString(),
															  "Property");
				context.ReportDiagnostic(diagnostic);
			}
		}

		private void AnalyzePublicStructComment(SyntaxNodeAnalysisContext context)
		{

			if (!((StructDeclarationSyntax)context.Node).Modifiers.Any(SyntaxKind.PublicKeyword)) return;
			var singleLineComments = from comment in context.Node.DescendantTrivia() where comment.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) select comment;
			if (singleLineComments.Count() == 0)
			{

				var diagnostic = Diagnostic.Create(Rule4,
															  ((StructDeclarationSyntax)context.Node).Identifier.GetLocation(),
															  ((StructDeclarationSyntax)context.Node).Identifier.ToString(),
															  "struct");
				context.ReportDiagnostic(diagnostic);
			}
		}

		private void AnalyzePublicEnumComment(SyntaxNodeAnalysisContext context)
		{

			if (!((EnumDeclarationSyntax)context.Node).Modifiers.Any(SyntaxKind.PublicKeyword)) return;
			var singleLineComments = from comment in context.Node.DescendantTrivia() where comment.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) select comment;
			if (singleLineComments.Count() == 0)
				{

					var diagnostic = Diagnostic.Create(Rule4, 
																  ((EnumDeclarationSyntax)context.Node).Identifier.GetLocation(), 
																  ((EnumDeclarationSyntax)context.Node).Identifier.ToString(), 
																  "enum");
					context.ReportDiagnostic(diagnostic);
				}
		}

		private void AnalyzePublicVarComment(SyntaxNodeAnalysisContext context)
		{

			if (!((FieldDeclarationSyntax)context.Node).Modifiers.Any(SyntaxKind.PublicKeyword)) return;
			var singleLineComments = from comment in context.Node.DescendantTrivia() where comment.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) select comment;
			if (singleLineComments.Count() == 0)
				foreach (var variable in ((FieldDeclarationSyntax)context.Node).Declaration.Variables)
				{
				
				var diagnostic = Diagnostic.Create(Rule4, 
															  variable.Identifier.GetLocation(), 
															  variable.Identifier.ToString(), 
															  ((FieldDeclarationSyntax)context.Node).Declaration.Type.ToString());
				context.ReportDiagnostic(diagnostic);
			}
		}

		private void AnalyzePublicMethodComment(SyntaxNodeAnalysisContext context)
		{
			
			if (!((MethodDeclarationSyntax)context.Node).Modifiers.Any(SyntaxKind.PublicKeyword)) return;
			var singleLineComments = from comment in context.Node.DescendantTrivia() where comment.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) select comment;
			if (singleLineComments.Count() == 0)
			{
				var diagnostic = Diagnostic.Create(Rule4, 
															  ((MethodDeclarationSyntax)context.Node).Identifier.GetLocation(), 
															  ((MethodDeclarationSyntax)context.Node).Identifier.Value.ToString(), 
															  "Method");
				context.ReportDiagnostic(diagnostic);
			}
		}

		private void AnalyzeSingleLineCommentSyntaxNode(SyntaxTreeAnalysisContext context)
		{
			SyntaxNode root = context.Tree.GetCompilationUnitRoot(context.CancellationToken);
			var commentNodes = from node in root.DescendantTrivia() where node.IsKind(SyntaxKind.SingleLineCommentTrivia) select node;  //  one could also select ''node.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)''
			foreach (var node in commentNodes)
				if (!Regex.IsMatch(node.ToString(), @"/{2,}\w"))
				{
					var diagnostic = Diagnostic.Create(Rule3, node.GetLocation());
					context.ReportDiagnostic(diagnostic);
				}
		}

		/// <summary>
		/// This is a presentation on finding Statements which are in the same Line as the Condition
		/// it is executed, when a Syntax Node is finished
		/// These Analyze-functions can check for&throw multiple rule violations.
		/// </summary>
		/// <param name="context">The Context ist the Sourcecode</param>
		private void AnalyzeIndentationOfSyntaxNode(SyntaxNodeAnalysisContext context)
		{
			dynamic syntobj = context.Node;

			if (!syntobj.Statement.HasLeadingTrivia && !syntobj.Statement.IsMissing) // it's not an EoL-Trivia && there is a statement (you are not typing)
			{
				var messageHint = Regex.Replace(((string)syntobj.Statement.GetText().ToString()), @"\s+|\t|\n|\r", " "); // cleanup the Warningmessage
				if (messageHint.Length > 30) messageHint = messageHint.Substring(0, 30) + "...";	// limit it to 30 chars
				/// Diagnostic.Create(the rule violated, the Location() - for the squiggles, [0..n] parameters - passed to the ''Rule.MessageFormat'')
				var diagnostic = Diagnostic.Create(Rule1, syntobj.GetLocation(), messageHint); 
				context.ReportDiagnostic(diagnostic);
			}
		}

		/// <summary>
		/// This checks, if a String is initialized with "" rather than String.Empty (TW-CD)
		/// it is executed, when a Syntax Node is finished
		/// </summary>
		/// <param name="context"></param>
		private void AnalyzeVariableDeclarationSyntaxNode(SyntaxNodeAnalysisContext context)
		{
			var syntax = (LocalDeclarationStatementSyntax)context.Node;
			if (context.SemanticModel.GetTypeInfo(syntax.Declaration.Type).ConvertedType.SpecialType == SpecialType.System_String)
				foreach (var variable in syntax.Declaration.Variables)
					if (variable.Initializer?.Value.ToString() == "\"\"" || variable.Initializer?.Value.ToString() == "string.Empty")
					{
						var diagnostic = Diagnostic.Create(Rule2, variable.GetLocation(), variable.Identifier.Text, variable.Initializer.Value.ToString());
						var alreadyWrong = false;
						foreach (var diag in variable.GetDiagnostics()) if (diag.Id == Rule2.Id) alreadyWrong = true;
						if (!alreadyWrong) context.ReportDiagnostic(diagnostic);
					}
			foreach (var token in syntax.Declaration.DescendantTokens()) if (token.RawKind == 8204 && !syntax.Declaration.Type.IsVar)	//ToDo: rk
			{
				var diagnostic = Diagnostic.Create(Rule5, syntax.GetLocation(), syntax.Declaration.Variables[0].Identifier.Value);
					var alreadyWrong = false;
					SyntaxNode root = context.Node;
					foreach (var diag in root.GetDiagnostics()) if (diag.Id == Rule5.Id) alreadyWrong = true;
					if (!alreadyWrong) context.ReportDiagnostic(diagnostic);
			}
		}
	}
}
