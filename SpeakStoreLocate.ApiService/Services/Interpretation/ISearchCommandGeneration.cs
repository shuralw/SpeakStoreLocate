using SpeakStoreLocate.ApiService.Models;

namespace SpeakStoreLocate.ApiService.Services.Interpretation;

internal interface ISearchCommandGeneration
{
    string GenerateSearchCommand(IEnumerable<StorageCommand> searchRequests);
}