﻿using System;
using System.Collections.Generic;
using HexControl.Core.Buffers.Extensions;
using HexControl.PatternLanguage.Literals;
using HexControl.PatternLanguage.Patterns;

namespace HexControl.PatternLanguage.AST;

internal abstract class AttributableASTNode : ASTNode
{
    private readonly List<ASTNodeAttribute> _attributes;

    protected AttributableASTNode()
    {
        _attributes = new List<ASTNodeAttribute>();
    }

    protected AttributableASTNode(AttributableASTNode other) : base(other)
    {
        // TODO: didn't clone here, bad?
        _attributes = new List<ASTNodeAttribute>(other.Attributes);
    }

    public IReadOnlyList<ASTNodeAttribute> Attributes => _attributes;

    public void AddAttribute(ASTNodeAttribute attribute)
    {
        _attributes.Add(attribute);
    }

    protected static void ApplyVariableAttributes(Evaluator evaluator, AttributableASTNode attributable,
        PatternData pattern)
    {
        var endOffset = evaluator.CurrentOffset;
        evaluator.CurrentOffset = pattern.Offset;

        foreach (var attribute in attributable._attributes)
        {
            // TODOO: amazingly bad code
            var name = attribute.Attribute;
            var value = attribute.Value;

            var node = (ASTNode)attributable;

            var requiresValue = () =>
            {
                if (value is null)
                {
                    throw new Exception($"used attribute '{name}' without providing a value"); // LOL
                }

                return true;
            };

            var noValue = () =>
            {
                if (value is not null)
                {
                    throw new Exception($"provided a value to attribute '{name}' which doesn't take one");
                }

                return true;
            };

            if (name == "color" && requiresValue())
            {
                var color = Convert.ToUInt32(value!, 16);
                pattern.Color = color >> 8; // TODO: change endianness :)
            }
            else if (name == "name" && requiresValue())
            {
                pattern.DisplayName = value!;
            }
            else if (name == "comment" && requiresValue())
            {
                pattern.Comment = value!;
            }
            else if (name == "hidden" && noValue())
            {
                pattern.Hidden = true;
            }
            else if (name == "inline" && noValue())
            {
                if (pattern is not IInlinable inlinable)
                {
                    throw new Exception("inline attribute can only be applied to nested types"); // pass node
                }

                inlinable.Inlined = true;
            }
            else if (name == "format" && requiresValue())
            {
                var functions = evaluator.GetCustomFunctions();
                if (!functions.ContainsKey(value!))
                {
                    throw new Exception($"cannot find formatter function '{value}'"); // pass node
                }

                var function = functions[value!];
                if (function.ParameterCount != 1)
                {
                    throw new Exception("formatter function needs exactly one parameter"); // pass node
                }

                pattern.FormatterFunction = function.Body;
            }
            else if (name == "transform" && requiresValue())
            {
                var functions = evaluator.GetCustomFunctions();
                if (!functions.ContainsKey(value!))
                {
                    throw new Exception($"cannot find transform function '{value}'"); // pass node
                }

                var function = functions[value!];
                if (function.ParameterCount != 1)
                {
                    throw new Exception("transform function needs exactly one parameter"); // pass node
                }

                pattern.TransformFunction = function.Body;
                throw new Exception("transform not implemented");
            }
            else if (name == "pointer_base" && requiresValue())
            {
                var functions = evaluator.GetCustomFunctions();
                if (!functions.ContainsKey(value!))
                {
                    throw new Exception($"cannot find pointer base function '{value}'"); // pass node
                }

                var function = functions[value!];
                if (function.ParameterCount != 1)
                {
                    throw new Exception("pointer base function needs exactly one parameter"); // pass node
                }

                if (pattern is PatternDataPointer pointerPattern)
                {
                    var pointerValue = pointerPattern.PointedAtAddress;

                    var result = function.Body(evaluator, new Literal[] {pointerValue});

                    if (result is null)
                    {
                        throw new Exception("pointer base function did not return a value"); // pass node
                    }

                    // TODO: literaltoUnsigned a bad choice??
                    pointerPattern.PointedAtAddress = result.ToSignedLong() + pointerValue;
                }
                else
                {
                    throw new Exception("pointer_base attribute may only be applied to a pointer");
                }
            }
        }


        evaluator.CurrentOffset = endOffset;
    }
}