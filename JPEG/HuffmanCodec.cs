using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JPEG
{
    internal class HuffmanNode
    {
        public byte? LeafLabel { get; set; }
        public int Frequency { get; set; }
        public HuffmanNode Left { get; set; }
        public HuffmanNode Right { get; set; }
    }

    internal class HuffmanCodec
    {

        public static byte[] Encode(byte[] data, out Dictionary<BitsWithLength, byte> decodeTable, Options options)
        {
            var frequences = ParallelCalcFrequences(data);
            var root = BuildHuffmanTree(frequences);

            var encodeTable = new BitsWithLength[byte.MaxValue + 1];
            FillEncodeTable(root, encodeTable);

            var chunkSize = 32*1024;//TODO optimize decoding with array preallocation
            var dataSize = data.Length;
            var chunkCount = dataSize/chunkSize + (dataSize % chunkSize == 0 ? 0 : 1);
            var chunks = Enumerable.Range(0, chunkCount)
                .Select(
                    i =>
                        new
                        {
                            Data = data.Skip(i*chunkSize).Take(chunkSize),
                            BitsBuffer = new BitsBuffer()
                        })
                        .ToList();
            Parallel.ForEach(chunks, new ParallelOptions {MaxDegreeOfParallelism = options.MaxDegreeOfParallelism}, p =>
            {
                foreach (var b in p.Data)
                    p.BitsBuffer.Add(encodeTable[b]);
            });
            decodeTable = CreateDecodeTable(encodeTable);
            return chunks.SelectMany(chunk => chunk.BitsBuffer.ToArray()).ToArray();
        }

        public static byte[] Decode(byte[] encodedData, Dictionary<BitsWithLength, byte> decodeTable, Options options)
        {
            var chunks = new List<HuffmanChunk>();

            for (int i = 0; i < encodedData.Length;)
            {
                var bitsCount = BitConverter.ToInt32(encodedData, i);
                var dataLength = bitsCount/8 + (bitsCount%8 == 0 ? 0 : 1);
                chunks.Add(new HuffmanChunk(
                    encodedData.Skip(i + 4).Take(dataLength).ToArray(),
                    bitsCount));
                i += 4 + dataLength;
            }

            return chunks
                .AsParallel()
                .AsOrdered()
                .WithDegreeOfParallelism(options.MaxDegreeOfParallelism)
                .SelectMany(chunk => DecodeChunk(chunk.Data, decodeTable, chunk.BitsCount))
                .ToArray();
        }

        private static IEnumerable<byte> DecodeChunk(byte[] data, Dictionary<BitsWithLength, byte> decodeTable, int totalBitsCount)
        {
            var result = new List<byte>();

            var bits = (int)0;
            var bitsCount = (int)0;
            for (var byteNum = 0; byteNum < data.Length; byteNum++)
            {
                var b = data[byteNum];
                for (var bitNum = 0; bitNum < 8 && byteNum * 8 + bitNum < totalBitsCount; bitNum++)
                {
                    bits = (int)((bits << 1) + ((b & (1 << (8 - bitNum - 1))) != 0 ? 1 : 0));
                    bitsCount++;

                    var key = new BitsWithLength(bits, bitsCount);
                    byte decodedByte;
                    if (decodeTable.TryGetValue(key, out decodedByte))
                    {
                        result.Add(decodedByte);
                        bitsCount = 0;
                        bits = 0;
                    }
                }
            }

            return result;
        }

        private static Dictionary<BitsWithLength, byte> CreateDecodeTable(BitsWithLength[] encodeTable)
        {
            var result = new Dictionary<BitsWithLength, byte>();
            for (var b = 0; b < encodeTable.Length; b++)
            {
                var bitsWithLength = encodeTable[b];
                result[bitsWithLength] = (byte) b;
            }
            return result;
        }

        
        private static void FillEncodeTable(HuffmanNode node, BitsWithLength[] encodeSubstitutionTable, byte bitvector = 0, byte depth = 0)
        {
            if (node.LeafLabel != null)
                encodeSubstitutionTable[node.LeafLabel.Value] = new BitsWithLength(bitvector, depth);
            else
            {
                if (node.Left != null)
                {
                    FillEncodeTable(node.Left, encodeSubstitutionTable, (byte)((bitvector << 1) + 1), (byte)(depth + 1));
                    FillEncodeTable(node.Right, encodeSubstitutionTable, (byte)((bitvector << 1) + 0), (byte)(depth + 1));
                }
            }
        }

        private static HuffmanNode BuildHuffmanTree(Dictionary<byte, int> frequences)
        {
            var nodes = new HashSet<HuffmanNode>(
                frequences.Keys.AsParallel().Select(b => new HuffmanNode
                {
                    Frequency = frequences[b],
                    LeafLabel = b
                }));
            while (nodes.Count > 1)
            {
                var firstMin = GetMin(nodes);
                nodes.Remove(firstMin);
                var secondMin = GetMin(nodes);
                nodes.Remove(secondMin);
                nodes.Add(new HuffmanNode
                {
                    Frequency = firstMin.Frequency + secondMin.Frequency,
                    Left = secondMin,
                    Right = firstMin
                });
            }
            return nodes.First();
        }
        
        private static HuffmanNode GetMin(HashSet<HuffmanNode> nodes) =>
            nodes.MinOrDefault(node => node.Frequency);

        private static Dictionary<byte, int> ParallelCalcFrequences(byte[] data) =>
            data
//                .AsParallel()
//                .WithDegreeOfParallelism(_options.MaxDegreeOfParallelism)
                .GroupBy(b => b)
                .ToDictionary(g => g.Key, g => g.Count());
    }
}