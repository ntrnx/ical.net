using Ical.Net.CalendarComponents;
using Xunit;

namespace Ical.Net.TestsTimeZone;

public class OccurrencesTest
{
	[Theory]
	// SUMMARY:St Yek - CC Msk Jan 16-18 at 14:15 UTC+5  2023-01-16T09:15:00Z -- 2023-01-18T09:45:00Z
	// DTSTART;TZID=Russia/Ekaterinburg:20230116T141500 --> UTC
	// DTEND;TZID=Russia/Ekaterinburg:20230116T144500

	// These 3 tests below have succeeded => Looks like beging and end are treated as UTC ...
	[InlineData("recurring_fixed_dates_tz_yek_msk", 3, "2023-01-16T09:15:00", "U", "2023-01-18T09:45:00", "U")]
	[InlineData("recurring_fixed_dates_tz_yek_msk", 3, "2023-01-16T09:15:00", "L", "2023-01-18T09:45:00", "L")]
	[InlineData("recurring_fixed_dates_tz_yek_msk", 3, "2023-01-16T09:15:00", "X", "2023-01-18T09:45:00", "X")]
	public void T01_OccurrencesSimpleRecurringEvent(string source, int occNum, string beginS, string kindB, string endS, string kindE)
	{
		DateTimeKind kindBegin = kindB switch
			{
				"U" => DateTimeKind.Utc,
				"L" => DateTimeKind.Local,
				_ => DateTimeKind.Unspecified
			};
		DateTime begin = DateTime.SpecifyKind(DateTime.Parse(beginS), kindBegin);

		DateTimeKind kindEnd = kindE switch
			{
				"U" => DateTimeKind.Utc,
				"L" => DateTimeKind.Local,
				_ => DateTimeKind.Unspecified
			};
		DateTime end = DateTime.SpecifyKind(DateTime.Parse(endS), kindEnd);

		CalendarEvent result = Calendar.Load(Samples.Recurring[source].body).Events.First();
		var resultOcc = result.GetOccurrences(begin, end);

		Assert.Equal(occNum, resultOcc.Count);
	}

	[Theory]
	// [InlineData("recurring_fixed_dates_tz_msk_msk_exdate")]
	// [InlineData("recurring_fixed_dates_tz_yek_msk_exdate")]
	// [InlineData("recurring_fixed_dates_tz_eu_west_msk_exdate")]
	// [InlineData("recurring_fixed_dates_tz_na_east_msk_exdate")]
	// [InlineData("recurring_all_day_msk_msk_exdate")]
	// [InlineData("recurring_all_day_yek_msk_exdate")]
	[InlineData("recurring_all_day_eu_west_msk_exdate")]
	// [InlineData("recurring_all_day_na_east_msk_exdate")]
	public void T04_RecurringExDate(string eventType)
	{
		var events = Calendar.Load(Samples.RecurringExDate[eventType].body).Events;
		CalendarEvent result = events.First();

		var expectedResult = Samples.RecurringExDate[eventType];

		DateTime begin = DateTime.SpecifyKind(new DateTime(2023,01,23), DateTimeKind.Utc);
		DateTime end   = DateTime.SpecifyKind(new DateTime(2023,01,24), DateTimeKind.Utc);
		var occurrences = result.GetOccurrences(begin, end);
		Assert.Equal(0, occurrences?.Count ?? 0);
	}
}
