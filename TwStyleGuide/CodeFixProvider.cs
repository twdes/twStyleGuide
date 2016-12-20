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
using Microsoft.CodeAnalysis.Formatting;

namespace TwStyleGuide
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
			StatementSyntax statement = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<StatementSyntax>().First();
			
			if (statement != null) context.RegisterCodeFix(CodeAction.Create(title: "Place the statement on a new line.",
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
			// See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
			return WellKnownFixAllProviders.BatchFixer;
		}

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
			var diagnostic = context.Diagnostics.First();
			var diagnosticSpan = diagnostic.Location.SourceSpan;

			var stringInitViolation = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<LocalDeclarationStatementSyntax>().First();

			//Register a code action that will invoke the fix.
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

			ExpressionSyntax es = SyntaxFactory.ParseExpression("String.Empty").WithLeadingTrivia(SyntaxFactory.Space);
			var newInitializer = SyntaxFactory.EqualsValueClause(es);

			var root = await document.GetSyntaxRootAsync();

			var newRoot = root.ReplaceNode(oldInitializer, newInitializer);

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
}