namespace Scraper.Crawlers;

public abstract class BaseCrawler(ILogger<BaseCrawler> logger, IDataStorage storage) : ICrawler
{
    protected abstract IReadOnlyDictionary<string, string> Mappings { get; }
    protected abstract string Url { get; }
    protected abstract string Source { get; }
    protected IDataStorage Storage => storage;

    public virtual Task Execute() => Task.CompletedTask;

    protected async Task<InsertTorrentResult> InsertTorrents(IReadOnlyCollection<Torrent> torrent)
    {
        var result = await storage.InsertTorrents(torrent);
        
        if (!result.Success)
        {
            logger.LogWarning("Ingestion Failed: [{Error}]", result.ErrorMessage);
            return result;
        }
            
        logger.LogInformation("Ingestion Successful - Wrote {Count} new torrents", result.InsertedCount);
        return result;
    }
}