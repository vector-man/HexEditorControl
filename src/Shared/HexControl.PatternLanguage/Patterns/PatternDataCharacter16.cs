﻿using HexControl.Core.Buffers;
using HexControl.Core.Helpers;

namespace HexControl.PatternLanguage.Patterns;

public class PatternDataCharacter16 : PatternData
{
    public PatternDataCharacter16(long offset, Evaluator evaluator, IntegerColor? color = null)
        : base(offset, 2, evaluator, color) { }

    private PatternDataCharacter16(PatternDataCharacter16 other) : base(other) { }

    public override PatternData Clone() => new PatternDataCharacter16(this);

    public override string GetFormattedName() => "char16";

    public override string ToString(Evaluator evaluator) =>
        evaluator.Buffer.ReadChar(Offset, Endian ?? evaluator.DefaultEndian).ToString();
}