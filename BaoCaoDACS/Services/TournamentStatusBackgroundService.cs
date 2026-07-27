using BaoCaoDACS.Models;
using Microsoft.EntityFrameworkCore;

namespace BaoCaoDACS.Services;

public sealed class TournamentStatusBackgroundService : BackgroundService
{
    private static readonly TimeSpan DailyRunTime = new(0, 5, 0);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TournamentStatusBackgroundService> _logger;

    public TournamentStatusBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<TournamentStatusBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayUntilNextRun();

            try
            {
                await Task.Delay(delay, stoppingToken);
                await UpdateTournamentStatusesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể cập nhật trạng thái giải đấu tự động.");
            }
        }
    }

    private async Task UpdateTournamentStatusesAsync(CancellationToken cancellationToken)
    {
        var today = GetVietnamNow().Date;
        var tomorrow = today.AddDays(1);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var upcomingCount = await context.Tournaments
            .Where(t => t.Status != "Cancelled"
                && t.StartDate >= tomorrow
                && t.Status != "Upcoming")
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(t => t.Status, "Upcoming"),
                cancellationToken);

        var ongoingCount = await context.Tournaments
            .Where(t => t.Status != "Cancelled"
                && t.StartDate < tomorrow
                && t.EndDate >= today
                && t.Status != "Ongoing")
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(t => t.Status, "Ongoing"),
                cancellationToken);

        var completedCount = await context.Tournaments
            .Where(t => t.Status != "Cancelled"
                && t.EndDate < today
                && t.Status != "Completed")
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(t => t.Status, "Completed"),
                cancellationToken);

        _logger.LogInformation(
            "Đã cập nhật trạng thái giải đấu ngày {Date}: {Upcoming} sắp diễn ra, {Ongoing} đang diễn ra, {Completed} đã kết thúc.",
            today,
            upcomingCount,
            ongoingCount,
            completedCount);
    }

    private static TimeSpan GetDelayUntilNextRun()
    {
        var now = GetVietnamNow();
        var nextRun = now.Date.AddDays(1).Add(DailyRunTime);
        return nextRun - now;
    }

    private static DateTime GetVietnamNow()
    {
        try
        {
            var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);
        }
        catch (TimeZoneNotFoundException)
        {
            return DateTime.UtcNow.AddHours(7);
        }
        catch (InvalidTimeZoneException)
        {
            return DateTime.UtcNow.AddHours(7);
        }
    }
}
