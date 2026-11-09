using System.Text.Json;
using System.Text.RegularExpressions;
using Shouldly;

namespace HaveTickets.Api.Tests;

/// <summary>
/// Discoverability baseline of the public demo (REQ-TKT-010 to REQ-TKT-015). The site is the
/// Blazor WebAssembly shell published to GitHub Pages, so the checks read the static files in
/// HaveTickets.WebUI/wwwroot that the browser receives. The API does not serve them, so there
/// is no HTTP-level check here.
/// </summary>
public sealed class SeoBaselineTests
{
    private const string HostRoot = "https://tickets.naindev.com/";
    private const string AuthorId = "https://www.naindev.com/#person";

    private static readonly string WebRoot = Path.Combine(FindRepositoryRoot(), "HaveTickets.WebUI", "wwwroot");
    private static readonly string Html = File.ReadAllText(Path.Combine(WebRoot, "index.html"));

    [Fact]
    public void Canonical_is_the_host_root() =>
        Matches(@"<link\s+rel=""canonical""\s+href=""([^""]*)""").ShouldBe([HostRoot]);

    [Fact]
    public void Description_is_single_and_within_snippet_length()
    {
        var descriptions = MetaContent("name", "description");

        descriptions.Count.ShouldBe(1);
        descriptions[0].Length.ShouldBeInRange(70, 160);
    }

    [Fact]
    public void Open_graph_matches_the_canonical()
    {
        MetaContent("property", "og:type").ShouldBe(["website"]);
        MetaContent("property", "og:site_name").ShouldBe(["HaveTickets"]);
        MetaContent("property", "og:url").ShouldBe([HostRoot]);
        MetaContent("property", "og:title").Count.ShouldBe(1);
        MetaContent("property", "og:description").ShouldBe(MetaContent("name", "description"));

        var image = MetaContent("property", "og:image");
        image.Count.ShouldBe(1);
        image[0].ShouldStartWith("https://");
    }

    [Fact]
    public void Twitter_card_is_complete()
    {
        MetaContent("name", "twitter:card").ShouldBe(["summary_large_image"]);
        MetaContent("name", "twitter:title").Count.ShouldBe(1);
        MetaContent("name", "twitter:description").Count.ShouldBe(1);
        MetaContent("name", "twitter:image").Count.ShouldBe(1);
    }

    [Fact]
    public void Author_link_points_to_the_main_site() =>
        Matches(@"<link\s+rel=""author""\s+href=""([^""]*)""").ShouldBe(["https://www.naindev.com/"]);

    [Fact]
    public void Json_ld_parses_and_names_the_author()
    {
        var blocks = Matches(@"<script\s+type=""application/ld\+json"">(.*?)</script>", RegexOptions.Singleline);

        blocks.Count.ShouldBe(1);
        blocks[0].ShouldNotContain("<");

        using var json = JsonDocument.Parse(blocks[0]);
        var root = json.RootElement;
        root.GetProperty("@type").GetString().ShouldBe("SoftwareApplication");
        root.GetProperty("url").GetString().ShouldBe(HostRoot);
        root.GetProperty("isAccessibleForFree").GetBoolean().ShouldBeTrue();
        root.GetProperty("author").GetProperty("@id").GetString().ShouldBe(AuthorId);
        root.GetProperty("sameAs").GetString().ShouldBe("https://github.com/Nain9Dev/HaveTickets");
    }

    [Fact]
    public void Robots_advertises_the_sitemap_and_blocks_nothing()
    {
        var robots = File.ReadAllText(Path.Combine(WebRoot, "robots.txt"));

        robots.ShouldMatch(@"(?m)^User-agent: \*\r?$");
        robots.ShouldMatch(@"(?m)^Allow: /\r?$");
        Regex.IsMatch(robots, @"(?m)^Disallow: *\S").ShouldBeFalse();
        robots.ShouldContain($"Sitemap: {HostRoot}sitemap.xml");
    }

    [Fact]
    public void Sitemap_lists_the_canonical_root()
    {
        var sitemap = File.ReadAllText(Path.Combine(WebRoot, "sitemap.xml"));

        Regex.Matches(sitemap, "<loc>([^<]+)</loc>").Select(m => m.Groups[1].Value).ToList().ShouldBe([HostRoot]);
    }

    [Fact]
    public void Llms_txt_starts_with_a_heading()
    {
        var lines = File.ReadAllLines(Path.Combine(WebRoot, "llms.txt"));

        lines[0].ShouldBe("# HaveTickets");
        lines.ShouldContain(l => l.StartsWith("> ", StringComparison.Ordinal));
        lines.ShouldContain(l => l.Contains("https://www.naindev.com/", StringComparison.Ordinal));
    }

    private static List<string> Matches(string pattern, RegexOptions options = RegexOptions.None) =>
        Regex.Matches(Html, pattern, options).Select(m => m.Groups[1].Value).ToList();

    private static List<string> MetaContent(string attribute, string value) =>
        Matches($@"<meta\s+{attribute}=""{Regex.Escape(value)}""\s+content=""([^""]*)""");

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "HaveTickets.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new DirectoryNotFoundException("HaveTickets.slnx not found above the test output.");
    }
}
