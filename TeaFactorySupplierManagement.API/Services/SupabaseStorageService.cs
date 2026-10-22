using System.Net.Http.Headers;
using System.Net.Sockets;
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
        private readonly IWebHostEnvironment _environment;

        public SupabaseStorageService(
            IConfiguration configuration,
            ILogger<SupabaseStorageService> logger,
            IHttpClientFactory httpClientFactory,
            IWebHostEnvironment environment)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _environment = environment;

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

            return await UploadImageAsync(storagePath, imageStream, contentType, allowLocalFallback: true);
        }

        public async Task<string> UploadDashboardUserProfileImageAsync(int userId, Stream imageStream, string fileName, string contentType)
        {
            var extension = NormalizeImageExtension(fileName, contentType);
            var storagePath = $"dashboard-users/{userId}/{Guid.NewGuid():N}{extension}";

            return await UploadImageAsync(storagePath, imageStream, contentType, allowLocalFallback: false);
        }

        private async Task<string> UploadImageAsync(string storagePath, Stream imageStream, string contentType, bool allowLocalFallback)
        {
            using var memoryStream = new MemoryStream();
            await imageStream.CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();

            _logger.LogInformation("Uploading {Bytes} bytes to '{Bucket}/{Path}'", bytes.Length, _bucketName, storagePath);

            var uploadUrl = $"{_supabaseUrl}/storage/v1/object/{_bucketName}/{storagePath}";

            try
            {
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
            catch (HttpRequestException ex) when (ex.InnerException is SocketException || ex.Message.Contains("No such host", StringComparison.OrdinalIgnoreCase))
            {
                if (!allowLocalFallback)
                {
                    throw new InvalidOperationException("Supabase is unreachable. Profile image was not saved.", ex);
                }

                _logger.LogWarning(ex, "Supabase is unreachable. Saving profile image locally instead.");
                return await SaveProfileImageLocallyAsync(storagePath, bytes);
            }
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

        public Task<(Stream? ImageStream, string ContentType)> DownloadProfileImageAsync(string imageUrl)
        {
            if (TryDecodeDataUri(imageUrl, out var dataStream, out var dataContentType))
            {
                return Task.FromResult<(Stream?, string)>((dataStream, dataContentType));
            }

            return DownloadProfileImageFromStorageAsync(imageUrl);
        }

        private async Task<(Stream? ImageStream, string ContentType)> DownloadProfileImageFromStorageAsync(string imageUrl)
        {
            var cleanUrl = imageUrl.Split('?')[0];

            if (IsLocalProfileImageUrl(cleanUrl))
            {
                var localPath = GetLocalPhysicalPath(cleanUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(localPath))
                {
                    _logger.LogWarning("Local profile image not found: {Path}", localPath);
                    return (null, "image/jpeg");
                }

                return (File.OpenRead(localPath), GetContentType(localPath));
            }

            // The bucket is private, so the "public" object route always 400s here.
            // Use "authenticated" instead — the service role key bypasses RLS on it.
            var downloadUrl = cleanUrl.Replace(
                "/storage/v1/object/public/",
                "/storage/v1/object/authenticated/",
                StringComparison.OrdinalIgnoreCase);

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _serviceRoleKey);

            try
            {
                using var response = await client.GetAsync(downloadUrl);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Supabase image download failed: {Status} for {Url}", response.StatusCode, downloadUrl);
                    return (null, "image/jpeg");
                }

                var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
                var bytes = await response.Content.ReadAsByteArrayAsync();
                return (new MemoryStream(bytes), contentType);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Supabase profile image could not be downloaded. Check Supabase:Url and network/DNS connectivity.");
                return (null, "image/jpeg");
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(ex, "Supabase profile image download timed out.");
                return (null, "image/jpeg");
            }
        }

        public async Task DeleteProfileImageAsync(string fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
                return;

            // Strip cache-bust param and extract path
            var cleanUrl = fileUrl.Split('?')[0];
            if (IsLocalProfileImageUrl(cleanUrl))
            {
                var localPath = GetLocalPhysicalPath(cleanUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(localPath))
                {
                    File.Delete(localPath);
                    _logger.LogInformation("Deleted local profile image: {Path}", localPath);
                }
                return;
            }

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

        private async Task<string> SaveProfileImageLocallyAsync(string storagePath, byte[] bytes)
        {
            var relativePath = Path.Combine("profile-images", storagePath);
            var physicalPath = GetLocalPhysicalPath(relativePath);
            var directory = Path.GetDirectoryName(physicalPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllBytesAsync(physicalPath, bytes);
            var publicPath = "/" + relativePath.Replace(Path.DirectorySeparatorChar, '/');
            _logger.LogInformation("Local profile image saved: {Path}", publicPath);
            return publicPath;
        }

        private string GetLocalPhysicalPath(string relativePath)
        {
            var webRoot = _environment.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRoot))
            {
                webRoot = Path.Combine(_environment.ContentRootPath, "wwwroot");
            }

            return Path.Combine(webRoot, relativePath);
        }

        private static bool TryDecodeDataUri(string imageUrl, out Stream? stream, out string contentType)
        {
            stream = null;
            contentType = "image/jpeg";

            if (string.IsNullOrWhiteSpace(imageUrl) || !imageUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var commaIndex = imageUrl.IndexOf(',');
            if (commaIndex < 0)
            {
                return false;
            }

            var header = imageUrl[5..commaIndex];
            var mediaType = header.Split(';')[0];
            if (!string.IsNullOrWhiteSpace(mediaType))
            {
                contentType = mediaType;
            }

            try
            {
                var bytes = Convert.FromBase64String(imageUrl[(commaIndex + 1)..]);
                stream = new MemoryStream(bytes);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static bool IsLocalProfileImageUrl(string imageUrl)
        {
            return imageUrl.StartsWith("/profile-images/", StringComparison.OrdinalIgnoreCase) ||
                   imageUrl.StartsWith("profile-images/", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetContentType(string path)
        {
            return Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".png" => "image/png",
                ".webp" => "image/webp",
                ".gif" => "image/gif",
                _ => "image/jpeg"
            };
        }

        private static string NormalizeImageExtension(string fileName, string contentType)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (extension is ".jpg" or ".jpeg" or ".png" or ".webp")
            {
                return extension;
            }

            return contentType.ToLowerInvariant() switch
            {
                "image/png" => ".png",
                "image/webp" => ".webp",
                _ => ".jpg"
            };
        }
    }
}
