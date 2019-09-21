using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;

namespace AsTaskBench
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
           var summary = BenchmarkRunner.Run<AsTaskBench>();
           MarkdownExporter.Console.ExportToLog(summary, ConsoleLogger.Default);
        }
    }
}