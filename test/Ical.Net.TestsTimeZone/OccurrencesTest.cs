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
		DateTime begin = DateTime.SpecifyKind(DateTime.Parse(beginS), DateTimeKind.Local).AddSeconds(-1);
		DateTime end = DateTime.SpecifyKind(DateTime.Parse(endS), DateTimeKind.Local).AddSeconds(1);

		CalendarEvent calendarEvent = Calendar.Load(Samples.Recurring[source].body).Events.First();
		var calBegin = new CalDateTime(begin, calendarEvent.DtStart.TzId);
		var calEnd = new CalDateTime(end, calendarEvent.DtEnd.TzId);

		// RecurrenceRules[0].Until is always UTC
		DateTime until = calendarEvent.RecurrenceRules[0].Until;

		// checking if this is eternal event
		// iCal.NET.Calendar.Load method puts DateTime.MinValue in the UNTIL field
		// if the event has no end
		if (until != DateTime.MinValue)
		{
			if (calendarEvent.IsAllDay)
			{
				// CGP has a bug: for ALL-DAY event UNTIL is kept in UTC as YYYY-MM-DD T HH:00:00Z
				// but not adjusted to real time zone which is wrong!
				until = DateTime.SpecifyKind(until, DateTimeKind.Local);
			}
			else
			{
				IDateTime calUntil = new CalDateTime(until, "UTC");
				calUntil = calUntil.ToTimeZone(calendarEvent.DtStart.TzId);
				until = calUntil.Value;
			}
		}

		calendarEvent.RecurrenceRules[0].Until = until;

		var resultOcc = calendarEvent.GetOccurrences(calBegin, calEnd);

		Assert.Equal(occNum, resultOcc.Count);
	}

	[Theory]
	// SUMMARY:ST-CC Jan 12-14  13:00 - 13:30   UNTIL=20230114T100000Z
	// DTSTART;TZID=Europe/Moscow:20230112T130000
	// DTEND;TZID=Europe/Moscow:20230112T133000
	// EXDATE;TZID=Europe/Moscow:20230113T130000

	[InlineData("recurring_fixed_dates_tz_msk_msk_exdate", 2, "2023-01-12T10:00:00", "2023-01-14T10:30:00")]
	[InlineData("recurring_fixed_dates_tz_msk_msk_exdate", 1, "2023-01-12T10:00:00", "2023-01-13T17:30:00")]
	[InlineData("recurring_fixed_dates_tz_msk_msk_exdate", 1, "2023-01-12T12:00:00", "2023-01-14T17:30:00")]
	[InlineData("recurring_fixed_dates_tz_msk_msk_exdate", 1, "2023-01-12T10:30:01", "2023-01-14T17:30:00")]
	[InlineData("recurring_fixed_dates_tz_msk_msk_exdate", 2, "2023-01-12T10:30:00", "2023-01-14T17:30:00")]

	// SUMMARY:SAM-MSK Feb 2 -> every 2 days 14:15-45 no UNTIL (2, 4 (changed to 5), 8, 10, 12, 18)
	// DTSTART;TZID=Europe/Samara:20230202T141500
	// DTEND;TZID=Europe/Samara:20230202T144500
	// EXDATE;TZID=Europe/Samara:20230206T141500,20230214T141500,20230216T141500
	[InlineData("recurring_fixed_dates_tz_samara_msk_changed", 2, "2023-02-02T10:15:00", "2023-02-04T10:45:00")]
	[InlineData("recurring_fixed_dates_tz_samara_msk_changed", 5, "2023-02-03T10:15:00", "2023-02-18T10:45:00")]
	[InlineData("recurring_fixed_dates_tz_samara_msk_changed", 6, "2023-02-02T10:45:00", "2023-02-18T10:45:00")]
	[InlineData("recurring_fixed_dates_tz_samara_msk_changed", 5, "2023-02-02T10:45:01", "2023-02-18T10:45:00")]

	// [InlineData("recurring_fixed_dates_tz_yek_msk_exdate")]
	// [InlineData("recurring_fixed_dates_tz_eu_west_msk_exdate")]
	// [InlineData("recurring_fixed_dates_tz_na_east_msk_exdate")]
	// [InlineData("recurring_all_day_msk_msk_exdate")]
	// [InlineData("recurring_all_day_yek_msk_exdate")]
	[InlineData("recurring_all_day_eu_west_msk_exdate", 0, "2023-01-22T19:00:00", "2023-01-23T19:00:00")]
	// [InlineData("recurring_all_day_na_east_msk_exdate")]
	public void T04_RecurringExDateWithUtcBoundaries(string sourceEvent, int occNum, string beginS, string utcEndS)
	{
		DateTime begin = DateTime.SpecifyKind(DateTime.Parse(beginS), DateTimeKind.Utc).AddSeconds(-1);
		DateTime end = DateTime.SpecifyKind(DateTime.Parse(utcEndS), DateTimeKind.Utc).AddSeconds(1);

		CalendarEvent calendarEvent;
		if (!sourceEvent.Contains("changed", StringComparison.OrdinalIgnoreCase))
		{
			calendarEvent = Calendar.Load(Samples.RecurringExDate[sourceEvent].body).Events.First();
		}
		else
		{
			calendarEvent = Calendar.Load(Samples.RecurringInstanceChanged[sourceEvent].body).Events.First();

		}
		IDateTime calBegin = new CalDateTime(begin).ToTimeZone(calendarEvent.DtStart.TzId);
		IDateTime calEnd = new CalDateTime(end).ToTimeZone(calendarEvent.DtStart.TzId);

		// RecurrenceRules[0].Until is always UTC
		DateTime until = calendarEvent.RecurrenceRules[0].Until;

		// checking if this is eternal event
		// iCal.NET.Calendar.Load method puts DateTime.MinValue in the UNTIL field
		// if the event has no end
		if (until != DateTime.MinValue)
		{
			if (calendarEvent.IsAllDay)
			{
				// CGP has a bug: for ALL-DAY event UNTIL is kept in UTC as YYYY-MM-DD T HH:00:00Z
				// but not adjusted to real time zone which is wrong!
				until = DateTime.SpecifyKind(until, DateTimeKind.Local);
			}
			else
			{
				IDateTime calUntil = new CalDateTime(until, "UTC");
				calUntil = calUntil.ToTimeZone(calendarEvent.DtStart.TzId);
				until = calUntil.Value;
			}
		}

		calendarEvent.RecurrenceRules[0].Until = until;

		var resultOcc = calendarEvent.GetOccurrences(calBegin, calEnd);
		Assert.Equal(occNum, resultOcc.Count);

		TimeSpan eventStartTime = calendarEvent.DtStart.AsUtc.TimeOfDay;
		TimeSpan eventEndTime = calendarEvent.DtEnd.AsUtc.TimeOfDay;
		foreach (Occurrence occ in resultOcc)
		{
			Assert.Equal(eventStartTime, occ.Period.StartTime.AsUtc.TimeOfDay);
			Assert.Equal(eventEndTime, occ.Period.EndTime.AsUtc.TimeOfDay);
		}
	}
}
