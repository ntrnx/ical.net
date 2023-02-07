using Ical.Net.CalendarComponents;
using Ical.Net.Utility;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using NodaTime;
using Xunit;

namespace Ical.Net.TestsTimeZone;

public class TimeZoneTest
{
    private readonly TimeZoneCreator _tzc;

    public TimeZoneTest()
    {
        using var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder
                   .SetMinimumLevel(LogLevel.Trace)
                   .AddConsole());
        ILogger logger = loggerFactory.CreateLogger<TimeZoneTest>();
        _tzc = new TimeZoneCreator(logger);
    }

    [Theory]
    [InlineData("single_fixed_dates_tz_eu_west", "2023-01-25T06:00:00", "2023-01-25T06:00:00")]

    // One second to finish standard (winter) time period => need to convert according to ST
    // => Take off standard time UTC offset
    [InlineData("single_fixed_dates_tz_eu_west", "2023-03-26T01:59:59", "2023-03-26T01:59:59")]

    // Change from STANDARD to DAYLIGHT: DTSTART:19710101T020000 - is time in STANDARD period!
    // Microsoft considers that STANDARD time already does not have 2:00 am, i.e. 2:00 is not valid time
    // it counts 1:59:59 -> 3:00:00
    [InlineData("single_fixed_dates_tz_eu_west", "2023-03-26T02:00:00", "invalid time")]

    // 2:15 not valid either, which is understandable and correct
    [InlineData("single_fixed_dates_tz_eu_west", "2023-03-26T02:15:00", "invalid time")]

    [InlineData("single_fixed_dates_tz_eu_west", "2023-03-26T03:00:00", "2023-03-26T02:00:00")]
    [InlineData("single_fixed_dates_tz_eu_west", "2023-06-07T12:00:00", "2023-06-07T11:00:00")]

    // We CANNOT specify one second to finish daylight saving period, because:
    // Change from DAYLIGHT to STANDARD: DTSTART:19710101T030000 - is time in DAYLIGHT period!
    // Miscrosoft considers that DAYLIGHT time does not have 3:00 am, i.e. 3:00 am belongs to STANDARD
    // Time flow: 1:59:59 (DST)->2:00 (DST) .. 2:59:59 (DST) -> 2:00:00 (ST) .. 2:59:59 (ST)->3:00:00 (STANDARD)
    // But if we take DateTime  time with Kind.Unspecified instance on last Sunday of October
    // between [2:00-2:59:59] it will be considered as STANDARD
    // => there is 1 hour gap in UTC counting, if converting continuous local time to UTC
    [InlineData("single_fixed_dates_tz_eu_west", "2023-10-29T01:59:59", "2023-10-29T00:59:59")]
    [InlineData("single_fixed_dates_tz_eu_west", "2023-10-29T02:00:00", "2023-10-29T02:00:00")]
    [InlineData("single_fixed_dates_tz_eu_west", "2023-10-29T02:59:59", "2023-10-29T02:59:59")]
    [InlineData("single_fixed_dates_tz_eu_west", "2023-10-29T03:00:00", "2023-10-29T03:00:00")]


    [InlineData("single_fixed_dates_tz_msk", "2023-10-29T03:00:00", "2023-10-29T00:00:00")]
    [InlineData("single_fixed_dates_tz_russian", "2023-10-29T03:00:00", "2023-10-29T00:00:00")]
    public void CreateTimeZoneFromEvent(string tz, string originalInTz, string convertedToUtc)
    {
        CalendarEvent calendarEvent = Ical.Net.Calendar.Load(Samples.Single[tz].body).Events.First();

        VTimeZone vTimeZone = calendarEvent.Calendar.TimeZones.First();
        TimeZoneInfo? ctz = _tzc.CreateTimeZone(vTimeZone);

        DateTime sourceDateTz = DateTime.SpecifyKind(DateTime.Parse(originalInTz), DateTimeKind.Unspecified);
        DateTime expectedTz;
        try
        {
            expectedTz = TimeZoneInfo.ConvertTimeToUtc(sourceDateTz, ctz);
            DateTime dateUtc = DateTime.SpecifyKind(DateTime.Parse(convertedToUtc), DateTimeKind.Utc);

            Assert.Equal(expectedTz, dateUtc);
        }
        catch (ArgumentException)
        {
            Assert.Equal("invalid time", convertedToUtc, ignoreCase:true);
        }
    }

	[Theory]
	// => there is 1 hour gap in UTC counting, if converting continuous local time to UTC
	[InlineData("single_fixed_dates_tz_eu_west", "2023-10-29T01:59:59", "2023-10-29T00:59:59")]

	// UTC 1:30 and UTC 2:30 will be converted to 2:30 Standard time!!!
	[InlineData("single_fixed_dates_tz_eu_west", "2023-10-29T02:30:00", "2023-10-29T01:30:00")]
	[InlineData("single_fixed_dates_tz_eu_west", "2023-10-29T02:30:00", "2023-10-29T02:30:00")]

	[InlineData("single_fixed_dates_tz_eu_west", "2023-10-29T02:00:00", "2023-10-29T02:00:00")]
	[InlineData("single_fixed_dates_tz_eu_west", "2023-10-29T02:59:59", "2023-10-29T02:59:59")]
	[InlineData("single_fixed_dates_tz_eu_west", "2023-10-29T03:00:00", "2023-10-29T03:00:00")]
	public void ConvertToUtcFromTimeZone(string tz, string expectedTzS, string originalUtcS)
	{
		CalendarEvent calendarEvent = Ical.Net.Calendar.Load(Samples.Single[tz].body).Events.First();

		VTimeZone vTimeZone = calendarEvent.Calendar.TimeZones.First();
		TimeZoneInfo? ctz = _tzc.CreateTimeZone(vTimeZone);

		DateTime originalUtcD = DateTime.SpecifyKind(DateTime.Parse(originalUtcS), DateTimeKind.Utc);

		DateTime actualTzD = TimeZoneInfo.ConvertTimeFromUtc(originalUtcD, ctz);
		DateTime expectedDt = DateTime.SpecifyKind(DateTime.Parse(expectedTzS), DateTimeKind.Unspecified);

		Assert.Equal(expectedDt, actualTzD);
	}


    [Theory]
    [InlineData("recurring_fixed_dates_tz_yek_msk", "2023-10-29T03:00:00", "2023-10-28T22:00:00")]

    // America/New-York time zone, behind UTc
    [InlineData("recurring_fixed_dates_tz_na_east_msk", "2023-01-29T14:15:00", "2023-01-29T19:15:00")]

    // America/New-York change from Standard to Daylight, [2:00-2:59:59] am does not exist
    [InlineData("recurring_fixed_dates_tz_na_east_msk", "2023-03-12T01:59:59", "2023-03-12T06:59:59")]
    [InlineData("recurring_fixed_dates_tz_na_east_msk", "2023-03-12T02:00:00", "invalid time")]
    [InlineData("recurring_fixed_dates_tz_na_east_msk", "2023-03-12T02:59:59", "invalid time")]
    [InlineData("recurring_fixed_dates_tz_na_east_msk", "2023-03-12T03:00:00", "2023-03-12T07:00:00")]

    // Daylight time
    [InlineData("recurring_fixed_dates_tz_na_east_msk", "2023-07-17T14:15:00", "2023-07-17T18:15:00")]

    // America/New-York change from Daylight to Standard, [1:00-2:00] are considered as Standard time
    [InlineData("recurring_fixed_dates_tz_na_east_msk", "2023-11-05T00:59:59", "2023-11-05T04:59:59")]
    [InlineData("recurring_fixed_dates_tz_na_east_msk", "2023-11-05T01:00:00", "2023-11-05T06:00:00")]
    [InlineData("recurring_fixed_dates_tz_na_east_msk", "2023-11-05T01:59:59", "2023-11-05T06:59:59")]
    [InlineData("recurring_fixed_dates_tz_na_east_msk", "2023-11-05T02:00:00", "2023-11-05T07:00:00")]

    // Australia time zone has Standard (winter) time in summer.
    // Daylight time is from October to April.
    // Daylight
    [InlineData("recurring_fixed_dates_tz_australia_msk", "2023-01-29T14:15:00", "2023-01-29T03:15:00")]
    // Daylight to Standard transition moment
    [InlineData("recurring_fixed_dates_tz_australia_msk", "2023-04-02T03:00:00", "2023-04-01T17:00:00")]
    // Standard time
    [InlineData("recurring_fixed_dates_tz_australia_msk", "2023-07-29T14:15:00", "2023-07-29T04:15:00")]
    // Standard to Daylight transition moment
    [InlineData("recurring_fixed_dates_tz_australia_msk", "2023-10-01T02:00:00", "invalid time")]
    [InlineData("recurring_fixed_dates_tz_australia_msk", "2023-10-01T02:15:00", "invalid time")]
    public void CreateTimeZoneFromRecurringEvent(string tz, string originalInTz, string convertedToUtc)
    {
        CalendarEvent calendarEvent = Ical.Net.Calendar.Load(Samples.Recurring[tz].body).Events.First();

        VTimeZone vTimeZone = calendarEvent.Calendar.TimeZones.First();
        TimeZoneInfo? ctz = _tzc.CreateTimeZone(vTimeZone);

        DateTime sourceDateTz = DateTime.SpecifyKind(DateTime.Parse(originalInTz), DateTimeKind.Unspecified);
        DateTime expectedTz;
        try
        {
            expectedTz = TimeZoneInfo.ConvertTimeToUtc(sourceDateTz, ctz);
            DateTime dateUtc = DateTime.SpecifyKind(DateTime.Parse(convertedToUtc), DateTimeKind.Utc);

            Assert.Equal(expectedTz, dateUtc);
        }
        catch (ArgumentException)
        {
            Assert.Equal("invalid time", convertedToUtc, ignoreCase:true);
        }
    }

	[Theory]
	[InlineData("UTC",                "2023-02-06T11:55:35", "2023-02-06T11:55:35")]
	[InlineData("WET",                "2023-02-06T11:55:35", "2023-02-06T11:55:35")]		//  +0
	[InlineData("CET",                "2023-02-06T11:55:35", "2023-02-06T10:55:35")]		//  +1
	[InlineData("EET",                "2023-02-06T11:55:35", "2023-02-06T09:55:35")]		//  +2
	[InlineData("Europe/Kaliningrad", "2023-02-06T11:55:35", "2023-02-06T09:55:35")]		//  +2
	[InlineData("Europe/Moscow",      "2023-02-06T11:55:35", "2023-02-06T08:55:35")]		//  +3
	[InlineData("Europe/Samara",      "2023-02-06T11:55:35", "2023-02-06T07:55:35")]		//  +4
	[InlineData("Asia/Yekaterinburg", "2023-02-06T11:55:35", "2023-02-06T06:55:35")]		//  +5
	[InlineData("Asia/Omsk",          "2023-02-06T11:55:35", "2023-02-06T05:55:35")]		//  +6
	[InlineData("Asia/Novosibirsk",   "2023-02-06T11:55:35", "2023-02-06T04:55:35")]		//  +7
	[InlineData("Asia/Krasnoyarsk",   "2023-02-06T11:55:35", "2023-02-06T04:55:35")]		//  +7
	[InlineData("Asia/Irkutsk",       "2023-02-06T11:55:35", "2023-02-06T03:55:35")]		//  +8
	[InlineData("Asia/Yakutsk",       "2023-02-06T11:55:35", "2023-02-06T02:55:35")]		//  +9
	[InlineData("Asia/Vladivostok",   "2023-02-06T11:55:35", "2023-02-06T01:55:35")]		// +10
	[InlineData("Asia/Magadan",       "2023-02-06T11:55:35", "2023-02-06T00:55:35")]		// +11
	[InlineData("Asia/Kamchatka",     "2023-02-06T11:55:35", "2023-02-05T23:55:35")]		// +12
	public void TimeZoneIdTest(string tzId, string originalInTz, string utc)
	{
		/*
		DateTimeZone? dtz = DateTimeZoneProviders.Tzdb.GetZoneOrNull(tzId);
		if (dtz is null)
		{
			dtz = NodaTime.Xml.XmlSerializationSettings.DateTimeZoneProvider.GetZoneOrNull(tzId);
		}
		*/
		DateTimeZone? dtz = DateUtil.GetZone(tzId);
		Assert.NotNull(dtz);

		DateTime originalDateTime = DateTime.SpecifyKind(DateTime.Parse(originalInTz), DateTimeKind.Local);
		var zonedOriginal = DateUtil.ToZonedDateTimeLeniently(originalDateTime, tzId);
		var converted = zonedOriginal.WithZone(DateUtil.GetZone("UTC"));

		DateTime originalConvertedBack = converted.ToDateTimeUnspecified();
		DateTime utcDateTimeUnspecified = DateTime.SpecifyKind(DateTime.Parse(utc), DateTimeKind.Local);
		Assert.Equal(originalConvertedBack, utcDateTimeUnspecified);
	}
}
