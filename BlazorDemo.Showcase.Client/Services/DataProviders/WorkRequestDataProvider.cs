using BlazorDemo.Showcase.Models;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;



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
                int pageSize = 20,
                CancellationToken cancellationToken = default)
        {

            tenantId = 1;
            customerId = 0;
            statusId = 1;
            createdBefore = DateTime.Parse("2025-12-31");
            createdAfter = DateTime.Parse("2020-01-01");
            pageNumber = 1;
            pageSize = 20;

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

        public async Task<WorkRequestDetail?> GetDetailAsync(int id, CancellationToken cancellationToken = default)
        {
            // GET detail by id via server proxy
            var request = new HttpRequestMessage(HttpMethod.Get, $"/workproxy/api/v1/WorkRequest/{id}");
            var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;
            return await response.Content.ReadFromJsonAsync<WorkRequestDetail>(cancellationToken: cancellationToken);
        }

        public async Task<bool> CreateAsync(WorkRequestDetail model, CancellationToken cancellationToken = default)
        {
            // POST create via server proxy
            using var response = await httpClient.PostAsJsonAsync("/workproxy/api/v1/WorkRequest", model, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateAsync(WorkRequestDetail model, CancellationToken cancellationToken = default)
        {
            if (model.Id <= 0)
                return false;
            using var response = await httpClient.PutAsJsonAsync($"/workproxy/api/v1/WorkRequest/{model.Id}", model, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        // Lightweight AppUser DTOs to populate customer dropdown
        public sealed class AppUserDto
        {
            public int Id { get; set; }
            public string? Name { get; set; }
            public int PrimaryTenantId { get; set; }
            public List<CustomerAppUserDto> CustomerAppUsers { get; set; } = new();
        }

        public sealed class CustomerAppUserDto
        {
            public int CustomerId { get; set; }
            public int AppUserId { get; set; }
            public int Role { get; set; }
            public CustomerDto? Customer { get; set; }
        }

        public sealed class CustomerDto
        {
            public int Id { get; set; }
            public string? Name { get; set; }
        }

        // User -> Customers lookup DTO
        public sealed class UserCustomerItem
        {
            public int Id { get; set; }
            public int TenantId { get; set; }
            public string? VatRegNo { get; set; }
            public string? Name { get; set; }
            public string? VoucherName { get; set; }
            public string? AddressLine { get; set; }
            public int Role { get; set; }
        }

        // Fetch customers available for the current user (used to populate the dropdown)
        public async Task<List<UserCustomerItem>?> GetUserCustomersAsync(CancellationToken cancellationToken = default)
        {
            using var response = await httpClient.GetAsync("/workproxy/api/v1/LookUp/user/customers", cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = doc.RootElement;
            var list = new List<UserCustomerItem>();
            if (root.ValueKind != JsonValueKind.Array)
                return list;
            foreach (var item in root.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                    continue;
                var dto = new UserCustomerItem();
                if (item.TryGetProperty("id", out var idEl) && idEl.TryGetInt32(out var idVal)) dto.Id = idVal;
                if (item.TryGetProperty("tenantId", out var tidEl) && tidEl.TryGetInt32(out var tidVal)) dto.TenantId = tidVal;
                if (item.TryGetProperty("vatRegNo", out var vatEl) && vatEl.ValueKind == JsonValueKind.String) dto.VatRegNo = vatEl.GetString();
                if (item.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String) dto.Name = nameEl.GetString();
                if (item.TryGetProperty("voucherName", out var vEl) && vEl.ValueKind == JsonValueKind.String) dto.VoucherName = vEl.GetString();
                if (item.TryGetProperty("addressLine", out var addrEl) && addrEl.ValueKind == JsonValueKind.String) dto.AddressLine = addrEl.GetString();
                if (item.TryGetProperty("role", out var roleEl) && roleEl.TryGetInt32(out var roleVal)) dto.Role = roleVal;
                if (dto.Id > 0)
                    list.Add(dto);
            }
            return list;
        }

        public async Task<AppUserDto?> GetCurrentAppUserAsync(CancellationToken cancellationToken = default)
        {
            // Retry a couple of times on 401 to bridge the post-login cookie race
            var attempts = 0;
            var delays = new[] { 150, 350 }; // ms
            HttpResponseMessage? response = null;
            try
            {
                while (true)
                {
                    response = await httpClient.GetAsync("/workproxy/api/v1/Auth/appuser", cancellationToken);
                    if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized)
                        break;
                    if (attempts >= 2)
                        break;
                    response.Dispose();
                    try { await Task.Delay(delays[attempts], cancellationToken); } catch { }
                    attempts++;
                }
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || !response.IsSuccessStatusCode)
                    return null;

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                var root = doc.RootElement;
                var user = new AppUserDto();

                static bool TryGetCaseInsensitive(JsonElement obj, string name, out JsonElement value)
                {
                    if (obj.ValueKind != JsonValueKind.Object)
                    {
                        value = default;
                        return false;
                    }
                    // Fast path exact
                    if (obj.TryGetProperty(name, out value))
                        return true;
                    foreach (var prop in obj.EnumerateObject())
                    {
                        if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
                        {
                            value = prop.Value;
                            return true;
                        }
                    }
                    value = default;
                    return false;
                }

                if (TryGetCaseInsensitive(root, "id", out var idEl) && idEl.TryGetInt32(out var idVal)) user.Id = idVal;
                if (TryGetCaseInsensitive(root, "name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String) user.Name = nameEl.GetString();
                if (TryGetCaseInsensitive(root, "primaryTenantId", out var ptiEl))
            {
                int parsed = 0;
                switch (ptiEl.ValueKind)
                {
                    case JsonValueKind.Number:
                        if (ptiEl.TryGetInt32(out var n)) parsed = n;
                        break;
                    case JsonValueKind.String:
                        var s = ptiEl.GetString();
                        if (!string.IsNullOrWhiteSpace(s) && int.TryParse(s, out var n2)) parsed = n2;
                        break;
                    case JsonValueKind.True:
                        parsed = 1;
                        break;
                    case JsonValueKind.False:
                        parsed = 0;
                        break;
                }
                user.PrimaryTenantId = parsed;
            }
                // Prefer customerAppUsers; fallback to customers
                if (TryGetCaseInsensitive(root, "customerAppUsers", out var cauEl) && cauEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in cauEl.EnumerateArray())
                    {
                        if (item.ValueKind != JsonValueKind.Object) continue;
                        var dto = new CustomerAppUserDto();
                        if (TryGetCaseInsensitive(item, "customerId", out var cid) && cid.TryGetInt32(out var cidVal)) dto.CustomerId = cidVal;
                        if (TryGetCaseInsensitive(item, "appUserId", out var auid) && auid.TryGetInt32(out var auidVal)) dto.AppUserId = auidVal;
                        if (TryGetCaseInsensitive(item, "role", out var roleEl) && roleEl.TryGetInt32(out var roleVal)) dto.Role = roleVal;
                        if (TryGetCaseInsensitive(item, "customer", out var custEl) && custEl.ValueKind == JsonValueKind.Object)
                        {
                            var c = new CustomerDto();
                            if (TryGetCaseInsensitive(custEl, "id", out var idc) && idc.TryGetInt32(out var idcVal)) c.Id = idcVal;
                            if (TryGetCaseInsensitive(custEl, "name", out var cname) && cname.ValueKind == JsonValueKind.String) c.Name = cname.GetString();
                            dto.Customer = c;
                        }
                        user.CustomerAppUsers.Add(dto);
                    }
                }
                else if (TryGetCaseInsensitive(root, "customers", out var customersEl) && customersEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var custEl in customersEl.EnumerateArray())
                    {
                        if (custEl.ValueKind != JsonValueKind.Object) continue;
                        var c = new CustomerDto();
                        if (TryGetCaseInsensitive(custEl, "id", out var idc) && idc.TryGetInt32(out var idcVal)) c.Id = idcVal;
                        if (TryGetCaseInsensitive(custEl, "name", out var cname) && cname.ValueKind == JsonValueKind.String) c.Name = cname.GetString();
                        user.CustomerAppUsers.Add(new CustomerAppUserDto { CustomerId = c.Id, Customer = c });
                    }
                }
                return user;
            }
            finally
            {
                response?.Dispose();
            }
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

