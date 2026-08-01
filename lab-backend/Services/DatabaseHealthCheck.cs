using LabManagementAPI.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LabManagementAPI.Services;

public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly AppDbContext _context;

    public DatabaseHealthCheck(AppDbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("MySQL sẵn sàng.")
                : HealthCheckResult.Unhealthy("Không thể kết nối MySQL.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(
                "Kiểm tra MySQL thất bại.",
                exception);
        }
    }
}
