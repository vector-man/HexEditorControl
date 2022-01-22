﻿using System.Collections.Generic;
using HexControl.Core.Helpers;

namespace HexControl.PatternLanguage.Patterns;

public class PatternDataStruct : PatternData, IInlinable
{
    public bool Inlined { get; set; }

    public PatternDataStruct(long offset, long size, Evaluator evaluator, uint color = 0)
        : base(offset, size, evaluator, color)
    {
        _members = new List<PatternData>();
    }

    private PatternDataStruct(PatternDataStruct other) : base(other)
    {
        _members = other._members.Clone();
    }

    public override PatternData Clone()
    {
        return new PatternDataStruct(this);
    }

    public override long Offset
    {
        get => base.Offset;
        set
        {
            if (!Local)
            {
                foreach (var member in _members)
                {
                    member.Offset = (value + (member.Offset - Offset));
                }
            }
            base.Offset = value;
        }
    }
        
    public override string GetFormattedName()
    {
        return $"struct {TypeName}";
    }

    public IReadOnlyList<PatternData> Members
    {
        get => _members;
        set
        {
            _members.Clear();

            foreach (var member in value)
            {
                // TODO: Was this safe to remove?
                //if (member is null)
                //{
                //    continue;
                //}

                _members.Add(member);
                member.Parent = this;
            }
        }
    }

    public override bool Equals(object? obj)
    {
        if (obj is not PatternDataStruct otherStruct)
        {
            return false;
        }

        if (_members.Count != otherStruct._members.Count)
        {
            return false;
        }

        for (var i = 0; i < _members.Count; i++)
        {
            if (!_members[i].Equals(otherStruct._members[i]))
            {
                return false;
            }
        }

        return base.Equals(obj);
    }

    private readonly List<PatternData> _members;
}