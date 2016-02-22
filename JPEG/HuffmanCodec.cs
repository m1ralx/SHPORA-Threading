﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JPEG
{
	class HuffmanNode
	{
		public byte? LeafLabel { get; set; }
		public int Frequency { get; set; }
		public HuffmanNode Left { get; set; }
		public HuffmanNode Right { get; set; }
	}

	class BitsWithLength
	{
		public int Bits { get; set; }
		public int BitsCount { get; set; }

		public class Comparer : IEqualityComparer<BitsWithLength>
		{
			public bool Equals(BitsWithLength x, BitsWithLength y)
			{
				if(x == y) return true;
				if(x == null || y == null)
					return false;
				return x.BitsCount == y.BitsCount && x.Bits == y.Bits;
			}

			public int GetHashCode(BitsWithLength obj)
			{
				if(obj == null)
					return 0;
				return obj.Bits ^ obj.BitsCount;
			}
		}
	}

	class BitsBuffer
	{
		private List<byte> buffer = new List<byte>();
		private BitsWithLength unfinishedBits = new BitsWithLength();

		public void Add(BitsWithLength bitsWithLength)
		{
			var bitsCount = bitsWithLength.BitsCount;
			var bits = bitsWithLength.Bits;

			int neededBits = 8 - unfinishedBits.BitsCount;
			while(bitsCount >= neededBits)
			{
				bitsCount -= neededBits;
				buffer.Add((byte) ((unfinishedBits.Bits << neededBits) + (bits >> bitsCount)));

				bits = bits & ((1 << bitsCount) - 1);

				unfinishedBits.Bits = 0;
				unfinishedBits.BitsCount = 0;

				neededBits = 8;
			}
			unfinishedBits.BitsCount +=  bitsCount;
			unfinishedBits.Bits = (unfinishedBits.Bits << bitsCount) + bits;
		}

		public byte[] ToArray(out long bitsCount)
		{
			bitsCount = buffer.Count * 8L + unfinishedBits.BitsCount;
			var result = new byte[bitsCount / 8 + (bitsCount % 8 > 0 ? 1 : 0)];
			buffer.CopyTo(result);
			if(unfinishedBits.BitsCount > 0)
				result[buffer.Count] = (byte) (unfinishedBits.Bits << (8 - unfinishedBits.BitsCount));
			return result;
		}
	}

	class HuffmanCodec
	{

	    private static List<ManualResetEvent> _doneEvents;
        public static byte[] Encode(byte[] data, out Dictionary<BitsWithLength, byte> decodeTable, out long bitsCount)
		{
			var frequences = CalcFrequences(data);

			var root = BuildHuffmanTree(frequences);

			var encodeTable = new BitsWithLength[byte.MaxValue + 1];
            _doneEvents = new List<ManualResetEvent>();
			FillEncodeTable(root, encodeTable);

            var bitsBuffer = new BitsBuffer();

            foreach (byte b in data)
				bitsBuffer.Add(encodeTable[b]);

			decodeTable = CreateDecodeTable(encodeTable);

			return bitsBuffer.ToArray(out bitsCount);
		}

		public static byte[] Decode(byte[] encodedData, Dictionary<BitsWithLength, byte> decodeTable, long bitsCount)
		{
			var result = new List<byte>();

			byte decodedByte;
			BitsWithLength sample = new BitsWithLength { Bits = 0, BitsCount = 0 };
			for(int byteNum = 0; byteNum < encodedData.Length; byteNum++)
			{
				var b = encodedData[byteNum];
				for(int bitNum = 0; bitNum < 8 && byteNum * 8 + bitNum < bitsCount; bitNum++)
				{
					sample.Bits = (sample.Bits << 1) + ((b & (1 << (8 - bitNum - 1))) != 0 ? 1 : 0);
					sample.BitsCount++;

					if(decodeTable.TryGetValue(sample, out decodedByte))
					{
						result.Add(decodedByte);

						sample.BitsCount = 0;
						sample.Bits = 0;
					}
				}
			}
			return result.ToArray();
		}

		private static Dictionary<BitsWithLength, byte> CreateDecodeTable(BitsWithLength[] encodeTable)
		{
			var result = new Dictionary<BitsWithLength, byte>(new BitsWithLength.Comparer());
			for(int b = 0; b < encodeTable.Length; b++)
			{
				var bitsWithLength = encodeTable[b];
				if(bitsWithLength == null)
					continue;

				result[bitsWithLength] = (byte) b;
			}
			return result;
		}
        private static void FillEncodeTable(HuffmanNode node, BitsWithLength[] encodeSubstitutionTable, int bitvector = 0, int depth = 0)
		{
            if (depth == 0)
                _doneEvents = new List<ManualResetEvent>();
			if(node.LeafLabel != null)
				encodeSubstitutionTable[node.LeafLabel.Value] = new BitsWithLength {Bits = bitvector, BitsCount = depth};
			else
			{
				if(node.Left != null)
				{
                    var firstEvent = new ManualResetEvent(false);
				    _doneEvents.Add(firstEvent);
//                    ThreadPool.QueueUserWorkItem(
//				        _ =>
//				        {
//				            FillEncodeTable(node.Left, encodeSubstitutionTable, (bitvector << 1) + 1, depth + 1);
//				            firstEvent.Set();
//				        });
					FillEncodeTable(node.Left, encodeSubstitutionTable, (bitvector << 1) + 1, depth + 1);

//                    var secondEvent = new ManualResetEvent(false);
//                    _doneEvents.Add(secondEvent);
//                    ThreadPool.QueueUserWorkItem(
//                        _ =>
//                        {
//                            FillEncodeTable(node.Right, encodeSubstitutionTable, (bitvector << 1) + 0, depth + 1);
//                            secondEvent.Set();
//                        });
                    FillEncodeTable(node.Right, encodeSubstitutionTable, (bitvector << 1) + 0, depth + 1);
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
                var firstMin = nodes.AsParallel().MinOrDefault(node => node.Frequency);
                nodes.Remove(firstMin);
                var secondMin = nodes.AsParallel().MinOrDefault(node => node.Frequency);
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

	    private static Dictionary<byte, int> CalcFrequences(byte[] data) =>
	        data
            .AsParallel()
            .GroupBy(b => b)
            .ToDictionary(g => g.Key, g => g.Count());
//        private static int[] CalcFrequences(byte[] data)
//		{
//			var result = new int[byte.MaxValue + 1];
//			foreach(var b in data)
//				result[b]++;
//			return result;
//		}
	}
}
