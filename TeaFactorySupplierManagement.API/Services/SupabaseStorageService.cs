using System.Net.Http.Headers;
using System.Text;
using TeaFactorySupplierManagement.Application.Interfaces;

namespace TeaFactorySupplierManagement.API.Services
{
    public class SupabaseStorageService : ISupabaseStorageService
    {
        private readonly string _supabaseUrl;
        private readonly string _serviceRoleKey;
        private readonly string _bucketName;
        private readonly ILogger<SupabaseStorageService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public SupabaseStorageService(
            IConfiguration configuration,
            ILogger<SupabaseStorageService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;

            _supabaseUrl = configuration["Supabase:Url"]
                ?? throw new InvalidOperationException("Supabase:Url is not configured.");
            _serviceRoleKey = configuration["Supabase:ServiceRoleKey"]
                ?? throw new InvalidOperationException("Supabase:ServiceRoleKey is not configured.");
            _bucketName = configuration["Supabase:BucketName"] ?? "profile-images";
        }

        public async Task<string> UploadProfileImageAsync(int regNo, Stream imageStream, string fileName, string contentType)
        {
            var extension = Path.GetExtension(fileName);
            if (string.IsNullOrEmpty(extension)) extension = ".jpg";
            var storagePath = $"supplier-{regNo}/profile{extension}";

            using var memoryStream = new MemoryStream();
            await imageStream.CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();

            _logger.LogInformation("Uploading {Bytes} bytes to '{Bucket}/{Path}'", bytes.Length, _bucketName, storagePath);

            var uploadUrl = $"{_supabaseUrl}/storage/v1/object/{_bucketName}/{storagePath}";

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _serviceRoleKey);
            client.DefaultRequestHeaders.Add("x-upsert", "true");

            using var content = new ByteArrayContent(bytes);
            content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

            var response = await client.PostAsync(uploadUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Supabase upload failed: {Status} - {Body}", response.StatusCode, responseBody);
                throw new InvalidOperationException($"Image upload failed: {response.StatusCode} - {responseBody}");
            }

            _logger.LogInformation("Supabase upload success: {Body}", responseBody);

            var publicUrl = $"{_supabaseUrl}/storage/v1/object/public/{_bucketName}/{storagePath}";

            _logger.LogInformation("Public URL: {Url}", publicUrl);
            return publicUrl;
        }

        public async Task<object> CheckStorageAsync()
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _serviceRoleKey);

            // 1. List all buckets
            var bucketsResp = await client.GetAsync($"{_supabaseUrl}/storage/v1/bucket");
            var bucketsBody = await bucketsResp.Content.ReadAsStringAsync();

            // 2. List objects inside profile-images bucket
            var listPayload = new StringContent(
                """{"prefix":"","limit":20,"offset":0}""",
                Encoding.UTF8, "application/json");
            var objectsResp = await client.PostAsync(
                $"{_supabaseUrl}/storage/v1/object/list/{_bucketName}", listPayload);
            var objectsBody = await objectsResp.Content.ReadAsStringAsync();

            // 3. Try uploading a tiny test file
            var testPath = "diagnostic-test/test.txt";
            var testContent = new ByteArrayContent(Encoding.UTF8.GetBytes("supabase-test"));
            testContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
            var testClient = _httpClientFactory.CreateClient();
            testClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _serviceRoleKey);
            testClient.DefaultRequestHeaders.Add("x-upsert", "true");
            var testUploadResp = await testClient.PostAsync(
                $"{_supabaseUrl}/storage/v1/object/{_bucketName}/{testPath}", testContent);
            var testUploadBody = await testUploadResp.Content.ReadAsStringAsync();

            return new
            {
                supabaseUrl = _supabaseUrl,
                bucketName = _bucketName,
                bucketsStatus = (int)bucketsResp.StatusCode,
                buckets = bucketsBody,
                objectsStatus = (int)objectsResp.StatusCode,
                objects = objectsBody,
                testUploadStatus = (int)testUploadResp.StatusCode,
                testUploadResponse = testUploadBody
            };
        }

        public async Task DeleteProfileImageAsync(string fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
                return;

            // Strip cache-bust param and extract path
            var cleanUrl = fileUrl.Split('?')[0];
            var marker = $"/storage/v1/object/public/{_bucketName}/";
            var markerIndex = cleanUrl.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex < 0)
                return;

            var storagePath = cleanUrl[(markerIndex + marker.Length)..];
            var deleteUrl = $"{_supabaseUrl}/storage/v1/object/{_bucketName}/{storagePath}";

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _serviceRoleKey);

                var response = await client.DeleteAsync(deleteUrl);
                _logger.LogInformation("Deleted '{Path}': {Status}", storagePath, response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete '{Path}'", storagePath);
            }
        }
    }
}
