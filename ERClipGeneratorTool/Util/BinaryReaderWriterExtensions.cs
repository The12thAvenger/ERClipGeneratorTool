using SoulsFormats;

namespace ERClipGeneratorTool.Util;

// Taken from SoulsAssetPipeline by Meowmaritus (https://github.com/Meowmaritus/SoulsAssetPipeline/)
public static class BinaryReaderWriterExtensions
{
    public static long GetNextPaddedOffsetAfterCurrentField(this BinaryReaderEx br, int currentFieldLength, int align)
    {
        long pos = br.Position;
        pos += currentFieldLength;
        if (align <= 0)
            return pos;
        if (pos % align > 0)
            pos += align - (pos % align);
        return pos;
    }
}