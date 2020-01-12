using Markdig;
using RazorLight;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Mies
{
    public class SiteGenerator
    {
        private MiesConfig MiesConfig;
        private SiteConfig SiteConfig;
        private ThemeConfig ThemeConfig;

        private MarkdownPipeline Pipeline;
        private RazorLightEngine Engine;

        public SiteGenerator (MiesConfig config) {
            Log.Information("Initializing site: " + config.SiteDirectory);
            Log.Information("Loading site file: " + config.SiteConfig);

            MiesConfig = config;
            SiteConfig = LoadSiteConfig(config);
            ThemeConfig = LoadThemeConfig(FindThemeConfig(SiteConfig));

            var templatesDir = GetThemeDirectory(ThemeConfig.TemplatesDir);
            Pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .UseYamlFrontMatter().Build();

            Engine = new RazorLightEngineBuilder()
                .UseFileSystemProject(templatesDir.FullName)
                .UseMemoryCachingProvider().Build();
        }

        private FileInfo FindThemeConfig (SiteConfig siteConfig) {
            var theme = siteConfig.ThemeFile;
            var path = Path.Combine(MiesConfig.SiteDirectory.FullName, theme);
            return new FileInfo(path);
        }

        /// <summary>
        /// Entry point for the CLI
        /// </summary>
        public async Task<int> Execute (int max) {
            await ProcessPages(max);
            return 0;
        }

        private DirectoryInfo GetSiteDirectory (string name) =>
            new DirectoryInfo(Path.Combine(SiteConfig.ConfigFile.DirectoryName, name));

        private DirectoryInfo GetThemeDirectory (string name) =>
            new DirectoryInfo(Path.Combine(ThemeConfig.ConfigFile.DirectoryName, name));


        private void CheckExistence (DirectoryInfo info, string debugname) {
            if (info.Exists) { return; }
            throw new DirectoryNotFoundException($"{debugname} not found: {info.FullName}");
        }

        private void CheckExistence (FileInfo info, string debugname) {
            if (info.Exists) { return; }
            throw new FileNotFoundException($"{debugname} not found: {info.FullName}");
        }



        private SiteConfig LoadSiteConfig (MiesConfig config) {
            CheckExistence(config.SiteDirectory, "Site directory");
            CheckExistence(config.SiteConfig, "Site config file");

            var contents = File.ReadAllText(config.SiteConfig.FullName);
            var result = YamlUtils.ParseYaml<SiteConfig>(config.SiteConfig, contents);
            result.ConfigFile = config.SiteConfig;
            return result;
        }

        private ThemeConfig LoadThemeConfig (FileInfo themeYaml) {
            CheckExistence(themeYaml, "Theme config file");

            var contents = File.ReadAllText(themeYaml.FullName);
            var result = YamlUtils.ParseYaml<ThemeConfig>(themeYaml, contents);
            result.ConfigFile = themeYaml;
            return result;
        }


        //
        // page loading and generation

        public async Task<AllPages> ProcessPages (int max) {
            var rawfiles = GetThemeDirectory(ThemeConfig.RawFilesDir);
            var inputs = GetSiteDirectory(SiteConfig.PagesDir);
            var outputs = GetSiteDirectory(SiteConfig.OutputsDir);

            Log.Debug($"  Theme: {ThemeConfig.ConfigFile.FullName}");
            Log.Debug($"  Input directory: {inputs.FullName}");
            Log.Debug($"  Output directory: {outputs.FullName}");

            var pages = inputs.EnumerateFiles("*.md", SearchOption.AllDirectories)
                .Take(max)
                .Select(p => PrepareEmptyPageResult(p, outputs))
                .ToList();

            var all = new AllPages() { Pages = pages };

            Log.Information($"Loading {all.Pages.Count} markdown pages...");
            foreach (var page in all.Pages) { LoadPage(page, all); }
            MoveIndexPagesToEnd(all.Pages);

            Log.Information("Converting pages to HTML...");
            foreach (var page in all.Pages) { await RenderPage(page); }

            Log.Information("Preparing the output directory...");
            ClearOutOutputDirectory(outputs);
            CopyRawFiles(rawfiles, outputs);

            Log.Information("Writing HTML pages to disk...");
            foreach (var page in all.Pages) { WritePage(page); }

            Log.Information($"Done. Processed {all.Pages.Count} pages.");
            return all;
        }

        /// <summary>
        /// Each empty result contains info about the page's filesystem paths, and will be filled in later
        /// </summary>
        private PageResult PrepareEmptyPageResult (FileInfo inpath, DirectoryInfo outputs) {
            var outfile = Path.ChangeExtension(inpath.Name, ".html");
            var outpath = new FileInfo(Path.Combine(outputs.FullName, outfile));
            return new PageResult { InPath = inpath, OutPath = outpath };
        }

        /// <summary>
        /// After reading all pages, but before processing them, let's find all index pages and move them
        /// to the end, so that they will be processed last (so they can access the entire list of
        /// pages processed before them, and do with them whatever they want).
        /// </summary>
        private void MoveIndexPagesToEnd (List<PageResult> results) {
            var indices = results.Where(p => p.Model.IsIndex).ToList();
            var others = results.Where(p => !p.Model.IsIndex).ToList();

            results.Clear();
            results.AddRange(others);
            results.AddRange(indices);
        }

        /// <summary>
        /// Destroy and recreate the output directory. 
        /// </summary>
        private void ClearOutOutputDirectory (DirectoryInfo outdir) {
            // just use the string path, we don't want to hold on to dirinfo that's being deleted
            string path = outdir.FullName;
            
            if (Directory.Exists(path)) {
                Log.Information($"Deleting output directory {path}");
                Directory.Delete(path, true);
            }

            Log.Information($"Creating output directory {path}");
            Directory.CreateDirectory(path);
        }

        /// <summary>
        /// Copy all the raw web files from the source directory into output
        /// </summary>
        private void CopyRawFiles (DirectoryInfo raw, DirectoryInfo output) {
            var count = CopyRawFilesHelper(raw, output);
            Log.Debug($"  Copied {count} files from raw files directory {raw.FullName}");
        }

        private int CopyRawFilesHelper (DirectoryInfo sourceDir, DirectoryInfo targetDir) {
            int count = 0;

            foreach (FileInfo sourceFile in sourceDir.GetFiles()) {
                var targetFileName = Path.Combine(targetDir.FullName, sourceFile.Name);
                File.Copy(sourceFile.FullName, targetFileName);
                count++;
            }

            foreach (DirectoryInfo sourceSubDir in sourceDir.GetDirectories()) {
                var targetSubDir = targetDir.CreateSubdirectory(sourceSubDir.Name);
                count += CopyRawFilesHelper(sourceSubDir, targetSubDir);
            }

            return count;
        }

        /// <summary>
        /// Blocking load from the input directory, followed by conversion of page contents
        /// from markdown to HTML. 
        /// </summary>
        /// <param name="page"></param>
        /// <param name="stats"></param>
        private void LoadPage (PageResult page, AllPages stats) {
            FileInfo inpath = page.InPath;
            Log.Debug($"  Loading page {inpath.Name}");

            var markdown = File.ReadAllText(inpath.FullName);
            var contents = Markdown.ToHtml(markdown, Pipeline);

            page.Model = YamlUtils.ExtractYamlHeader<PageModel>(inpath, markdown);
            page.Model.Markdown = markdown;
            page.Model.Contents = contents;
            page.Model.PageLink = page.OutPath.Name;
            page.Model.Site = SiteConfig;
            page.Model.All = stats;
        }

        /// <summary>
        /// Renders the HTML page using a .cshtml template file and the contents from
        /// the already-transformed markdown file
        /// </summary>
        private async Task RenderPage (PageResult page) {
            Log.Debug($"  Rendering page {page.InPath.Name} => {page.OutPath.Name}");

            try {
                page.HtmlOutput = await Engine.CompileRenderAsync(page.Model.Template, page.Model);
            } catch (Exception e) {
                throw new InvalidOperationException($"Error while rendering HTML for page {page.InPath.Name}", e);
            }
        }

        /// <summary>
        /// Blocking write to the output directory
        /// </summary>
        private void WritePage (PageResult page) {
            Log.Debug($"  Writing page {page.OutPath.Name}");
            File.WriteAllText(page.OutPath.FullName, page.HtmlOutput);
        }

    }
}
