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
            if (false)
            {
                var code0 = @"
                // コメントとかってどうなるんだろ? 全部消される、、？
                using Systeaaaam;

                namespace MyNamespace
                {
                    // うーんこの辺も無理？
                    class MyClass
                    {
                        /*
                            複数行のコメントはどうなる？
                        */
                        static void Main(string[] args)// 後ろコメント
                        {
                            Console./*やめて欲しいレベルのインライン*/WriteLine(""Hello,\n world!"");
                        }
                    }

                    #if A
                    int a = 0;
                    #endif
                }";
                args = new string[2];
                args[0] = Convert.ToBase64String(Encoding.UTF8.GetBytes(code0));
                args[1] = "A,B";
            }

            var base64EndocedCode = args[0];
            var symbols = new string[0];
            var hasDefineSymbols = false;
            if (1 < args.Length)
            {
                hasDefineSymbols = true;
                symbols = args[1].Split(',');
            }

            var code = Encoding.UTF8.GetString(Convert.FromBase64String(base64EndocedCode));



            var totalLineCount = code.Split('\n').Length;

            var options = new CSharpParseOptions().WithDocumentationMode(DocumentationMode.Parse);
            if (hasDefineSymbols)
            {
                options = new CSharpParseOptions().WithDocumentationMode(DocumentationMode.Parse).WithPreprocessorSymbols(symbols);
            }

            var syntaxTree = CSharpSyntaxTree.ParseText(code, options);

            var root = syntaxTree.GetRoot();

            var triviasEnumerable = root
                .DescendantTrivia()
                .Where(t =>
                {
                    var tokenStr = t.ToString();
                    // if (string.IsNullOrEmpty(tokenStr))
                    // {
                    //     return false;
                    // }

                    if (string.IsNullOrWhiteSpace(tokenStr))
                    {
                        return false;
                    }

                    if (tokenStr == " ")
                    {
                        return false;
                    }

                    if (tokenStr == "\n")
                    {
                        return false;
                    }

                    return true;
                })
                .Select(trivia =>
                {
                    // Console.WriteLine("triviaLine:" + trivia.ToString() + " kind:" + (int)trivia.Kind());
                    return new ASTContainer(trivia.ToString(), (int)trivia.Kind(), syntaxTree.GetLineSpan(trivia.Span));
                });

            var tokensEnumerable = root
                .DescendantTokens()
                .Select(t =>
                {
                    return new ASTContainer(t.ToString(), (int)t.Kind(), syntaxTree.GetLineSpan(t.Span));
                });

            var builder = new StringBuilder();

            // // triviaとtokenを混ぜて、出てくる順に並べる。
            var tokenAndTrivias = triviasEnumerable.Concat(tokensEnumerable).ToList()
                .OrderBy(t =>
                {
                    var location = t.Span;
                    var startLine = location.StartLinePosition.Line;
                    return startLine;
                })
                .ThenBy(t =>
                {
                    var location = t.Span;
                    var startColumn = location.StartLinePosition.Character;
                    return startColumn;
                }
            );

            foreach (var token in tokenAndTrivias)
            {
                builder.AppendLine(Convert.ToBase64String(Encoding.UTF8.GetBytes(token.Token)));
                builder.AppendLine(token.Kind.ToString());

                var location = token.Span;
                var startLine = location.StartLinePosition.Line;
                var startColumn = location.StartLinePosition.Character;
                var endLine = location.EndLinePosition.Line;
                var endColumn = location.EndLinePosition.Character;
                builder.AppendLine(startLine + "," + startColumn + "," + endLine + "," + endColumn);
            }

            Console.WriteLine(builder.ToString());
        }
    }

    public class ASTContainer
    {
        public readonly string Token;
        public readonly int Kind;
        public readonly Microsoft.CodeAnalysis.FileLinePositionSpan Span;

        public ASTContainer(string token, int kind, Microsoft.CodeAnalysis.FileLinePositionSpan span)
        {
            this.Token = token;
            this.Kind = kind;
            this.Span = span;
        }
    }
}