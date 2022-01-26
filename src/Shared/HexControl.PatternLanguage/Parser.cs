﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using HexControl.Core.Buffers.Extensions;
using HexControl.Core.Helpers;
using HexControl.Core.Numerics;
using HexControl.PatternLanguage.AST;
using HexControl.PatternLanguage.Exceptions;
using HexControl.PatternLanguage.Literals;
using HexControl.PatternLanguage.Tokens;

namespace HexControl.PatternLanguage;

internal class Parser
{
    private static readonly Token KeywordStruct = CreateToken(Token.TokenType.Keyword, Token.Keyword.Struct);
    private static readonly Token KeywordUnion = CreateToken(Token.TokenType.Keyword, Token.Keyword.Union);
    private static readonly Token KeywordUsing = CreateToken(Token.TokenType.Keyword, Token.Keyword.Using);
    private static readonly Token KeywordEnum = CreateToken(Token.TokenType.Keyword, Token.Keyword.Enum);
    private static readonly Token KeywordBitfield = CreateToken(Token.TokenType.Keyword, Token.Keyword.Bitfield);
    private static readonly Token KeywordLe = CreateToken(Token.TokenType.Keyword, Token.Keyword.LittleEndian);
    private static readonly Token KeywordBe = CreateToken(Token.TokenType.Keyword, Token.Keyword.BigEndian);
    private static readonly Token KeywordIf = CreateToken(Token.TokenType.Keyword, Token.Keyword.If);
    private static readonly Token KeywordElse = CreateToken(Token.TokenType.Keyword, Token.Keyword.Else);
    private static readonly Token KeywordParent = CreateToken(Token.TokenType.Keyword, Token.Keyword.Parent);
    private static readonly Token KeywordThis = CreateToken(Token.TokenType.Keyword, Token.Keyword.This);
    private static readonly Token KeywordWhile = CreateToken(Token.TokenType.Keyword, Token.Keyword.While);
    private static readonly Token KeywordFor = CreateToken(Token.TokenType.Keyword, Token.Keyword.For);
    private static readonly Token KeywordFunction = CreateToken(Token.TokenType.Keyword, Token.Keyword.Function);
    private static readonly Token KeywordReturn = CreateToken(Token.TokenType.Keyword, Token.Keyword.Return);
    private static readonly Token KeywordNamespace = CreateToken(Token.TokenType.Keyword, Token.Keyword.Namespace);
    private static readonly Token KeywordIn = CreateToken(Token.TokenType.Keyword, Token.Keyword.In);
    private static readonly Token KeywordOut = CreateToken(Token.TokenType.Keyword, Token.Keyword.Out);
    private static readonly Token KeywordBreak = CreateToken(Token.TokenType.Keyword, Token.Keyword.Break);
    private static readonly Token KeywordContinue = CreateToken(Token.TokenType.Keyword, Token.Keyword.Continue);
    private static readonly Token Integer = CreateToken(Token.TokenType.Integer, (UInt128)0);

    private static readonly Token
        Identifier = CreateToken(Token.TokenType.Identifier, ""); // TODO: shouldn't this be Token.Identifier?

    private static readonly Token String = CreateToken(Token.TokenType.String, (Literal)"");
    private static readonly Token OperatorAt = CreateToken(Token.TokenType.Operator, Token.Operator.AtDeclaration);
    private static readonly Token OperatorAssignment = CreateToken(Token.TokenType.Operator, Token.Operator.Assignment);
    private static readonly Token OperatorInherit = CreateToken(Token.TokenType.Operator, Token.Operator.Inherit);
    private static readonly Token OperatorPlus = CreateToken(Token.TokenType.Operator, Token.Operator.Plus);
    private static readonly Token OperatorMinus = CreateToken(Token.TokenType.Operator, Token.Operator.Minus);
    private static readonly Token OperatorStar = CreateToken(Token.TokenType.Operator, Token.Operator.Star);
    private static readonly Token OperatorSlash = CreateToken(Token.TokenType.Operator, Token.Operator.Slash);
    private static readonly Token OperatorPercent = CreateToken(Token.TokenType.Operator, Token.Operator.Percent);
    private static readonly Token OperatorShiftleft = CreateToken(Token.TokenType.Operator, Token.Operator.ShiftLeft);
    private static readonly Token OperatorShiftright = CreateToken(Token.TokenType.Operator, Token.Operator.ShiftRight);
    private static readonly Token OperatorBitor = CreateToken(Token.TokenType.Operator, Token.Operator.BitOr);
    private static readonly Token OperatorBitand = CreateToken(Token.TokenType.Operator, Token.Operator.BitAnd);
    private static readonly Token OperatorBitxor = CreateToken(Token.TokenType.Operator, Token.Operator.BitXor);
    private static readonly Token OperatorBitnot = CreateToken(Token.TokenType.Operator, Token.Operator.BitNot);
    private static readonly Token OperatorBoolequals = CreateToken(Token.TokenType.Operator, Token.Operator.BoolEquals);

    private static readonly Token OperatorBoolnotequals =
        CreateToken(Token.TokenType.Operator, Token.Operator.BoolNotEquals);

    private static readonly Token
        OperatorBoolgreaterthan = CreateToken(Token.TokenType.Operator, Token.Operator.BoolGreaterThan);

    private static readonly Token OperatorBoollessthan =
        CreateToken(Token.TokenType.Operator, Token.Operator.BoolLessThan);

    private static readonly Token OperatorBoolgreaterthanorequals =
        CreateToken(Token.TokenType.Operator, Token.Operator.BoolGreaterThanOrEquals);

    private static readonly Token OperatorBoollessthanorequals =
        CreateToken(Token.TokenType.Operator, Token.Operator.BoolLessThanOrEquals);

    private static readonly Token OperatorBooland = CreateToken(Token.TokenType.Operator, Token.Operator.BoolAnd);
    private static readonly Token OperatorBoolor = CreateToken(Token.TokenType.Operator, Token.Operator.BoolOr);
    private static readonly Token OperatorBoolxor = CreateToken(Token.TokenType.Operator, Token.Operator.BoolXor);
    private static readonly Token OperatorBoolnot = CreateToken(Token.TokenType.Operator, Token.Operator.BoolNot);

    private static readonly Token OperatorTernaryconditional =
        CreateToken(Token.TokenType.Operator, Token.Operator.TernaryConditional);

    private static readonly Token OperatorDollar = CreateToken(Token.TokenType.Operator, Token.Operator.Dollar);
    private static readonly Token OperatorAddressof = CreateToken(Token.TokenType.Operator, Token.Operator.AddressOf);
    private static readonly Token OperatorSizeof = CreateToken(Token.TokenType.Operator, Token.Operator.SizeOf);

    private static readonly Token
        OperatorScoperesolution = CreateToken(Token.TokenType.Operator, Token.Operator.ScopeResolution);

    private static readonly Token ValuetypePadding = CreateToken(Token.TokenType.ValueType, Token.ValueType.Padding);
    private static readonly Token ValuetypeAny = CreateToken(Token.TokenType.ValueType, Token.ValueType.Any);

    private static readonly Token SeparatorRoundbracketopen =
        CreateToken(Token.TokenType.Separator, Token.Separator.RoundBracketOpen);

    private static readonly Token SeparatorRoundbracketclose =
        CreateToken(Token.TokenType.Separator, Token.Separator.RoundBracketClose);

    private static readonly Token SeparatorCurlybracketopen =
        CreateToken(Token.TokenType.Separator, Token.Separator.CurlyBracketOpen);

    private static readonly Token SeparatorCurlybracketclose =
        CreateToken(Token.TokenType.Separator, Token.Separator.CurlyBracketClose);

    private static readonly Token SeparatorSquarebracketopen =
        CreateToken(Token.TokenType.Separator, Token.Separator.SquareBracketOpen);

    private static readonly Token SeparatorSquarebracketclose =
        CreateToken(Token.TokenType.Separator, Token.Separator.SquareBracketClose);

    private static readonly Token SeparatorComma = CreateToken(Token.TokenType.Separator, Token.Separator.Comma);
    private static readonly Token SeparatorDot = CreateToken(Token.TokenType.Separator, Token.Separator.Dot);

    private static readonly Token SeparatorEndofexpression =
        CreateToken(Token.TokenType.Separator, Token.Separator.EndOfExpression);

    private static readonly Token SeparatorEndofprogram =
        CreateToken(Token.TokenType.Separator, Token.Separator.EndOfProgram);

    private readonly List<List<string>> _currentNamespace = new();

    private readonly Dictionary<string, ASTNode> _types = new();

    private bool _hasReset = true;

    private int _originalTokenIndex;
    private int _tokenIndex;
    private IReadOnlyList<Token> _tokens;

    public Parser()
    {
        _tokens = new List<Token>();
    }

    public Token GetToken(int offset) => _tokens[_tokenIndex + offset];

    private static Token CreateToken(Token.TokenType type, Token.Separator separator) =>
        CreateToken(type, new Token.EnumValue<Token.Separator>(separator));

    private static Token CreateToken(Token.TokenType type, Token.Operator @operator) =>
        CreateToken(type, new Token.EnumValue<Token.Operator>(@operator));

    private static Token CreateToken(Token.TokenType type, Token.Keyword keyword) =>
        CreateToken(type, new Token.EnumValue<Token.Keyword>(keyword));

    private static Token CreateToken(Token.TokenType type, Token.ValueType valueType) =>
        CreateToken(type, new Token.EnumValue<Token.ValueType>(valueType));

    private static Token CreateToken<TType>(Token.TokenType type, TType value)
        where TType : Token.ITokenValue, IEquatable<TType> => new Token<TType>(type, value, -1, -1, -1);

    private static Token CreateToken(Token.TokenType type, Literal literal) =>
        CreateToken(type, new Token.LiteralValue(literal));

    private static Token CreateToken(Token.TokenType type, string str) =>
        CreateToken(type, new Token.IdentifierValue(str));

    public List<ASTNode> Parse(List<Token> tokens)
    {
        _types.Clear();
        _currentNamespace.Clear();
        _currentNamespace.Add(new List<string>());
        _tokens = tokens;

        var program = ParseTillToken(SeparatorEndofprogram);

        if (program.Count == 0 || _tokenIndex != _tokens.Count)
        {
            throw new ParserException(this, "program is empty!");
        }

        return program;
    }

    private int GetLineNumber(int index) => _tokens[_tokenIndex + index].LineNumber;


    private T Create<T>(T node) where T : ASTNode
    {
        node.LineNumber = GetLineNumber(-1);
        return node;
    }

    private T GetValue<T>(int index) where T : notnull
    {
        var token = GetToken(index);
        return token switch
        {
            Token<Token.EnumValue<T>> enumValue => enumValue.Value.Value,
            Token<Token.LiteralValue> {Value: {Literal: T literal}} => literal,
            _ => throw new ParserException(this, "failed to decode token, invalid type", index)
        };
    }

    private Token.IdentifierValue GetIdentifier(int index)
    {
        var token = _tokens[_tokenIndex + index];
        if (token is Token<Token.IdentifierValue> identifier)
        {
            return identifier.Value;
        }

        throw new ParserException(this, "failed to decode token, expected an identifier", index);
    }

    private string GetNamespacePrefixedName(string name)
    {
        var builder = ObjectPool<StringBuilder>.Shared.Rent();

        try
        {
            builder.Clear();
            foreach (var part in _currentNamespace.Last())
            {
                builder.Append(part).Append("::");
            }

            builder.Append(name);

            return builder.ToString();
        }
        finally
        {
            ObjectPool<StringBuilder>.Shared.Return(builder);
        }
    }

    private ASTNode ParseFunctionCall()
    {
        var functionName = ParseNamespaceResolution();

        if (!Sequence(SeparatorRoundbracketopen))
        {
            throw new ParserException(this, "expected '(' after function name");
        }

        var @params = new List<ASTNode>();

        while (!Sequence(SeparatorRoundbracketclose))
        {
            @params.Add(ParseMathematicalExpression());

            if (Sequence(SeparatorComma, SeparatorRoundbracketclose))
            {
                throw new ParserException(this, "unexpected ',' at end of function parameter list");
            }

            if (Sequence(SeparatorRoundbracketclose))
            {
                break;
            }

            if (!Sequence(SeparatorComma))
            {
                throw new ParserException(this, "missing ',' between parameters");
            }
        }

        return Create(new ASTNodeFunctionCall(functionName, @params));
    }

    private ASTNode ParseStringLiteral()
    {
        var literal = GetValue<Literal>(-1);
        if (literal is null)
        {
            throw new InvalidOperationException("literal should not be null");
        }

        return Create(new ASTNodeLiteral(literal));
    }

    private string ParseNamespaceResolution()
    {
        var builder = ObjectPool<StringBuilder>.Shared.Rent();
        try
        {
            builder.Clear();
            while (true)
            {
                builder.Append(GetIdentifier(-1).Value);

                if (Sequence(OperatorScoperesolution, Identifier))
                {
                    builder.Append("::");
                }
                else
                {
                    break;
                }
            }

            return builder.ToString();
        }
        finally
        {
            ObjectPool<StringBuilder>.Shared.Return(builder);
        }
    }

    private ASTNode ParseScopeResolution()
    {
        var builder = ObjectPool<StringBuilder>.Shared.Rent();

        try
        {
            builder.Clear();
            while (true)
            {
                builder.Append(GetIdentifier(-1).Value);

                if (Sequence(OperatorScoperesolution, Identifier))
                {
                    if (Peek(OperatorScoperesolution) && Peek(Identifier, 1))
                    {
                        builder.Append("::");
                    }
                    else
                    {
                        var name = builder.ToString();
                        if (!_types.ContainsKey(name))
                        {
                            throw new ParserException(this, $"cannot access scope of invalid type '{builder}'", -1);
                        }

                        return Create(new ASTNodeScopeResolution(_types[name].Clone(),
                            GetIdentifier(-1).Value));
                    }
                }
                else
                {
                    break;
                }
            }

            throw new ParserException(this, "failed to parse scope resolution, expected 'TypeName::Identifier'");
        }
        finally
        {
            ObjectPool<StringBuilder>.Shared.Return(builder);
        }
    }

    private ASTNode ParseRValue(ASTNodeRValue.Path path)
    {
        if (Peek(Identifier, -1))
        {
            path.Values.Add(GetIdentifier(-1).Value);
        }
        else if (Peek(KeywordParent, -1))
        {
            path.Values.Add("parent");
        }
        else if (Peek(KeywordThis, -1))
        {
            path.Values.Add("this");
        }

        if (Sequence(SeparatorSquarebracketopen))
        {
            path.Values.Add(ParseMathematicalExpression());
            if (!Sequence(SeparatorSquarebracketclose))
            {
                throw new ParserException(this, "expected closing ']' at end of array indexing");
            }
        }

        if (Sequence(SeparatorDot))
        {
            if (OneOf(Identifier, KeywordParent))
            {
                return ParseRValue(path);
            }

            throw new ParserException(this, "expected member name or 'parent' keyword", -1);
        }

        return Create(new ASTNodeRValue(path));
    }

    private ASTNode ParseFactor()
    {
        if (Sequence(Integer))
        {
            var literal = GetValue<Literal>(-1);
            if (literal is null)
            {
                throw new ParserException(this, "previous token was not a literal", -1);
            }

            return new ASTNodeLiteral(literal);
        }

        if (Peek(OperatorPlus) || Peek(OperatorMinus) || Peek(OperatorBitnot) || Peek(OperatorBoolnot))
        {
            return ParseMathematicalExpression();
        }

        if (Sequence(SeparatorRoundbracketopen))
        {
            var node = ParseMathematicalExpression();
            if (!Sequence(SeparatorRoundbracketclose))
            {
                throw new ParserException(this, "expected closing parenthesis");
            }

            return node;
        }

        if (Sequence(Identifier))
        {
            var originalPos = _tokenIndex;
            ParseNamespaceResolution();
            var isFunction = Peek(SeparatorRoundbracketopen);
            _tokenIndex = originalPos;


            if (isFunction)
            {
                return ParseFunctionCall();
            }

            if (Peek(OperatorScoperesolution))
            {
                return ParseScopeResolution();
            }

            return ParseRValue(new ASTNodeRValue.Path());
        }

        if (OneOf(KeywordParent, KeywordThis))
        {
            return ParseRValue(new ASTNodeRValue.Path());
        }

        if (Sequence(OperatorDollar))
        {
            return new ASTNodeRValue(new ASTNodeRValue.Path("$"));
        }

        if (OneOf(OperatorAddressof, OperatorSizeof) && Sequence(SeparatorRoundbracketopen))
        {
            var op = GetValue<Token.Operator>(-2);

            if (!OneOf(Identifier, KeywordParent, KeywordThis))
            {
                throw new ParserException(this, "expected rvalue identifier");
            }

            var node = Create(new ASTNodeTypeOperator(op, ParseRValue(new ASTNodeRValue.Path())));
            if (!Sequence(SeparatorRoundbracketclose))
            {
                throw new ParserException(this, "expected closing parenthesis");
            }

            return node;
        }

        throw new ParserException(this, "expected value or parenthesis");
    }

    private ASTNode ParseCastExpression()
    {
        if (Peek(KeywordBe) || Peek(KeywordLe) || Peek(ValuetypeAny))
        {
            var type = ParseType(true);

            if (type.Type is not ASTNodeBuiltinType)
            {
                throw new ParserException(this, "invalid type used for pointer size");
            }

            if (!Peek(SeparatorRoundbracketopen))
            {
                throw new ParserException(this, "expected '(' before cast expression");
            }

            var node = ParseFactor();

            return new ASTNodeCast(node, type);
        }

        return ParseFactor();
    }

    private ASTNode ParseUnaryExpression()
    {
        if (OneOf(OperatorPlus, OperatorMinus, OperatorBoolnot, OperatorBitnot))
        {
            var op = GetValue<Token.Operator>(-1);

            return Create(new ASTNodeMathematicalExpression(new ASTNodeLiteral((long)0), ParseCastExpression(), op));
        }

        if (Sequence(String))
        {
            return ParseStringLiteral();
        }

        return ParseCastExpression();
    }

    private ASTNode ParseMultiplicativeExpression()
    {
        var node = ParseUnaryExpression();

        while (OneOf(OperatorStar, OperatorSlash, OperatorPercent))
        {
            var op = GetValue<Token.Operator>(-1);
            node = Create(new ASTNodeMathematicalExpression(node, ParseUnaryExpression(), op));
        }

        return node;
    }

    private ASTNode ParseAdditiveExpression()
    {
        var node = ParseMultiplicativeExpression();

        while (Variant(OperatorPlus, OperatorMinus))
        {
            var op = GetValue<Token.Operator>(-1);
            node = Create(new ASTNodeMathematicalExpression(node, ParseMultiplicativeExpression(), op));
        }

        return node;
    }

    private ASTNode ParseShiftExpression()
    {
        var node = ParseAdditiveExpression();

        while (Variant(OperatorShiftleft, OperatorShiftright))
        {
            var op = GetValue<Token.Operator>(-1);
            node = Create(new ASTNodeMathematicalExpression(node, ParseAdditiveExpression(), op));
        }

        return node;
    }

    private ASTNode ParseRelationExpression()
    {
        var node = ParseShiftExpression();

        while (Sequence(OperatorBoolgreaterthan) || Sequence(OperatorBoollessthan) ||
               Sequence(OperatorBoolgreaterthanorequals) || Sequence(OperatorBoollessthanorequals))
        {
            var op = GetValue<Token.Operator>(-1);
            node = Create(new ASTNodeMathematicalExpression(node, ParseShiftExpression(), op));
        }

        return node;
    }

    private ASTNode ParseEqualityExpression()
    {
        var node = ParseRelationExpression();

        while (Sequence(OperatorBoolequals) || Sequence(OperatorBoolnotequals))
        {
            var op = GetValue<Token.Operator>(-1);
            node = Create(new ASTNodeMathematicalExpression(node, ParseRelationExpression(), op));
        }

        return node;
    }

    private ASTNode ParseBinaryAndExpression()
    {
        var node = ParseEqualityExpression();

        while (Sequence(OperatorBitand))
        {
            node = Create(new ASTNodeMathematicalExpression(node, ParseEqualityExpression(), Token.Operator.BitAnd));
        }

        return node;
    }

    private ASTNode ParseBinaryXorExpression()
    {
        var node = ParseBinaryAndExpression();

        while (Sequence(OperatorBitxor))
        {
            node = Create(new ASTNodeMathematicalExpression(node, ParseBinaryAndExpression(), Token.Operator.BitXor));
        }

        return node;
    }

    private ASTNode ParseBinaryOrExpression()
    {
        var node = ParseBinaryXorExpression();

        while (Sequence(OperatorBitor))
        {
            node = Create(new ASTNodeMathematicalExpression(node, ParseBinaryXorExpression(), Token.Operator.BitOr));
        }

        return node;
    }

    private ASTNode ParseBooleanAnd()
    {
        var node = ParseBinaryOrExpression();

        while (Sequence(OperatorBooland))
        {
            node = Create(new ASTNodeMathematicalExpression(node, ParseBinaryOrExpression(), Token.Operator.BoolAnd));
        }

        return node;
    }

    private ASTNode ParseBooleanXor()
    {
        var node = ParseBooleanAnd();

        while (Sequence(OperatorBoolxor))
        {
            node = Create(new ASTNodeMathematicalExpression(node, ParseBooleanAnd(), Token.Operator.BoolXor));
        }

        return node;
    }

    private ASTNode ParseBooleanOr()
    {
        var node = ParseBooleanXor();

        while (Sequence(OperatorBoolor))
        {
            node = Create(new ASTNodeMathematicalExpression(node, ParseBooleanXor(), Token.Operator.BoolOr));
        }

        return node;
    }

    private ASTNode ParseTernaryConditional()
    {
        var node = ParseBooleanOr();

        while (Sequence(OperatorTernaryconditional))
        {
            var second = ParseBooleanOr();

            if (!Sequence(OperatorInherit))
            {
                throw new ParserException(this, "expected ':' in ternary expression");
            }

            var third = ParseBooleanOr();
            node = Create(new ASTNodeTernaryExpression(node, second, third, Token.Operator.TernaryConditional));
        }

        return node;
    }

    private ASTNode ParseMathematicalExpression() => ParseTernaryConditional();

    private ASTNode ParseFunctionDefinition()
    {
        var functionName = GetIdentifier(-2).Value;
        var @params = new List<(string, ASTNode)>();

        // Parse parameter list
        var hasParams = !Peek(SeparatorRoundbracketclose);
        var unnamedParamCount = 0;
        while (hasParams)
        {
            var type = ParseType(true);

            if (Sequence(Identifier))
            {
                @params.Add((GetIdentifier(-1).Value, type));
            }
            else
            {
                @params.Add((unnamedParamCount.ToString(), type));
                unnamedParamCount++;
            }

            if (!Sequence(SeparatorComma))
            {
                if (Sequence(SeparatorRoundbracketclose))
                {
                    break;
                }

                throw new ParserException(this, "expected closing ')' after parameter list");
            }
        }

        if (!hasParams)
        {
            if (!Sequence(SeparatorRoundbracketclose))
            {
                throw new ParserException(this, "expected closing ')' after parameter list");
            }
        }

        if (!Sequence(SeparatorCurlybracketopen))
        {
            throw new ParserException(this, "expected opening '{' after function definition");
        }


        // Parse function body
        var body = new List<ASTNode>();

        while (!Sequence(SeparatorCurlybracketclose))
        {
            body.Add(ParseFunctionStatement());
        }

        return Create(new ASTNodeFunctionDefinition(GetNamespacePrefixedName(functionName), @params, body));
    }

    private ASTNode ParseFunctionVariableDecl()
    {
        ASTNode? statement;
        var type = ParseType(true);

        if (Sequence(Identifier))
        {
            var identifier = GetIdentifier(-1).Value;
            statement = ParseMemberVariable(type);

            if (Sequence(OperatorAssignment))
            {
                var expression = ParseMathematicalExpression();

                statement = Create(new ASTNodeCompoundStatement(new[]
                    {statement, Create(new ASTNodeAssignment(identifier, expression))}));
            }
        }
        else
        {
            throw new ParserException(this, "invalid variable declaration");
        }

        return statement;
    }

    private ASTNode ParseFunctionStatement()
    {
        var needsSemicolon = true;
        ASTNode statement;

        if (Sequence(Identifier, OperatorAssignment))
        {
            statement = ParseFunctionVariableAssignment();
        }
        else if (OneOf(KeywordReturn, KeywordBreak, KeywordContinue))
        {
            statement = ParseFunctionControlFlowStatement();
        }
        else if (Sequence(KeywordIf, SeparatorRoundbracketopen))
        {
            statement = ParseFunctionConditional();
            needsSemicolon = false;
        }
        else if (Sequence(KeywordWhile, SeparatorRoundbracketopen))
        {
            statement = ParseFunctionWhileLoop();
            needsSemicolon = false;
        }
        else if (Sequence(KeywordFor, SeparatorRoundbracketopen))
        {
            statement = ParseFunctionForLoop();
            needsSemicolon = false;
        }
        else if (Sequence(Identifier))
        {
            var originalPos = _tokenIndex;
            ParseNamespaceResolution();
            var isFunction = Peek(SeparatorRoundbracketopen);

            if (isFunction)
            {
                _tokenIndex = originalPos;
                statement = ParseFunctionCall();
            }
            else
            {
                _tokenIndex = originalPos - 1;
                statement = ParseFunctionVariableDecl();
            }
        }
        else if (Peek(KeywordBe) || Peek(KeywordLe) || Peek(ValuetypeAny))
        {
            statement = ParseFunctionVariableDecl();
        }
        else
        {
            throw new ParserException(this, "invalid sequence");
        }

        if (needsSemicolon && !Sequence(SeparatorEndofexpression))
        {
            throw new ParserException(this, "missing ';' at end of expression", -1);
        }

        // Consume superfluous semicolons
        // ReSharper disable once EmptyEmbeddedStatement
        while (needsSemicolon && Sequence(SeparatorEndofexpression))
        {
            // ignore
        }

        return statement;
    }

    private ASTNode ParseFunctionVariableAssignment()
    {
        var lvalue = GetIdentifier(-2).Value;

        var rvalue = ParseMathematicalExpression();

        return Create(new ASTNodeAssignment(lvalue, rvalue));
    }

    private ASTNode ParseFunctionControlFlowStatement()
    {
        ControlFlowStatement type;
        if (Peek(KeywordReturn, -1))
        {
            type = ControlFlowStatement.Return;
        }
        else if (Peek(KeywordBreak, -1))
        {
            type = ControlFlowStatement.Break;
        }
        else if (Peek(KeywordContinue, -1))
        {
            type = ControlFlowStatement.Continue;
        }
        else
        {
            throw new ParserException(this, "invalid control flow statement. Expected 'return', 'break' or 'continue'");
        }

        if (Peek(SeparatorEndofexpression))
        {
            return Create(new ASTNodeControlFlowStatement(type, null));
        }

        return Create(new ASTNodeControlFlowStatement(type, ParseMathematicalExpression()));
    }

    private List<ASTNode> ParseStatementBody()
    {
        List<ASTNode> body = new();

        if (Sequence(SeparatorCurlybracketopen))
        {
            while (!Sequence(SeparatorCurlybracketclose))
            {
                body.Add(ParseFunctionStatement());
            }
        }
        else
        {
            body.Add(ParseFunctionStatement());
        }

        return body;
    }

    private ASTNode ParseFunctionConditional()
    {
        var condition = ParseMathematicalExpression();

        if (!Sequence(SeparatorRoundbracketclose))
        {
            throw new ParserException(this, "expected closing ')' after statement head");
        }

        var trueBody = ParseStatementBody();

        List<ASTNode> falseBody = new();
        if (Sequence(KeywordElse))
        {
            falseBody = ParseStatementBody();
        }

        return Create(new ASTNodeConditionalStatement(condition, trueBody, falseBody));
    }

    private ASTNode ParseFunctionWhileLoop()
    {
        var condition = ParseMathematicalExpression();

        if (!Sequence(SeparatorRoundbracketclose))
        {
            throw new ParserException(this, "expected closing ')' after statement head");
        }

        var body = ParseStatementBody();

        return Create(new ASTNodeWhileStatement(condition, body));
    }

    private ASTNode ParseFunctionForLoop()
    {
        var variable = ParseFunctionVariableDecl();

        if (!Sequence(SeparatorComma))
        {
            throw new ParserException(this, "expected ',' after for loop variable declaration");
        }

        var condition = ParseMathematicalExpression();

        if (!Sequence(SeparatorComma))
        {
            throw new ParserException(this, "expected ',' after for loop condition");
        }

        if (!Sequence(Identifier, OperatorAssignment))
        {
            throw new ParserException(this, "expected for loop variable assignment");
        }

        var postExpression = ParseFunctionVariableAssignment();

        if (!Sequence(SeparatorRoundbracketclose))
        {
            throw new ParserException(this, "expected closing ')' after statement head");
        }

        var body = ParseStatementBody();

        return Create(new ASTNodeCompoundStatement(
            new[] {variable, Create(new ASTNodeWhileStatement(condition, body, postExpression))}, true));
    }

    private void ParseAttribute(AttributableASTNode? currentNode)
    {
        if (currentNode is null)
        {
            throw new ParserException(this, "tried to apply attribute to invalid statement");
        }

        do
        {
            if (!Sequence(Identifier))
            {
                throw new ParserException(this, "expected attribute expression");
            }

            var attribute = GetIdentifier(-1).Value;

            if (Sequence(SeparatorRoundbracketopen, String, SeparatorRoundbracketclose))
            {
                var value = GetValue<Literal>(-2);

                if (value is StringLiteral valueStr)
                {
                    currentNode.AddAttribute(Create(new ASTNodeAttribute(attribute, valueStr.Value)));
                }
                else
                {
                    throw new ParserException(this, "expected string attribute argument");
                }
            }
            else
            {
                currentNode.AddAttribute(Create(new ASTNodeAttribute(attribute)));
            }
        } while (Sequence(SeparatorComma));

        if (!Sequence(SeparatorSquarebracketclose, SeparatorSquarebracketclose))
        {
            throw new ParserException(this, "expected ']]' to finish attribute");
        }
    }

    private ASTNode ParseConditional()
    {
        var condition = ParseMathematicalExpression();
        List<ASTNode> trueBody = new();
        List<ASTNode> falseBody = new();

        if (Sequence(SeparatorRoundbracketclose, SeparatorCurlybracketopen))
        {
            while (!Sequence(SeparatorCurlybracketclose))
            {
                trueBody.Add(ParseMember());
            }
        }
        else if (Sequence(SeparatorRoundbracketclose))
        {
            trueBody.Add(ParseMember());
        }
        else
        {
            throw new ParserException(this, "expected body of conditional statement");
        }

        if (Sequence(KeywordElse, SeparatorCurlybracketopen))
        {
            while (!Sequence(SeparatorCurlybracketclose))
            {
                falseBody.Add(ParseMember());
            }
        }
        else if (Sequence(KeywordElse))
        {
            falseBody.Add(ParseMember());
        }

        return Create(new ASTNodeConditionalStatement(condition, trueBody, falseBody));
    }

    private ASTNode ParseWhileStatement()
    {
        var condition = ParseMathematicalExpression();

        if (!Sequence(SeparatorRoundbracketclose))
        {
            throw new ParserException(this, "expected closing ')' after while head");
        }

        return Create(new ASTNodeWhileStatement(condition, Array.Empty<ASTNode>())); // TODO: should NOT be null
    }

    private ASTNodeTypeDecl ParseType(bool allowFunctionTypes = false)
    {
        Endianess? endian = null;

        if (Sequence(KeywordLe))
        {
            endian = Endianess.Little;
        }
        else if (Sequence(KeywordBe))
        {
            endian = Endianess.Big;
        }

        if (Sequence(Identifier))
        {
            // Custom type
            var typeName = ParseNamespaceResolution();

            Debug.WriteLine(typeName + "-> " + GetNamespacePrefixedName(typeName));

            if (_types.ContainsKey(typeName))
            {
                var clone = _types[typeName].Clone();
                return Create(new ASTNodeTypeDecl(string.Empty, clone, endian));
            }

            if (_types.ContainsKey(GetNamespacePrefixedName(typeName)))
            {
                return Create(new ASTNodeTypeDecl(string.Empty, _types[GetNamespacePrefixedName(typeName)].Clone(),
                    endian));
            }

            throw new ParserException(this, $"unknown type '{typeName}'");
        }

        if (Sequence(ValuetypeAny))
        {
            // Builtin type
            var type = GetValue<Token.ValueType>(-1);
            if (!allowFunctionTypes)
            {
                if (type == Token.ValueType.String)
                {
                    throw new ParserException(this, "cannot use 'str' in this context. Use a character array instead");
                }

                if (type == Token.ValueType.Auto)
                {
                    throw new ParserException(this, "cannot use 'auto' in this context");
                }
            }

            return Create(new ASTNodeTypeDecl(string.Empty, new ASTNodeBuiltinType(type), endian));
        }

        throw new ParserException(this, "failed to parse type. Expected identifier or builtin type");
    }

    private ASTNode ParseUsingDeclaration()
    {
        var name = ParseNamespaceResolution();

        if (!Sequence(OperatorAssignment))
        {
            throw new ParserException(this, "expected '=' after type name of using declaration");
        }

        var type = ParseType();
        if (type is null)
        {
            throw new ParserException(this, "invalid type used in variable declaration");
        }

        return AddType(name, type, type.Endian);
    }

    private ASTNode ParsePadding()
    {
        var size = ParseMathematicalExpression();

        if (!Sequence(SeparatorSquarebracketclose))
        {
            throw new ParserException(this, "expected closing ']' at end of array declaration", -1); // -1
        }

        // TODO: not null!! ???
        return Create(new ASTNodeArrayVariableDecl("",
            new ASTNodeTypeDecl("", new ASTNodeBuiltinType(Token.ValueType.Padding)), size));
    }

    private ASTNode ParseMemberVariable(ASTNodeTypeDecl type)
    {
        if (Peek(SeparatorComma))
        {
            var variables = new List<ASTNode>();

            do
            {
                variables.Add(Create(new ASTNodeVariableDecl(GetIdentifier(-1).Value, type.Clone())));
            } while (Sequence(SeparatorComma, Identifier));

            return Create(new ASTNodeMultiVariableDecl(variables));
        }

        return Create(new ASTNodeVariableDecl(GetIdentifier(-1).Value, type));
    }

    private ASTNode ParseMemberArrayVariable(ASTNodeTypeDecl type)
    {
        var name = GetIdentifier(-2).Value;

        ASTNode? size = null;

        if (!Sequence(SeparatorSquarebracketclose))
        {
            if (Sequence(KeywordWhile, SeparatorRoundbracketopen))
            {
                size = ParseWhileStatement();
            }
            else
            {
                size = ParseMathematicalExpression();
            }

            if (!Sequence(SeparatorSquarebracketclose))
            {
                throw new ParserException(this, "expected closing ']' at end of array declaration", -1);
            }
        }

        return Create(new ASTNodeArrayVariableDecl(name, type, size));
    }

    private ASTNode ParseMemberPointerVariable(ASTNodeTypeDecl type)
    {
        var name = GetIdentifier(-2).Value;

        var sizeType = ParseType();
        if (sizeType.Type is not ASTNodeBuiltinType builtinType || !Token.IsUnsigned(builtinType.Type))
        {
            throw new ParserException(this, "invalid type used for pointer size", -1);
        }

        return Create(new ASTNodePointerVariableDecl(name, type, sizeType));
    }

    private ASTNode ParseMember()
    {
        ASTNode? member = null;

        if (Peek(KeywordBe) || Peek(KeywordLe) || Peek(ValuetypeAny) || Peek(Identifier))
        {
            // Some kind of variable definition
            var isFunction = false;

            if (Peek(Identifier))
            {
                var originalPos = _tokenIndex;
                _tokenIndex++;
                ParseNamespaceResolution();
                isFunction = Peek(SeparatorRoundbracketopen);
                _tokenIndex = originalPos;

                if (isFunction)
                {
                    _tokenIndex++;
                    member = ParseFunctionCall();
                }
            }


            if (!isFunction)
            {
                var type = ParseType();

                // sequenceNot is an ugly hack. it does not use  and therefore when calling reset() it reset back to the previous begin() -> the identifier.
                if (Sequence(Identifier, SeparatorSquarebracketopen) &&
                    SequenceNot(SeparatorSquarebracketopen))
                {
                    member = ParseMemberArrayVariable(type);
                }
                else if (Sequence(Identifier))
                {
                    member = ParseMemberVariable(type);
                }
                else if (Sequence(OperatorStar, Identifier, OperatorInherit))
                {
                    member = ParseMemberPointerVariable(type);
                }
                else
                {
                    throw new ParserException(this, "invalid variable declaration");
                }
            }
        }
        else if (Sequence(ValuetypePadding, SeparatorSquarebracketopen))
        {
            member = ParsePadding();
        }
        else if (Sequence(KeywordIf, SeparatorRoundbracketopen))
        {
            return ParseConditional();
        }
        else if (Sequence(SeparatorEndofprogram))
        {
            throw new ParserException(this, "unexpected end of program", -2);
        }
        else
        {
            throw new ParserException(this, "invalid struct member");
        }

        if (Sequence(SeparatorSquarebracketopen, SeparatorSquarebracketopen))
        {
            ParseAttribute(member as AttributableASTNode);
        }

        if (!Sequence(SeparatorEndofexpression))
        {
            throw new ParserException(this, "Missing ';' at end of expression", -1);
        }

        // Consume superfluous semicolons
        // ReSharper disable once EmptyEmbeddedStatement
        while (Sequence(SeparatorEndofexpression))
        {
            // do nothing
        }

        if (member is null)
        {
            // TODO: wrong token index
            throw new ParserException(this, "could not parse member");
        }

        return member;
    }

    private ASTNode ParseStruct()
    {
        var typeName = GetIdentifier(-1).Value;

        var structNode = Create(new ASTNodeStruct());
        var typeDecl = AddType(typeName, structNode);

        if (Sequence(OperatorInherit, Identifier))
        {
            // Inheritance

            do
            {
                var inheritedTypeName = GetIdentifier(-1).Value;
                if (!_types.ContainsKey(inheritedTypeName))
                {
                    throw new ParserException(this, $"cannot inherit from unknown type '{inheritedTypeName}'", -1);
                }

                structNode.AddInheritance(_types[inheritedTypeName].Clone());
            } while (Sequence(SeparatorComma, Identifier));
        }
        else if (Sequence(OperatorInherit, ValuetypeAny))
        {
            throw new ParserException(this, "cannot inherit from builtin type");
        }

        if (!Sequence(SeparatorCurlybracketopen))
        {
            throw new ParserException(this, "expected '{' after struct definition", -1);
        }

        while (!Sequence(SeparatorCurlybracketclose))
        {
            structNode.AddMember(ParseMember());
        }

        return typeDecl;
    }

    private ASTNode ParseUnion()
    {
        var typeName = GetIdentifier(-2).Value;

        var unionNode = Create(new ASTNodeUnion());
        var typeDecl = AddType(typeName, unionNode);

        while (!Sequence(SeparatorCurlybracketclose))
        {
            unionNode.AddMember(ParseMember());
        }

        return typeDecl;
    }

    private ASTNode ParseEnum()
    {
        var typeName = GetIdentifier(-2).Value;

        var underlyingType = ParseType();
        if (underlyingType.Endian is not null)
        {
            throw new ParserException(this, "underlying type may not have an endian specification", -2);
        }

        var enumNode = Create(new ASTNodeEnum(underlyingType));
        var typeDecl = AddType(typeName, enumNode);

        if (!Sequence(SeparatorCurlybracketopen))
        {
            throw new ParserException(this, "expected '{' after enum definition", -1);
        }

        ASTNode? lastEntry = null;
        while (!Sequence(SeparatorCurlybracketclose))
        {
            if (Sequence(Identifier, OperatorAssignment))
            {
                var name = GetIdentifier(-2).Value;
                var value = ParseMathematicalExpression();

                enumNode.AddEntry(name, value);
                lastEntry = value;
            }
            else if (Sequence(Identifier))
            {
                ASTNode valueExpr;
                var name = GetIdentifier(-1).Value;
                if (enumNode.Entries.Count == 0 || lastEntry is null)
                {
                    valueExpr = lastEntry = Create(new ASTNodeLiteral((ulong)0));
                }
                else
                {
                    valueExpr = lastEntry = Create(new ASTNodeMathematicalExpression(lastEntry.Clone(),
                        new ASTNodeLiteral((ulong)1), Token.Operator.Plus));
                }

                enumNode.AddEntry(name, valueExpr);
            }
            else if (Sequence(SeparatorEndofprogram))
            {
                throw new ParserException(this, "unexpected end of program", -1);
            }
            else
            {
                throw new ParserException(this, "invalid enum entry", -1);
            }

            if (!Sequence(SeparatorComma))
            {
                if (Sequence(SeparatorCurlybracketclose))
                {
                    break;
                }

                throw new ParserException(this, "missing ',' between enum entries", -1);
            }
        }

        return typeDecl;
    }

    private ASTNode ParseBitfield()
    {
        var typeName = GetIdentifier(-2).Value;

        var bitfieldNode = Create(new ASTNodeBitfield());
        var typeDecl = AddType(typeName, bitfieldNode);

        while (!Sequence(SeparatorCurlybracketclose))
        {
            if (Sequence(Identifier, OperatorInherit))
            {
                var name = GetIdentifier(-2).Value;
                bitfieldNode.AddEntry(name, ParseMathematicalExpression());
            }
            else if (Sequence(ValuetypePadding, OperatorInherit))
            {
                bitfieldNode.AddEntry("padding", ParseMathematicalExpression());
            }
            else if (Sequence(SeparatorEndofprogram))
            {
                throw new ParserException(this, "unexpected end of program", -2);
            }
            else
            {
                throw new ParserException(this, "invalid bitfield member");
            }

            if (!Sequence(SeparatorEndofexpression))
            {
                throw new ParserException(this, "missing ';' at end of expression", -1);
            }

            // Consume superfluous semicolons
            // ReSharper disable once EmptyEmbeddedStatement
            while (Sequence(SeparatorEndofexpression))
            {
                // ignore
            }
        }

        return typeDecl;
    }

    private ASTNode ParseVariablePlacement(ASTNodeTypeDecl type)
    {
        var inVariable = false;
        var outVariable = false;

        var name = GetIdentifier(-1).Value;

        ASTNode? placementOffset = null;
        if (Sequence(OperatorAt))
        {
            placementOffset = ParseMathematicalExpression();
        }
        else if (Sequence(KeywordIn))
        {
            inVariable = true;
        }
        else if (Sequence(KeywordOut))
        {
            outVariable = true;
        }

        return Create(new ASTNodeVariableDecl(name, type, placementOffset, inVariable, outVariable));
    }

    private ASTNode ParseArrayVariablePlacement(ASTNodeTypeDecl type)
    {
        var name = GetIdentifier(-2).Value;

        ASTNode? size = null;

        if (!Sequence(SeparatorSquarebracketclose))
        {
            if (Sequence(KeywordWhile, SeparatorRoundbracketopen))
            {
                size = ParseWhileStatement();
            }
            else
            {
                size = ParseMathematicalExpression();
            }

            if (!Sequence(SeparatorSquarebracketclose))
            {
                throw new ParserException(this, "expected closing ']' at end of array declaration", -1);
            }
        }

        if (!Sequence(OperatorAt))
        {
            throw new ParserException(this, "expected placement instruction", -1);
        }

        var placementOffset = ParseMathematicalExpression();

        return Create(new ASTNodeArrayVariableDecl(name, type, size, placementOffset));
    }

    private ASTNode ParsePointerVariablePlacement(ASTNodeTypeDecl type)
    {
        var name = GetIdentifier(-2).Value;

        var sizeType = ParseType();
        if (sizeType.Type is not ASTNodeBuiltinType builtinType || !Token.IsUnsigned(builtinType.Type))
        {
            throw new ParserException(this, "invalid type used for pointer size", -1);
        }

        if (!Sequence(OperatorAt))
        {
            throw new ParserException(this, "expected placement instruction", -1);
        }

        var placementOffset = ParseMathematicalExpression();
        return Create(new ASTNodePointerVariableDecl(name, type, sizeType, placementOffset));
    }

    private ASTNode ParsePlacement()
    {
        var type = ParseType();

        if (Sequence(Identifier, SeparatorSquarebracketopen))
        {
            return ParseArrayVariablePlacement(type);
        }

        if (Sequence(Identifier))
        {
            return ParseVariablePlacement(type);
        }

        if (Sequence(OperatorStar, Identifier, OperatorInherit))
        {
            return ParsePointerVariablePlacement(type);
        }

        throw new ParserException(this, "invalid sequence");
    }

    private List<ASTNode> ParseNamespace()
    {
        List<ASTNode> statements = new();

        if (!Sequence(Identifier))
        {
            throw new ParserException(this, "expected namespace identifier");
        }

        // TODO: right? -> was m_currNamespace.Last() originally?
        _currentNamespace.Add(new List<string>());

        while (true)
        {
            _currentNamespace.Last().Add(GetIdentifier(-1).Value);

            if (!Sequence(OperatorScoperesolution, Identifier))
            {
                break;
            }
        }

        if (!Sequence(SeparatorCurlybracketopen))
        {
            throw new ParserException(this, "expected '{' at start of namespace");
        }

        while (!Sequence(SeparatorCurlybracketclose))
        {
            statements = ParseStatements();
        }

        // pop_back()
        _currentNamespace.RemoveAt(_currentNamespace.Count - 1);

        return statements;
    }

    private List<ASTNode> ParseStatements()
    {
        ASTNode? statement;

        if (Sequence(KeywordUsing, Identifier))
        {
            statement = ParseUsingDeclaration();
        }
        else if (Peek(Identifier))
        {
            var originalPos = _tokenIndex;
            _tokenIndex++;
            ParseNamespaceResolution();
            var isFunction = Peek(SeparatorRoundbracketopen);
            _tokenIndex = originalPos;

            if (isFunction)
            {
                _tokenIndex++;
                statement = ParseFunctionCall();
            }
            else
            {
                statement = ParsePlacement();
            }
        }
        else if (Peek(KeywordBe) || Peek(KeywordLe) || Peek(ValuetypeAny))
        {
            statement = ParsePlacement();
        }
        else if (Sequence(KeywordStruct, Identifier))
        {
            statement = ParseStruct();
        }
        else if (Sequence(KeywordUnion, Identifier, SeparatorCurlybracketopen))
        {
            statement = ParseUnion();
        }
        else if (Sequence(KeywordEnum, Identifier, OperatorInherit))
        {
            statement = ParseEnum();
        }
        else if (Sequence(KeywordBitfield, Identifier, SeparatorCurlybracketopen))
        {
            statement = ParseBitfield();
        }
        else if (Sequence(KeywordFunction, Identifier, SeparatorRoundbracketopen))
        {
            statement = ParseFunctionDefinition();
        }
        else if (Sequence(KeywordNamespace))
        {
            return ParseNamespace();
        }
        else
        {
            throw new ParserException(this, "invalid sequence");
        }

        if (Sequence(SeparatorSquarebracketopen, SeparatorSquarebracketopen))
        {
            ParseAttribute(statement as AttributableASTNode);
        }

        if (!Sequence(SeparatorEndofexpression))
        {
            throw new ParserException(this, "missing ';' at end of expression", -1);
        }

        // Consume superfluous semicolons
        // ReSharper disable once EmptyEmbeddedStatement
        while (Sequence(SeparatorEndofexpression))
        {
            // ignore
        }

        return new List<ASTNode>(new[] {statement});
    }

    private ASTNodeTypeDecl AddType(string name, ASTNode node, Endianess? endian = null)
    {
        var typeName = GetNamespacePrefixedName(name);

        if (_types.ContainsKey(typeName))
        {
            throw new ParserException(this, $"redefinition of type '{typeName}'");
        }

        var typeDecl = Create(new ASTNodeTypeDecl(typeName, node, endian));
        _types.Add(typeName, typeDecl);

        return typeDecl;
    }

    //private List<ASTNode> ParseTillToken(Token component) => ParseTillToken(component.Type, component.Value);

    private List<ASTNode> ParseTillToken(Token token)
    {
        var program = new List<ASTNode>();
        //this->m_curr->type != endTokenType || (*this->m_curr) != value
        while (_tokens[_tokenIndex].Type != token.Type || !_tokens[_tokenIndex].TokenValueEquals(token))
        {
            foreach (var statement in ParseStatements())
            {
                program.Add(statement);
            }
        }

        _tokenIndex++;

        return program;
    }

    //private void ThrowParseError(string error, int token = -1)
    //{
    //    throw new LangException(this._tokens[this._tokenIndex + token].lineNumber, "Parser: " + error);
    //}

    //private void ThrowParseError(string error, uint token)
    //{
    //    throw new LangException(this._tokens[this._tokenIndex + (int)token].lineNumber, "Parser: " + error);
    //}

    /* Token consuming */

    private void Begin()
    {
        if (!_hasReset)
        {
            return;
        }

        _originalTokenIndex = _tokenIndex;
    }

    private void Reset()
    {
        _hasReset = true;
        _tokenIndex = _originalTokenIndex;
    }

    private bool Sequence(params Token[] components)
    {
        Begin();

        for (var i = 0; i < components.Length; i++)
        {
            if (!Peek(components[i]))
            {
                Reset();
                return false;
            }

            _tokenIndex++;
        }

        return true;
    }

    private bool SequenceNot(params Token[] components)
    {
        for (var i = 0; i < components.Length; i++)
        {
            if (!Peek(components[i]))
            {
                return true;
            }

            _tokenIndex++;
        }

        Reset();
        return false;
    }

    private bool OneOf(params Token[] components)
    {
        Begin();

        for (var i = 0; i < components.Length; i++)
        {
            var component = components[i];
            if (Peek(component))
            {
                _tokenIndex++;
                return true;
            }
        }

        Reset();
        return false;
    }

    private bool Variant(Token c1, Token c2)
    {
        Begin();

        if (!Peek(c1))
        {
            if (!Peek(c2))
            {
                Reset();
                return false;
            }
        }

        _tokenIndex++;

        return true;
    }

    //private bool Optional(Token.Type type, object value)
    //{
    //    if (Peek(type, value))
    //    {
    //        this._mMatchedOptionals.Add(this._tokens[_tokenIndex]);
    //        this._tokenIndex++;
    //    }

    //    return true;
    //}

    private bool Peek(Token component, int index = 0) =>
        //return Peek(component.Type, component.Value, index);
        _tokens[_tokenIndex + index].Type == component.Type && _tokens[_tokenIndex + index].TokenValueEquals(component);

    //private record Component(Token.TokenType Type, Token.ITokenValue Value)
    //{
    //    public Component(Token.TokenType type, Token.Separator separator) : this(type,
    //        new Token.EnumValue<Token.Separator>(separator))
    //    {
    //    }

    //    public Component(Token.TokenType type, Token.Keyword keyword) : this(type,
    //        new Token.EnumValue<Token.Keyword>(keyword))
    //    {
    //    }

    //    public Component(Token.TokenType type, Token.Operator @operator) : this(type,
    //        new Token.EnumValue<Token.Operator>(@operator))
    //    {
    //    }

    //    public Component(Token.TokenType type, Token.ValueType valueType) : this(type,
    //        new Token.EnumValue<Token.ValueType>(valueType))
    //    {
    //    }

    //    public Component(Token.TokenType type, Literal literal) : this(type,
    //        new Token.LiteralValue(literal))
    //    {
    //    }
    //}

    //private bool Peek(Token.Type type, object value, int index = 0)
    //{
    //    return this._tokens[_tokenIndex + index].type == type && this._tokens[_tokenIndex + index].TokenValueEquals(value);
    //}
}