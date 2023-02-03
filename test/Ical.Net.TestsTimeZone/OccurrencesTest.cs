using Ical.Net.CalendarComponents;
using Xunit;

namespace Ical.Net.TestsTimeZone;

public class OccurrencesTest
{
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
