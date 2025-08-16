using System.Text;
using SpeakStoreLocate.ApiService.Models;

namespace SpeakStoreLocate.ApiService.Services.Interpretation;

class SearchCommandGeneration : ISearchCommandGeneration
{
    public string GenerateSearchCommand(IEnumerable<StorageCommand> searchRequests)
    {
        StringBuilder result = new StringBuilder();

        string systemPrompt =
            "Du bist ein Kommissionierungssystem für ein Lager, das für den User herausfindet, wo sich Objekte befinden. Du lieferst ";
        foreach (var searchRequest in searchRequests)
        {
            result.Append(searchRequest.ItemName);
        }

        return result.ToString();
    }
}