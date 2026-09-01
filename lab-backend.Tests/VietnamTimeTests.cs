using System;
using LabManagementAPI.Services;
using LabManagementAPI.Models;
using Xunit;

namespace LabManagementAPI.Tests;

public class VietnamTimeTests
{
    [Fact]
    public void Today_rolls_over_at_vietnam_midnight_instead_of_utc_midnight()
    {
        var beforeMidnight = new DateTime(2026, 8, 29, 16, 59, 59, DateTimeKind.Utc);
        var midnight = new DateTime(2026, 8, 29, 17, 0, 0, DateTimeKind.Utc);

        Assert.Equal(new DateTime(2026, 8, 29), VietnamTime.Today(beforeMidnight));
        Assert.Equal(new DateTime(2026, 8, 30), VietnamTime.Today(midnight));
    }

    [Fact]
    public void StartOfDayUtc_is_the_previous_day_at_17_hours_utc()
    {
        var result = VietnamTime.StartOfDayUtc(new DateTime(2026, 8, 30));

        Assert.Equal(new DateTime(2026, 8, 29, 17, 0, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public void Date_treats_offsetless_database_timestamps_as_utc()
    {
        var databaseValue = new DateTime(2026, 8, 29, 17, 0, 0, DateTimeKind.Unspecified);

        Assert.Equal(new DateTime(2026, 8, 30), VietnamTime.Date(databaseValue));
    }

    [Fact]
    public void Status_helpers_accept_legacy_values_but_return_canonical_codes_and_vietnamese_labels()
    {
        Assert.Equal(EquipmentStatuses.Available, EquipmentStatuses.Normalize("Rảnh"));
        Assert.Equal(EquipmentStatuses.Broken, EquipmentStatuses.Normalize("broken"));
        Assert.Equal("Đã duyệt, chờ bàn giao", StatusCodeMap.Label(BorrowStatuses.Approved));
        Assert.Equal("Hỏng", StatusCodeMap.Label(EquipmentStatuses.Broken));
        Assert.Equal("Đang bảo hành", StatusCodeMap.Label("Bảo hành"));
    }
}
