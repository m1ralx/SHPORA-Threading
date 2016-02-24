using System;
using System.Collections.Generic;

namespace JPEG
{
    internal class BitsBuffer
    {
        private readonly List<byte> buffer = new List<byte>();
        private int unfinishedBits;
        private int unfinishedBitsCount;

        public void Add(BitsWithLength bitsWithLength)
        {
            var bitsCount = bitsWithLength.BitsCount;
            var bits = bitsWithLength.Bits;

            var neededBits = (int)(8 - unfinishedBitsCount);
            while (bitsCount >= neededBits)
            {
                bitsCount -= neededBits;
                buffer.Add((byte)((unfinishedBits << neededBits) + (bits >> bitsCount)));

                bits = (int)(bits & ((1 << bitsCount) - 1));

                unfinishedBits = 0;
                unfinishedBitsCount = 0;

                neededBits = 8;
            }
            unfinishedBitsCount += bitsCount;
            unfinishedBits = (int)((unfinishedBits << bitsCount) + bits);
        }

        public byte[] ToArray()
        {
            var bitsCount = buffer.Count*8 + unfinishedBitsCount;
            var dataLength = bitsCount/8 + (bitsCount%8 > 0 ? 1 : 0);
            var result = new byte[4 + dataLength];
            BitConverter.GetBytes(bitsCount).CopyTo(result, 0);
            buffer.CopyTo(result, 4);
            if (unfinishedBitsCount > 0)
                result[buffer.Count] = (byte) (unfinishedBits << (8 - unfinishedBitsCount));
            return result;
        }
    }
}