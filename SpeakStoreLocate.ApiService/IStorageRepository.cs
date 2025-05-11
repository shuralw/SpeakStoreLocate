namespace SpeakStoreLocate.ApiService;

public interface IStorageRepository
{
    Task<IEnumerable<StorageItem>> GetStorageItems();
    Task<List<string>> PerformActions(List<StorageCommand> commands);
}