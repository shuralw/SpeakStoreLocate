using SpeakStoreLocate.ApiService.Models;

namespace SpeakStoreLocate.ApiService.Services.Storage;

public interface IStorageRepository
{
    Task<IEnumerable<StorageItem>> GetStorageItems();
    Task<List<string>> PerformActions(List<StorageCommand> commands);
    Task<IEnumerable<string>> GetStorageLocations();
}