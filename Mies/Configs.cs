using System;
using System.Collections.Generic;
using System.IO;

namespace Mies
{
    /// <summary>
    /// App configuration parameters passed from command line
    /// </summary>
    public class MiesConfig
    {
        public FileInfo SiteConfig { get; set; }
        public DirectoryInfo SiteDirectory { get; set; }
    }

    /// <summary>
    /// Site configuration data read from site.yaml.
    /// All directories are assumed to be relative to the location of site.yaml
    /// unless otherwise specified.
    /// </summary>
    public class SiteConfig
    {
        public string PagesDir;     // subdirectory containing .md markdown files with page content
        public string TemplatesDir; // subdirectory containing .cshtml template files
        public string RawFilesDir;  // contents of this subdirectory will be copied verbatim (e.g. for images, .css files)
        public string OutputsDir;   // working directory where the output will be placed

        public string Generator = "MIES";
        public string Title, Author, Description;   // filled in by the user, and used to fill in website metadata
        public int RecentPosts;                     // controls how many recent posts show up on the main page
    }

    /// <summary>
    /// Per-page metadata. Contains a combination of per-page metadata, as well as
    /// references to broad generator state (e.g. site config, pages produced so far)
    /// so that page templates can make use of it if desired.
    /// </summary>
    public class PageModel
    {
        // note: the following get read from a yaml header at the top of each markdown page
        public string PageTitle;  // title of the page, shown in the browser title bar, metadata, and in inbound links
        public string PageDesc;   // short one liner description, used in table of contents as well as metadata
        public DateTime? Date;    // timestamp for this page; may be null in which case it will not be displayed
        public bool IsBlogPost;   // if true, this page will be added chronologically on the index page and in the table of contents
        public bool IsIndex;      // if true, this page will be generated last, so that it can index the other ones
        public string Template;   // which .cshtml file to use for this page

        // note: the following get filled in at runtime from the markdown file.
        // they are generated after
        public string Markdown;   // the raw markdown text loaded for this page
        public string Contents;   // the results of converting markdown to HTML, but before pushing through the page template
        public string PageLink;   // name of the target HTML file, e.g. "foo.html" if the source was "foo.md"

        // note: the following gets filled in for the entire site
        public SiteConfig Site;   // global site configuration
        public AllPages All;      // all the pages produced so far
    }

    /// <summary>
    /// Wrapper around a single page, including its filesystem location, the page model,
    /// and the final HTML text of the page.
    /// </summary>
    public class PageResult
    {
        public FileInfo InPath;   // source path of the markdown file
        public FileInfo OutPath;  // target path of the HTML file in the output directory 
        public PageModel Model;   // model generated for this page
        public string HtmlOutput; // final text of the HTML page, after markdown conversion and templating
    }

    /// <summary>
    /// Generator state, which includes all pages processed by this generator.
    /// Depending on the stage of execution, this could contain just raw models,
    /// or models that have been converted from markdown to html, or full results
    /// including final HTML text for each page.
    /// </summary>
    public class AllPages
    {
        public List<PageResult> Pages = new List<PageResult>();
    }
}
