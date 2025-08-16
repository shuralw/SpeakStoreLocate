using SpeakStoreLocate.ApiService.Models;

namespace SpeakStoreLocate.ApiService.Services.Interpretation;

internal interface ISearchRequestStringifier
{
    string Stringify(StorageCommand searchCommand);
}