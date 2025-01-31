﻿using System.Text;
using HexControl.Core.Helpers;

namespace HexControl.PatternLanguage.Patterns;

public class PatternDataString16 : PatternData
{
    public PatternDataString16(long offset, long size, Evaluator evaluator, IntegerColor? color = null)
        : base(offset, size, evaluator, color) { }

    private PatternDataString16(PatternDataString16 other) : base(other) { }

    public override PatternData Clone() => new PatternDataString16(this);

    public override string GetFormattedName() => "String";

    public override string ToString(Evaluator evaluator)
    {
        var bytes = new byte[Size];
        //std::string buffer(this->getSize(), 0x00);
        evaluator.Buffer.Read(bytes, Offset);

        // TODO: change the endianess
        //for (auto & c : buffer)
        //    c = hex::changeEndianess(c, 2, this->getEndian());

        //std::erase_if(buffer, [](auto c){
        //    return c == 0x00;
        //});

        return Encoding.UTF8.GetString(bytes);
    }
}