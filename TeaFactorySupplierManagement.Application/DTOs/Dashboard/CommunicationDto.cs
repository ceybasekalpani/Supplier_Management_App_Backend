namespace TeaFactorySupplierManagement.Application.DTOs.Dashboard
{
    public class CommunicationDashboardDto
    {
        public List<DashboardNewsDto> News { get; set; } = new();
        public List<DashboardNotificationDto> Notifications { get; set; } = new();
    }

    public class DashboardNewsDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Created { get; set; } = string.Empty;
        public string Expiry { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Priority { get; set; }
        public bool IsActive { get; set; }
        public bool ShowPopup { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public string AudienceType { get; set; } = "AllSuppliers";
        public int? TargetRegNo { get; set; }
        public string? TargetRoute { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public int DismissedCount { get; set; }
    }

    public class DashboardNotificationDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ScheduledFor { get; set; }
        public string? SentAt { get; set; }
        public int Priority { get; set; }
        public bool IsActive { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public int? TargetRegNo { get; set; }
        public string? TargetRoute { get; set; }
        public string AudienceType { get; set; } = "AllSuppliers";
        public string CreatedBy { get; set; } = string.Empty;
        public int ReadCount { get; set; }
        public int DismissedCount { get; set; }
    }

    public class CreateDashboardNewsDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Content { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime? StartDate { get; set; }
        public bool IsActive { get; set; } = true;
        public bool? Active { get; set; }
        public bool ShowPopup { get; set; }
        public int Priority { get; set; } = 1;
        public bool PublishNow { get; set; } = true;
        public string AudienceType { get; set; } = "AllSuppliers";
        public object? TargetRegNo { get; set; }
        public string? TargetRoute { get; set; }
    }

    public class UpdateDashboardNewsDto : CreateDashboardNewsDto
    {
    }

    public class CreateDashboardNotificationDto
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "info";
        public DateTime? Schedule { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Priority { get; set; } = 1;
        public bool IsActive { get; set; } = true;
        public bool? Active { get; set; }
        public object? TargetRegNo { get; set; }
        public string? TargetRoute { get; set; }
        public bool PublishNow { get; set; } = true;
        public string AudienceType { get; set; } = "AllSuppliers";
    }

    public class UpdateDashboardNotificationDto : CreateDashboardNotificationDto
    {
    }
}
