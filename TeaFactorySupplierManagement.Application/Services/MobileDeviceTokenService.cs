using Microsoft.EntityFrameworkCore;
using TeaFactorySupplierManagement.Application.DTOs.MobileDeviceTokens;
using TeaFactorySupplierManagement.Application.Interfaces;
using TeaFactorySupplierManagement.Domain.Entities;
using TeaFactorySupplierManagement.Infrastructure.Data;

namespace TeaFactorySupplierManagement.Application.Services
{
    public class MobileDeviceTokenService : IMobileDeviceTokenService
    {
        private readonly ApplicationDbContext _context;
        private readonly IFirebasePushService _firebasePushService;

        public MobileDeviceTokenService(ApplicationDbContext context, IFirebasePushService firebasePushService)
        {
            _context = context;
            _firebasePushService = firebasePushService;
        }

        public async Task SaveTokenAsync(int regNo, SaveMobileDeviceTokenDto request)
        {
            var token = request.FcmToken.Trim();
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException("FCM token is required.");
            }

            var supplierExists = await _context.RegSuppliers.AnyAsync(x => x.RegNo == regNo);
            if (!supplierExists)
            {
                throw new ArgumentException("Supplier not found.");
            }

            var existing = await _context.MobileDeviceTokens
                .FirstOrDefaultAsync(x => x.FcmToken == token);

            if (existing == null)
            {
                _context.MobileDeviceTokens.Add(new MobileDeviceToken
                {
                    RegNo = regNo,
                    FcmToken = token,
                    Platform = CleanOptional(request.Platform),
                    DeviceId = CleanOptional(request.DeviceId),
                    IsActive = true,
                    LastSeenAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                existing.RegNo = regNo;
                existing.Platform = CleanOptional(request.Platform);
                existing.DeviceId = CleanOptional(request.DeviceId);
                existing.IsActive = true;
                existing.LastSeenAt = DateTime.UtcNow;
                existing.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            await _firebasePushService.SubscribeToAllSuppliersTopicAsync(token);
        }

        public async Task DeactivateTokenAsync(int regNo, string fcmToken)
        {
            var token = fcmToken.Trim();
            if (string.IsNullOrWhiteSpace(token))
            {
                return;
            }

            var existing = await _context.MobileDeviceTokens
                .FirstOrDefaultAsync(x => x.RegNo == regNo && x.FcmToken == token);

            if (existing == null)
            {
                return;
            }

            existing.IsActive = false;
            existing.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _firebasePushService.UnsubscribeFromAllSuppliersTopicAsync(token);
        }

        private static string? CleanOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
