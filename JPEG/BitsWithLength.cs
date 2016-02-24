using System.Runtime.InteropServices;

namespace JPEG
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct BitsWithLength
    {
        public BitsWithLength(int bits, int bitsCount)
        {
            Bits = bits;
            BitsCount = bitsCount;
        }

        public readonly int Bits;

        public readonly int BitsCount;

        public bool Equals(BitsWithLength other)
        {
            return Bits == other.Bits && BitsCount == other.BitsCount;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is BitsWithLength && Equals((BitsWithLength) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Bits.GetHashCode()*397) ^ BitsCount.GetHashCode();
            }
        }
    }
}