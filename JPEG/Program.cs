using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using CommandLine;

namespace JPEG
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var options = new Options();
            Parser.Default.ParseArguments(args, options);
            try
            {
                Encode(options);
                Decode(options);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static void Encode(Options options)
        {
            var compressionLevel = CalcCompressionLevel(options);
            var compressedFileName = options.PathToBmp + ".compressed." + options.DCTSize + "." + compressionLevel;

            var bmp = (Bitmap)Image.FromFile(options.PathToBmp);

            CheckCorrectSize(options, bmp);

            var grayscaleMatrix = bmp.ToGrayscaleMatrix();
            var compressedImage = grayscaleMatrix.ParallelCompressWithDCT(options);

            var bytesForHuffman = compressedImage.ToBytesArray();
            var decodeTable = null as Dictionary<BitsWithLength, byte>;
            var encodedWithHuffman = HuffmanCodec.Encode(bytesForHuffman, out decodeTable, options);
            using (FileStream fs = File.OpenWrite(compressedFileName))
            {
                var encodedDataLength = BitConverter.GetBytes(encodedWithHuffman.Length);
                fs.Write(encodedDataLength, 0, 4);
                
                fs.Write(encodedWithHuffman, 0, encodedWithHuffman.Length);

                var serializedTable = SerializeDecodeTable(decodeTable);
                fs.Write(serializedTable, 0, serializedTable.Length);
            }
        }

        private static unsafe byte[] SerializeDecodeTable(Dictionary<BitsWithLength, byte> decodeTable)
        {
            var data = new byte[4 + decodeTable.Count*5];
            Buffer.BlockCopy(BitConverter.GetBytes(decodeTable.Count), 0, data, 0, 4);
            var index = 4;
            fixed (byte* dataPtr = data)
            {
                foreach (var pair in decodeTable)
                {
                    *(BitsWithLength*) (dataPtr + index) = pair.Key;
                    *(dataPtr + index + 4) = pair.Value;
                    index += 5;
                }
            }
            return data;
        }

        private static unsafe Dictionary<BitsWithLength, byte> DeserializeDecodeTable(byte[] data, int offset)
        {
            var count = BitConverter.ToInt32(data, offset);
            var table = new Dictionary<BitsWithLength, byte>(count);
            fixed (byte* dataPtr = &data[offset])
            {
                for (int i = 0; i < count; i++)
                {
                    var entryIndex = dataPtr + 4 + i*5;
                    table.Add(*(BitsWithLength*) entryIndex, *(entryIndex + 4));
                }
            }
            return table;
        } 

        private static void CheckCorrectSize(Options options, Bitmap bmp)
        {
            if (bmp.Width % options.DCTSize != 0 || bmp.Height % options.DCTSize != 0)
                throw new Exception($"Image width and height must be multiple of {options.DCTSize}");
        }

        private static int CalcCompressionLevel(Options options) => options.DCTSize * options.Quota / 100;

        private static void Decode(Options options)
        {
            var uncompressedFileName = options.PathToEncoded + ".uncompressed." + options.DCTSize + "." +
                                       CalcCompressionLevel(options) + ".bmp";
            var data = File.ReadAllBytes(options.PathToEncoded);
            var encodedDataLength = BitConverter.ToInt32(data, 0);

            var encodedData = new byte[encodedDataLength];
            Buffer.BlockCopy(data, 4, encodedData, 0, encodedDataLength);

            var decodeTable = DeserializeDecodeTable(data, 4 + encodedDataLength);

            var decodedHuffman = HuffmanCodec.Decode(encodedData, decodeTable, options);
            var compressedImg = CompressedImage.LoadFromBytesArray(decodedHuffman, options.DCTSize);
            compressedImg
                .ParallelUncompressWithDCT(options)
                .GrayscaleMatrixToBitmap()
                .Save(uncompressedFileName);
        }
    }
}