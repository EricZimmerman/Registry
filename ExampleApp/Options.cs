using System.Collections.Generic;
using System.Text;
using CommandLine;

class Options {
  [Option('f', "file", Required = false,
    HelpText = "Name of registry hive to process",SetName= "file")]
  public string HiveName { get; set; }

  [Option('d', "directory", Required = false,
  HelpText = "Name of directory to lok for registry hives to process", SetName = "dir")]
  public string DirectoryName { get; set; }

  
  public string GetUsage()
  {
      // this without using CommandLine.Text
      //  or using HelpText.AutoBuild
      var usage = new StringBuilder();
      usage.AppendLine("Registry example app help");
      usage.AppendLine("-d <directory>: Process files found in <directory>");
      usage.AppendLine("-f <file>: Process <file>");
      usage.AppendLine("");
      usage.AppendLine("One or the other must be specified, but not both");
      return usage.ToString();
  }
}

