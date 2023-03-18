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
        public const string INDENT = "    ";

        static MonkiriGata()
        {
            Debug.Log("実行されてる");

            // C# code to parse
            string code = @"
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
                }";

            Format(code);
            // var testTargetProjectFolderPath = "./testTargetProject";

            // foreach (string filePath in Directory.GetFiles(testTargetProjectFolderPath, "*.cs", SearchOption.AllDirectories))
            // {
            //     // .csファイルを1つずつ読み込み、リストに追加する
            //     var fileContent = File.ReadAllText(filePath);
            //     Debug.Log("filePath:" + filePath + " fileContent:" + fileContent);
            //     try
            //     {
            //         Format(fileContent);
            //     }
            //     catch (Exception e)
            //     {
            //         Debug.LogError("filePath:" + filePath + " e:" + e);
            //         return;
            //     }
            // }
        }


        private static void Format(string code)
        {
            var exePath = "./monkiriExe/bin/Release/net7.0/osx-arm64/publish/monkiriGate";
            var process = new D.Process();
            process.StartInfo.Arguments = Convert.ToBase64String(Encoding.UTF8.GetBytes(code));
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

            Debug.Log("output:" + output);
            return;


            var tokenAndKinds = new List<TokenAndKindAndLocation>();
            var lines = output.Split('\n');

            // EoF tokenまで読む
            for (var i = 0; i < lines.Length; i += 3)
            {
                var tokenAndKindAndLoc = new TokenAndKindAndLocation(lines[i], lines[i + 1], lines[i + 2]);
                tokenAndKinds.Add(tokenAndKindAndLoc);
                if (tokenAndKindAndLoc.IsEoF())
                {
                    break;
                }
            }
            return;


            // ASTからコードを構築する。
            var builder = new StringBuilder();
            var before = SK.None;
            var indentLevel = 0;
            var isNewLine = false;
            var isInFor = false;
            var isInForCount = 0;
            foreach (var tokenAndKind in tokenAndKinds)
            {
                var token = tokenAndKind.token;
                var kind = tokenAndKind.kind;

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
                        builder.AppendLine();
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
                        // case SK.
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
                case SK.None:
                case SK.SemicolonToken:
                case SK.DotToken:

                case SK.OpenParenToken:
                case SK.CloseParenToken:
                case SK.OpenBracketToken:
                // case SK.CloseBracketToken:
                case SK.OpenBraceToken:
                case SK.CloseBraceToken:
                case SK.ExclamationToken:
                    return false;

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
                // case SK.
                // case SK.
                // case SK.
                // case SK.

                case SK.CloseBracketToken:
                    return true;
                default:
                    throw new Exception("before:" + beforeKind + " がunhandled.");
                    break;
            }

            return false;
        }
    }

    public struct TokenAndKindAndLocation
    {
        public readonly string token;
        public readonly SK kind;

        public TokenAndKindAndLocation(string tokenLine, string kindLine, string locationLine)
        {
            this.token = tokenLine;
            if (Enum.TryParse<SK>(kindLine, false, out var k))
            {
                this.kind = k;
            }
            else
            {
                Debug.Log("parse failed, token:" + token);
                this.kind = SK.OutOfKind;
            }
            Debug.Log("tokenLine:" + tokenLine + " locationLine:" + locationLine);
        }

        public bool IsEoF()
        {
            return kind == SK.EndOfFileToken;
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