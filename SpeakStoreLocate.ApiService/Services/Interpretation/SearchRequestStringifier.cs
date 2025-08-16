using SpeakStoreLocate.ApiService.Models;

namespace SpeakStoreLocate.ApiService.Services.Interpretation;

class SearchRequestStringifier : ISearchRequestStringifier
{
    public string Stringify(StorageCommand searchCommand)
    {
        return searchCommand.ItemName;
    }
}