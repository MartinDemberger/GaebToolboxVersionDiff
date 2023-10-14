using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Microsoft.XmlDiffPatch;
using Spectre.Console;

namespace GaebToolBoxVersionCompare2
{
    internal class Program
    {
        private const string _fileTypeGaeb90 = "GAEB90";
        private const string _fileTypeGaeb2000 = "GAEB2000";
        private const string _fileTypeGaebXml = "GAEBDAXML";
        private const string _appCore = "CoreApp.exe";
        private const string _appFramework = "FrameworkApp.exe";

        private static void Main()
        {
            AnsiConsole.Write(new FigletText("GAEB-Toolbox")
                .LeftAligned()
                .Color(Color.Blue));

            var settings = new Options();

            while (true)
            {
                var command = AnsiConsole.Prompt<CommandType>(new SelectionPrompt<CommandType>()
                    .AddChoices(new[]
                    {
                        CommandType.Run,
                        CommandType.Configuration,
                        CommandType.Exit,
                    })
                    );

                switch (command)
                {
                    case CommandType.Run:
                        Run(settings);
                        break;

                    case CommandType.Configuration:
                        Configure(settings);
                        break;

                    case CommandType.Exit:
                        return;
                }
            }
        }

        public static void Configure(Options settings)
        {
            settings.InputPath = AnsiConsole.Prompt(new TextPrompt<string>("Input path")
                .DefaultValue(settings.InputPath)
                );
            settings.OutputPath = AnsiConsole.Prompt(new TextPrompt<string>("Output path")
                .DefaultValue(settings.OutputPath)
                );
            settings.ErrorsToPrint = AnsiConsole.Prompt(new TextPrompt<int>("Erroneous files to print")
                .DefaultValue(settings.ErrorsToPrint)
                );
            settings.SerialNumber = AnsiConsole.Prompt(new TextPrompt<string>("Serial number for GAEB Toolbox")
                .DefaultValue(settings.SerialNumber)
                );
            settings.ConverterSerialNumber = AnsiConsole.Prompt(new TextPrompt<string>("Serial number for Converter")
                .DefaultValue(settings.ConverterSerialNumber)
                );
        }

        public static void Run(Options settings)
        {
            AnsiConsole.Write("Compare all files in ");
            AnsiConsole.Write(new TextPath(Path.GetFullPath(settings.InputPath)));
            AnsiConsole.WriteLine();
            var fileList = ReadFileList(settings.InputPath);
            AnsiConsole.MarkupLine($"Found [blue]{fileList.Count}[/] files.");

            var table = new Table()
                .Border(TableBorder.MinimalHeavyHead)
                //.Expand()
                ;
            table.AddColumn("Filename", column => column
                .Alignment(Justify.Left)
                .NoWrap()
                .Width(60)
                );
            table.AddColumn("GAEB-90", _ => _.Centered());
            table.AddColumn("GAEB-2000", _ => _.Centered());
            table.AddColumn("GAEB-XML", _ => _.Centered());

            var errors = new List<ErrorDetails>();
            var fileCount = 0;
            var gaeb90ErrorCount = 0;
            var gaeb2000ErrorCount = 0;
            var gaebXmlErrorCount = 0;

            AnsiConsole.Live(table)
                .AutoClear(false)
                .Start(ctx =>
                {
                    foreach (var file in fileList)
                    {
                        table.AddRow(
                            new TextPath(file),
                            new Markup("...", new Style(decoration: Decoration.Dim)),
                            new Markup("...", new Style(decoration: Decoration.Dim)),
                             new Markup("...", new Style(decoration: Decoration.Dim)));
                        ctx.Refresh();

                        var rowNumber = table.Rows.Count - 1;

                        var gaeb90 = CheckGaeb90(file, settings);
                        HandleResult(gaeb90, 1);
                        if (gaeb90 != null)
                            gaeb90ErrorCount++;

                        var gaeb2000 = CheckGaeb2000(file, settings);
                        HandleResult(gaeb2000, 2);
                        if (gaeb2000 != null)
                            gaeb2000ErrorCount++;

                        var gaebXml = CheckGaebXml(file, settings);
                        HandleResult(gaebXml, 3);
                        if (gaebXml != null)
                            gaebXmlErrorCount++;

                        void HandleResult(ErrorDetails errorDetails, int column)
                        {
                            if (errorDetails == null)
                                table.Rows.Update(rowNumber, column, new Markup("[green]OK[/]"));
                            else
                            {
                                table.Rows.Update(rowNumber, column, new Markup("[red]ERROR[/]"));
                                errors.Add(errorDetails);
                            }
                            fileCount++;
                            ctx.Refresh();
                        }
                    }
                });

            AnsiConsole.Write(new BarChart()
                .Label("Success rate")
                .CenterLabel()
                .AddItem("Total", fileCount)
                .AddItem("Errors GAEB-90", gaeb90ErrorCount, Color.Yellow)
                .AddItem("Errors GAEB-2000", gaeb2000ErrorCount, Color.IndianRed)
                .AddItem("Errors GAEB-XML", gaebXmlErrorCount, Color.Red)
                );

            for (var i = 0; i < Math.Min(settings.ErrorsToPrint, errors.Count); i++)
            {
                AnsiConsole.Write(new Rule($"[red]{errors[i].File}[/]")
                    .Alignment(Justify.Left)
                    .DoubleBorder()
                    );

                if (errors[i].ErrorMessage != null)
                {
                    AnsiConsole.Write(new Markup($"[red]{Markup.Escape(errors[i].ErrorMessage)}[/]")
                        );
                    AnsiConsole.WriteLine();
                }

                var errorTable = new Table()
                    .Border(TableBorder.None)
                    .HideHeaders()
                    ;
                errorTable.AddColumn("Name", c => c
                    .RightAligned()
                    );
                errorTable.AddColumn("Value");
                if (errors[i].FrameworkPath != null)
                    errorTable.AddRow(
                        new Markup("[gray]File from .Net Framework[/]"),
                        new TextPath(errors[i].FrameworkPath)
                        );
                if (errors[i].CorePath != null)
                    errorTable.AddRow(
                        new Markup("[gray]File from .Net Core[/]"),
                        new TextPath(errors[i].CorePath)
                        );
                AnsiConsole.Write(errorTable);

                if (errors[i].Exception != null)
                    AnsiConsole.WriteException(errors[i].Exception);
            }
        }

        private static List<string> ReadFileList(string inputPath)
        {
            var result = new List<string>();
            result.AddRange(Directory.EnumerateFiles(inputPath)
                .Where(f => !f.EndsWith(".md"))
                );
            foreach (var subDirectory in Directory.EnumerateDirectories(inputPath))
                result.AddRange(ReadFileList(subDirectory));
            return result;
        }

        public static ErrorDetails? CheckGaeb90(string path, Options settings)
        {
            var result = new ErrorDetails()
            {
                File = path,
            };
            try
            {
                var fileName = Path.GetFileName(path);
                result.CorePath = Path.Combine(settings.OutputPath, "core", "gaeb90", fileName);
                result.FrameworkPath = Path.Combine(settings.OutputPath, "framework", "gaeb90", fileName);

                Convert(path, result.CorePath, _fileTypeGaeb90, _appCore, settings);
                Convert(path, result.FrameworkPath, _fileTypeGaeb90, _appFramework, settings);

                var diff = GetTextDiff(result.FrameworkPath, result.CorePath);
                if (diff != null)
                {
                    result.ErrorMessage = diff;
                    return result;
                }

                return null;
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                return result;
            }
        }

        public static ErrorDetails? CheckGaeb2000(string path, Options settings)
        {
            var result = new ErrorDetails()
            {
                File = path,
            };
            try
            {
                var fileName = Path.GetFileName(path);
                result.CorePath = Path.Combine(settings.OutputPath, "core", "gaeb2000", fileName);
                result.FrameworkPath = Path.Combine(settings.OutputPath, "framework", "gaeb2000", fileName);

                Convert(path, result.CorePath, _fileTypeGaeb2000, _appCore, settings);
                Convert(path, result.FrameworkPath, _fileTypeGaeb2000, _appFramework, settings);

                var diff = GetTextDiff(result.FrameworkPath, result.CorePath);
                if (diff != null)
                {
                    result.ErrorMessage = diff;
                    return result;
                }

                return null;
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                return result;
            }
        }

        public static ErrorDetails? CheckGaebXml(string path, Options settings)
        {
            var result = new ErrorDetails()
            {
                File = path,
            };
            try
            {
                var fileName = Path.GetFileName(path);
                result.CorePath = Path.Combine(settings.OutputPath, "core", "gaebXml", fileName);
                result.FrameworkPath = Path.Combine(settings.OutputPath, "framework", "gaebXml", fileName);

                Convert(path, result.CorePath, _fileTypeGaebXml, _appCore, settings);
                Convert(path, result.FrameworkPath, _fileTypeGaebXml, _appFramework, settings);

                var diff = GetXmlDiff(result.FrameworkPath, result.CorePath);
                if (diff != null)
                {
                    result.ErrorMessage = diff;
                    return result;
                }

                return null;
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                return result;
            }
        }

        private static void Convert(string inputPath, string outputPath, string format, string app, Options settings)
        {
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = app,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    Arguments = $"-i \"{inputPath}\" -o \"{outputPath}\" -f \"{format}\" -s \"{settings.SerialNumber}\" -c \"{settings.ConverterSerialNumber}\"",
                },
            };

            process.Start();

            var output = new StringBuilder();
            string? line;
            while ((line = process.StandardOutput.ReadLine()) != null)
                output.AppendLine(line);

            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                throw new Exception($"error converting with {app}: {output}");
            }
        }

        private static string? GetTextDiff(string leftFile, string rightFile)
        {
            var leftText = File.ReadAllText(leftFile);
            var rightText = File.ReadAllText(rightFile);

            var diff = InlineDiffBuilder.Diff(leftText, rightText);
            if (!diff.HasDifferences)
                return null;

            var result = new StringBuilder();
            var previousType = ChangeType.Unchanged;
            foreach (var line in diff.Lines)
            {
                if (previousType != line.Type)
                {
                    switch (line.Type)
                    {
                        case ChangeType.Inserted:
                            if (previousType == ChangeType.Deleted)
                                result.AppendLine($"===== (line: {line.Position})");
                            else
                                result.AppendLine($">>>>> (line: {line.Position})");
                            break;
                        case ChangeType.Deleted:
                            if (previousType == ChangeType.Inserted)
                                result.AppendLine($"===== (line: {line.Position})");
                            else
                                result.AppendLine($">>>>> (line: {line.Position})");
                            break;
                        default:
                            result.AppendLine($"<<<<< (line: {line.Position})");
                            break;
                    }
                }
                previousType = line.Type;

                switch (line.Type)
                {
                    case ChangeType.Inserted:
                        result.Append($"+ ");
                        break;
                    case ChangeType.Deleted:
                        result.Append($"- ");
                        break;
                    default:
                        result.Append($"  ");
                        break;
                }
                result.AppendLine(line.Text);
            }
            return result.ToString();
        }

        private static string? GetXmlDiff(string leftFile, string rightFile)
        {
            var differ = new XmlDiff()
            {
                IgnoreComments = true,
                IgnoreWhitespace = true,
            };

            using var sw = new StringWriter();
            var settings = new XmlWriterSettings() { OmitXmlDeclaration = true, Indent = true, };
            using var writer = XmlWriter.Create(sw, settings);

            if (differ.Compare(leftFile, rightFile, false, writer))
                return null;

            return sw.ToString();
        }
    }
}
