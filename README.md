# Mies

### Less is more! A minimalist static blog generator. 

Mies ingests markdown files, and generates a static blog. 

Built on .NET Core 3. No extra databases or web frameworks required.



# Details

Mies is a static blog generator. The user writes a blog as a collection of markdown files, plus a set of shared templates that control their appearance. From this input, Mies generates a complete set of HTML pages plus a dynamic front page with most recent posts.

Some benefits of this tool in particular:

### Blog posts are written in enhanced markdown

Markdown is user-friendly, and provides a good separation between text and its visual representation. 

Additionally, our flavor of markdown is enhanced by a variety of extensions, for tables, links, lists, and more: see [the markdig page for the full list](https://github.com/lunet-io/markdig).

### Powerful templating

Visual appearance comes from a small number of user-customizable templates. 

Each template is actually a Razor page (Razor is a .NET templating engine), so it can contain arbitrary C# code. This makes templates very powerful: one can do extensive customization of behavior just by scripting templates, without touching generator source code.

### Less is more

Mies doesn't have a lot of the "semantic decoration" that is commonly associated with blogs. This is intentional. A lot of irrelevant information actually doesn't help the reader; they're just decorative elements, distracting, and ultimately useless.

Some examples of deliberately omitted semantic decorations:
  - Tags. There are no tags on posts, and no tag wordclouds or directories. Tags are hugely distracting visually, while at the same time useless to the reader, except in the cases of _very_ large sites.
  - Calendar sidebar. Posts can be timestamped if desired, but there is no sidebar listing of posts by year and month. Those calendar sidebars are only good for high-traffic sites that have a lot of content, otherwise they just add visual noise, and brings to attention the gaps in the blog author's writing cadence.
  - Social buttons or other social media integration. Because, really, when is the last time anyone _intentionally_ clicked on one of those "retweet this" buttons? These are, again, distracting visually from the content of the page.

By contrast, this blog generator takes a minimalist approach. **Your words should be the blog's focus,** and everything else should be in the service of this. 

### Simple implementation

Finally, this is not an all-purpose tool: it doesn't have a lot of bells and whistles, it only makes simple blogs, and it makes them quickly.

But a static blog makes for very fast load times, and minimal administrative hassle, compared to a dynamic blog engine like WordPress - I'm not interested in administering web servers and sql databases anymore :)

Additionally, on a more personal note: it's built in C# on .NET, which I use every day anyway; not having to install big toolchains like Ruby just to update a simple blog is very nice




# How To Use It

This section needs fleshing out, but basically:
  1. Download and build this project. I use Visual Studio 2019, and the project requires .NET Core 3.1 
  2. Once Mies.exe is built, run it against the included SampleSite sample site directory:

		```
		PS C:\Users\Rob\Documents\mies> .\Mies\bin\Debug\netcoreapp3.1\Mies.exe .\SampleSite\
		[19:34:46 INF] Initializing site: .\SampleSite\
		[19:34:46 INF] Loading site file: C:\Users\Rob\Documents\mies\SampleSite\site.yaml
		[19:34:46 INF] Loading 6 markdown pages...
		[19:34:46 INF] Converting pages to HTML...
		[19:34:49 INF] Preparing the output directory...
		[19:34:49 INF] Deleting output directory C:\Users\Rob\Documents\mies\SampleSite\output
		[19:34:49 INF] Creating output directory C:\Users\Rob\Documents\mies\SampleSite\output
		[19:34:49 INF] Writing HTML pages to disk...
		[19:34:49 INF] Done. Processed 6 pages.
		[19:34:49 INF] Generated website in 3.8660648 seconds.
		```
  3. Check out the resulting SampleSite/output directory to see what it generates.

A site definition has the following structure:

```
  site directory
  |
  +-- html-templates 
  | +-- header.cshtml, footer.cshtml, page.cshtml, etc. ...
  |
  +-- input-pages
  | +-- about.md, index.md, post-1.md, post-2.md, etc. ...
  |
  +-- input-raw-files
  | +-- site.css, other static files ...
  |
  +-- output
  | +-- (empty directory, this is where the blog will be generated)
  |
  +-- site.yaml: defines global blog info, such as title, author, etc.
```

Markdown page content lives in `input-pages`. Each markdown page starts with a yaml header that specifies common metadata, like page title and publishing date, as well as which template file to use for that page. 

When the pages get processed, each markdown file gets converted to HTML, and then its template gets loaded from `html-templates` to generate the actual final page. Each template can then have full control over where to insert the page contents, whether to include header or footer, and so on.

Additionally, any static files (like images, javascript files, fonts, etc) can be placed inside `input-raw-files`. They will be copied verbatim into the output directory.

It's probably best to just run the generator on the included sample site, and check it out for yourself! :)





## License and Credits

Mies is licensed under AGPL 3.0. Copyright 2020 Robert Zubek.

The project links against the following open source libraries:
  - .NET Core: https://github.com/dotnet/core
  - RazorLight: https://github.com/toddams/RazorLight
  - Markdig: https://github.com/lunet-io/markdig
  - Newtonsoft.JSON: https://github.com/JamesNK/Newtonsoft.Json
  - CommandLineUtils: https://github.com/natemcmaster/CommandLineUtils
  - SharpYaml: https://github.com/xoofx/SharpYaml
  - Serilog: https://github.com/serilog/serilog

These are not included in this source repo, but they and their dependencies are of course compiled into any binary builds. These libraries are copyright their respective authors and contributors, please see their repos for details.


