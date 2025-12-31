using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Microsoft.CodeAnalysis;
using SpeakStoreLocate.ApiService.Models;
using SpeakStoreLocate.ApiService.Utils;
using SpeakStoreLocate.ApiService.Middleware;
using System.Diagnostics;
using SpeakStoreLocate.ApiService.Utilities;

namespace SpeakStoreLocate.ApiService.Services.Storage;

public class AwsStorageRepository : IStorageRepository
{
    private readonly ILogger<AwsStorageRepository> _logger;
    private readonly IDynamoDBContext _dbContext;
    private readonly IUserContext _userContext;

    public AwsStorageRepository(
        ILogger<AwsStorageRepository> logger,
        IDynamoDBContext dbContext,
        IUserContext userContext)
    {
        this._dbContext = dbContext;
        _logger = logger;
        _userContext = userContext;
    }

    public async Task<IEnumerable<StorageItem>> GetStorageItems()
    {
        var sw = Stopwatch.StartNew();
        var conditions = new List<ScanCondition>
        {
            new ScanCondition("UserId", ScanOperator.Equal, _userContext.UserId)
        };
        var results = await _dbContext.ScanAsync<StorageItem>(conditions).GetRemainingAsync();

        _logger.LogDebug("GetStorageItems completed. Count={Count} ElapsedMs={ElapsedMs}", results.Count, sw.ElapsedMilliseconds);

        return results;
    }

    public async Task<List<string>> PerformActions(List<StorageCommand> commands)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("PerformActions started. CommandCount={CommandCount}", commands?.Count ?? 0);
        List<string> performedActions = new List<string>();

        // 7) Befehle speichern
        foreach (var cmd in commands)
        {
            _logger.LogDebug("Processing command. Method={Method} ItemNameLength={ItemNameLength} DestinationLength={DestinationLength}",
                cmd.Method.ToString(),
                LoggingSanitizer.SafeLength(cmd.ItemName),
                LoggingSanitizer.SafeLength(cmd.Destination));

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
                    NormalizedName = cmd.ItemName.NormalizeForSearch(),
                    NormalizedLocation = cmd.Destination.NormalizeForSearch(),
                    UserId = _userContext.UserId
                };

                await _dbContext.SaveAsync(storageItem);

                _logger.LogInformation("Einlagerung: {storageItemName} wurde in {storageItemDestination} eingelagert",
                    storageItem.Name,
                    storageItem.Location);

                performedActions.Add($"{storageItem.Name} wurde in {storageItem.Location} eingelagert");
            }
        }

        _logger.LogInformation("PerformActions completed. ActionCount={ActionCount} ElapsedMs={ElapsedMs}",
            performedActions.Count,
            sw.ElapsedMilliseconds);
        _logger.LogDebug("Performed action count (debug). ActionCount={ActionCount}", performedActions.Count);
        return performedActions;
    }

    public async Task<IEnumerable<string>> GetStorageLocations()
    {
        var sw = Stopwatch.StartNew();
        var storageItems = await _dbContext
            .ScanAsync<StorageItem>(new List<ScanCondition>
            {
                new ScanCondition("UserId", ScanOperator.Equal, _userContext.UserId)
            })
            .GetRemainingAsync();

        var distinctLocations = storageItems
            .Select(item => item.Location)
            .Where(location => !string.IsNullOrEmpty(location))
            .Distinct()
            .ToList();

        _logger.LogDebug("GetStorageLocations completed. DistinctCount={DistinctCount} TotalItems={TotalItems} ElapsedMs={ElapsedMs}",
            distinctLocations.Count,
            storageItems.Count,
            sw.ElapsedMilliseconds);
        
        return distinctLocations;
    }

    public async Task<StorageItem?> UpdateStorageItemAsync(string id, string? name, string? location)
    {
        // Load item by hash key (Id)
        var item = await _dbContext.LoadAsync<StorageItem>(id);
        if (item == null)
        {
            return null;
        }

        // Ensure user owns the item
        if (!string.Equals(item.UserId, _userContext.UserId, StringComparison.Ordinal))
        {
            // Not authorized to modify
            return null;
        }

        var originalLocation = item.Location;

        if (!string.IsNullOrWhiteSpace(name))
        {
            item.Name = name;
            item.NormalizedName = name.NormalizeForSearch();
        }
        if (!string.IsNullOrWhiteSpace(location))
        {
            item.Location = location!;
            item.NormalizedLocation = location.NormalizeForSearch();
        }

        await _dbContext.SaveAsync(item);

        _logger.LogInformation("Item {ItemId} updated. Name={Name}, Location: {OldLoc} -> {NewLoc}",
            item.Id, item.Name, originalLocation, item.Location);

        return item;
    }

    private async Task<StorageItem> FindStorageItemByName(StorageCommand cmd)
    {
        var sw = Stopwatch.StartNew();
        var normalizedQuery = cmd.ItemName.NormalizeForSearch();
        _logger.LogDebug("FindStorageItemByName started. ItemNameLength={ItemNameLength} NormalizedQueryLength={NormalizedQueryLength}",
            LoggingSanitizer.SafeLength(cmd.ItemName),
            LoggingSanitizer.SafeLength(normalizedQuery));

        var conditions = new List<ScanCondition>
        {
            new ScanCondition("UserId", ScanOperator.Equal, _userContext.UserId),
            new ScanCondition("NormalizedName", ScanOperator.Contains, normalizedQuery)
        };

        IEnumerable<StorageItem> results =
            await _dbContext.ScanAsync<StorageItem>(conditions).GetRemainingAsync();

        var list = results as IList<StorageItem> ?? results.ToList();
        _logger.LogDebug("FindStorageItemByName query completed. MatchCount={MatchCount} ElapsedMs={ElapsedMs}",
            list.Count,
            sw.ElapsedMilliseconds);

        if (list.Count == 1)
        {
            return list[0];
        }

        if (list.Count == 0)
        {
            _logger.LogWarning("No storage item match found. NormalizedQuery={NormalizedQuery}", normalizedQuery);
            throw new InvalidOperationException("No storage item match found.");
        }

        _logger.LogWarning("Multiple storage item matches found. NormalizedQuery={NormalizedQuery} MatchCount={MatchCount}",
            normalizedQuery,
            list.Count);
        throw new InvalidOperationException("Multiple storage item matches found.");
    }
}