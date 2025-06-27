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
        var device = (await dbContext.Devices
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Key == deviceDto.Key)) ?? new Device()
        {
            Key = deviceDto.Key,
        };

        device.Name = deviceDto.Name;
        device.FcmToken = deviceDto.FcmToken;

        device = dbContext.Update(device).Entity;
        await dbContext.SaveChangesAsync();

        return device;
    }
}