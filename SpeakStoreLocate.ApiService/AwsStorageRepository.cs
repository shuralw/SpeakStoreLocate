using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;

namespace SpeakStoreLocate.ApiService;

public class AwsStorageRepository : IStorageRepository
{
    private readonly ILogger<AwsStorageRepository> _logger;
    private readonly IDynamoDBContext _dbContext;

    public AwsStorageRepository(
        ILogger<AwsStorageRepository> logger,
        IDynamoDBContext dbContext)
    {
        this._dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IEnumerable<StorageItem>> GetStorageItems()
    {
        var conditions = new List<ScanCondition>();
        var results = await _dbContext.ScanAsync<StorageItem>(conditions).GetRemainingAsync();

        return results;
    }

    public async Task<List<string>> PerformActions(List<StorageCommand> commands)
    {
        List<string> performedActions = new List<string>();

        // 7) Befehle speichern
        foreach (var cmd in commands)
        {
            if (cmd.Method == METHODS.ENTNAHME)
            {
                if (string.IsNullOrEmpty(cmd.Destination))
                {
                    continue;
                }

                var storageItem = await FindStorageItemByName(cmd);

                await _dbContext.DeleteAsync(storageItem);

                this._logger.LogInformation(
                    "Das Objekt {cmdItemName} wurde aus dem Lagerort {cmdDestination} erfolgreich entfernt",
                    cmd.ItemName,
                    cmd.Destination);

                performedActions.Add(
                    $"Das Objekt {cmd.ItemName} wurde aus dem Lagerort {cmd.Destination} erfolgreich entfernt");
            }

            if (cmd.Method == METHODS.SUCHE)
            {
                var storageItem = await FindStorageItemByName(cmd);

                _logger.LogInformation("{itemName} befindet sich hier: {storageItemDestination}",
                    cmd.ItemName,
                    storageItem.Location);
                performedActions.Add($"{cmd.ItemName} befindet sich hier: {storageItem.Location}");
            }

            if (cmd.Method == METHODS.UMLAGERN)
            {
                if (string.IsNullOrEmpty(cmd.Destination))
                {
                    continue;
                }

                var storageItem = await FindStorageItemByName(cmd);

                storageItem.Location = cmd.Destination;

                await _dbContext.SaveAsync(storageItem);

                _logger.LogInformation(
                    "Umlagerung: {storageItemName} wurde von {storageItemDestination} nach {cmdDestination} umgelagert",
                    storageItem.Name,
                    storageItem.Location,
                    cmd.Destination);

                performedActions.Add(
                    $"Umlagerung: {storageItem.Name} wurde von {storageItem.Location} nach {cmd.Destination} umgelagert");
            }

            if (cmd.Method == METHODS.EINLAGERN)
            {
                if (string.IsNullOrEmpty(cmd.Destination))
                {
                    continue;
                }

                var storageItem = new StorageItem
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = cmd.ItemName,
                    Location = cmd.Destination,
                    NormalizedName = cmd.ItemName.NormalizeForSearch()
                };

                await _dbContext.SaveAsync(storageItem);

                _logger.LogInformation("Einlagerung: {storageItemName} wurde in {storageItemDestination} eingelagert",
                    storageItem.Name,
                    storageItem.Location);

                performedActions.Add($"{storageItem.Name} wurde in {storageItem.Location} eingelagert");
            }
        }

        _logger.LogDebug("Anzahl an Ergebnissen: {performedActions}", performedActions.Count().ToString());
        return performedActions;
    }

    private async Task<StorageItem> FindStorageItemByName(StorageCommand cmd)
    {
        var normalizedQuery = cmd.ItemName.NormalizeForSearch();
        _logger.LogDebug("itemname: {itemname}", cmd.ItemName);
        _logger.LogDebug("itemname: {itemname}", cmd.Destination);
        _logger.LogDebug("normalizedQuery: {normalizedQuery}", normalizedQuery);
        var conditions = new List<ScanCondition>
        {
            new ScanCondition("NormalizedName", ScanOperator.Contains, normalizedQuery)
        };

        IEnumerable<StorageItem> results =
            await _dbContext.ScanAsync<StorageItem>(conditions).GetRemainingAsync();

        var storageItem = results.Single();
        return storageItem;
    }
}