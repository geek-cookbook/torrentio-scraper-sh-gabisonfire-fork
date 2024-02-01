namespace Scraper.Crawlers;

public abstract class BaseXmlCrawler(IHttpClientFactory httpClientFactory, ILogger<BaseXmlCrawler> logger, IDataStorage storage) : BaseCrawler(logger, storage)
{
    public override async Task Execute()
    {
        logger.LogInformation("Starting {Source} crawl", Source);
        
        using var client = httpClientFactory.CreateClient("Scraper");
        var xml = await client.GetStringAsync(Url);
        var xmlRoot = XElement.Parse(xml);
        
        var torrents = xmlRoot.Descendants("item")
            .Select(ParseTorrent)
            .Where(x => x is not null)
            .ToList();
        
        if (torrents.Count == 0)
        {
            logger.LogWarning("No torrents found in {Source} response", Source);
            return;
        }
        
        await InsertTorrents(torrents!);
    }

    protected virtual void HandleInfoHash(XElement itemNode, Torrent torrent, string infoHashKey)
    {
        if (!Mappings.ContainsKey(infoHashKey))
        {
            return;
        }

        var infoHash = itemNode.Element(Mappings[infoHashKey])?.Value;

        if (infoHash is not null)
        {
            torrent.InfoHash = infoHash;
        }
    }

    protected abstract Torrent? ParseTorrent(XElement itemNode);
}