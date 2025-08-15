#r "nuget: ForgejoApiClient, 12.0.1-rev.3"
#nullable enable
using System.Runtime.CompilerServices;
using System.Threading;
using ForgejoApiClient;
using ForgejoApiClient.Api;

public static async IAsyncEnumerable<Repository> AllReposAsync(this ForgejoClient self, [EnumeratorCancellation] CancellationToken cancelToken = default)
{
    var settings = await self.Settings.GetApiSettingsAsync(cancelToken);
    var limit = (int?)settings.max_response_items ?? 50;
    var page = 1;
    while (true)
    {
        var result = await self.Repository.SearchAsync(paging: new(page: page, limit: limit), cancelToken: cancelToken);
        if (result.data == null || result.data.Count <= 0) break;
        page++;

        foreach (var repo in result.data)
        {
            yield return repo;
        }
    }
}
