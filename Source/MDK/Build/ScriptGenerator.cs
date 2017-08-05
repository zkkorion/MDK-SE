using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MDK.Build
{
    /// <summary>
    /// The generator used to produce the final script after all the rest of the build operation is complete.
    /// </summary>
    public class ScriptGenerator
    {
        static readonly string[] NewLines = {"\r\n", "\n"};

        /// <summary>
        /// Generates a final script of the given document. This document has been generated by the build process and
        /// contains the entire script in syntax tree form.
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        public async Task<string> Generate(Document document)
        {
            var root = await document.GetSyntaxRootAsync().ConfigureAwait(false);
            var trimmer = new SyntaxTrimmer();
            trimmer.Visit(root);

            string script;
            var programPartLines = string.Join("\n\n", trimmer.ProgramNodes.Select(GenerateFinalCode)).Split(NewLines, StringSplitOptions.None);
            var programPart = DeIndent(programPartLines);
            var extensionPartLines = string.Join(" ", trimmer.ExtensionNodes.Select(GenerateFinalCode)).Split(NewLines, StringSplitOptions.None);
            var extensionPart = DeIndent(extensionPartLines);
            if (extensionPart.EndsWith("}"))
                script = $"{programPart}\n\n}}\n\n{extensionPart.Substring(0, extensionPart.Length - 1)}";
            else
                script = programPart;

            return script;
        }

        string GenerateFinalCode(SyntaxNode n)
        {
            return n.ToFullString().Trim();
        }

        string DeIndent(string[] programPartLines)
        {
            var indent = int.MaxValue;
            for (var i = 0; i < programPartLines.Length; i++)
            {
                var line = programPartLines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    programPartLines[i] = "";
                    continue;
                }

                var match = Regex.Match(line, @"^\s+");
                if (match.Success)
                {
                    var tabLength = TabLengthOfValue(match.Value);
                    if (tabLength > 0 && tabLength < indent)
                        indent = tabLength;
                }
            }
            if (indent == int.MaxValue)
            {
                return string.Join("\r\n", programPartLines);
            }
            for (var i = 0; i < programPartLines.Length; i++)
            {
                var line = programPartLines[i];
                if (line.Length == 0)
                    continue;
                var tabs = 0;
                for (var j = 0; j < indent; j++)
                {
                    var ch = line[j];
                    if (ch == '\t')
                    {
                        tabs += 4;
                    }
                    else if (ch == ' ')
                    {
                        tabs++;
                    }
                    else
                    {
                        programPartLines[i] = line.TrimStart();
                        break;
                    }
                    if (tabs >= indent)
                    {
                        programPartLines[i] = line.Substring(j + 1);
                        break;
                    }
                }
            }
            return string.Join("\r\n", programPartLines);
        }

        int TabLengthOfValue(string value)
        {
            return value.Select(c => c == '\t' ? 4 : 1).Sum();
        }

        class SyntaxTrimmer : CSharpSyntaxWalker
        {
            List<SyntaxNode> _programNodes = new List<SyntaxNode>();
            List<SyntaxNode> _extensionNodes = new List<SyntaxNode>();
            List<SyntaxNode> _rootNodes;

            public SyntaxTrimmer()
            {
                _rootNodes = _extensionNodes;
            }

            public IEnumerable<SyntaxNode> ProgramNodes => _programNodes;
            public IEnumerable<SyntaxNode> ExtensionNodes => _extensionNodes;

            public override void VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                if (node.GetFullName(DeclarationFullNameFlags.WithoutNamespaceName) == "Program")
                {
                    _rootNodes = _programNodes;
                    try
                    {
                        base.VisitClassDeclaration(node);
                    }
                    finally
                    {
                        _rootNodes = _extensionNodes;
                    }
                    return;
                }
                _rootNodes.Add(node);
            }

            public override void VisitStructDeclaration(StructDeclarationSyntax node)
            {
                _rootNodes.Add(node);
            }


            public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
            {
                _rootNodes.Add(node);
            }

            public override void VisitEventFieldDeclaration(EventFieldDeclarationSyntax node)
            {
                _rootNodes.Add(node);
            }

            public override void VisitEventDeclaration(EventDeclarationSyntax node)
            {
                _rootNodes.Add(node);
            }

            public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
            {
                _rootNodes.Add(node);
            }

            public override void VisitDelegateDeclaration(DelegateDeclarationSyntax node)
            {
                _rootNodes.Add(node);
            }

            public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
            {
                _rootNodes.Add(node);
            }

            public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
            {
                _rootNodes.Add(node);
            }

            public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                _rootNodes.Add(node);
            }
        }
    }
}
