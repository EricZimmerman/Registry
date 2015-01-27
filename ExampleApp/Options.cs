using System.Text;
using CommandLine;

internal class Options
{
    [Option('f', "file", Required = false,
        HelpText = "Name of registry hive to process")]
    public string HiveName { get; set; }

    [Option('d', "directory", Required = false,
        HelpText = "Name of directory to lok for registry hives to process")]
    public string DirectoryName { get; set; }

    [Option('e', DefaultValue = false, Required = false,
        HelpText = "If true, export a file that can be compared to other Registry parsers")]
    public bool ExportHiveData { get; set; }

    [Option('p', DefaultValue = false, Required = false,
    HelpText = "If true, pause after processing a hive and wait for keypress to continue")]
    public bool PauseAfterEachFile { get; set; }

    public string GetUsage()
    {
        var usage = new StringBuilder();
        usage.AppendLine("Registry example app help");
        usage.AppendLine("-d <directory>: Process files found in <directory>");
        usage.AppendLine("-f <file>: Process <file>");
        usage.AppendLine("-p: Pause after processing each file");
        usage.AppendLine(
            "-e: If present, export a file that can be compared to other Registry parsers to same directory as hive is found in");

        usage.AppendLine("");
        usage.AppendLine("-d or -f must be specified, but not both");
        return usage.ToString();
    }
}