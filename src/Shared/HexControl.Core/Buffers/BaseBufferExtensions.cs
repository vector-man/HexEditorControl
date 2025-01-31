﻿using HexControl.Core.Helpers;
using HexControl.Core.Numerics;

namespace HexControl.Core.Buffers;

public enum Endianess
{
    Native,
    Big,
    Little
}

public static class BaseBufferExtensions
{
    private static readonly ExactArrayPool<byte> Pool = ExactArrayPool<byte>.Shared;

    public static Int128 ReadInt128(this BaseBuffer buffer, long offset, Endianess endianess = Endianess.Native)
    {
        if (offset + Int128.Size > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Cannot read longer than buffer length.");
        }

        var bytes = Pool.Rent(Int128.Size);
        try
        {
            buffer.Read(bytes, offset);
            bytes = SwapEndianess(bytes, endianess);

            var span = bytes.AsSpan();
            var a = BitConverter.ToInt64(span.Slice(0, Int128.Size / 2));
            var b = BitConverter.ToInt64(span.Slice(Int128.Size / 2, Int128.Size / 2));
            return new Int128(a, b);
        }
        finally
        {
            Pool.Return(bytes);
        }
    }

    public static UInt128 ReadUInt128(this BaseBuffer buffer, long offset, Endianess endianess = Endianess.Native)
    {
        if (offset + UInt128.Size > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Cannot read longer than buffer length.");
        }

        var bytes = Pool.Rent(UInt128.Size);
        try
        {
            buffer.Read(bytes, offset);
            bytes = SwapEndianess(bytes, endianess);

            var span = bytes.AsSpan();
            var a = BitConverter.ToUInt64(span.Slice(0, UInt128.Size / 2));
            var b = BitConverter.ToUInt64(span.Slice(UInt128.Size / 2, UInt128.Size / 2));
            return new UInt128(a, b);
        }
        finally
        {
            Pool.Return(bytes);
        }
    }

    public static long ReadInt64(this BaseBuffer buffer, long offset, Endianess endianess = Endianess.Native)
    {
        if (offset + sizeof(long) > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Cannot read longer than buffer length.");
        }

        var bytes = Pool.Rent(sizeof(long));
        try
        {
            buffer.Read(bytes, offset);
            return BitConverter.ToInt64(SwapEndianess(bytes, endianess));
        }
        finally
        {
            Pool.Return(bytes);
        }
    }

    public static ulong ReadUInt64(this BaseBuffer buffer, long offset, Endianess endianess = Endianess.Native)
    {
        if (offset + sizeof(ulong) > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Cannot read longer than buffer length.");
        }

        var bytes = Pool.Rent(sizeof(ulong));
        try
        {
            buffer.Read(bytes, offset);
            return BitConverter.ToUInt64(SwapEndianess(bytes, endianess));
        }
        finally
        {
            Pool.Return(bytes);
        }
    }

    public static int ReadInt32(this BaseBuffer buffer, long offset, Endianess endianess = Endianess.Native)
    {
        if (offset + sizeof(int) > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Cannot read longer than buffer length.");
        }

        var bytes = Pool.Rent(sizeof(int));
        try
        {
            buffer.Read(bytes, offset);
            return BitConverter.ToInt32(SwapEndianess(bytes, endianess));
        }
        finally
        {
            Pool.Return(bytes);
        }
    }

    public static uint ReadUInt32(this BaseBuffer buffer, long offset, Endianess endianess = Endianess.Native)
    {
        if (offset + sizeof(uint) > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Cannot read longer than buffer length.");
        }

        var bytes = Pool.Rent(sizeof(uint));

        try
        {
            buffer.Read(bytes, offset);
            return BitConverter.ToUInt32(SwapEndianess(bytes, endianess));
        }
        finally
        {
            Pool.Return(bytes);
        }
    }

    public static short ReadInt16(this BaseBuffer buffer, long offset, Endianess endianess = Endianess.Native)
    {
        if (offset + sizeof(short) > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Cannot read longer than buffer length.");
        }

        var bytes = Pool.Rent(sizeof(short));
        try
        {
            buffer.Read(bytes, offset);
            return BitConverter.ToInt16(SwapEndianess(bytes, endianess));
        }
        finally
        {
            Pool.Return(bytes);
        }
    }

    public static ushort ReadUInt16(this BaseBuffer buffer, long offset, Endianess endianess = Endianess.Native)
    {
        if (offset + sizeof(ushort) > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Cannot read longer than buffer length.");
        }

        var bytes = Pool.Rent(sizeof(ushort));
        try
        {
            buffer.Read(bytes, offset);
            return BitConverter.ToUInt16(SwapEndianess(bytes, endianess));
        }
        finally
        {
            Pool.Return(bytes);
        }
    }

    public static char ReadChar(this BaseBuffer buffer, long offset, Endianess endianess = Endianess.Native)
    {
        if (offset + sizeof(short) > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Cannot read longer than buffer length.");
        }

        var bytes = Pool.Rent(sizeof(char));
        try
        {
            buffer.Read(bytes, offset);
            return (char)BitConverter.ToInt16(SwapEndianess(bytes, endianess));
        }
        finally
        {
            Pool.Return(bytes);
        }
    }

    public static sbyte ReadByte(this BaseBuffer buffer, long offset)
    {
        if (offset + sizeof(byte) > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Cannot read longer than buffer length.");
        }

        var bytes = Pool.Rent(sizeof(sbyte));
        try
        {
            buffer.Read(bytes, offset);
            return (sbyte)bytes[0];
        }
        finally
        {
            Pool.Return(bytes);
        }
    }

    public static byte ReadUByte(this BaseBuffer buffer, long offset)
    {
        if (offset + sizeof(byte) > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Cannot read longer than buffer length.");
        }

        var bytes = Pool.Rent(sizeof(byte));
        try
        {
            buffer.Read(bytes, offset);
            return bytes[0];
        }
        finally
        {
            Pool.Return(bytes);
        }
    }

    private static byte[] SwapEndianess(byte[] buffer, Endianess endianess)
    {
        var shouldSwap = BitConverter.IsLittleEndian && endianess is Endianess.Big ||
                         !BitConverter.IsLittleEndian && endianess is Endianess.Little;
        if (shouldSwap)
        {
            Array.Reverse(buffer);
        }

        return buffer;
    }
}