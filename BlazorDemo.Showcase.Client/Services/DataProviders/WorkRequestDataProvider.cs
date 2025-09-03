using BlazorDemo.Showcase.Models;
using System.Net.Http.Json;
using System.Net.Http.Headers;



namespace BlazorDemo.Showcase.Services.DataProviders {


    public class WorkRequestDataProvider : DataProvider
    {
        private readonly HttpClient httpClient;
    // No JS/token dependency when routing via server proxy

    public WorkRequestDataProvider(HttpClient httpClient) : base(httpClient)
        {
            this.httpClient = httpClient;
        }



        // Satisfy the abstract contract. Returning empty keeps current WLoadDataAsync usage unchanged.
        protected override string GetBasePath() => string.Empty;

        // Make sure the base path points to the correct endpoint
       // protected override string GetBasePath() => "https://work.sbdw.cobra.local/api/v1/WorkRequest";

        // public Task<List<WorkRequest>?> GetAsync(CancellationToken cancellationToken = default)
        // {
        //     return LoadDataAsync<List<WorkRequest>>(cancellationToken: cancellationToken);
        // }

        public async Task<List<WorkRequest>?> GetAsync(
                int tenantId = 0,
                int customerId = 0,
                int statusId = 0,
                DateTime createdBefore = default,
                DateTime createdAfter = default,
                int pageNumber = 1,
                int pageSize = 1000,
                CancellationToken cancellationToken = default)
        {

            tenantId = 1;
            customerId = 30;
            statusId = 1;
            createdBefore = DateTime.Parse("2025-12-31");
            createdAfter = DateTime.Parse("2020-01-01");
            pageNumber = 1;
            pageSize = 1000;

            var query = new List<string>();
            query.Add($"TenantId={tenantId}");
           // query.Add($"CustomerId={customerId}");
         //   if (statusId != 0) query.Add($"StatusId={statusId}");
            // if (createdBefore != default) query.Add($"CreatedBefore={Uri.EscapeDataString(createdBefore.ToString("o"))}");
            // if (createdAfter != default) query.Add($"CreatedAfter={Uri.EscapeDataString(createdAfter.ToString("o"))}");
            // query.Add($"PageNumber={pageNumber}");
            // query.Add($"PageSize={pageSize}");

            var queryString = query.Count > 0 ? "?" + string.Join("&", query) : string.Empty;
           // var url = $"{GetBasePath()}{queryString}";

            var data = await WLoadDataPropertyAsync<List<WorkRequest>>(
                ["/api/v1/WorkRequest", queryString],
                cancellationToken: cancellationToken,
                propertyName: "data");

            return data;

        }


        /*
                public async Task<WorkRequestDetail?> GetAsync(int index, bool full = false, CancellationToken cancellationToken = default)
                {
                    var workrequest = await LoadDataAsync<WorkRequestDetail>([index.ToString()], cancellationToken);

                    if (full && workrequest != null)
                    {
                        //   workrequest.Opportunities = await GetOpportunitiesAsync(index, cancellationToken);
                        //   workrequest.Notes = await GetNotesAsync(index, cancellationToken);
                    }

                    return workrequest;
                }

                Task<List<Opportunity>?> GetOpportunitiesAsync(int index, CancellationToken cancellationToken = default)
                {
                    return LoadDataAsync<List<Opportunity>>([index.ToString(), "Opportunities"], cancellationToken);
                }

                Task<List<Note>?> GetNotesAsync(int index, CancellationToken cancellationToken = default)
                {
                    return LoadDataAsync<List<Note>>([index.ToString(), "Notes"], cancellationToken);
                }
        */
    }
}

