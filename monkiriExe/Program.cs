using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CSharpSyntaxAnalysis
{
    class MonkiriExe
    {
        static void Main(string[] args)
        {
            string base64EndocedCode = args[0];
            var code = Encoding.UTF8.GetString(Convert.FromBase64String(base64EndocedCode));

            var totalLineCount = code.Split('\n').Length;

            var options = new CSharpParseOptions().WithDocumentationMode(DocumentationMode.Diagnose);
            var syntaxTree = CSharpSyntaxTree.ParseText(code, options);

            var root = syntaxTree.GetRoot();

            var triviasEnumerable = root.DescendantTrivia();
            var tokensEnumerable = root.DescendantTokens();

            var builder = new StringBuilder();

            // triviaとtokenを混ぜて、出てくる順に並べる。
            var tokens = tokensEnumerable.ToList();

            var tokenAndTrivias = new List<SyntaxNode>();
            var toIndex = 0;
            for (var i = 0; i < totalLineCount; i++)
            {
                FileLinePositionSpan tokenLocation;
                if (toIndex < tokens.Count())
                {
                    tokenLocation = syntaxTree.GetLineSpan(tokens[toIndex].Span);
                }
                // TODO: ここでlocation順になんとかすればいける。
                // if ()
                // {

                //     continue;
                // }
            }



            // foreach (var token in tokenAndTrivias)
            // {
            //     builder.AppendLine(token.ToString());
            //     builder.AppendLine("" + (int)token.Kind());

            //     var location = syntaxTree.GetLineSpan(token.Span);
            //     var startLine = location.StartLinePosition.Line;
            //     var startColumn = location.StartLinePosition.Character;
            //     var endLine = location.EndLinePosition.Line;
            //     var endColumn = location.EndLinePosition.Character;
            //     builder.AppendLine(startLine + "," + startColumn + "," + endLine + "," + endColumn);
            // }

            Console.WriteLine(builder.ToString());
        }
    }
}