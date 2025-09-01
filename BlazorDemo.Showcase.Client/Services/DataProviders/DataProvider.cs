using System.Net.Http.Json;
using System.Text.Json; // added

namespace BlazorDemo.Showcase.Services.DataProviders {
    public abstract class DataProvider
    {
        readonly HttpClient _httpClient;

        private protected DataProvider(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        protected abstract string GetBasePath();

        protected Task<T?> LoadDataAsync<T>(string[]? pathItems = null, CancellationToken cancellationToken = default)
        {
            var resultPath = GetBasePath();
            if (pathItems != null)
            {
                foreach (var pathItem in pathItems)
                    resultPath += $"/{pathItem.ToString()}";
            }
            return _httpClient!.GetFromJsonAsync<T>(resultPath, cancellationToken);
        }

        // Convenience: load only a named top-level property (default: "data") from the default-domain response
        protected Task<T?> LoadDataPropertyAsync<T>(string propertyName = "data", string[]? pathItems = null, CancellationToken cancellationToken = default)
        {
            var resultPath = GetBasePath();
            if (pathItems != null)
            {
                foreach (var pathItem in pathItems)
                    resultPath += $"/{pathItem.ToString()}";
            }
            var request = new HttpRequestMessage(HttpMethod.Get, resultPath);
            return SendAndReadPropertyAsync<T>(request, propertyName, cancellationToken);
        }

        // Convenience: directly get the "data" array from the default-domain response
        protected Task<TItem[]?> LoadDataArrayAsync<TItem>(string[]? pathItems = null, CancellationToken cancellationToken = default)
            => LoadDataPropertyAsync<TItem[]>("data", pathItems, cancellationToken);

        protected Task<T?> WLoadDataAsync<T>(string[]? pathItems = null, CancellationToken cancellationToken = default, string? wjwtToken = null)
        {
            // Route through server proxy; server attaches Authorization from HttpOnly cookie.
            var resultPath = CombineUrl("/workproxy", pathItems);

            var request = new HttpRequestMessage(HttpMethod.Get, resultPath);
            return SendAndReadAsync<T>(request, cancellationToken);
        }

        // Convenience: load only a named top-level property (default: "data") from the work-domain response
        protected Task<T?> WLoadDataPropertyAsync<T>(string[]? pathItems = null, CancellationToken cancellationToken = default, string? wjwtToken = null, string propertyName = "data")
        {
            var resultPath = CombineUrl("/workproxy", pathItems);

            var request = new HttpRequestMessage(HttpMethod.Get, resultPath);
            return SendAndReadPropertyAsync<T>(request, propertyName, cancellationToken);
        }

        // Convenience: directly get the "data" array from the work-domain response
        protected Task<TItem[]?> WLoadDataArrayAsync<TItem>(string[]? pathItems = null, CancellationToken cancellationToken = default, string? wjwtToken = null)
            => WLoadDataPropertyAsync<TItem[]>(pathItems, cancellationToken, wjwtToken, "data");

        // Helper to combine base URL with optional path segments
        private static string CombineUrl(string baseUrl, string[]? pathItems)
        {
            var result = (baseUrl ?? string.Empty).TrimEnd('/');

            if (pathItems == null || pathItems.Length == 0)
                return result;

            bool hasQuery = false;

            foreach (var raw in pathItems)
            {
                if (string.IsNullOrWhiteSpace(raw))
                    continue;

                var item = raw.Trim();

                // Query string handling: append with ? or & without inserting a slash
                if (item.StartsWith("?") || item.StartsWith("&") || (item.Contains("=") && (item.Contains("?") || hasQuery)))
                {
                    var q = item.TrimStart('?').TrimStart('&');
                    if (!hasQuery)
                    {
                        if (!string.IsNullOrEmpty(q))
                            result += "?" + q;
                        hasQuery = true;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(q))
                            result += "&" + q;
                    }
                    continue;
                }

                // Path segment handling: skip slash-only segments to avoid creating //
                var segment = item.Trim('/');
                if (segment.Length == 0)
                    continue;

                result += "/" + segment;
            }

            return result;
        }

        private async Task<T?> SendAndReadAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
        }

        // Helper: read only a specific top-level property and deserialize that section to T
        private async Task<T?> SendAndReadPropertyAsync<T>(HttpRequestMessage request, string propertyName, CancellationToken cancellationToken)
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return default;

            if (!doc.RootElement.TryGetProperty(propertyName, out var element) || element.ValueKind == JsonValueKind.Undefined || element.ValueKind == JsonValueKind.Null)
                return default;

            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            return element.Deserialize<T>(options);
        }
    }
}
