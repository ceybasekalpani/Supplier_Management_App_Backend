namespace TeaFactorySupplierManagement.Application.Interfaces
{
    public interface ISupabaseStorageService
    {
        Task<string> UploadProfileImageAsync(int regNo, Stream imageStream, string fileName, string contentType);
        Task DeleteProfileImageAsync(string fileUrl);
        Task<object> CheckStorageAsync();
    }
}
