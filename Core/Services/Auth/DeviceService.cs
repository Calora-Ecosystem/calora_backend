using BRB.Core.EF.Attributes;
using Core.Brokers.DbContext;
using Core.Entities.Auth;
using Core.Services.Auth.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Core.Services.Auth;

[Injectable]
public class DeviceService(AppDbContext dbContext)
{
    public async Task<Device> CreateOrUpdateDeviceAndGet(long userId, DeviceDto deviceDto)
    {
        Device device = null!;

        await dbContext.Transactional(async () =>
        {
            await dbContext.Devices.Where(x => x.UserId == userId)
                .ExecuteUpdateAsync(x => x.SetProperty(y => y.IsActive, false));

            device = (await dbContext.Devices
                .FirstOrDefaultAsync(x => x.UserId == userId && x.Key == deviceDto.Key)) ?? new Device()
            {
                Key = deviceDto.Key,
                UserId = userId
            };

            device.Name = deviceDto.Name;
            device.FcmToken = string.IsNullOrWhiteSpace(deviceDto.FcmToken) ? null : deviceDto.FcmToken.Trim();
            device.IsActive = true;

            device = dbContext.Update(device).Entity;
            await dbContext.SaveChangesAsync();
        });
        
        return device;
    }
}