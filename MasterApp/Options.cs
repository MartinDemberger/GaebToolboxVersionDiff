namespace GaebToolBoxVersionCompare2
{
    public class Options
    {
        public string InputPath { get; set; } = "..\\InputFiles";
        public int ErrorsToPrint { get; set; } = 3;
        public string? SerialNumber { get; set; }
        public string? ConverterSerialNumber { get; set; }
        public string OutputPath { get; set; } = "..\\OutputFiles";
    }
}
