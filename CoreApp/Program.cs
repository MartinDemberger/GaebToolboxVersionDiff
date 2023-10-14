using System;
using System.IO;
using CommandLine;



#if !NET
using GaebToolBoxV330;
#else
using GAEB_Toolbox_33;
#endif

namespace GaebToolBoxVersionCompare2
{
    internal class Program
    {
        public class Options
        {
            [Option('i', "input", Required = true, HelpText = "The input file to read.")]
            public string InputPath { get; set; }

            [Option('o', "output", Required = true, HelpText = "The output file to write.")]
            public string OutputPath { get; set; }

            [Option('f', "format", Required = true, HelpText = "The output format to write.")]
            public string OutputFormat { get; set; }

            [Option('s', "serial", Required = true, HelpText = "The serial number for toolbox.")]
            public string SerialNumber { get; set; }

            [Option('c', "conerter-serial", Required = false, HelpText = "The serial number for converter.")]
            public string ConverterSerialNumber { get; set; }
        }

        private const string _fileTypeGaeb90 = "GAEB90";
        private const string _fileTypeGaeb2000 = "GAEB2000";
        private const string _fileTypeGaebXml = "GAEBDAXML";

        private static void Main(string[] args)
        {
            var options = Parser.Default.ParseArguments<Options>(args);
            try
            {
                //    var inputPath = args[0];
                //    var outputPath = args[1];
                //    var outputFormat = args[2];
                //    var serialNumber = args[3];
                //    string converterSerialNumber = null;
                //    if (args.Length > 4)
                //        converterSerialNumber = args[4];

                var gaeb = CreateToolbox(options.Value.SerialNumber, options.Value.ConverterSerialNumber);
                var fileType = gaeb.gGetFileType(options.Value.InputPath);
                SetFileType(gaeb, fileType);

                var success = gaeb.gRead(options.Value.InputPath);
                if (!success)
                    throw new Exception($"File can't be read");
                SetFileType(gaeb, options.Value.OutputFormat);

                var outputDirectory = Directory.GetParent(options.Value.OutputPath);
                Directory.CreateDirectory(outputDirectory.FullName);
                success = gaeb.gWrite(options.Value.OutputPath);
                if (!success)
                    throw new Exception($"File can't be written.");
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Console.Write(ex);
                Environment.Exit(1);
            }
        }

        private static void SetFileType(GX_GAEB content, string format)
        {
            content.gSetOptions("disableGaeb90");
            content.gSetOptions("disableGaeb2000");
            content.gSetOptions("disableGaebXml31");
            content.gSetOptions("disableGaebXml32");
            switch (format)
            {
                case _fileTypeGaeb90:
                    content.gSetOptions("enableGaeb90");
                    break;
                case _fileTypeGaeb2000:
                    content.gSetOptions("enableGaeb2000");
                    break;
                case _fileTypeGaebXml:
                    content.gSetOptions("enableGaebXml32");
                    break;
                default:
                    throw new Exception($"Unknown file type {format}");
            }
        }

        private static GX_GAEB CreateToolbox(string serialNumber, string converterSerialNumber)
        {
            var gaeb = new GX_GAEB();
            gaeb.gSetSerialNumber(serialNumber);
            if (!string.IsNullOrWhiteSpace(converterSerialNumber))
                gaeb.gSetOptions(converterSerialNumber);
            gaeb.gSetOptions("modifyDate");
            gaeb.gSetOptions("gaeb90useWordSoftBreak");
            gaeb.gSetOptions("xmlPrettyPrintOff");
            gaeb.gSetOptions("SaveGaeb2000RtfTexte");
            gaeb.gSetOptions("SaveGaeb2000OZ");
            gaeb.gSetOptions("anyElement");
            gaeb.gSetOptions("anyObject");
            gaeb.gSetOptions("gaeb90useZA77");
            gaeb.gSetOptions("gaeb90useZA75");
            gaeb.gSetOptions("gaeb90useT2");
            gaeb.gSetOptions("gaeb90useZA86");
            gaeb.gSetOptions("xmlUseUnknownObjects");
            gaeb.gSetOptions("encodeStlbBau");

            return gaeb;
        }
    }
}
