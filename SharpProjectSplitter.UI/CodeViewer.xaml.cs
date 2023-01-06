using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SharpProjectSplitter.UI
{
    /// <summary>
    /// Interaction logic for CodeViewer.xaml
    /// </summary>
    public partial class CodeViewer : UserControl, INotifyPropertyChanged
    {
        private SyntaxTree m_syntaxTree;
        private SemanticModel m_semanticModel;

        public CodeViewer()
        {
            InitializeComponent();

        }

        internal enum TokenKind
        {
            None,
            Keyword,
            Identifier,
            StringLiteral,
            CharacterLiteral,
            Comment,
            DisabledText,
            Region
        }

        public string OpenedFileName => m_syntaxTree?.FilePath ?? string.Empty;
        public Visibility CopyPathButtonVisibility => m_syntaxTree != null ? Visibility.Visible : Visibility.Collapsed;
        private void OnCopyFilePathClicked(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(new FileInfo(m_syntaxTree.FilePath).FullName);
        }

        public void SetText(SyntaxTree st, SemanticModel semanticModel, SyntaxNode selection = null)
        {
            m_syntaxTree = st;
            m_semanticModel = semanticModel;


            var doc = new System.Windows.Documents.FlowDocument();
            int lineNumber = 0;
            Paragraph para = new Paragraph();
            para.Inlines.Add(new Run((lineNumber++).ToString() + " |  "));
            
            (int, int) selectionBegin = default, selectionEnd = default;
            new ColorizeSyntaxWalker().DoVisit(st.GetRoot(), semanticModel, selection, (TokenKind kind, string token, bool isSelection) =>
            {
                var run = new Run(token);
                Colorize(kind, run);
                if (token.Contains("\n"))
                {
                    doc.Blocks.Add(para);
                    para = new Paragraph();
                    para.Inlines.Add(new Run((lineNumber++).ToString() + " |  "));
                }
                else
                {
                    if (isSelection)
                    {
                        if (selectionBegin == default)
                            selectionBegin = (lineNumber-1, para.Inlines.Count);
                        selectionEnd = (lineNumber-1, para.Inlines.Count);
                    }
                    para.Inlines.Add(run);
                }
            });
            CodeTextBox.Document = doc;
            CodeTextBox.Focus();
            if (selectionBegin != default)
            {
                TextPointer selectionStart = (doc.Blocks.ElementAt(selectionBegin.Item1) as Paragraph).Inlines.ElementAt(selectionBegin.Item2).ContentStart;
                TextPointer selectionEnds = (doc.Blocks.ElementAt(selectionEnd.Item1) as Paragraph).Inlines.ElementAt(selectionEnd.Item2).ContentEnd;
                CodeTextBox.CaretPosition = selectionStart;
                CodeTextBox.Selection.Select(selectionStart, selectionEnds);
            }

            RaisePropertyChanged(nameof(CopyPathButtonVisibility));
            RaisePropertyChanged(nameof(OpenedFileName));
        }
        //hey
        SolidColorBrush m_green = new SolidColorBrush(Color.FromArgb(255, 27, 139, 117));
        SolidColorBrush m_strings = new SolidColorBrush(Color.FromArgb(255, 168, 29, 65));
        SolidColorBrush m_gray = new SolidColorBrush(Color.FromArgb(255, 160, 160, 160));
        SolidColorBrush m_identifiers = new SolidColorBrush(Color.FromArgb(255, 35, 111, 141));
        SolidColorBrush m_keyWord = new SolidColorBrush(Color.FromArgb(255, 3, 3, 253));
        SolidColorBrush m_textColor = new SolidColorBrush(Color.FromArgb(255, 67, 7, 128));

        private void RaisePropertyChanged(string property)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(
            IntPtr hWnd, // handle to destination window
            int Msg, // message
            int wParam, // first message parameter
            int[] lParam // second message parameter
            );

        private const int EM_SETTABSTOPS = 0x00CB;

        public event PropertyChangedEventHandler PropertyChanged;

        private void Colorize(TokenKind kind, Run run)
        {
            switch(kind)
            {
                case TokenKind.CharacterLiteral:
                    run.Foreground = m_strings; break;
                case TokenKind.Comment:
                    run.Foreground = m_green; break;
                case TokenKind.DisabledText:
                    run.Foreground = m_gray; break;
                case TokenKind.Identifier:
                    run.Foreground = m_identifiers; break;
                case TokenKind.Keyword:
                    run.Foreground = m_keyWord; break;
                case TokenKind.None:
                    run.Foreground = m_textColor; break;
                case TokenKind.Region:
                    run.Foreground = m_gray; break;
                case TokenKind.StringLiteral:
                    run.Foreground = m_strings; break;
            }
        }

        internal class ColorizeSyntaxWalker : CSharpSyntaxWalker
        {
            private SemanticModel semanticModel;
            private Action<TokenKind, string, bool> writeDelegate;
            private SyntaxNode selection;
            private bool m_isSelection;

            public ColorizeSyntaxWalker()
                : base(depth: SyntaxWalkerDepth.StructuredTrivia) { }
            internal void DoVisit(SyntaxNode token, SemanticModel semanticModel, SyntaxNode selection, Action<TokenKind, string, bool> writeDelegate)
            {
                this.semanticModel = semanticModel;
                this.writeDelegate = writeDelegate;
                this.selection = selection;
                Visit(token);
                m_isSelection = false;
            }

            public override void Visit(SyntaxNode node)
            {
                if (selection != null && node == selection)
                    m_isSelection = true;
                base.Visit(node);

                if (selection != null && node == selection)
                    m_isSelection = false;
            }

            // Handle SyntaxTokens
            public override void VisitToken(SyntaxToken token)
            {
                base.VisitLeadingTrivia(token);


                var isProcessed = false;
                if (token.IsKeyword())
                {
                    writeDelegate(TokenKind.Keyword, token.ToString(), m_isSelection);
                    isProcessed = true;
                }
                else
                {
                    switch (token.Kind())
                    {
                        case SyntaxKind.StringLiteralToken:
                            writeDelegate(TokenKind.StringLiteral, token.ToString(), m_isSelection);
                            isProcessed = true;
                            break;
                        case SyntaxKind.CharacterLiteralToken:
                            writeDelegate(TokenKind.CharacterLiteral, token.ToString(), m_isSelection);
                            isProcessed = true;
                            break;
                        case SyntaxKind.IdentifierToken:
                            if (token.Parent is SimpleNameSyntax)
                            {
                                // SimpleName is the base type of IdentifierNameSyntax, GenericNameSyntax etc.
                                // This handles type names that appear in variable declarations etc.
                                // e.g. "TypeName x = a + b;"
                                var name = (SimpleNameSyntax)token.Parent;
                                var symbolInfo = semanticModel.GetSymbolInfo(token.Parent);// (name);
                                if (symbolInfo.Symbol != null && symbolInfo.Symbol.Kind != SymbolKind.ErrorType)
                                {
                                    switch (symbolInfo.Symbol.Kind)
                                    {
                                        case SymbolKind.NamedType:
                                            writeDelegate(TokenKind.Identifier, token.ToString(), m_isSelection);
                                            isProcessed = true;
                                            break;
                                        case SymbolKind.Namespace:
                                        case SymbolKind.Parameter:
                                        case SymbolKind.Local:
                                        case SymbolKind.Field:
                                        case SymbolKind.Property:
                                            writeDelegate(TokenKind.None, token.ToString(), m_isSelection);
                                            isProcessed = true;
                                            break;
                                        default:
                                            break;
                                    }
                                }
                            }
                            else if (token.Parent is TypeDeclarationSyntax)
                            {
                                // TypeDeclarationSyntax is the base type of ClassDeclarationSyntax etc.
                                // This handles type names that appear in type declarations
                                // e.g. "class TypeName { }"
                                var name = (TypeDeclarationSyntax)token.Parent;
                                var symbol = semanticModel.GetDeclaredSymbol(name);
                                if (symbol != null && symbol.Kind != SymbolKind.ErrorType)
                                {
                                    switch (symbol.Kind)
                                    {
                                        case SymbolKind.NamedType:
                                            writeDelegate(TokenKind.Identifier, token.ToString(), m_isSelection);
                                            isProcessed = true;
                                            break;
                                    }
                                }
                            }
                            break;
                    }
                }

                if (!isProcessed)
                    HandleSpecialCaseIdentifiers(token);

                base.VisitTrailingTrivia(token);
            }

            private void HandleSpecialCaseIdentifiers(SyntaxToken token)
            {
                var parentKind = token.Parent.Kind();
                var parentParentKind = token.Parent.Parent?.Kind();
                switch (token.Kind())
                {
                    
                    // Special cases that are not handled because there is no semantic context/model that can truely identify identifiers.
                    case SyntaxKind.IdentifierToken:
                        if ((parentKind == SyntaxKind.IdentifierName && parentParentKind == SyntaxKind.Parameter)
                          || (parentKind== SyntaxKind.EnumDeclaration)
                          || (parentKind== SyntaxKind.IdentifierName && parentParentKind== SyntaxKind.Attribute)
                          || (parentKind== SyntaxKind.IdentifierName && parentParentKind== SyntaxKind.CatchDeclaration)
                          || (parentKind== SyntaxKind.IdentifierName && parentParentKind== SyntaxKind.ObjectCreationExpression)
                          )
                        {

                            writeDelegate(TokenKind.Identifier, token.ToString(), m_isSelection);
                        }
                        else if(
                          (parentKind== SyntaxKind.IdentifierName && parentParentKind== SyntaxKind.ForEachStatement && !(token.GetNextToken().Kind() == SyntaxKind.CloseParenToken))
                          || (parentKind== SyntaxKind.IdentifierName && token.Parent.Parent?.Parent?.Kind() == SyntaxKind.CaseSwitchLabel && !(token.GetPreviousToken().Kind() == SyntaxKind.DotToken))
                          || (parentKind== SyntaxKind.IdentifierName && parentParentKind== SyntaxKind.MethodDeclaration)
                          || (parentKind== SyntaxKind.IdentifierName && parentParentKind== SyntaxKind.CastExpression)
                          //e.g. "private static readonly HashSet patternHashSet = new HashSet();" the first HashSet in this case
                          || (parentKind== SyntaxKind.GenericName && parentParentKind== SyntaxKind.VariableDeclaration)
                          //e.g. "private static readonly HashSet patternHashSet = new HashSet();" the second HashSet in this case
                          || (parentKind== SyntaxKind.GenericName && parentParentKind== SyntaxKind.ObjectCreationExpression)
                          //e.g. "public sealed class BuilderRouteHandler : IRouteHandler" IRouteHandler in this case
                          || (parentKind== SyntaxKind.IdentifierName && parentParentKind== SyntaxKind.BaseList)
                          )
                        {

                            writeDelegate(TokenKind.Identifier, token.ToString(), m_isSelection);
                        }
                        else if(
                          //e.g. "Type baseBuilderType = typeof(BaseBuilder);" BaseBuilder in this case
                          (parentKind== SyntaxKind.IdentifierName && token.Parent.Parent?.Parent?.Parent?.Kind() == SyntaxKind.TypeOfExpression)
                          // e.g. "private DbProviderFactory dbProviderFactory;" OR "DbConnection connection = dbProviderFactory.CreateConnection();"
                          || (parentKind== SyntaxKind.IdentifierName && parentParentKind== SyntaxKind.VariableDeclaration)
                          // e.g. "DbTypes = new Dictionary();" DbType in this case
                          || (parentKind== SyntaxKind.IdentifierName && parentParentKind== SyntaxKind.TypeArgumentList)
                          )
                        {

                            writeDelegate(TokenKind.Identifier, token.ToString(), m_isSelection);
                        }
                        else if(
                          // e.g. "DbTypes.Add("int", DbType.Int32);" DbType in this case
                          (parentKind== SyntaxKind.IdentifierName && parentParentKind== SyntaxKind.SimpleMemberAccessExpression && token.Parent.Parent?.Parent?.Kind() == SyntaxKind.Argument && !(token.GetPreviousToken().Kind() == SyntaxKind.DotToken || Char.IsLower(token.ToString()[0])))
                          // e.g. "schemaCommand.CommandType = CommandType.Text;" CommandType in this case
                          || (parentKind== SyntaxKind.IdentifierName && parentParentKind== SyntaxKind.SimpleMemberAccessExpression && !(token.GetPreviousToken().Kind() == SyntaxKind.DotToken || Char.IsLower(token.ToString()[0])))
                          )
                        {
                            writeDelegate(TokenKind.Identifier, token.ToString(), m_isSelection);
                        }
                        else
                        {
                            writeDelegate(TokenKind.None, token.ToString(), m_isSelection);
                        }
                        break;
                    default:
                        writeDelegate(TokenKind.None, token.ToString(), m_isSelection);
                        break;
                }
            }



            // Handle SyntaxTrivia
            public override void VisitTrivia(SyntaxTrivia trivia)
            {
                switch (trivia.Kind())
                {
                    case SyntaxKind.MultiLineCommentTrivia:
                    case SyntaxKind.SingleLineCommentTrivia:
                        writeDelegate(TokenKind.Comment, trivia.ToString(), m_isSelection);
                        break;
                    case SyntaxKind.DisabledTextTrivia:
                        writeDelegate(TokenKind.DisabledText, trivia.ToString(), m_isSelection);
                        break;
                    case SyntaxKind.DocumentationCommentExteriorTrivia:
                    case SyntaxKind.EndOfDocumentationCommentToken:
                    case SyntaxKind.SingleLineDocumentationCommentTrivia:
                    case SyntaxKind.MultiLineDocumentationCommentTrivia:
                    case SyntaxKind.XmlComment:
                    case SyntaxKind.XmlCommentEndToken:
                    case SyntaxKind.XmlCommentStartToken:
                        writeDelegate(TokenKind.Comment, trivia.ToString(), m_isSelection);
                        break;
                    case SyntaxKind.RegionDirectiveTrivia:
                    case SyntaxKind.RegionKeyword:
                    case SyntaxKind.EndRegionDirectiveTrivia:
                    case SyntaxKind.EndRegionKeyword:
                        writeDelegate(TokenKind.Region, trivia.ToString(), m_isSelection);
                        break;
                    default:
                        writeDelegate(TokenKind.None, trivia.ToString(), m_isSelection);
                        break;
                }

            }
        }
    }
}
