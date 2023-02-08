using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Xunit;

namespace Ical.Net.TestsTimeZone;

/// <summary>
/// Проверяет, что количество инстансов у повторяющихся событий вычисляется верно
/// для разных диапазонов, типов и часовых поясов событий
/// </summary>
public class OccurrencesTest
{
	[Theory]
	// SUMMARY:St Yek - CC Msk Jan 16-18 at 14:15 UTC+5  2023-01-16T09:15:00Z -- 2023-01-18T09:45:00Z
	// DTSTART;TZID=Russia/Ekaterinburg:20230116T141500 --> UTC
	// DTEND;TZID=Russia/Ekaterinburg:20230116T144500
	[InlineData("recurring_fixed_dates_tz_yek_msk", 3, "2023-01-16T14:15:00", "2023-01-18T14:45:00")]
	[InlineData("recurring_fixed_dates_tz_yek_msk", 2, "2023-01-16T14:15:00", "2023-01-18T14:00:00")]
	[InlineData("recurring_fixed_dates_tz_yek_msk", 1, "2023-01-16T14:45:01", "2023-01-18T14:00:00")]
	[InlineData("recurring_fixed_dates_tz_yek_msk", 2, "2023-01-16T14:45:01", "2023-01-18T14:15:01")]
	[InlineData("recurring_fixed_dates_tz_yek_msk", 2, "2023-01-16T14:45:00", "2023-01-17T14:15:00")]
	[InlineData("recurring_fixed_dates_tz_yek_msk", 1, "2023-01-17T14:45:00", "2023-01-18T14:00:00")]

	// SUMMARY:Jan 12-14 13:00-13:30 MSK
	// DTSTART;TZID=Europe/Moscow:20230112T130000
	// DTEND;TZID=Europe/Moscow:20230112T133000

	[InlineData("recurring_fixed_dates_tz_msk_msk", 3, "2023-01-12T13:00:00", "2023-01-14T13:30:00")]
	[InlineData("recurring_fixed_dates_tz_msk_msk", 2, "2023-01-13T13:00:00", "2023-01-14T13:30:00")]
	[InlineData("recurring_fixed_dates_tz_msk_msk", 1, "2023-01-14T13:00:00", "2023-01-14T13:30:00")]
	[InlineData("recurring_fixed_dates_tz_msk_msk", 0, "2023-01-12T10:00:00", "2023-01-12T12:59:59")]
	[InlineData("recurring_fixed_dates_tz_msk_msk", 0, "2023-01-14T13:30:01", "2023-01-14T13:59:59")]


	public void T01_OccurrencesSimpleRecurringEvent(string source, int occNum, string beginS, string endS)
	{
		DateTime begin = DateTime.SpecifyKind(DateTime.Parse(beginS), DateTimeKind.Local); //.AddSeconds(-1);
		DateTime end = DateTime.SpecifyKind(DateTime.Parse(endS), DateTimeKind.Local); //.AddSeconds(1);

		CalendarEvent result = Calendar.Load(Samples.Recurring[source].body).Events.First();
		var calBegin = new CalDateTime(begin, result.DtStart.TzId);
		var calEnd = new CalDateTime(end, result.DtEnd.TzId);

		DateTime until = result.RecurrenceRules[0].Until;

		// checking if this is eternal event
		// iCal.NET.Calendar.Load method puts DateTime.MinValue in the UNTIL field
		// if the event has no end
		if (until != DateTime.MinValue)
		{
			IDateTime calUntil = new CalDateTime(until, "UTC");
			calUntil = calUntil.ToTimeZone(result.DtStart.TzId);
			until = calUntil.Value;
		}

		result.RecurrenceRules[0].Until = until;

		var resultOcc = result.GetOccurrences(calBegin, calEnd);

		Assert.Equal(occNum, resultOcc.Count);
	}

	[Theory]
	// SUMMARY:ST-CC Jan 12-14  15:00 - 13:00   UNTIL=20230114T100000Z
	// DTSTART;TZID=Europe/Moscow:20230112T130000
	// DTEND;TZID=Europe/Moscow:20230112T133000
	// EXDATE;TZID=Europe/Moscow:20230113T130000

	[InlineData("recurring_fixed_dates_tz_msk_msk_exdate", 2, "2023-01-12T13:00:00", "2023-01-14T13:30:00")]
	[InlineData("recurring_fixed_dates_tz_msk_msk_exdate", 1, "2023-01-12T13:00:00", "2023-01-13T20:30:00")]
	[InlineData("recurring_fixed_dates_tz_msk_msk_exdate", 1, "2023-01-12T15:00:00", "2023-01-14T20:30:00")]

	// [InlineData("recurring_fixed_dates_tz_yek_msk_exdate")]
	// [InlineData("recurring_fixed_dates_tz_eu_west_msk_exdate")]
	// [InlineData("recurring_fixed_dates_tz_na_east_msk_exdate")]
	// [InlineData("recurring_all_day_msk_msk_exdate")]
	// [InlineData("recurring_all_day_yek_msk_exdate")]
	[InlineData("recurring_all_day_eu_west_msk_exdate", 0, "2023-01-23T00:00:00", "2023-01-24T00:00:00")]
	// [InlineData("recurring_all_day_na_east_msk_exdate")]
	public void T04_RecurringExDate(string sourceEvent, int occNum, string beginS, string endS)
	{
		DateTime begin = DateTime.SpecifyKind(DateTime.Parse(beginS), DateTimeKind.Local); //.AddSeconds(-1);
		DateTime end = DateTime.SpecifyKind(DateTime.Parse(endS), DateTimeKind.Local); //.AddSeconds(1);

		CalendarEvent result = Calendar.Load(Samples.RecurringExDate[sourceEvent].body).Events.First();
		var calBegin = new CalDateTime(begin, result.DtStart.TzId);
		var calEnd = new CalDateTime(end, result.DtEnd.TzId);

		DateTime until = result.RecurrenceRules[0].Until;

		// checking if this is eternal event
		// iCal.NET.Calendar.Load method puts DateTime.MinValue in the UNTIL field
		// if the event has no end
		if (until != DateTime.MinValue)
		{
			IDateTime calUntil = new CalDateTime(until, "UTC");
			calUntil = calUntil.ToTimeZone(result.DtStart.TzId);
			until = calUntil.Value;
		}

		result.RecurrenceRules[0].Until = until;

		var resultOcc = result.GetOccurrences(calBegin, calEnd);

		Assert.Equal(occNum, resultOcc.Count);
	}
}
