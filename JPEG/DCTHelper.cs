using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JPEG
{
    public static class DCTHelper
    {
        public static CompressedImage CompressWithDCT(this double[,] channelPixels, int DCTSize, int compressionLevel = 4)
        {
            int frequencesPerBlock = -1;

            var height = channelPixels.GetLength(0);
            var width = channelPixels.GetLength(1);

            var result = new List<double>();

            for (int y = 0; y < height; y += DCTSize)
            {
                for (int x = 0; x < width; x += DCTSize)
                {
                    var subMatrix = channelPixels.GetSubMatrix(y, DCTSize, x, DCTSize, DCTSize);
                    subMatrix.ShiftMatrixValues(-128);

                    var channelFreqs = DCT.DCT2D(subMatrix);

                    frequencesPerBlock = DCTSize * DCTSize;
                    for (int i = 0; i < DCTSize; i++)
                    {
                        for (int j = 0; j < DCTSize; j++)
                        {
                            if (i + j < compressionLevel)
                            {
                                result.Add(channelFreqs[i, j]);
                                continue;
                            }
                            channelFreqs[i, j] = 0;
                            frequencesPerBlock--;
                        }
                    }
                }
            }

            return new CompressedImage { CompressionLevel = compressionLevel, FrequencesPerBlock = frequencesPerBlock, Frequences = result, Height = height, Width = width };
        }

        public static double[,] UncompressWithDCT(this CompressedImage image, int DCTSize)
        {
            var result = new double[image.Height, image.Width];

            int freqNum = 0;
            for (int y = 0; y < image.Height; y += DCTSize)
            {
                for (int x = 0; x < image.Width; x += DCTSize)
                {
                    var channelFreqs = new double[DCTSize, DCTSize];
                    for (int i = 0; i < DCTSize; i++)
                    {
                        for (int j = 0; j < DCTSize; j++)
                        {
                            if (i + j < image.CompressionLevel)
                                channelFreqs[i, j] = image.Frequences[freqNum++];
                        }
                    }
                    var processedSubmatrix = DCT.IDCT2D(channelFreqs);
                    processedSubmatrix.ShiftMatrixValues(128);
                    result.SetSubmatrix(processedSubmatrix, y, x);
                }
            }
            return result;
        }
    }
}
