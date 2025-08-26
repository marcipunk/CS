using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BlazorDemo.Showcase.Utils;

namespace BlazorDemo.Showcase.Models {
    public class WorkRequest {
public int Id { get; set; }
public int TenantId { get; set; }
public DateTime TimeStamp { get; set; }
public int? AvatarId { get; set; }
public int CustomerId { get; set; }
public int RequestorId { get; set; }
public DateTime CreatedDate { get; set; }
public DateTime? EarliestStartDate { get; set; }
public DateTime? LatestEndDate { get; set; }
public string? PreferedWorkerName { get; set; }
public string? Subject { get; set; }
public string? Description { get; set; }
public int StatusId { get; set; }
public string? RejectionReason { get; set; }
public string? TenantName { get; set; }
public string? RequestorName { get; set; }
public string? CustomerName { get; set; }
public string? Status { get; set; }
public int Role { get; set; }
   
    }

    // Add a DTO matching the API's paged envelope
    public class PagedResponse<T> {
        [JsonPropertyName("pageNumber")]
        public int PageNumber { get; set; }

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; }

        [JsonPropertyName("totalPages")]
        public int TotalPages { get; set; }

        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }

        [JsonPropertyName("nextPage")]
        public string? NextPage { get; set; }

        [JsonPropertyName("previousPage")]
        public string? PreviousPage { get; set; }

        [JsonPropertyName("firstPage")]
        public string? FirstPage { get; set; }

        [JsonPropertyName("lastPage")]
        public string? LastPage { get; set; }

        [JsonPropertyName("data")]
        public List<T> Data { get; set; } = new();

        [JsonPropertyName("succeeded")]
        public bool Succeeded { get; set; }

        // Will have ValueKind.Null when the JSON value is null
        [JsonPropertyName("errors")]
        public JsonElement Errors { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("responseTime")]
        public DateTimeOffset? ResponseTime { get; set; }
    }
}
