using Ical.Net.Utility;
using Ical.Net.DataTypes;
using NodaTime;
using Xunit;

namespace Ical.Net.TestsTimeZone;

/// <summary>
/// Tests conversion datetime with TzID to/from UTC
/// All sample data are provided in the class
/// </summary>
public class TimeZoneConversionTest
{
	/// <summary>
	/// Tests if the valid time zone can be created with the specified TzId,
	/// and conversion based on this Id is correct
	/// </summary>
	/// <param name="tzId">TZID</param>
	/// <param name="originalInTz">Original time in time zone</param>
	/// <param name="utc">UTC equivalent</param>
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
		DateTimeZone? dtz = DateUtil.GetZone(tzId);
		Assert.NotNull(dtz);

		DateTime originalDateTime = DateTime.SpecifyKind(DateTime.Parse(originalInTz), DateTimeKind.Local);
		var zonedOriginal = DateUtil.ToZonedDateTimeLeniently(originalDateTime, tzId);
		var converted = zonedOriginal.WithZone(DateUtil.GetZone("UTC"));

		DateTime originalConvertedBack = converted.ToDateTimeUnspecified();
		DateTime utcDateTimeUnspecified = DateTime.SpecifyKind(DateTime.Parse(utc), DateTimeKind.Local);
		Assert.Equal(originalConvertedBack, utcDateTimeUnspecified);
	}

	/// <summary>
	/// Creates <see cref="CalDateTime"/> instance with the specified TzId,
	/// and tests if <see cref="CalDateTime.ToTimeZone"/> converts to UTC correctly
	/// </summary>
	/// <param name="tzId">TZID</param>
	/// <param name="originalInTz">Original time in time zone</param>
	/// <param name="utc">UTC equivalent</param>
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
	public void IDateTimeToTimeZoneTest_TzToUtc(string tzId, string originalInTz, string utc)
	{
		DateTime originalDateTime = DateTime.SpecifyKind(DateTime.Parse(originalInTz), DateTimeKind.Local);
		CalDateTime calOriginal = new CalDateTime(originalDateTime, tzId);

		var calConvertedToUtc = calOriginal.ToTimeZone("UTC");
		DateTime utcDateTime = DateTime.SpecifyKind(DateTime.Parse(utc), DateTimeKind.Utc);
		CalDateTime calExpected = new CalDateTime(utcDateTime, "UTC");
		Assert.Equal(calExpected, calConvertedToUtc);
	}

		/// <summary>
	/// Creates <see cref="CalDateTime"/> instance in UTC
	/// and tests if <see cref="CalDateTime.ToTimeZone"/> converts to the specified TzId, correctly
	/// </summary>
	/// <param name="tzId">TZID</param>
	/// <param name="originalUtcS">Original time in UTC</param>
	/// <param name="expectedTzS">Converted to Time Zone</param>
	[Theory]
	[InlineData("UTC",                "2023-02-06T11:55:35", "2023-02-06T11:55:35")]
	[InlineData("WET",                "2023-02-06T11:55:35", "2023-02-06T11:55:35")]		//  +0
	[InlineData("CET",                "2023-02-06T10:55:35", "2023-02-06T11:55:35")]		//  +1
	[InlineData("EET",                "2023-02-06T09:55:35", "2023-02-06T11:55:35")]		//  +2
	[InlineData("Europe/Kaliningrad", "2023-02-06T09:55:35", "2023-02-06T11:55:35")]		//  +2
	[InlineData("Europe/Moscow",      "2023-02-06T08:55:35", "2023-02-06T11:55:35")]		//  +3
	[InlineData("Europe/Samara",      "2023-02-06T07:55:35", "2023-02-06T11:55:35")]		//  +4
	[InlineData("Asia/Yekaterinburg", "2023-02-06T06:55:35", "2023-02-06T11:55:35")]		//  +5
	[InlineData("Asia/Omsk",          "2023-02-06T05:55:35", "2023-02-06T11:55:35")]		//  +6
	[InlineData("Asia/Novosibirsk",   "2023-02-06T04:55:35", "2023-02-06T11:55:35")]		//  +7
	[InlineData("Asia/Krasnoyarsk",   "2023-02-06T04:55:35", "2023-02-06T11:55:35")]		//  +7
	[InlineData("Asia/Irkutsk",       "2023-02-06T03:55:35", "2023-02-06T11:55:35")]		//  +8
	[InlineData("Asia/Yakutsk",       "2023-02-06T02:55:35", "2023-02-06T11:55:35")]		//  +9
	[InlineData("Asia/Vladivostok",   "2023-02-06T01:55:35", "2023-02-06T11:55:35")]		// +10
	[InlineData("Asia/Magadan",       "2023-02-06T00:55:35", "2023-02-06T11:55:35")]		// +11
	[InlineData("Asia/Kamchatka",     "2023-02-05T23:55:35", "2023-02-06T11:55:35")]		// +12
	public void IDateTimeToTimeZoneTest_UtcToTz(string tzId, string originalUtcS, string expectedTzS)
	{
		DateTime originalDateTimeUtc = DateTime.SpecifyKind(DateTime.Parse(originalUtcS), DateTimeKind.Utc);
		CalDateTime calOriginalUtc = new CalDateTime(originalDateTimeUtc, "UTC");

		var calConvertedToTz = calOriginalUtc.ToTimeZone(tzId);

		DateTime expectedTzDateTime = DateTime.SpecifyKind(DateTime.Parse(expectedTzS), DateTimeKind.Local);
		CalDateTime calExpected = new CalDateTime(expectedTzDateTime, tzId);

		Assert.Equal(calExpected, calConvertedToTz);
	}

}
