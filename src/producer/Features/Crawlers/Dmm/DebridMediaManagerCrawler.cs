namespace Producer.Features.Crawlers.Dmm;

public partial class DebridMediaManagerCrawler(
    IHttpClientFactory httpClientFactory,
    ILogger<DebridMediaManagerCrawler> logger,
    IDataStorage storage,
    GithubConfiguration githubConfiguration) : BaseCrawler(logger, storage)
{
    [GeneratedRegex("""<iframe src="https:\/\/debridmediamanager.com\/hashlist#(.*)"></iframe>""")]
    private static partial Regex HashCollectionMatcher();

    private const string DownloadBaseUrl = "https://raw.githubusercontent.com/debridmediamanager/hashlists/main";

    protected override IReadOnlyDictionary<string, string> Mappings => new Dictionary<string, string>();
    protected override string Url => "https://api.github.com/repos/debridmediamanager/hashlists/git/trees/main?recursive=1";
    protected override string Source => "DMM";

    public override async Task Execute()
    {
        var client = httpClientFactory.CreateClient("Scraper");
        client.DefaultRequestHeaders.Authorization = new("Bearer", githubConfiguration.PAT);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("curl");

        var jsonBody = await client.GetStringAsync(Url);

        var json =  JsonDocument.Parse(jsonBody);

        var entriesArray = json.RootElement.GetProperty("tree");

        logger.LogInformation("Found {Entries} total DMM pages", entriesArray.GetArrayLength());

        foreach (var entry in entriesArray.EnumerateArray())
        {
            await ParsePage(entry, client);
        }
    }

    private async Task ParsePage(JsonElement entry, HttpClient client)
    {
        var (pageIngested, name) = await IsAlreadyIngested(entry);

        if (string.IsNullOrEmpty(name) || pageIngested)
        {
            return;
        }

        var pageSource = await client.GetStringAsync($"{DownloadBaseUrl}/{name}");

        await ExtractPageContents(pageSource, name);
    }

    private async Task ExtractPageContents(string pageSource, string name)
    {
        var match = HashCollectionMatcher().Match(pageSource);

        if (!match.Success)
        {
            logger.LogWarning("Failed to match hash collection for {Name}", name);
            await Storage.MarkPageAsIngested(name);
            return;
        }

        var encodedJson = match.Groups.Values.ElementAtOrDefault(1);

        if (string.IsNullOrEmpty(encodedJson?.Value))
        {
            logger.LogWarning("Failed to extract encoded json for {Name}", name);
            return;
        }

        await ProcessExtractedContentsAsTorrentCollection(encodedJson.Value, name);
    }

    private async Task ProcessExtractedContentsAsTorrentCollection(string encodedJson, string name)
    {
        var decodedJson = LZString.DecompressFromEncodedURIComponent(encodedJson);

        var json = JsonDocument.Parse(decodedJson);

        await InsertTorrentsForPage(json);

        var result = await Storage.MarkPageAsIngested(name);

        if (!result.Success)
        {
            logger.LogWarning("Failed to mark page as ingested: [{Error}]", result.ErrorMessage);
            return;
        }

        logger.LogInformation("Successfully marked page as ingested");
    }

    private Torrent? ParseTorrent(JsonElement item)
    {

        if (!item.TryGetProperty("filename", out var filenameElement) ||
            !item.TryGetProperty("bytes", out var bytesElement) ||
            !item.TryGetProperty("hash", out var hashElement))
        {
            return null;
        }

        var torrent = new Torrent
        {
            Source = Source,
            Name = filenameElement.GetString(),
            Size = bytesElement.GetInt64().ToString(),
            InfoHash = hashElement.ToString(),
            Seeders = 0,
            Leechers = 0,
        };

        if (string.IsNullOrEmpty(torrent.Name))
        {
            return null;
        }

        var parsedTorrent = TorrentTitleParser.Parse(torrent.Name);

        if (parsedTorrent.IsInvalid)
        {
            return null;
        }

        if (parsedTorrent.IsMovie)
        {
            torrent.Category = "movies";
            torrent.Name = parsedTorrent.Movie.Title;

            return torrent;
        }

        if (parsedTorrent.IsShow)
        {
            torrent.Category = "tv";
            torrent.Name = parsedTorrent.Show.Title;

            return torrent;
        }

        return null;
    }

    private async Task InsertTorrentsForPage(JsonDocument json)
    {
        var torrents = json.RootElement.EnumerateArray()
            .Select(ParseTorrent)
            .Where(t => t is not null)
            .ToList();

        if (torrents.Count == 0)
        {
            logger.LogWarning("No torrents found in {Source} response", Source);
            return;
        }

        await InsertTorrents(torrents!);
    }

    private async Task<(bool Success, string? Name)> IsAlreadyIngested(JsonElement entry)
    {
        var name = entry.GetProperty("path").GetString();

        if (string.IsNullOrEmpty(name))
        {
            return (false, null);
        }

        var pageIngested = await Storage.PageIngested(name);

        return (pageIngested, name);
    }
}
