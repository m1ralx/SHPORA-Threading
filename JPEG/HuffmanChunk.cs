namespace JPEG
{
    internal class HuffmanChunk
    {
        public HuffmanChunk(byte[] data, int bitsCount)
        {
            Data = data;
            BitsCount = bitsCount;
        }

        public byte[] Data{ get; private set; }
        public int BitsCount{ get; private set; }
    }
}