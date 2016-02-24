using System.Linq;
using System.Threading.Tasks;

namespace JPEG
{
    public static class DCTUncompressor
    {

        public static double[,] UncompressWithDCT(this CompressedImage image, Options options)
        {
            var result = new double[image.Height, image.Width];
            var DCTSize = options.DCTSize;
            var freqNum = 0;

            for (var y = 0; y < image.Height; y += DCTSize)
            {
                for (var x = 0; x < image.Width; x += DCTSize)
                {
                    var channelFreqs = new double[DCTSize, DCTSize];
                    for (var i = 0; i < DCTSize; i++)
                    {
                        for (var j = 0; j < DCTSize; j++)
                        {
                            if (i + j < image.CompressionLevel)
                                channelFreqs[i, j] = image.Frequences[freqNum++];
                        }
                    }
                    var processedSubmatrix = DCTHelper.IDCT2D(channelFreqs);
                    processedSubmatrix.ShiftMatrixValues(128);
                    result.SetSubmatrix(processedSubmatrix, y, x);
                }
            }
            return result;
        }
        public static double[,] ParallelUncompressWithDCT(this CompressedImage image, Options options)
        {
            var result = new double[image.Height, image.Width];
            var blocksCount = image.Width * image.Height / (options.DCTSize * options.DCTSize);
            var freqsCount = Enumerable.Range(1, image.CompressionLevel).Sum();
            Parallel.For(0, blocksCount, blockIndex =>
            {
                var y = blockIndex/(image.Width/ options.DCTSize);
                var x = blockIndex%(image.Width/ options.DCTSize);
                var channelFreqs = new double[options.DCTSize, options.DCTSize];
                var freqNum = blockIndex*freqsCount;

                for (var i = 0; i < options.DCTSize; i++)
                {
                    for (var j = 0; j < options.DCTSize; j++)
                    {
                        if (i + j < image.CompressionLevel)
                            channelFreqs[i, j] = image.Frequences[freqNum++];
                    }
                }
                var processedSubmatrix = DCTHelper.IDCT2D(channelFreqs);
                processedSubmatrix.ShiftMatrixValues(128);
                result.SetSubmatrix(processedSubmatrix, y * options.DCTSize, x * options.DCTSize);
            });
            return result;
        }
    }
}
