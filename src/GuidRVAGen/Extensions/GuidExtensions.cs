using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace GuidRVAGen.Extensions;

internal static class GuidExtensions
{
    private static Guid ConstructGuid(ReadOnlySpan<byte> span, bool bigEndian)
    {
        if (span.Length != 16)
        {
            throw new ArgumentException("The input span must be 16 bytes long.", nameof(span));
        }

        int a = MemoryMarshal.Read<int>(span[0..4]);
        if (BitConverter.IsLittleEndian == bigEndian)
            a = BinaryPrimitives.ReverseEndianness(a);
        short b = MemoryMarshal.Read<short>(span[4..2]);
        if (BitConverter.IsLittleEndian == bigEndian)
            b = BinaryPrimitives.ReverseEndianness(b);
        short c = MemoryMarshal.Read<short>(span[6..2]);
        if (BitConverter.IsLittleEndian == bigEndian)
            c = BinaryPrimitives.ReverseEndianness(c);

        return new Guid(a, b, c, span[8], span[9], span[10], span[11], span[12], span[13], span[14], span[15]);
    }

    public static bool TryWriteBytes(this Guid guid, Span<byte> destination)
    {
        if (destination.Length < 16)
            return false;

        if (BitConverter.IsLittleEndian)
        {
            MemoryMarshal.TryWrite(destination, ref guid);
        }
        else
        {
            MemoryMarshal.TryWrite(destination, ref guid);
            guid = ConstructGuid(destination, false);
            MemoryMarshal.TryWrite(destination, ref guid);
        }
        return true;
    }
}