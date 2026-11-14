using System.Buffers.Binary;
using System.Text.Json;
using System.Text.RegularExpressions;
using Shouldly;

namespace HaveTickets.Api.Tests;

/// <summary>
/// Install contract of the public demo (REQ-TKT-016 to REQ-TKT-018): the web app manifest, the
/// install icons and the head tags that let Android Chrome and iOS Safari install the site. Like
/// the discoverability baseline, the checks read the static files in HaveTickets.WebUI/wwwroot
/// that GitHub Pages serves. Icon sizes come from the PNG header, so no image library is needed.
/// </summary>
public sealed class PwaInstallTests
{
    private const string AppName = "HaveTickets";
    private const string PageBackground = "#0a0a0f";

    private static readonly string RepositoryRoot = FindRepositoryRoot();
    private static readonly string WebRoot = Path.Combine(RepositoryRoot, "HaveTickets.WebUI", "wwwroot");
    private static readonly string Html = File.ReadAllText(Path.Combine(WebRoot, "index.html"));

    private static ReadOnlySpan<byte> PngSignature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    [Fact]
    public void Manifest_names_the_app_and_opens_it_standalone_from_the_root()
    {
        using var manifest = ReadManifest();
        var root = manifest.RootElement;

        root.GetProperty("name").GetString().ShouldBe(AppName);
        root.GetProperty("short_name").GetString().ShouldBe(AppName);

        root.GetProperty("id").GetString().ShouldBe("./");
        root.GetProperty("start_url").GetString().ShouldBe("./");
        root.GetProperty("scope").GetString().ShouldBe("./");
        root.GetProperty("display").GetString().ShouldBe("standalone");

        Matches(@"<html\s+lang=""([^""]*)""").ShouldBe(["en"]);
        root.GetProperty("lang").GetString().ShouldBe("en");
        root.GetProperty("dir").GetString().ShouldBe("ltr");
        root.GetProperty("description").GetString().ShouldBe(MetaContent("description").Single());

        root.GetProperty("theme_color").GetString().ShouldBe(PageBackground);
        root.GetProperty("background_color").GetString().ShouldBe(PageBackground);
        root.TryGetProperty("prefer_related_applications", out _)
            .ShouldBeFalse("a related native app would cancel the browser install");
    }

    [Fact]
    public void Manifest_icons_exist_at_the_declared_size()
    {
        // One purpose per entry: Chrome asks for the 192 and 512 px icons to offer the install, and
        // launchers crop the maskable one to their own shape, so it is a separate file.
        (string Src, string Sizes, string Purpose)[] expected =
        [
            ("./icons/icon-192.png", "192x192", "any"),
            ("./icons/icon-512.png", "512x512", "any"),
            ("./icons/icon-maskable-512.png", "512x512", "maskable"),
        ];

        using var manifest = ReadManifest();

        manifest.RootElement.GetProperty("icons").EnumerateArray()
            .Select(icon => $"{Text(icon, "src")} {Text(icon, "sizes")} {Text(icon, "type")} {Text(icon, "purpose")}")
            .ShouldBe(expected.Select(icon => $"{icon.Src} {icon.Sizes} image/png {icon.Purpose}"));

        foreach (var icon in expected)
        {
            PngSize(icon.Src).ShouldBe(icon.Sizes, $"{icon.Src} must measure what the manifest declares");
        }
    }

    [Fact]
    public void Entry_page_links_the_manifest_and_carries_the_ios_tags()
    {
        Matches(@"<link\s+rel=""manifest""\s+href=""([^""]*)""").ShouldBe(["manifest.webmanifest"]);
        Matches(@"<link\s+rel=""apple-touch-icon""\s+href=""([^""]*)""").ShouldBe(["icons/apple-touch-icon.png"]);
        PngSize("icons/apple-touch-icon.png").ShouldBe("180x180");

        MetaContent("mobile-web-app-capable").ShouldBe(["yes"]);
        MetaContent("apple-mobile-web-app-capable").ShouldBe(["yes"]);
        MetaContent("apple-mobile-web-app-title").ShouldBe([AppName]);
        MetaContent("apple-mobile-web-app-status-bar-style").ShouldBe(["black"]);
        MetaContent("theme-color").ShouldBe([PageBackground]);

        // GitHub Pages answers a deep route with the 404.html copy of this page. Its relative
        // links only resolve from the site root when they come after <base href="/">.
        var baseAt = Html.IndexOf("<base href=\"/\"", StringComparison.Ordinal);
        baseAt.ShouldBeGreaterThanOrEqualTo(0);
        Html.IndexOf("<link rel=\"manifest\"", StringComparison.Ordinal).ShouldBeGreaterThan(baseAt);
        Html.IndexOf("<link rel=\"apple-touch-icon\"", StringComparison.Ordinal).ShouldBeGreaterThan(baseAt);
    }

    [Fact]
    public void Single_page_fallback_is_a_copy_of_the_entry_page()
    {
        var workflow = File.ReadAllText(Path.Combine(RepositoryRoot, ".github", "workflows", "deploy.yml"));

        // 404.html is not a file of its own: the deploy copies the published entry page, so the
        // fallback carries the same head.
        workflow.ShouldMatch(@"cp\s+release/wwwroot/index\.html\s+release/wwwroot/404\.html");
    }

    private static JsonDocument ReadManifest() =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(WebRoot, "manifest.webmanifest")));

    private static string? Text(JsonElement element, string member) => element.GetProperty(member).GetString();

    /// <summary>Reads the pixel size from the IHDR chunk, the first chunk of every PNG file.</summary>
    private static string PngSize(string relativePath)
    {
        Span<byte> header = stackalloc byte[24];
        using (var file = File.OpenRead(Path.Combine(WebRoot, relativePath)))
        {
            file.ReadExactly(header);
        }

        // Eight signature bytes, then the chunk length and its type; the width and the height
        // follow as big-endian integers.
        header[..8].SequenceEqual(PngSignature).ShouldBeTrue($"{relativePath} must be a PNG file");
        header.Slice(12, 4).SequenceEqual("IHDR"u8).ShouldBeTrue($"{relativePath} must start with the IHDR chunk");

        return $"{BinaryPrimitives.ReadInt32BigEndian(header[16..])}x{BinaryPrimitives.ReadInt32BigEndian(header[20..])}";
    }

    private static List<string> Matches(string pattern) =>
        Regex.Matches(Html, pattern).Select(m => m.Groups[1].Value).ToList();

    private static List<string> MetaContent(string name) =>
        Matches($@"<meta\s+name=""{Regex.Escape(name)}""\s+content=""([^""]*)""");

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
