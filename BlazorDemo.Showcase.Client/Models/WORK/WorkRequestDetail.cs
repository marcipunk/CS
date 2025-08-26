using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using BlazorDemo.Showcase.Utils;

namespace BlazorDemo.Showcase.Models {
    public class WorkRequestDetail {
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
}
