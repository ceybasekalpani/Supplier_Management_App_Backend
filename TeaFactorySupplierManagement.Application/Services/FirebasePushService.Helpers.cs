namespace TeaFactorySupplierManagement.Application.Services
{
    public partial class FirebasePushService
    {
        private static string NormalizeAudienceType(string? value)
        {
            if (string.Equals(value, "SpecificSupplier", StringComparison.OrdinalIgnoreCase))
            {
                return "SpecificSupplier";
            }

            return string.Equals(value, "Route", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, "RouteWise", StringComparison.OrdinalIgnoreCase)
                ? "Route"
                : "AllSuppliers";
        }

        private static string? CleanOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string NormalizeRequestType(string? value)
        {
            var normalized = value?.Trim().ToLowerInvariant();
            return normalized switch
            {
                "cash" or "advance" => "advance",
                "fertilizer" => "fertilizer",
                "item" or "items" => "items",
                _ => "advance"
            };
        }

        private static string NormalizeStatus(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "updated"
                : value.Trim().ToLowerInvariant();
        }

        private static string NormalizeConfigurationCategory(string? value)
        {
            var normalized = value?.Trim().ToLowerInvariant();
            return normalized switch
            {
                "fertilizer" or "fertilizers" => "fertilizer",
                "item" or "items" => "item",
                _ => "configuration"
            };
        }

        private static string BuildRequestStatusTitle(string requestType, string status)
        {
            return $"{ToDisplayRequestType(requestType)} request {ToDisplayStatus(status)}";
        }

        private static string BuildRequestStatusBody(string requestType, string status, string? requestSummary, string? remarks)
        {
            var displayType = ToDisplayRequestType(requestType).ToLowerInvariant();
            var cleanSummary = string.IsNullOrWhiteSpace(requestSummary) ? string.Empty : $" ({requestSummary.Trim()})";
            var message = $"Your {displayType} request{cleanSummary} has been {ToDisplayStatus(status).ToLowerInvariant()}.";

            if (status == "rejected" && !string.IsNullOrWhiteSpace(remarks))
            {
                message += $" Reason: {remarks.Trim()}";
            }

            return message;
        }

        private static string BuildDisbursementStatusTitle(string requestType, string status)
        {
            return $"{ToDisplayRequestType(requestType)} disbursement {ToDisplayStatus(status)}";
        }

        private static string BuildDisbursementStatusBody(string requestType, string status)
        {
            return $"Your {ToDisplayRequestType(requestType).ToLowerInvariant()} disbursement has been {ToDisplayStatus(status).ToLowerInvariant()}.";
        }

        private static string ToDisplayRequestType(string requestType)
        {
            return requestType switch
            {
                "advance" => "Advance",
                "fertilizer" => "Fertilizer",
                "items" => "Item",
                _ => "Request"
            };
        }

        private static string ToDisplayStatus(string status)
        {
            return status switch
            {
                "approved" => "Approved",
                "rejected" => "Rejected",
                "issued" => "Issued",
                "paid" => "Paid",
                _ => "Updated"
            };
        }

        private static string ToDisplayConfigurationCategory(string category)
        {
            return category switch
            {
                "fertilizer" => "Fertilizer type",
                "item" => "Item type",
                _ => "Configuration"
            };
        }
    }
}
