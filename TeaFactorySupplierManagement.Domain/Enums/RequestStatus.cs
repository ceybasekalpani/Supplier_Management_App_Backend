namespace TeaFactorySupplierManagement.Domain.Enums
{
    public enum RequestStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2,
        Paid = 3,
        Dispatched = 4,
        Issued = 5,
        Cancelled = 6
    }

    public enum CashRequestType
    {
        Advance = 0,
        Loan = 1
    }

    public enum NotificationType
    {
        Info = 0,
        Success = 1,
        Warning = 2,
        Error = 3
    }

    public enum Theme
    {
        Light = 0,
        Dark = 1
    }

    public enum Language
    {
        English = 0,
        Sinhala = 1
    }
}