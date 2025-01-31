﻿using System.IO.MemoryMappedFiles;

namespace HexControl.Core.Buffers;

public interface IFindStrategy
{
    long SearchInFile(
        MemoryMappedViewAccessor accessor,
        long startOffset,
        long maxSearchLength,
        bool backward);

    long SearchInBuffer(byte[] buffer, long startOffset, long maxSearchLength, bool backward);
    long SearchInBuffer(BaseBuffer buffer, long startOffset, long maxSearchLength, bool backward);
}

internal class KmpFindStrategy : IFindStrategy
{
    private readonly Lazy<int[]> _kmpFailure;
    private readonly byte[] _pattern;
    private readonly byte[] _readBuffer;

    public KmpFindStrategy(byte[] pattern)
    {
        _pattern = pattern;
        _kmpFailure = new Lazy<int[]>(ComputeKmpFailureFunction);
        _readBuffer = new byte[4096];
    }

    public unsafe long SearchInFile(
        MemoryMappedViewAccessor accessor,
        long startOffset,
        long maxSearchLength,
        bool backward)
    {
        var safeBuffer = accessor.SafeMemoryMappedViewHandle;
        var bytes = (byte*)safeBuffer.DangerousGetHandle();
        return backward
            ? LastIndexOf(bytes, startOffset, maxSearchLength)
            : FirstIndexOf(bytes, (long)safeBuffer.ByteLength, startOffset, maxSearchLength);
    }

    public unsafe long SearchInBuffer(byte[] buffer, long startOffset, long maxSearchLength, bool backward)
    {
        fixed (byte* bytes = buffer)
        {
            return backward
                ? LastIndexOf(bytes, startOffset, maxSearchLength)
                : FirstIndexOf(bytes, buffer.LongLength, startOffset, maxSearchLength);
        }
    }

    public long SearchInBuffer(BaseBuffer buffer, long startOffset, long maxSearchLength, bool backward)
    {
        var provider = new BufferByteProvider(buffer, _readBuffer);

        return backward
            ? LastIndexOfInProvider(provider, startOffset, maxSearchLength)
            : FirstIndexOfInProvider(provider, startOffset, maxSearchLength);
    }

    // See: https://en.wikipedia.org/wiki/Knuth%E2%80%93Morris%E2%80%93Pratt_algorithm
    private int[] ComputeKmpFailureFunction()
    {
        var table = new int[_pattern.Length];
        if (_pattern.Length >= 1)
        {
            table[0] = -1;
        }

        if (_pattern.Length >= 2)
        {
            table[1] = 0;
        }

        var pos = 2;
        var cnd = 0;
        while (pos < _pattern.Length)
        {
            if (_pattern[pos - 1] == _pattern[cnd])
            {
                table[pos] = cnd + 1;
                cnd++;
                pos++;
            }
            else if (cnd > 0)
            {
                cnd = table[cnd];
            }
            else
            {
                table[pos] = 0;
                pos++;
            }
        }

        return table;
    }

    private unsafe long FirstIndexOfInProvider(BufferByteProvider provider, long startIndex, long maxSearchLength)
    {
        var currentIndex = startIndex;
        long i = 0;

        var patternLength = _pattern.Length;
        fixed (byte* pattern = _pattern)
        fixed (int* table = _kmpFailure.Value)
        {
            while (currentIndex + i < provider.Length && currentIndex - startIndex + i < maxSearchLength)
            {
                if (pattern[i] == provider.ReadByte(currentIndex + i))
                {
                    if (i == patternLength - 1)
                    {
                        return currentIndex;
                    }

                    i++;
                }
                else
                {
                    if (table[i] > -1)
                    {
                        currentIndex = currentIndex + i - table[i];
                        i = table[i];
                    }
                    else
                    {
                        currentIndex++;
                        i = 0;
                    }
                }
            }
        }

        return -1;
    }

    private unsafe long LastIndexOfInProvider(BufferByteProvider provider, long startIndex, long maxSearchLength)
    {
        var currentIndex = startIndex;
        long i = 0;

        var patternLength = _pattern.Length;
        fixed (byte* pattern = _pattern)
        fixed (int* table = _kmpFailure.Value)
        {
            while (currentIndex - i >= 0 && startIndex - currentIndex + i < maxSearchLength)
            {
                if (pattern[patternLength - i - 1] == provider.ReadByte(currentIndex - i))
                {
                    if (i == patternLength - 1)
                    {
                        return currentIndex - patternLength + 1;
                    }

                    i++;
                }
                else
                {
                    if (table[i] > -1)
                    {
                        currentIndex -= i;
                        i = table[i];
                    }
                    else
                    {
                        currentIndex--;
                        i = 0;
                    }
                }
            }
        }

        return -1;
    }

    private unsafe long FirstIndexOf(byte* bytes, long length, long startIndex, long maxSearchLength)
    {
        var currentIndex = startIndex;
        long i = 0;

        var patternLength = _pattern.Length;
        fixed (byte* pattern = _pattern)
        fixed (int* table = _kmpFailure.Value)
        {
            while (currentIndex + i < length && currentIndex - startIndex + i < maxSearchLength)
            {
                if (pattern[i] == bytes[currentIndex + i])
                {
                    if (i == patternLength - 1)
                    {
                        return currentIndex;
                    }

                    i++;
                }
                else
                {
                    if (table[i] > -1)
                    {
                        currentIndex = currentIndex + i - table[i];
                        i = table[i];
                    }
                    else
                    {
                        currentIndex++;
                        i = 0;
                    }
                }
            }
        }

        return -1;
    }

    private unsafe long LastIndexOf(byte* bytes, long startIndex, long maxSearchLength)
    {
        var currentIndex = startIndex;
        long i = 0;

        var patternLength = _pattern.Length;
        fixed (byte* pattern = _pattern)
        fixed (int* table = _kmpFailure.Value)
        {
            while (currentIndex - i >= 0 && startIndex - currentIndex + i < maxSearchLength)
            {
                if (pattern[patternLength - i - 1] == bytes[currentIndex - i])
                {
                    if (i == patternLength - 1)
                    {
                        return currentIndex - patternLength + 1;
                    }

                    i++;
                }
                else
                {
                    if (table[i] > -1)
                    {
                        currentIndex -= i;
                        i = table[i];
                    }
                    else
                    {
                        currentIndex--;
                        i = 0;
                    }
                }
            }
        }

        return -1;
    }

    private ref struct BufferByteProvider
    {
        private readonly BaseBuffer _buffer;
        private readonly byte[] _readBuffer;
        private long _currentOffset = -1;

        public BufferByteProvider(BaseBuffer buffer, byte[] readBuffer)
        {
            _buffer = buffer;
            _readBuffer = readBuffer;

            _currentOffset = -1;
        }

        public long Length => _buffer.Length;

        public byte ReadByte(long offset)
        {
            var requestedOffset = offset - offset % _readBuffer.Length;
            if (_currentOffset == -1 || requestedOffset != _currentOffset)
            {
                _currentOffset = requestedOffset;
                _buffer.Read(_readBuffer, _currentOffset);
            }

            return _readBuffer[offset - _currentOffset];
        }
    }
}