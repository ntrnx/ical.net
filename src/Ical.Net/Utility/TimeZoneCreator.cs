using System;
using System.Linq;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Microsoft.Extensions.Logging;

namespace Ical.Net.Utility
{
	public class TimeZoneCreator
	{
		private ILogger _logger;

		public TimeZoneCreator(ILogger logger)
		{
			_logger = logger;
		}

		/// <summary>
		/// Создаёт часовой пояс на основе распарсенного iCalendar события.
		/// </summary>
		/// <param name="vTimeZone">Распарсенный элемент VTIMEZONE события iCalendar</param>
		/// <returns>
		/// Элемент <see cref="TimeZoneInfo"/> с правилами перехода на зимнее и летнее время
		/// или null, если правила перехода не удовлетворяют требованиям.
		/// </returns>
		/// <remarks>
		/// Требования описаны здесь
		/// https://gitlab.dev.ntrnx.com/video-conferencing/resampler/-/wikis/%D0%A0%D0%B0%D0%B1%D0%BE%D1%82%D0%B0-%D1%81-%D1%87%D0%B0%D1%81%D0%BE%D0%B2%D1%8B%D0%BC%D0%B8-%D0%BF%D0%BE%D1%8F%D1%81%D0%B0%D0%BC%D0%B8
		/// </remarks>
		internal TimeZoneInfo CreateTimeZone(VTimeZone vTimeZone)
		{
			TimeZoneInfo ctz;

			// First check if there is only one rule, i.e. no daylight saving period, like in Russia
			int timeZoneInfoCount = vTimeZone.TimeZoneInfos.Count;
			if (timeZoneInfoCount == 1)
			{
				VTimeZoneInfo vTimeZoneInfo = vTimeZone.TimeZoneInfos[0];

				if (ValidateSingleTimeZoneInfo(vTimeZoneInfo))
				{
					ctz = TimeZoneInfo.CreateCustomTimeZone(
						vTimeZone.TzId,
						vTimeZoneInfo.OffsetTo.Offset,
						vTimeZoneInfo.TimeZoneName,
						vTimeZoneInfo.TimeZoneName);
				}
				else
				{
					ctz = default;
				}

				return ctz;
			}

			if (timeZoneInfoCount == 0 || timeZoneInfoCount > 2)
			{
				_logger.LogError("Cannot process VTIMZONE with 0 or more than 2 sections");
				return default;
			}

			VTimeZoneInfo vTimeZoneInfoStandard = vTimeZone.TimeZoneInfos[0];
			VTimeZoneInfo vTimeZoneInfoDaylight = vTimeZone.TimeZoneInfos[1];

			if (!ValidateTimeZoneInfo(vTimeZoneInfoStandard) ||
				!ValidateTimeZoneInfo(vTimeZoneInfoDaylight))
			{
				return default;
			}

			// Check which recurring rule starts earlier
			if (vTimeZoneInfoStandard.RecurrenceRules[0].ByMonth[0] <
				vTimeZoneInfoDaylight.RecurrenceRules[0].ByMonth[0])
			{
				vTimeZoneInfoStandard = vTimeZone.TimeZoneInfos[1];
				vTimeZoneInfoDaylight = vTimeZone.TimeZoneInfos[0];
			}

			// Delta can be negative number for South hemisphere, like Australia or South Africa.
			TimeSpan delta = vTimeZoneInfoDaylight.OffsetTo.Offset - vTimeZoneInfoDaylight.OffsetFrom.Offset;

			RecurrencePattern tzRecurringRule = vTimeZoneInfoDaylight.RecurrenceRules[0];
			int week = tzRecurringRule.ByDay.First().Offset;
			week = week < 0 ? 5 : week; // we assume that if negative Offset can only be -1
			TimeZoneInfo.TransitionTime transitionRuleStart = TimeZoneInfo.TransitionTime
				.CreateFloatingDateRule(
					new DateTime(1, 1, 1,
						vTimeZoneInfoDaylight.Start.Hour,
						vTimeZoneInfoDaylight.Start.Minute,
						vTimeZoneInfoDaylight.Start.Second),
					tzRecurringRule.ByMonth.First(),
					week,
					tzRecurringRule.ByDay.First().DayOfWeek);

			tzRecurringRule = vTimeZoneInfoStandard.RecurrenceRules[0];
			week = tzRecurringRule.ByDay.First().Offset;
			week = week < 0 ? 5 : week; // we assume that if negative Offset can only be -1
			TimeZoneInfo.TransitionTime transitionRuleEnd = TimeZoneInfo.TransitionTime
				.CreateFloatingDateRule(
					new DateTime(1, 1, 1,
						vTimeZoneInfoStandard.Start.Hour,
						vTimeZoneInfoStandard.Start.Minute,
						vTimeZoneInfoStandard.Start.Second),
					tzRecurringRule.ByMonth.First(),
					week,
					tzRecurringRule.ByDay.First().DayOfWeek);

			DateTime daylightStart = vTimeZoneInfoDaylight.Start.Value;
			DateTime standardStart = vTimeZoneInfoStandard.Start.Value;
			DateTime adjustmentStart = daylightStart > standardStart
				? daylightStart
				: standardStart;

			// dateStart, dateEnd should not have Time part and be of Unspecified kind
			TimeZoneInfo.AdjustmentRule adjustment = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
				DateTime.SpecifyKind(adjustmentStart.Date, DateTimeKind.Unspecified),
				DateTime.MaxValue,
				delta,
				transitionRuleStart,
				transitionRuleEnd);

			TimeZoneInfo.AdjustmentRule[] adjustments = { adjustment };

			ctz = TimeZoneInfo.CreateCustomTimeZone(
				vTimeZone.TzId,
				vTimeZoneInfoStandard.OffsetTo.Offset,
				vTimeZone.TzId,
				vTimeZoneInfoStandard.TimeZoneName,
				vTimeZoneInfoDaylight.TimeZoneName,
				adjustments);

			return ctz;
		}

		/// <summary>
		/// Validates if time zone info corresponds to time zone CommuniGate restrictions
		/// https://gitlab.dev.ntrnx.com/video-conferencing/resampler/-/wikis/%D0%A0%D0%B0%D0%B1%D0%BE%D1%82%D0%B0-%D1%81-%D1%87%D0%B0%D1%81%D0%BE%D0%B2%D1%8B%D0%BC%D0%B8-%D0%BF%D0%BE%D1%8F%D1%81%D0%B0%D0%BC%D0%B8#%D0%BE%D0%B3%D1%80%D0%B0%D0%BD%D0%B8%D1%87%D0%B5%D0%BD%D0%B8%D1%8F-%D0%BD%D0%B0-%D0%B7%D0%B0%D0%B4%D0%B0%D0%BD%D0%B8%D0%B5-%D1%87%D0%B0%D1%81%D0%BE%D0%B2%D0%BE%D0%B3%D0%BE-%D0%BF%D0%BE%D1%8F%D1%81%D0%B0-%D0%B2-%D1%81%D0%BE%D0%B1%D1%8B%D1%82%D0%B8%D0%B8
		/// </summary>
		/// <param name="vTimeZoneInfo"></param>
		/// <returns></returns>
		private bool ValidateTimeZoneInfo(VTimeZoneInfo vTimeZoneInfo)
		{
			if (vTimeZoneInfo.ExceptionDates.Any())
			{
				_logger.LogError("VTIMEZONE STANDARD/DAYLIGHT section cannot have EXDATE part");
				return false;
			}

			if (vTimeZoneInfo.ExceptionRules.Any())
			{
				_logger.LogError("VTIMEZONE STANDARD/DAYLIGHT section cannot have ExceptionRules");
				return false;
			}

			if (vTimeZoneInfo.RecurrenceDates.Any())
			{
				_logger.LogError("Resampler cannot process VTIMEZONE STANDARD/DAYLIGHT section with RDATE part");
				return false;
			}

			if (!vTimeZoneInfo.RecurrenceRules.Any())
			{
				_logger.LogError("VTIMEZONE STANDARD/DAYLIGHT section shuld have RRULE part");
				return false;
			}

			if (vTimeZoneInfo.RecurrenceRules.Count > 1)
			{
				_logger.LogError("VTIMEZONE STANDARD/DAYLIGHT section shuld have only one RRULE part");
				return false;
			}

			var recurrenceRule = vTimeZoneInfo.RecurrenceRules[0];
			if (recurrenceRule.Until != DateTime.MinValue)
			{
				_logger.LogError("Resampler cannot process VTIMEZONE STANDARD/DAYLIGHT section RRULE part" +
					"with UNTIL value {Until:u}", recurrenceRule.Until);
				return false;
			}

			if (recurrenceRule.Frequency != FrequencyType.Yearly && recurrenceRule.Interval != 1)
			{
				_logger.LogError("Resampler cannot process VTIMEZONE STANDARD/DAYLIGHT section RRULE part" +
					" with FREQ other than YEARLY, INTERVAL=1. Current values FREQ={Freq} and INTERVAL={Interval}",
					recurrenceRule.Frequency, recurrenceRule.Interval);
				return false;
			}

			bool otherComponents = recurrenceRule.ByHour.Any()
			|| recurrenceRule.ByMinute.Any()
			|| recurrenceRule.BySecond.Any()
			|| recurrenceRule.ByMonthDay.Any()
			|| recurrenceRule.BySetPosition.Any()
			|| recurrenceRule.ByWeekNo.Any()
			|| recurrenceRule.ByYearDay.Any();
			if (otherComponents)
			{
				_logger.LogError("Resampler cannot process VTIMEZONE STANDARD/DAYLIGHT section RRULE part" +
					" with components other than BYMONTH and BYDAY");
				return false;
			}

			if (recurrenceRule.ByMonth.Count != 1)
			{
				_logger.LogError("Resampler cannot process VTIMEZONE STANDARD/DAYLIGHT section RRULE part" +
					" with other than one BYMONTH rule");
				return false;
			}

			if (recurrenceRule.ByDay.Count != 1)
			{
				_logger.LogError("Resampler cannot process VTIMEZONE STANDARD/DAYLIGHT section RRULE part" +
					" with other than one BYDAY rule");
				return false;
			}

			if (recurrenceRule.ByDay[0].Offset < -1)
			{
				_logger.LogError("Resampler cannot process VTIMEZONE STANDARD/DAYLIGHT section RRULE part" +
					" with BYDAY offset less than -1");
				return false;
			}

			return true;
		}

		/// <summary>
		/// Validates if the time zone info corresponds to the single time zone CommuniGate restriction.
		/// If the time zone has single time zone info it means there is no change to daylight saving time.
		/// In other words, time zone info does not have RRULE or RDATE,
		/// and TZOFFSETFROM value should be equal to TZOFFSETTO one.
		/// https://gitlab.dev.ntrnx.com/video-conferencing/resampler/-/wikis/%D0%A0%D0%B0%D0%B1%D0%BE%D1%82%D0%B0-%D1%81-%D1%87%D0%B0%D1%81%D0%BE%D0%B2%D1%8B%D0%BC%D0%B8-%D0%BF%D0%BE%D1%8F%D1%81%D0%B0%D0%BC%D0%B8#%D0%BE%D0%B3%D1%80%D0%B0%D0%BD%D0%B8%D1%87%D0%B5%D0%BD%D0%B8%D1%8F-%D0%BD%D0%B0-%D0%B7%D0%B0%D0%B4%D0%B0%D0%BD%D0%B8%D0%B5-%D1%87%D0%B0%D1%81%D0%BE%D0%B2%D0%BE%D0%B3%D0%BE-%D0%BF%D0%BE%D1%8F%D1%81%D0%B0-%D0%B2-%D1%81%D0%BE%D0%B1%D1%8B%D1%82%D0%B8%D0%B8
		/// </summary>
		/// <param name="vTimeZoneInfo">STANDARD rule</param>
		/// <returns>true if corresponds, false otherwise</returns>
		private bool ValidateSingleTimeZoneInfo(VTimeZoneInfo vTimeZoneInfo)
		{
			if (!string.Equals(vTimeZoneInfo.Name, "STANDARD", StringComparison.OrdinalIgnoreCase))
			{
				_logger.LogWarning("VTIMZONE single section should have name STANDARD not {Name}",
					vTimeZoneInfo.Name);
			}

			if (vTimeZoneInfo.ExceptionDates.Any())
			{
				_logger.LogError("VTIMEZONE single STANDARD section cannot have EXDATE part");
				return false;
			}

			if (vTimeZoneInfo.ExceptionRules.Any())
			{
				_logger.LogError("VTIMEZONE single STANDARD section cannot have ExceptionRules");
				return false;
			}

			if (vTimeZoneInfo.RecurrenceDates.Any())
			{
				_logger.LogError("Resampler cannot process VTIMEZONE single STANDARD section with RDATE part");
				return false;
			}

			if (vTimeZoneInfo.RecurrenceRules.Any())
			{
				_logger.LogError("Resampler cannot process VTIMEZONE single STANDARD section with RRULE part");
				return false;
			}

			TimeSpan tzOffsetFrom = vTimeZoneInfo.OffsetFrom.Offset;
			TimeSpan tzOffsetTo = vTimeZoneInfo.OffsetTo.Offset;
			if (tzOffsetFrom != tzOffsetTo)
			{
				_logger.LogError("Resampler cannot process VTIMEZONE single STANDARD section when" +
					" TZOFFSETFROM {From} is not equal to TZOFFSETTO {To}", tzOffsetFrom, tzOffsetTo);
				return false;
			}

			return true;
		}
	}
}
