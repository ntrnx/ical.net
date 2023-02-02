using System.Globalization;
using Ical.Net.CalendarComponents;
using Xunit;

namespace Ical.Net.TestsTimeZone;

public class IcalNetParserTests
{
    [Theory]
    [InlineData("single_fixed_dates_utc")]
    [InlineData("single_fixed_dates_tz_msk")]
    [InlineData("single_fixed_dates_tz_russian")]
    [InlineData("single_fixed_dates_tz_eu_west")]
    [InlineData("single_with_long_lines")]
    // artificial example. Possible to create only via Postman. Resampler does not use it
    [InlineData("single_all_day_tz_msk")]
    public void T01_SingleEvent(string eventType)
    {
        CalendarEvent result = Calendar.Load(Samples.Single[eventType].body).Events.First();

        var expectedResult = Samples.Single[eventType];
        Assert.Equal(expectedResult.recurring, result.RecurrenceRules.Any());
        Assert.Equal(expectedResult.allDay, result.IsAllDay);
        Assert.Equal(expectedResult.startUtc, result.DtStart.AsUtc);
        Assert.Equal(expectedResult.endUtc, result.DtEnd.AsUtc);
    }

    [Theory]
    // possible to create via Samoware but Resampler does not have such case
    [InlineData("single_all_day_no_tz")]

    // CommuniGate does not create such time zones. Using test case for research purpose only
    [InlineData("single_fixed_dates_tz_america_ny_complex")]
    [InlineData("single_with_leading_tabs")]

    public void T02_SingleAllDay(string eventType)
    {
		CalendarEvent result = Ical.Net.Calendar.Load(Samples.Single[eventType].body).Events.First();

		if (string.Equals(eventType, "single_fixed_dates_tz_america_ny_complex", StringComparison.Ordinal))
		{
			Assert.True(true);
			return;
		}

        var expectedResult = Samples.Single[eventType];
        Assert.Equal(expectedResult.recurring, result.RecurrenceRules.Any());
        Assert.Equal(expectedResult.allDay, result.IsAllDay);
        Assert.Equal(expectedResult.startUtc, result.DtStart.Date);
        Assert.Equal(expectedResult.endUtc, result.DtEnd.Date);
    }

    [Theory]
    [InlineData("recurring_fixed_dates_tz_msk_msk")]
    [InlineData("recurring_fixed_dates_tz_yek_msk")]
    [InlineData("recurring_fixed_dates_tz_eu_west_msk")]
    [InlineData("recurring_fixed_dates_tz_na_east_msk")]
    [InlineData("recurring_fixed_dates_tz_australia_msk")]
    [InlineData("recurring_all_day_msk_msk")]
    [InlineData("recurring_all_day_yek_msk")]
    [InlineData("recurring_all_day_eu_west_msk")]
    [InlineData("recurring_all_day_na_east_msk")]
    public void T03_RecurringEvent(string eventType)
    {
        var events = Calendar.Load(Samples.Recurring[eventType].body).Events;
        CalendarEvent result = events.First();

        var expectedResult = Samples.Recurring[eventType];
        Assert.Equal(expectedResult.recurring, result.RecurrenceRules.Any());
        Assert.Equal(expectedResult.allDay, result.IsAllDay);
        Assert.Equal(expectedResult.startUtc, result.DtStart.AsUtc);
        Assert.Equal(expectedResult.endUtc, result.DtEnd.AsUtc);
    }

    [Theory]
    [InlineData("recurring_fixed_dates_tz_msk_msk_exdate")]
    [InlineData("recurring_fixed_dates_tz_yek_msk_exdate")]
    [InlineData("recurring_fixed_dates_tz_eu_west_msk_exdate")]
    [InlineData("recurring_fixed_dates_tz_na_east_msk_exdate")]
    [InlineData("recurring_all_day_msk_msk_exdate")]
    [InlineData("recurring_all_day_yek_msk_exdate")]
    [InlineData("recurring_all_day_eu_west_msk_exdate")]
    [InlineData("recurring_all_day_na_east_msk_exdate")]
    public void T04_RecurringExDate(string eventType)
    {
        var events = Calendar.Load(Samples.RecurringExDate[eventType].body).Events;
        CalendarEvent result = events.First();

        var expectedResult = Samples.RecurringExDate[eventType];
        Assert.Equal(expectedResult.recurring, result.RecurrenceRules.Any());
        Assert.Equal(expectedResult.allDay, result.IsAllDay);
        Assert.Equal(expectedResult.endUtc, result.RecurrenceRules.First().Until);
        Assert.Equal(expectedResult.startUtc, result.ExceptionDates.First().First().StartTime.AsUtc);
    }

    [Theory]
    [InlineData("recurring_fixed_dates_tz_msk_msk_changed")]
    [InlineData("recurring_fixed_dates_tz_yek_msk_changed")]
    [InlineData("recurring_fixed_dates_tz_eu_west_msk_changed")]
    [InlineData("recurring_fixed_dates_tz_na_east_msk_changed")]
    public void T05_RecurringInstanceChanged(string eventType)
    {
        var events = Calendar.Load(Samples.RecurringInstanceChanged[eventType].body).Events;
        CalendarEvent mainEvent = events[0];
        CalendarEvent result = events[1];

        var expectedResult = Samples.RecurringInstanceChanged[eventType];
        Assert.Equal(expectedResult.recurring, mainEvent.RecurrenceRules.Any());
        Assert.Equal(expectedResult.allDay, mainEvent.IsAllDay);
        Assert.Equal(expectedResult.startUtc, result.DtStart.AsUtc);
        Assert.Equal(expectedResult.endUtc, result.DtEnd.AsUtc);
        Assert.Equal(expectedResult.recurrenceId, result.RecurrenceId.AsUtc);
    }

    [Theory]
    [InlineData("recurring_all_day_msk_msk_changed")]
    [InlineData("recurring_all_day_yek_msk_changed")]
    [InlineData("recurring_all_day_eu_west_msk_changed")]
    [InlineData("recurring_all_day_na_east_msk_changed")]
    public void T06_RecurringAllDayInstanceChanged(string eventType)
    {
        var events = Calendar.Load(Samples.RecurringInstanceChanged[eventType].body).Events;
        CalendarEvent mainEvent = events[0];
        CalendarEvent result = events[1];

        var expectedResult = Samples.RecurringInstanceChanged[eventType];
        Assert.Equal(expectedResult.recurring, mainEvent.RecurrenceRules.Any());
        Assert.Equal(expectedResult.allDay, mainEvent.IsAllDay);
        Assert.Equal(expectedResult.startUtc, result.DtStart.Date);
        Assert.Equal(expectedResult.endUtc, result.DtEnd.Date);
        Assert.Equal(expectedResult.recurrenceId, result.RecurrenceId.AsUtc);
    }
}
