namespace API.Application.Common;

public static class VietnamTime
{
    public static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);
    private static readonly TimeZoneInfo Zone = ResolveTimeZone();

    public static DateTime ToVietnamTime(DateTime dateTime)
    {
        var utc = ToUtcFromVietnamStandard(dateTime);
        return TimeZoneInfo.ConvertTimeFromUtc(utc, Zone);
    }

    public static DateTime UtcNowInVietnam() => DateTime.UtcNow + VietnamOffset;

    public static DateTimeOffset UtcNowOffsetInVietnam() => new(DateTime.UtcNow + VietnamOffset, VietnamOffset);

    public static DateTime VietnamDayStartToUtc(DateTime vietnamDate)
    {
        var dateInVietnam = GetVietnamDate(vietnamDate);
        var localMidnight = DateTime.SpecifyKind(dateInVietnam, DateTimeKind.Unspecified);
        return new DateTimeOffset(localMidnight, VietnamOffset).UtcDateTime;
    }

    public static DateTime VietnamDayEndExclusiveToUtc(DateTime vietnamDate)
    {
        return VietnamDayStartToUtc(vietnamDate).AddDays(1);
    }

    public static DateTime GetVietnamDate(DateTime dateTime)
    {
        return dateTime.Kind == DateTimeKind.Unspecified
            ? dateTime.Date
            : ToVietnamTime(dateTime).Date;
    }

    private static TimeZoneInfo ResolveTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.CreateCustomTimeZone(
                    "Vietnam Standard Time (Fixed)",
                    VietnamOffset,
                    "Vietnam Standard Time",
                    "Vietnam Standard Time");
            }
        }
    }

    public static DateTime ToUtcFromVietnamStandard(DateTime dateTime)
    {
        return dateTime.Kind switch
        {
            DateTimeKind.Utc => dateTime,
            DateTimeKind.Local => new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified), VietnamOffset).UtcDateTime,
            _ => new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified), VietnamOffset).UtcDateTime
        };
    }
}
