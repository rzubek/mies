using McMaster.Extensions.CommandLineUtils;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Mies
{
    [Command(Name = "Mies", Description = "Less is more. A minimalist static blog generator.")]
    [HelpOption()]
    public class CLI
    {
        [Argument(0, Name = "dir", Description = "Site directory that contains site.yaml file and other contents")]
        public string SiteDir { get; set; } = null;

        [Option("-sitefile", Description = "Site config file name, defaults to site.yaml")]
        public string InputFile { get; set; } = "site.yaml";

        [Option("-v", Description = "Enables verbose logging")]
        public bool Verbose { get; set; } = false;

        [Option("-max", Description = "If set, the max number of pages to be processed")]
        public int MaxPages { get; set; } = int.MaxValue;

        private static Task<int> Main (string[] args) => CommandLineApplication.ExecuteAsync<CLI>(args);

        public async Task<int> OnExecuteAsync (CommandLineApplication app, CancellationToken _) {
            ConfigureLogger();

            if (string.IsNullOrEmpty(SiteDir)) {
                app.ShowHelp();
                return 0;
            }

            try {
                var timer = Stopwatch.StartNew();
                Log.Debug("Starting up at " + DateTime.Now.ToLongTimeString());

                var config = MakeGeneratorConfig();
                var gen = new SiteGenerator(config);
                int status = await gen.Execute(MaxPages);

                Log.Information($"Generated website in {timer.Elapsed.TotalSeconds} seconds.");
                return status;

            } catch (Exception ex) {
                if (Verbose) {
                    Log.Error("Failed to process site directory " + SiteDir);
                }
                Log.Error(ex.Message);
                Log.Error(ex.InnerException?.Message ?? "");
                Log.Debug(ex.StackTrace);
                Log.Debug(ex.InnerException?.StackTrace);
                return 1;
            }
        }

        private void ConfigureLogger () {
            var config = new LoggerConfiguration();
            config = Verbose ? config.MinimumLevel.Debug() : config.MinimumLevel.Information();
            config = config.WriteTo.Console();
            Log.Logger = config.CreateLogger();
        }

        private MiesConfig MakeGeneratorConfig () {
            var dir = new DirectoryInfo(SiteDir);
            Log.Debug($"  Input directory: {dir.FullName}, exists = {dir.Exists}");

            return new MiesConfig {
                SiteDirectory = dir,
                SiteConfig = new FileInfo(Path.Combine(dir.FullName, InputFile)),
            };
        }
    }
}
