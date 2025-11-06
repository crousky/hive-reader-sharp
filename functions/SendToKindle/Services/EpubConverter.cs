using HtmlAgilityPack;
using System.Text;
using System.IO.Compression;

namespace SendToKindle.Services;

public class EpubConverter : IEpubConverter
{
    public async Task<byte[]> ConvertHtmlToEpub(string html, string title, string author, string sourceUrl)
    {
        // Clean the HTML content
        var cleanedHtml = CleanHtml(html);

        // Create EPUB in memory
        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            // Add mimetype file (must be first and uncompressed)
            var mimetypeEntry = archive.CreateEntry("mimetype", CompressionLevel.NoCompression);
            using (var writer = new StreamWriter(mimetypeEntry.Open()))
            {
                await writer.WriteAsync("application/epub+zip");
            }

            // Add META-INF/container.xml
            CreateMetaInf(archive);

            // Add content.opf
            CreateContentOpf(archive, title, author);

            // Add toc.ncx
            CreateTocNcx(archive, title, author);

            // Add XHTML content
            CreateXhtmlContent(archive, cleanedHtml, title);

            // Add stylesheet
            CreateStylesheet(archive);
        }

        return memoryStream.ToArray();
    }

    private string CleanHtml(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Remove scripts, styles, and other unwanted elements
        var nodesToRemove = new[] { "script", "style", "nav", "header", "footer", "iframe", "object", "embed" };
        foreach (var tag in nodesToRemove)
        {
            var nodes = doc.DocumentNode.SelectNodes($"//{tag}");
            if (nodes != null)
            {
                foreach (var node in nodes.ToList())
                {
                    node.Remove();
                }
            }
        }

        // Try to find the main content
        var mainContent = doc.DocumentNode.SelectSingleNode("//article") ??
                         doc.DocumentNode.SelectSingleNode("//main") ??
                         doc.DocumentNode.SelectSingleNode("//div[@class='content']") ??
                         doc.DocumentNode.SelectSingleNode("//div[@id='content']") ??
                         doc.DocumentNode.SelectSingleNode("//body");

        if (mainContent != null)
        {
            return mainContent.InnerHtml;
        }

        return doc.DocumentNode.InnerHtml;
    }

    private void CreateMetaInf(ZipArchive archive)
    {
        var containerXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<container version=""1.0"" xmlns=""urn:oasis:names:tc:opendocument:xmlns:container"">
    <rootfiles>
        <rootfile full-path=""OEBPS/content.opf"" media-type=""application/oebps-package+xml""/>
    </rootfiles>
</container>";

        var entry = archive.CreateEntry("META-INF/container.xml");
        using var writer = new StreamWriter(entry.Open());
        writer.Write(containerXml);
    }

    private void CreateContentOpf(ZipArchive archive, string title, string author)
    {
        var uniqueId = Guid.NewGuid().ToString();
        var currentDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        var contentOpf = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<package xmlns=""http://www.idpf.org/2007/opf"" unique-identifier=""BookId"" version=""3.0"">
    <metadata xmlns:dc=""http://purl.org/dc/elements/1.1/"" xmlns:opf=""http://www.idpf.org/2007/opf"">
        <dc:title>{EscapeXml(title)}</dc:title>
        <dc:creator>{EscapeXml(author)}</dc:creator>
        <dc:language>en</dc:language>
        <dc:identifier id=""BookId"">{uniqueId}</dc:identifier>
        <meta property=""dcterms:modified"">{currentDate}</meta>
    </metadata>
    <manifest>
        <item id=""content"" href=""content.xhtml"" media-type=""application/xhtml+xml""/>
        <item id=""ncx"" href=""toc.ncx"" media-type=""application/x-dtbncx+xml""/>
        <item id=""style"" href=""style.css"" media-type=""text/css""/>
    </manifest>
    <spine toc=""ncx"">
        <itemref idref=""content""/>
    </spine>
</package>";

        var entry = archive.CreateEntry("OEBPS/content.opf");
        using var writer = new StreamWriter(entry.Open());
        writer.Write(contentOpf);
    }

    private void CreateTocNcx(ZipArchive archive, string title, string author)
    {
        var uniqueId = Guid.NewGuid().ToString();

        var tocNcx = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<ncx xmlns=""http://www.daisy.org/z3986/2005/ncx/"" version=""2005-1"">
    <head>
        <meta name=""dtb:uid"" content=""{uniqueId}""/>
        <meta name=""dtb:depth"" content=""1""/>
        <meta name=""dtb:totalPageCount"" content=""0""/>
        <meta name=""dtb:maxPageNumber"" content=""0""/>
    </head>
    <docTitle>
        <text>{EscapeXml(title)}</text>
    </docTitle>
    <docAuthor>
        <text>{EscapeXml(author)}</text>
    </docAuthor>
    <navMap>
        <navPoint id=""content"" playOrder=""1"">
            <navLabel>
                <text>{EscapeXml(title)}</text>
            </navLabel>
            <content src=""content.xhtml""/>
        </navPoint>
    </navMap>
</ncx>";

        var entry = archive.CreateEntry("OEBPS/toc.ncx");
        using var writer = new StreamWriter(entry.Open());
        writer.Write(tocNcx);
    }

    private void CreateXhtmlContent(ZipArchive archive, string htmlContent, string title)
    {
        var xhtml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE html>
<html xmlns=""http://www.w3.org/1999/xhtml"" xmlns:epub=""http://www.idpf.org/2007/ops"">
<head>
    <title>{EscapeXml(title)}</title>
    <link rel=""stylesheet"" type=""text/css"" href=""style.css""/>
</head>
<body>
    <h1>{EscapeXml(title)}</h1>
    {htmlContent}
</body>
</html>";

        var entry = archive.CreateEntry("OEBPS/content.xhtml");
        using var writer = new StreamWriter(entry.Open());
        writer.Write(xhtml);
    }

    private void CreateStylesheet(ZipArchive archive)
    {
        var css = @"
body {
    font-family: Georgia, serif;
    line-height: 1.6;
    margin: 1em;
    color: #333;
}

h1, h2, h3, h4, h5, h6 {
    font-family: Arial, sans-serif;
    margin-top: 1em;
    margin-bottom: 0.5em;
    color: #000;
}

h1 {
    font-size: 2em;
    border-bottom: 2px solid #333;
    padding-bottom: 0.3em;
}

h2 {
    font-size: 1.5em;
}

h3 {
    font-size: 1.3em;
}

p {
    margin: 1em 0;
    text-align: justify;
}

img {
    max-width: 100%;
    height: auto;
    display: block;
    margin: 1em auto;
}

a {
    color: #0066cc;
    text-decoration: none;
}

blockquote {
    margin: 1em 2em;
    padding: 0.5em 1em;
    border-left: 4px solid #ccc;
    font-style: italic;
}

code {
    font-family: 'Courier New', monospace;
    background-color: #f4f4f4;
    padding: 0.2em 0.4em;
    border-radius: 3px;
}

pre {
    background-color: #f4f4f4;
    padding: 1em;
    overflow-x: auto;
    border-radius: 5px;
}

ul, ol {
    margin: 1em 0;
    padding-left: 2em;
}

li {
    margin: 0.5em 0;
}
";

        var entry = archive.CreateEntry("OEBPS/style.css");
        using var writer = new StreamWriter(entry.Open());
        writer.Write(css);
    }

    private string EscapeXml(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }
}
