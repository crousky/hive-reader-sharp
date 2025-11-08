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
        var nodesToRemove = new[] { "script", "style", "nav", "header", "footer", "iframe", "object", "embed", "form", "button", "svg" };
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

        // Remove common ad and navigation patterns by class/id/data attributes
        var unwantedPatterns = new[]
        {
            "//*[contains(@class, 'ad-')]",
            "//*[contains(@class, 'advertisement')]",
            "//*[contains(@class, 'promo')]",
            "//*[contains(@class, 'subscribe')]",
            "//*[contains(@class, 'newsletter')]",
            "//*[contains(@class, 'social')]",
            "//*[contains(@class, 'share')]",
            "//*[contains(@class, 'comment')]",
            "//*[contains(@class, 'sidebar')]",
            "//*[contains(@class, 'widget')]",
            "//*[contains(@class, 'related')]",
            "//*[contains(@class, 'recommend')]",
            "//*[contains(@class, 'outbrain')]",
            "//*[contains(@class, 'taboola')]",
            "//*[contains(@id, 'sidebar')]",
            "//*[contains(@id, 'comment')]",
            "//*[starts-with(@data-testid, 'subscribe')]",
            "//*[starts-with(@data-testid, 'author-avatar')]",
            "//*[contains(@data-qa, 'ad')]",
            "//*[contains(@data-qa, 'subscribe')]",
            "//*[contains(@data-qa, 'newsletter')]",
            "//*[contains(@data-qa, 'comments')]",
            "//*[@role='separator']",
            "//wp-ad",
            "//wp-ad-wrapper"
        };

        foreach (var pattern in unwantedPatterns)
        {
            var nodes = doc.DocumentNode.SelectNodes(pattern);
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
            // Clean up the main content further
            CleanNode(mainContent);

            // Extract only paragraph text and headings for cleaner output
            var cleanedContent = ExtractArticleContent(mainContent);
            return cleanedContent;
        }

        return doc.DocumentNode.InnerHtml;
    }

    private void CleanNode(HtmlNode node)
    {
        // Remove all inline styles
        var nodesWithStyle = node.SelectNodes(".//*[@style]");
        if (nodesWithStyle != null)
        {
            foreach (var n in nodesWithStyle.ToList())
            {
                n.Attributes.Remove("style");
            }
        }

        // Remove all class attributes to prevent CSS issues
        var nodesWithClass = node.SelectNodes(".//*[@class]");
        if (nodesWithClass != null)
        {
            foreach (var n in nodesWithClass.ToList())
            {
                n.Attributes.Remove("class");
            }
        }

        // Remove all id attributes
        var nodesWithId = node.SelectNodes(".//*[@id]");
        if (nodesWithId != null)
        {
            foreach (var n in nodesWithId.ToList())
            {
                n.Attributes.Remove("id");
            }
        }

        // Remove data attributes
        var allNodes = node.SelectNodes(".//*");
        if (allNodes != null)
        {
            foreach (var n in allNodes)
            {
                var attrsToRemove = n.Attributes
                    .Where(a => a.Name.StartsWith("data-") || a.Name.StartsWith("aria-"))
                    .ToList();

                foreach (var attr in attrsToRemove)
                {
                    n.Attributes.Remove(attr);
                }
            }
        }
    }

    private string ExtractArticleContent(HtmlNode mainContent)
    {
        var result = new StringBuilder();

        // Extract only meaningful content: paragraphs, headings, lists, blockquotes
        var contentNodes = mainContent.SelectNodes(".//p | .//h1 | .//h2 | .//h3 | .//h4 | .//h5 | .//h6 | .//ul | .//ol | .//blockquote | .//img");

        if (contentNodes != null)
        {
            foreach (var node in contentNodes)
            {
                // Skip nodes that are inside unwanted containers
                if (IsInsideUnwantedContainer(node))
                    continue;

                // Skip empty paragraphs
                if (node.Name == "p" && string.IsNullOrWhiteSpace(node.InnerText))
                    continue;

                // For images, only keep if they have proper src
                if (node.Name == "img")
                {
                    var src = node.GetAttributeValue("src", "");
                    if (string.IsNullOrWhiteSpace(src) || src.Contains("avatar") || src.Contains("icon"))
                        continue;
                }

                result.AppendLine(node.OuterHtml);
            }
        }

        return result.ToString();
    }

    private bool IsInsideUnwantedContainer(HtmlNode node)
    {
        var current = node.ParentNode;
        while (current != null)
        {
            var className = current.GetAttributeValue("class", "");
            var dataQa = current.GetAttributeValue("data-qa", "");
            var dataTestId = current.GetAttributeValue("data-testid", "");

            if (className.Contains("byline") ||
                className.Contains("author-bio") ||
                className.Contains("subscribe") ||
                className.Contains("newsletter") ||
                className.Contains("promo") ||
                className.Contains("ad") ||
                dataQa.Contains("author") ||
                dataQa.Contains("subscribe") ||
                dataTestId.Contains("author") ||
                dataTestId.Contains("subscribe"))
            {
                return true;
            }

            current = current.ParentNode;
        }
        return false;
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
