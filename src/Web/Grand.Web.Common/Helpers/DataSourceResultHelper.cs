using Grand.Domain;
using Grand.Web.Common.DataSource;

namespace Grand.Web.Common.Helpers;
public static class DataSourceResultHelper
{
    public static async Task<DataSourceResult> GetSearchResult<T>(
        string id, IEnumerable<T> items, Func<T, Task<string>> getName) where T : BaseEntity
    {
        var searchModels = new List<SearchModel>();

        if (!string.IsNullOrEmpty(id))
        {
            var currentItem = items.FirstOrDefault(item => item.Id == id);
            if (currentItem != null)
            {
                searchModels.Add(new SearchModel(currentItem.Id, await getName(currentItem)));
            }
        }

        foreach (var item in items)
        {
            if (item.Id != id)
            {
                searchModels.Add(new SearchModel(item.Id, await getName(item)));
            }
        }

        return new DataSourceResult {
            Data = searchModels,
            Total = searchModels.Count
        };
    }
    private sealed record SearchModel(string Id, string Name);
}
