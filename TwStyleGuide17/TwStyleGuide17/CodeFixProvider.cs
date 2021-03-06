﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Formatting;

namespace TwStyleGuide17
{
	/// <summary>
	/// IMPORTANT!: Only the shown functions should be in here.
	/// other functions (''public void Herbert() { // I like Mondays }'') will block every CodeFixProvider in here! Check that, if something doesn't work!
	/// </summary>
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(StatementIndentCodeFixProvider)), Shared]
	public class StatementIndentCodeFixProvider : CodeFixProvider
	{
		/// <summary>
		/// This array publishes the DiagnosticIds, this CodeFixProvider can Fix. 
		/// </summary>
		public sealed override ImmutableArray<string> FixableDiagnosticIds
		{
			get { return ImmutableArray.Create("TW0001"); }
		}

		/// <summary>
		/// This indicates, that the Fix can be run beside other Fixes (async)
		/// </summary>
		/// <returns>I don't care</returns>
		public sealed override FixAllProvider GetFixAllProvider()
		{
			return WellKnownFixAllProviders.BatchFixer;
		}

		/// <summary>
		/// This knits the CodeFix to the Function which actually does the Job
		/// CodeAction.Create() can use createChangedDocument as Well as createChangedSolution(), indication if the whole solution will be changed by the Fix or only the actual Document.
		/// Use this wisely because the performance will drop
		/// In case of Solution the Fixwer have to return Task of Solution, otherwise Task of Document
		/// </summary>
		/// <param name="context">the context is the Location, reported by the Diagnostic Analyser, to knit the Fix-Function to that</param>
		/// <returns></returns>
		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
			var diagnostic = context.Diagnostics.First();
			var diagnosticSpan = diagnostic.Location.SourceSpan;
			var statement = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<StatementSyntax>().First();

			if (statement != null)
				context.RegisterCodeFix(CodeAction.Create(title: "Place the statement on a new line.",
																		createChangedDocument: c => PlaceOnNewLine(context.Document, statement, c),
																		equivalenceKey: "Place the statement on a new line."),
																		diagnostic);
		}

		/// <summary>
		/// This is the actual Fix
		/// </summary>
		/// <param name="document">the whole Document (Tree)</param>
		/// <param name="ifStatement">the problem</param>
		/// <param name="cancellationToken">used to check for that</param>
		/// <returns>the Changes in the Syntaxtree</returns>
		private Task<Document> PlaceOnNewLine(Document document, StatementSyntax statement, CancellationToken cancellationToken)
		{
			SyntaxNode oldRoot;
			document.TryGetSyntaxRoot(out oldRoot);

			dynamic dynamicStatement = statement;

			// the Replace*-function ensures that only the changes use-up memory, while the unchanged content is ref'd in memory (that's at least the idea)
			// .WithAdditionalAnnotations(Formatter.Annotation) formats the indentation according to the settings of the editor (tabs or [1..n]whitespaces) - neat
			var newRoot = oldRoot.ReplaceNode((SyntaxNode)dynamicStatement.Statement, ((StatementSyntax)dynamicStatement.Statement).WithLeadingTrivia(SyntaxFactory.LineFeed).WithAdditionalAnnotations(Formatter.Annotation));
			var newDocument = document.WithSyntaxRoot(newRoot);
			return Task.FromResult(newDocument);
		}
	}

	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(StringEmptyCodeFixProvider)), Shared]
	public class StringEmptyCodeFixProvider : CodeFixProvider
	{
		public sealed override ImmutableArray<string> FixableDiagnosticIds
		{
			get { return ImmutableArray.Create("TW0002"); }
		}

		public sealed override FixAllProvider GetFixAllProvider()
		{
			return WellKnownFixAllProviders.BatchFixer;
		}

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
			var diagnostic = context.Diagnostics.First();
			var diagnosticSpan = diagnostic.Location.SourceSpan;

			var stringInitViolation = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<LocalDeclarationStatementSyntax>().First();

			// Register a code action that will invoke the fix.
			context.RegisterCodeFix(
				CodeAction.Create(
					title: "Initialize the string with String.Empty.",
					createChangedDocument: c => UseStringEmpty(context.Document, stringInitViolation, c),
				equivalenceKey: "Initialize the string with String.Empty."),
				context.Diagnostics);

		}

		private async Task<Document> UseStringEmpty(Document document, LocalDeclarationStatementSyntax stringInitViolation, CancellationToken cancellationToken)
		{
			var semanticModel = await document.GetSemanticModelAsync(cancellationToken);

			var oldInitializer = stringInitViolation.Declaration.Variables[0].Initializer;

			var root = await document.GetSyntaxRootAsync();

			var newRoot = root.ReplaceNode(oldInitializer, SyntaxFactory.ParseExpression("String.Empty").WithLeadingTrivia(SyntaxFactory.Space));

			var newDocument = document.WithSyntaxRoot(newRoot);
			return newDocument;
		}
	}

	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CommentSingleSpaceCodeFixProvider)), Shared]
	public class CommentSingleSpaceCodeFixProvider : CodeFixProvider
	{
		/// <summary>
		/// This array publishes the DiagnosticIds, this CodeFixProvider can Fix. 
		/// </summary>
		public sealed override ImmutableArray<string> FixableDiagnosticIds
		{
			get { return ImmutableArray.Create("TW0003"); }
		}

		/// <summary>
		/// This indicates, that the Fix can be run beside other Fixes (async)
		/// </summary>
		/// <returns>I don't care</returns>
		public sealed override FixAllProvider GetFixAllProvider()
		{
			// See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
			return WellKnownFixAllProviders.BatchFixer;
		}

		/// <summary>
		/// This knits the CodeFix to the Function which actually does the Job
		/// CodeAction.Create() can use createChangedDocument as Well as createChangedSolution(), indication if the whole solution will be changed by the Fix or only the actual Document.
		/// Use this wisely because the performance will drop
		/// In case of Solution the Fixwer have to return Task of Solution, otherwise Task of Document
		/// </summary>
		/// <param name="context">the context is the Location, reported by the Diagnostic Analyser, to knit the Fix-Function to that</param>
		/// <returns></returns>
		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
			var diagnostic = context.Diagnostics.First();
			var diagnosticSpan = diagnostic.Location.SourceSpan;
			var comment = root.FindTrivia(diagnosticSpan.Start);

			// Register a code action that will invoke the fix.
			context.RegisterCodeFix(CodeAction.Create(title: "Insert the missing whitespace.",
																	  createChangedDocument: c => InsertWhitespaceInComment(context.Document, comment, c),
																	  equivalenceKey: "Insert the missing whitespace."),
											 diagnostic);
		}

		private Task<Document> InsertWhitespaceInComment(Document document, SyntaxTrivia comment, CancellationToken c)
		{
			SyntaxNode oldRoot;
			document.TryGetSyntaxRoot(out oldRoot);

			var oldComment = comment.ToString();
			var index = 0;
			while (index < oldComment.Length && oldComment[index] == '/')
				index++;

			var spacedComment = SyntaxFactory.SyntaxTrivia(SyntaxKind.SingleLineCommentTrivia, oldComment.Insert(index, " ")); // this RegExp places the whitespace behind the last slash

			var newRoot = oldRoot.ReplaceTrivia(comment, spacedComment);
			var newDocument = document.WithSyntaxRoot(newRoot);
			return Task.FromResult(newDocument);
		}
	}

	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ImplicitVariablesCodeFixProvider)), Shared]
	public class ImplicitVariablesCodeFixProvider : CodeFixProvider
	{
		/// <summary>
		/// This array publishes the DiagnosticIds, this CodeFixProvider can Fix. 
		/// </summary>
		public sealed override ImmutableArray<string> FixableDiagnosticIds
		{
			get { return ImmutableArray.Create("TW0005"); }
		}

		/// <summary>
		/// This indicates, that the Fix can be run beside other Fixes (async)
		/// </summary>
		/// <returns>I don't care</returns>
		public sealed override FixAllProvider GetFixAllProvider()
		{
			// See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
			return WellKnownFixAllProviders.BatchFixer;
		}

		/// <summary>
		/// This knits the CodeFix to the Function which actually does the Job
		/// CodeAction.Create() can use createChangedDocument as Well as createChangedSolution(), indication if the whole solution will be changed by the Fix or only the actual Document.
		/// Use this wisely because the performance will drop
		/// In case of Solution the Fixwer have to return Task of Solution, otherwise Task of Document
		/// </summary>
		/// <param name="context">the context is the Location, reported by the Diagnostic Analyser, to knit the Fix-Function to that</param>
		/// <returns></returns>
		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
			var diagnostic = context.Diagnostics.First();
			var diagnosticSpan = diagnostic.Location.SourceSpan;
			LocalDeclarationStatementSyntax statement = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<LocalDeclarationStatementSyntax>().First();

			if (statement != null) context.RegisterCodeFix(CodeAction.Create(title: "Change the declaration to implicit.",
																								  createChangedDocument: c => ImplicitDeclaration(context.Document, statement, c),
																								  equivalenceKey: "Change the declaration to implicit."),
																		  diagnostic);
		}

		/// <summary>
		/// This is the actual Fix
		/// </summary>
		/// <param name="document">the whole Document (Tree)</param>
		/// <param name="ifStatement">the problem</param>
		/// <param name="cancellationToken">used to check for that</param>
		/// <returns>the Changes in the Syntaxtree</returns>
		private Task<Document> ImplicitDeclaration(Document document, LocalDeclarationStatementSyntax statement, CancellationToken cancellationToken)
		{
			SyntaxNode oldRoot;
			document.TryGetSyntaxRoot(out oldRoot);

			var declarationType = statement.Declaration.Type;
			var varDeclarationType = SyntaxFactory.IdentifierName("var").WithAdditionalAnnotations(Formatter.Annotation);

			var newStatement = statement.ReplaceNode(declarationType, varDeclarationType);
			var newRoot = oldRoot.ReplaceNode(statement, newStatement);
			var newDocument = document.WithSyntaxRoot(newRoot);
			return Task.FromResult(newDocument);
		}
	}

	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RatherSwitchThanIfCodeFixProvider)), Shared]
	public class RatherSwitchThanIfCodeFixProvider : CodeFixProvider
	{
		/// <summary>
		/// This array publishes the DiagnosticIds, this CodeFixProvider can Fix. 
		/// </summary>
		public sealed override ImmutableArray<string> FixableDiagnosticIds
		{
			get { return ImmutableArray.Create("TW0006"); }
		}

		/// <summary>
		/// This indicates, that the Fix can be run beside other Fixes (async)
		/// </summary>
		/// <returns>I don't care</returns>
		public sealed override FixAllProvider GetFixAllProvider()
		{
			// See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
			return WellKnownFixAllProviders.BatchFixer;
		}

		/// <summary>
		/// This knits the CodeFix to the Function which actually does the Job
		/// CodeAction.Create() can use createChangedDocument as Well as createChangedSolution(), indication if the whole solution will be changed by the Fix or only the actual Document.
		/// Use this wisely because the performance will drop
		/// In case of Solution the Fixwer have to return Task of Solution, otherwise Task of Document
		/// </summary>
		/// <param name="context">the context is the Location, reported by the Diagnostic Analyser, to knit the Fix-Function to that</param>
		/// <returns></returns>
		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
			var diagnostic = context.Diagnostics.First();
			var diagnosticSpan = diagnostic.Location.SourceSpan;
			IfStatementSyntax statement = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<IfStatementSyntax>().First();

			if (statement != null) context.RegisterCodeFix(CodeAction.Create(title: "Change these Ifs to one switch()-statement.",
																								  createChangedDocument: c => SwitchIt(context.Document, statement, c),
																								  equivalenceKey: "Change these Ifs to one switch()-statement."),
																		  diagnostic);
		}

		/// <summary>
		/// This is the actual Fix
		/// </summary>
		/// <param name="document">the whole Document (Tree)</param>
		/// <param name="ifStatement">the problem</param>
		/// <param name="cancellationToken">used to check for that</param>
		/// <returns>the Changes in the Syntaxtree</returns>
		private Task<Document> SwitchIt(Document document, IfStatementSyntax statement, CancellationToken cancellationToken)
		{
			SyntaxNode oldRoot;
			document.TryGetSyntaxRoot(out oldRoot);

			var cases = new SyntaxList<SwitchSectionSyntax>();

			var upperIf = statement;

			while (upperIf.Else?.ChildNodes().Count() > 0 && upperIf.Else.ChildNodes().First().IsKind(SyntaxKind.IfStatement))
			{
				if (upperIf.Statement.IsKind(SyntaxKind.ReturnStatement) || upperIf.Statement.ChildNodes().Last().IsKind(SyntaxKind.ReturnStatement))
					cases = cases.Add(SyntaxFactory.SwitchSection(SyntaxFactory.List(new List<SwitchLabelSyntax> { SyntaxFactory.CaseSwitchLabel(((BinaryExpressionSyntax)upperIf.Condition).Right) }),
											SyntaxFactory.List(new List<StatementSyntax> { upperIf.Statement })));
				else
					cases = cases.Add(SyntaxFactory.SwitchSection(SyntaxFactory.List(new List<SwitchLabelSyntax> { SyntaxFactory.CaseSwitchLabel(((BinaryExpressionSyntax)upperIf.Condition).Right) }),
										SyntaxFactory.List(new List<StatementSyntax> { upperIf.Statement, SyntaxFactory.BreakStatement() })));

				upperIf = (IfStatementSyntax)upperIf.Else.ChildNodes().First();
			}

			// include the last if-statement
			if (upperIf.Statement.IsKind(SyntaxKind.ReturnStatement) || upperIf.Statement.ChildNodes().Last().IsKind(SyntaxKind.ReturnStatement))
				cases = cases.Add(SyntaxFactory.SwitchSection(SyntaxFactory.List(new List<SwitchLabelSyntax> { SyntaxFactory.CaseSwitchLabel(((BinaryExpressionSyntax)upperIf.Condition).Right) }),
																		 SyntaxFactory.List(new List<StatementSyntax> { upperIf.Statement })));
			else
				cases = cases.Add(SyntaxFactory.SwitchSection(SyntaxFactory.List(new List<SwitchLabelSyntax> { SyntaxFactory.CaseSwitchLabel(((BinaryExpressionSyntax)upperIf.Condition).Right) }),
																			 SyntaxFactory.List(new List<StatementSyntax> { upperIf.Statement, SyntaxFactory.BreakStatement() })));
			// include a possible last else as the default
			if (upperIf.Else != null)
				if (upperIf.Else.Statement.IsKind(SyntaxKind.ReturnStatement) || upperIf.Else.Statement.ChildNodes().Last().IsKind(SyntaxKind.ReturnStatement))
					cases = cases.Add(SyntaxFactory.SwitchSection(SyntaxFactory.List(new List<SwitchLabelSyntax> { SyntaxFactory.DefaultSwitchLabel() }),
										SyntaxFactory.List(new List<StatementSyntax> { upperIf.Else.Statement })));
				else
					cases = cases.Add(SyntaxFactory.SwitchSection(SyntaxFactory.List(new List<SwitchLabelSyntax> { SyntaxFactory.DefaultSwitchLabel() }),
										SyntaxFactory.List(new List<StatementSyntax> { upperIf.Else.Statement, SyntaxFactory.BreakStatement() })));

			SwitchStatementSyntax newSwitch = SyntaxFactory.SwitchStatement(((BinaryExpressionSyntax)statement.Condition).Left, cases);

			var newRoot = oldRoot.ReplaceNode(statement, newSwitch.WithAdditionalAnnotations(Formatter.Annotation));
			var newDocument = document.WithSyntaxRoot(newRoot);
			return Task.FromResult(newDocument);
		}
	}
}