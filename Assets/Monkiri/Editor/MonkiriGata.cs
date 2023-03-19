using D = System.Diagnostics;
using UnityEditor;
using UnityEngine;
using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using System.IO;

namespace MonkiriGata.Core
{
    [InitializeOnLoad]
    class MonkiriGata
    {
        public const string INDENT = "    ";// 4スペース

        static MonkiriGata()
        {
            Debug.Log("実行されてる");

            // C# code to parse
            string code = @"
                // コメントとかってどうなるんだろ? 全部消される、、？ -> 残せるようになった。
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

                        /// ドキュメントコメントフォーマット
                        void Something()
                        {

                        }

                        #if UNITY_EDITOR
                        // コメントと
                        int sampleInt = 0;
                        #endif
                    }
                }";


            Format(code, "UNITY_EDITOR");


            var testTargetProjectFolderPath = "./testTargetProject";

            foreach (string filePath in Directory.GetFiles(testTargetProjectFolderPath, "*.cs", SearchOption.AllDirectories))
            {
                // .csファイルを1つずつ読み込み、リストに追加する
                var fileContent = File.ReadAllText(filePath);
                Debug.Log("filePath:" + filePath + " fileContent:" + fileContent);
                try
                {
                    Format(fileContent, "UNITY_EDITOR");
                }
                catch (Exception e)
                {
                    Debug.LogError("filePath:" + filePath + " e:" + e);
                    return;
                }
            }
        }

        /*
            フォーマット処理
        */
        private static void Format(string code, params string[] defines)
        {
            var exePath = "./monkiriExe/bin/Release/net7.0/osx-arm64/publish/monkiriGate";
            var process = new D.Process();
            process.StartInfo.Arguments = Convert.ToBase64String(Encoding.UTF8.GetBytes(code)) + " " + string.Join(",", defines);
            process.StartInfo.FileName = exePath;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (!string.IsNullOrEmpty(error))
            {
                Debug.Log("failed to read AST from code. error:" + error);
                return;
            }
            // Debug.Log("output:" + output);

            var tokenAndKinds = new List<TokenAndKindAndLocation>();
            var lines = output.Split('\n');

            // EoF tokenまで読む
            for (var i = 0; i < lines.Length; i += 3)
            {
                var tokenLine = Encoding.UTF8.GetString(Convert.FromBase64String(lines[i]));
                var kindLine = lines[i + 1];
                var location = lines[i + 2];

                // Debug.Log("token:" + tokenLine + " kind:" + kindLine + " location:" + location);

                var isContinued = false;
                if ((i + 2) + 3 < lines.Length && IsEndLineContinues(location, lines[(i + 2) + 3]))
                {
                    isContinued = true;
                }


                var tokenAndKindAndLoc = new TokenAndKindAndLocation(tokenLine, kindLine, isContinued);
                // Debug.Log("token:" + tokenAndKindAndLoc.Token + " kind:" + tokenAndKindAndLoc.Kind + " continued:" + tokenAndKindAndLoc.IsContinedToAnotherToken + " location:" + location);

                tokenAndKinds.Add(tokenAndKindAndLoc);

                if (tokenAndKindAndLoc.IsEoF())
                {
                    break;
                }
            }

            // ASTからコードを構築する。
            var builder = new StringBuilder();
            var before = SK.None;
            var indentLevel = 0;
            var isNewLine = false;
            var isInFor = false;
            var isInForCount = 0;
            foreach (var tokenAndKind in tokenAndKinds)
            {
                var token = tokenAndKind.Token;
                var kind = tokenAndKind.Kind;
                // Debug.Log("レイアウト時 token:" + token + " isNewLine:" + isNewLine + " kokomade:" + builder.ToString());

                // 空のものは飛ばす
                if (string.IsNullOrEmpty(token))
                {
                    switch (kind)
                    {
                        case SK.EndOfFileToken:
                        case SK.OmittedArraySizeExpressionToken:
                        case SK.IdentifierToken:
                        case SK.GreaterThanToken:
                            continue;
                        case SK.CommaToken:
                            token = ".";
                            break;
                        case SK.CloseParenToken:
                            token = ")";
                            break;

                        default:
                            throw new Exception("kind:" + kind + " は空っぽ");
                    }
                }

                switch (kind)
                {
                    case SK.OpenBraceToken:// {
                        // ここだ、{の位置に種類がある。newLineの場合は改行しないでいい。
                        if (isNewLine)
                        {
                            // pass.
                        }
                        else
                        {
                            builder.AppendLine();
                        }
                        builder.AppendTokenWithIndent(token, indentLevel);
                        indentLevel++;
                        builder.AppendLine();

                        isNewLine = true;
                        break;
                    case SK.CloseBraceToken:// }
                        indentLevel--;
                        builder.AppendTokenWithIndent(token, indentLevel);
                        builder.AppendLine();

                        isNewLine = true;
                        break;

                    case SK.SemicolonToken:
                        builder.Append(token);

                        // 必要だったら改行する
                        if (!isInFor)
                        {
                            builder.AppendLine();
                            isNewLine = true;
                        }
                        break;

                    case SK.SingleLineCommentTrivia:
                        if (isNewLine)
                        {
                            builder.AppendTokenWithIndent(token, indentLevel);
                        }
                        else
                        {
                            builder.Append(token);
                        }

                        // singleLineは必ず改行する必要がある
                        builder.AppendLine();
                        isNewLine = true;
                        break;

                    case SK.SingleLineDocumentationCommentTrivia:
                        if (isNewLine)
                        {
                            builder.AppendTokenWithIndent("/// " + token, indentLevel);
                        }
                        else
                        {
                            builder.Append("/// " + token);
                        }

                        // ドキュメントはなぜかデフォルトで改行が含まれる。不思議。
                        isNewLine = true;
                        break;

                    case SK.MultiLineCommentTrivia:
                        var multilineCommentTokenLines = token.Split('\n');

                        var lastIndex = multilineCommentTokenLines.Length - 1;

                        var contentLines = new List<string>();

                        // /*
                        var firstLine = multilineCommentTokenLines[0];
                        contentLines.Add(firstLine);

                        // 複数行ある場合
                        if (1 < multilineCommentTokenLines.Length)
                        {
                            for (var i = 0; i < Mathf.Max(lastIndex - 1, 0); i++)
                            {
                                var middleLine = multilineCommentTokenLines[1 + i];

                                // [ ]コメント
                                contentLines.Add(INDENT + middleLine.TrimStart(' ', '\t'));
                            }

                            // [ ]*/
                            var lastLine = " " + multilineCommentTokenLines[lastIndex].TrimStart(' ', '\t');
                            contentLines.Add(lastLine);
                        }

                        if (isNewLine)
                        {
                            // 複数行が新しい行で始まるので、すべての行をappendすればいい。
                            for (var i = 0; i < contentLines.Count; i++)
                            {
                                var commentToken = contentLines[i];
                                builder.AppendTokenWithIndent(commentToken, indentLevel);
                                // 最終行未満の行には改行を加える
                                if (i < contentLines.Count - 1)
                                {
                                    builder.AppendLine();
                                }
                            }
                        }
                        else
                        {
                            // TODO: 途中からの継続で改行が入る場合はわかんないけど非常に嫌なコード書かれてそうなので、このケースに遭遇したら何かしよう。
                            Debug.LogWarning((contentLines.Count == 1) + "途中からで複数行コメント");
                            var commentToken = string.Join("\n", contentLines);
                            builder.Append(commentToken);
                        }

                        if (tokenAndKind.IsContinedToAnotherToken)
                        {
                            // 別のtokenにつながっているのでnewLine判定は終わりになる。
                            isNewLine = false;
                            break;
                        }

                        builder.AppendLine();
                        isNewLine = true;
                        break;

                    case SK.MultiLineDocumentationCommentTrivia:
                        if (isNewLine)
                        {
                            builder.AppendTokenWithIndent("/*" + token, indentLevel);
                        }
                        else
                        {
                            builder.Append("/*" + token);
                        }

                        if (tokenAndKind.IsContinedToAnotherToken)
                        {
                            // 別のtokenにつながっているのでnewLine判定は終わりになる。
                            isNewLine = false;
                            break;
                        }

                        builder.AppendLine();
                        isNewLine = true;
                        break;

                    case SK.IfDirectiveTrivia:
                    case SK.ElifDirectiveTrivia:
                    case SK.ElseDirectiveTrivia:
                    case SK.EndIfDirectiveTrivia:
                        builder.Append(token);
                        builder.AppendLine();
                        isNewLine = true;
                        break;

                    case SK.PragmaWarningDirectiveTrivia:
                    case SK.RegionDirectiveTrivia:
                    case SK.EndRegionDirectiveTrivia:
                        builder.AppendTokenWithIndent(token, indentLevel);
                        builder.AppendLine();
                        isNewLine = true;
                        break;

                    // identifier, NumericLiteralToken など
                    case SK.IdentifierToken:
                    case SK.CommaToken:
                    case SK.NumericLiteralToken:
                    case SK.UsingKeyword:
                    case SK.NamespaceKeyword:
                    case SK.ClassKeyword:
                    case SK.StaticKeyword:
                    case SK.VoidKeyword:
                    case SK.StringKeyword:
                    case SK.StringLiteralToken:
                    case SK.LessThanToken:
                    case SK.EqualsToken:
                    case SK.NewKeyword:
                    case SK.PlusToken:
                    case SK.MinusToken:
                    case SK.PublicKeyword:
                    case SK.DelegateKeyword:
                    case SK.GreaterThanToken:
                    case SK.IntKeyword:
                    case SK.ObjectKeyword:
                    case SK.StructKeyword:
                    case SK.ReadOnlyKeyword:
                    case SK.BoolKeyword:
                    case SK.FalseKeyword:
                    case SK.ThisKeyword:
                    case SK.ReturnKeyword:
                    case SK.BarBarToken:
                    case SK.OverrideKeyword:
                    case SK.ForKeyword:
                    case SK.PlusPlusToken:
                    case SK.MinusMinusToken:
                    case SK.ExclamationToken:
                    case SK.EqualsEqualsToken:
                    case SK.UIntKeyword:
                    case SK.LongKeyword:
                    case SK.PrivateKeyword:
                    case SK.IfKeyword:
                    case SK.LessThanEqualsToken:
                    case SK.AmpersandAmpersandToken:
                    case SK.NullKeyword:
                    case SK.ExclamationEqualsToken:
                    case SK.ElseKeyword:
                    case SK.DoubleKeyword:
                    case SK.EqualsGreaterThanToken:
                    case SK.AsKeyword:
                    case SK.WhileKeyword:
                    case SK.YieldKeyword:
                    case SK.ForEachKeyword:
                    case SK.InKeyword:
                    case SK.BreakKeyword:
                    case SK.EnumKeyword:
                    case SK.ConstKeyword:
                    case SK.PlusEqualsToken:
                    case SK.WhereKeyword:
                    case SK.ColonToken:
                    case SK.ContinueKeyword:
                    case SK.TrueKeyword:
                    case SK.SwitchKeyword:
                    case SK.CaseKeyword:
                    case SK.DefaultKeyword:
                    case SK.ProtectedKeyword:
                    case SK.VirtualKeyword:
                    case SK.TryKeyword:
                    case SK.TypeOfKeyword:
                    case SK.CatchKeyword:
                    case SK.AsteriskToken:
                    case SK.SlashToken:
                    case SK.PartialKeyword:
                    case SK.ThrowKeyword:
                    case SK.OutKeyword:
                    case SK.ByteKeyword:
                    case SK.IsKeyword:
                    case SK.GotoKeyword:
                    case SK.LockKeyword:
                    case SK.ParamsKeyword:
                    case SK.CharKeyword:
                    case SK.CharacterLiteralToken:
                    case SK.InterfaceKeyword:
                    case SK.RefKeyword:
                    case SK.GetKeyword:
                    case SK.SetKeyword:
                    case SK.FloatKeyword:
                    case SK.BarToken:
                    case SK.AsyncKeyword:
                    case SK.AwaitKeyword:
                    case SK.WhenKeyword:
                    case SK.PercentToken:
                    case SK.QuestionToken:
                    case SK.FinallyKeyword:
                    case SK.GreaterThanEqualsToken:
                    case SK.AmpersandToken:
                    case SK.InternalKeyword:
                    case SK.BaseKeyword:
                    case SK.BarEqualsToken:
                    case SK.EventKeyword:
                    case SK.AmpersandEqualsToken:
                    case SK.OperatorKeyword:
                    case SK.MinusEqualsToken:
                    case SK.SealedKeyword:
                    case SK.SByteKeyword:
                    case SK.ShortKeyword:
                    case SK.UShortKeyword:
                    case SK.ULongKeyword:
                    case SK.DecimalKeyword:
                    case SK.AbstractKeyword:
                    case SK.TildeToken:
                    case SK.DisabledTextTrivia:
                        // case SK.
                        // case SK.
                        // case SK.
                        // case SK.
                        // 直前のものがtokenだったり,だったらスペースを開ける
                        if (ShouldAddSpeceBefore(before, kind))
                        {
                            builder.Append(" " + token);
                            continue;
                        }

                        // この中だけ;が改行しない
                        if (kind == SK.ForKeyword)
                        {
                            isInFor = true;
                        }

                        // 新規の行であればindentを見る
                        if (isNewLine)
                        {
                            builder.AppendTokenWithIndent(token, indentLevel);
                        }
                        else
                        {
                            builder.Append(token);
                        }

                        switch (kind)
                        {
                            case SK.SemicolonToken:
                                builder.AppendLine();
                                break;
                        }

                        if (isNewLine)
                        {
                            isNewLine = false;
                        }
                        break;

                    // 括弧類
                    case SK.OpenParenToken:
                    case SK.CloseParenToken:
                    case SK.OpenBracketToken:
                    case SK.CloseBracketToken:
                        // forの中だけ、()の一致を見て、0になった瞬間にforが終わっているので、ここ以外では;に改行力を持たせる。
                        if (isInFor)
                        {
                            switch (kind)
                            {
                                case SK.OpenParenToken:
                                    isInForCount++;
                                    break;
                                case SK.CloseParenToken:
                                    isInForCount--;
                                    if (isInForCount == 0)
                                    {
                                        // ここでfor()が終わっているので、モードを終わらせる。
                                        isInFor = false;
                                    }
                                    break;
                            }
                        }

                        // 新規の行であればindentを見る
                        if (isNewLine)
                        {
                            builder.AppendTokenWithIndent(token, indentLevel);
                        }
                        else
                        {
                            builder.Append(token);
                        }

                        if (isNewLine)
                        {
                            isNewLine = false;
                        }
                        break;

                    case SK.DotToken:
                        builder.Append(token);
                        break;


                    default:
                        throw new Exception("token:" + token + " kind:" + kind);
                }

                before = kind;
            }
            Debug.Log("result:" + builder);
        }

        private static bool ShouldAddSpeceBefore(SK beforeKind, SK currentKind)
        {
            // 特定のtokenの場合は前にかかわらずIDでない = 隙間を開けない。
            switch (currentKind)
            {
                case SK.PlusPlusToken:
                case SK.MinusMinusToken:
                case SK.CommaToken:
                    return false;
            }

            switch (beforeKind)
            {
                // このグループが前方に来ている場合、スペースを開けない
                case SK.None:
                case SK.SemicolonToken:
                case SK.DotToken:

                case SK.OpenParenToken:
                case SK.CloseParenToken:
                case SK.OpenBracketToken:
                // case SK.CloseBracketToken: close ] は.などがつながる場合がある。
                case SK.OpenBraceToken:
                case SK.CloseBraceToken:
                case SK.ExclamationToken:

                case SK.SingleLineCommentTrivia:
                case SK.SingleLineDocumentationCommentTrivia:
                case SK.MultiLineCommentTrivia:
                case SK.MultiLineDocumentationCommentTrivia:

                case SK.IfDirectiveTrivia:
                case SK.ElifDirectiveTrivia:
                case SK.ElseDirectiveTrivia:
                case SK.EndIfDirectiveTrivia:

                case SK.PragmaWarningDirectiveTrivia:
                case SK.RegionDirectiveTrivia:
                case SK.EndRegionDirectiveTrivia:
                    return false;

                // このグループが前方に来ている場合、スペースを開ける
                case SK.IdentifierToken:
                case SK.NumericLiteralToken:
                case SK.UsingKeyword:
                case SK.NamespaceKeyword:
                case SK.ClassKeyword:
                case SK.StaticKeyword:
                case SK.VoidKeyword:
                case SK.StringKeyword:
                case SK.StringLiteralToken:
                case SK.LessThanToken:
                case SK.EqualsToken:
                case SK.NewKeyword:
                case SK.PlusToken:
                case SK.MinusToken:
                case SK.CommaToken:
                case SK.PublicKeyword:
                case SK.BoolKeyword:
                case SK.ReturnKeyword:
                case SK.BarBarToken:
                case SK.PrivateKeyword:
                case SK.YieldKeyword:
                case SK.WhereKeyword:
                case SK.ReadOnlyKeyword:
                case SK.AmpersandAmpersandToken:
                case SK.CaseKeyword:
                case SK.DefaultKeyword:
                case SK.ProtectedKeyword:
                case SK.SlashToken:
                case SK.AsteriskToken:
                case SK.EqualsGreaterThanToken:
                case SK.ThrowKeyword:
                case SK.ObjectKeyword:
                case SK.EqualsEqualsToken:
                case SK.IntKeyword:
                case SK.GotoKeyword:
                case SK.ParamsKeyword:
                case SK.AsKeyword:
                case SK.FloatKeyword:
                case SK.ExclamationEqualsToken:
                case SK.OutKeyword:
                case SK.AwaitKeyword:
                case SK.ConstKeyword:
                case SK.AsyncKeyword:
                case SK.ColonToken:
                case SK.DoubleKeyword:
                case SK.ElseKeyword:
                case SK.GreaterThanToken:
                case SK.PlusPlusToken:
                case SK.QuestionToken:
                case SK.ThisKeyword:
                case SK.InternalKeyword:
                case SK.EnumKeyword:
                case SK.PercentToken:
                case SK.GreaterThanEqualsToken:
                case SK.SealedKeyword:
                case SK.CharKeyword:
                case SK.LongKeyword:
                case SK.MinusMinusToken:
                case SK.NullKeyword:
                case SK.StructKeyword:
                case SK.AmpersandToken:
                case SK.RefKeyword:
                // case SK.

                case SK.CloseBracketToken:
                    return true;
                default:
                    throw new Exception("before:" + beforeKind + " がunhandled.");
                    break;
            }

            return false;
        }

        private static bool IsEndLineContinues(string currentLocation, string nextLocation)
        {
            var locationInfo = currentLocation.Split(',');
            var nextLocationInfo = nextLocation.Split(',');
            if (locationInfo[2] == nextLocationInfo[0])
            {
                return true;
            }

            return false;
        }
    }

    public struct TokenAndKindAndLocation
    {
        public readonly string Token;
        public readonly SK Kind;
        public readonly bool IsContinedToAnotherToken;

        public TokenAndKindAndLocation(string tokenLine, string kindLine, bool isContinedToAnotherToken)
        {
            this.Token = tokenLine;
            if (Enum.TryParse<SK>(kindLine, false, out var k))
            {
                this.Kind = k;
            }
            else
            {
                Debug.LogError("parse failed, token:" + Token);
                this.Kind = SK.OutOfKind;
            }

            this.IsContinedToAnotherToken = isContinedToAnotherToken;
        }

        public bool IsEoF()
        {
            return Kind == SK.EndOfFileToken;
        }
    }

    public static class StringBuilderExtension
    {
        public static void AppendTokenWithIndent(this StringBuilder sb, string token, int indentLevel)
        {
            for (var i = 0; i < indentLevel; i++)
            {
                sb.Append(MonkiriGata.INDENT);
            }
            sb.Append(token);
        }
    }
}