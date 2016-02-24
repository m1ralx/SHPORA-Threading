using CommandLine;

namespace JPEG
{
    public class Options
    {
        [Option('e', "encode", 
            HelpText = "Path to bmp file.")]
        public string PathToBmp { get; set; }

        [Option('t', "threads", DefaultValue = 1,
            HelpText = "Max parallelism degree.")]
        public int MaxDegreeOfParallelism { get; set; }

        [Option('w', "window", DefaultValue = 8,
            HelpText = "Size of DCT window")]
        public int DCTSize { get; set; }

        [Option('q', "quota", DefaultValue = 100,
            HelpText = "Percentage of not ignoring high frequency")]
        public int Quota { get; set; }

        [Option('d', "decode", 
            Required = false,
            HelpText = "Path to encoded image.")]
        public string PathToEncoded { get; set; }
    }
}