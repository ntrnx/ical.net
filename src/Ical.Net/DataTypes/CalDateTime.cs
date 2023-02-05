using System;
using System.IO;
using System.Linq;
using Ical.Net.CalendarComponents;
using Ical.Net.Serialization.DataTypes;
using Ical.Net.Utility;
using NodaTime;

namespace Ical.Net.DataTypes
{
    /// <summary>
    /// The iCalendar expansion of the .NET <see cref="DateTime"/> class.
    /// <remarks>
    /// <see cref="CalDateTime"/> properties are get-only. You can change property values
    /// only through the methods. Changing one property may adjusts other properties accordingly.
    /// <para>
    /// You can get <see cref="CalDateTime"/> value from the <see cref="Value"/> property.
    /// <see cref="Value"/> does not use <see cref="DateTimeKind.Local"/> kind modifier.
    /// Possible options are the floowing:
    /// </para>
    /// <list type="number">
    ///		<item>
    ///			<see cref="DateTimeKind.Utc"/> - then <see cref="TzId"/> is set to "UTC"
    ///			and <see cref="TimeZoneInfo"/> is set to <see cref="TimeZoneInfo.Utc"/>
    ///		</item>
	///		<item>
	///			<see cref="DateTimeKind.Unspecified"/> and <see cref="TzId"/> is null
	///			and <see cref="TimeZoneInfo"/> is null. This value does not belong to any time zone,
	///			it means that depending on the time zone it will have different UTC value.
	///		</item>
	///		<item>
	///			<see cref="DateTimeKind.Unspecified"/> and <see cref="TzId"/> is set to some "TimeZone"
	///			and <see cref="TimeZoneInfo"/> is also set to the same "TimeZone". This value does not belong to any time zone,
	///			it means that depending on the time zone it will have different UTC value.
	///		</item>
    /// </list>
    /// <p>Time zone rules</p>
    /// <list type="number">
    ///		<item>
    ///			You cannot change time zone value of the <see cref="CalDateTime"/> instance.
    ///		</item>
	///		<item>
	///			To get <see cref="CalDateTime"/> instance with the <see cref="Value"/> but in the different time zone
	///			need to use constructor.
	///		</item>
    /// </list>
    /// In addition to the features of the <see cref="DateTime"/> class, the <see cref="CalDateTime"/>
    /// class handles time zone differences, and integrates seamlessly into the iCalendar framework.
    /// </remarks>
    /// </summary>
    public sealed class CalDateTime : EncodableDataType, IDateTime
    {
        public static CalDateTime Now => new CalDateTime(DateTime.Now);

        public static CalDateTime Today => new CalDateTime(DateTime.Today);

        private bool _hasDate;
        private bool _hasTime;

        public CalDateTime() { }

        public CalDateTime(IDateTime value)
        {
            Initialize(value.Value, value.TzId, value.TimeZoneInfo);
        }

        public CalDateTime(DateTime value) : this(value, default(string), default(TimeZoneInfo)) { }

        /// <summary>
        /// Specifying a `tzId` value will override `value`'s `DateTimeKind` property. If the time zone specified is UTC, the underlying `DateTimeKind` will be
        /// `Utc`. If a non-UTC time zone is specified, the underlying `DateTimeKind` property will be `Local`. If no time zone is specified, the `DateTimeKind`
        /// property will be left untouched.
        /// </summary>
        // public CalDateTime(DateTime value, string tzId)
        // {
        //     Initialize(value, tzId, default(Calendar));
        // }
        public CalDateTime(DateTime value, string tzId, TimeZoneInfo timeZoneInfo)
        {
			if (tzId != timeZoneInfo.Id)
			{
				throw new ArgumentException($"tzId {tzId} is not equal to timeZoneInfo.Id {timeZoneInfo.Id}");
			}
            Initialize(value, tzId, timeZoneInfo);
        }

		private void Initialize(DateTime value, string tzId, TimeZoneInfo timeZoneInfo)
		{
			Value = new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, value.Kind);
			HasDate = true;
			HasTime = value.Second != 0 || value.Minute != 0 || value.Hour != 0;
			if (value.Kind)
			{
				TimeZoneInfo = timeZoneInfo;
			}
			TzId = tzId;
		}

        public override ICalendarObject AssociatedObject
        {
            get => base.AssociatedObject;
            set
            {
                if (!Equals(AssociatedObject, value))
                {
                    base.AssociatedObject = value;
                }
            }
        }

        public override void CopyFrom(ICopyable obj)
        {
            base.CopyFrom(obj);

            var dt = obj as IDateTime;
            if (dt == null)
            {
                return;
            }

            _value = dt.Value;
            _hasDate = dt.HasDate;
            _hasTime = dt.HasTime;
			TimeZoneInfo = dt.TimeZoneInfo;
			_tzId = dt.TzId;

            AssociateWith(dt);
        }

        public bool Equals(CalDateTime other)
            => this == other;

        public override bool Equals(object other)
            => other is IDateTime && (CalDateTime) other == this;

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Value.GetHashCode();
                hashCode = (hashCode * 397) ^ HasDate.GetHashCode();
                hashCode = (hashCode * 397) ^ AsUtc.GetHashCode();
                hashCode = (hashCode * 397) ^ (TzId != null ? TzId.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator <(CalDateTime left, IDateTime right)
            => left != null && right != null && left.AsUtc < right.AsUtc;

        public static bool operator >(CalDateTime left, IDateTime right)
            => left != null && right != null && left.AsUtc > right.AsUtc;

        public static bool operator <=(CalDateTime left, IDateTime right)
            => left != null && right != null && left.AsUtc <= right.AsUtc;

        public static bool operator >=(CalDateTime left, IDateTime right)
            => left != null && right != null && left.AsUtc >= right.AsUtc;

        public static bool operator ==(CalDateTime left, IDateTime right)
        {
            return ReferenceEquals(left, null) || ReferenceEquals(right, null)
                ? ReferenceEquals(left, right)
                : right is CalDateTime
                    && left.Value.Equals(right.Value)
                    && left.HasDate == right.HasDate
                    && left.AsUtc.Equals(right.AsUtc)
                    && string.Equals(left.TzId, right.TzId, StringComparison.OrdinalIgnoreCase);
        }

        public static bool operator !=(CalDateTime left, IDateTime right)
            => !(left == right);

        public static TimeSpan operator -(CalDateTime left, IDateTime right)
        {
            left.AssociateWith(right);
            return left.AsUtc - right.AsUtc;
        }

        public static IDateTime operator -(CalDateTime left, TimeSpan right)
        {
            var copy = left.Copy<IDateTime>();
            copy.Value -= right;
            return copy;
        }

        public static IDateTime operator +(CalDateTime left, TimeSpan right)
        {
            var copy = left.Copy<IDateTime>();
            copy.Value += right;
            return copy;
        }

        public static implicit operator CalDateTime(DateTime left) => new CalDateTime(left);

        /// <summary>
        /// Converts the date/time to the date/time of the computer running the program. If the DateTimeKind is Unspecified, it's assumed that the underlying
        /// Value already represents the system's datetime.
        /// </summary>
        public DateTime AsSystemLocal
        {
            get
            {
                if (Value.Kind == DateTimeKind.Unspecified)
                {
                    return HasTime
                        ? Value
                        : Value.Date;
                }

                return HasTime
                    ? Value.ToLocalTime()
                    : Value.ToLocalTime().Date;
            }
        }

        private DateTime _asUtc;
        /// <summary>
        /// Returns a representation of the DateTime in Coordinated Universal Time (UTC)
        /// </summary>
        public DateTime AsUtc
        {
            get
            {
                if (_asUtc )
                {
                    // In order of weighting:
                    //  1) Specified TzId
                    //  2) Value having a DateTimeKind.Utc
                    //  3) Use the OS's time zone

                    if (!string.IsNullOrWhiteSpace(TzId))
                    {
                        if (TimeZoneInfo != null)
                        {
                            DateTime toCovert = DateTime.SpecifyKind(_value, DateTimeKind.Unspecified);
                            _asUtc = TimeZoneInfo.ConvertTimeToUtc(toCovert, TimeZoneInfo);
                        }
                        else
                        {
                            var asLocal = DateUtil.ToZonedDateTimeLeniently(Value, TzId);
                            _asUtc = asLocal.ToDateTimeUtc();
                        }
                    }
                    else if(IsUtc || Value.Kind == DateTimeKind.Utc)
                    {
                        _asUtc = DateTime.SpecifyKind(Value, DateTimeKind.Utc);
                    }
                    else
                    {
                        _asUtc = DateTime.SpecifyKind(Value, DateTimeKind.Local).ToUniversalTime();
                    }
                }
                return _asUtc;
            }
        }

        private DateTime _value;
        public DateTime Value
        {
            get => _value;
            set
            {
                if (_value == value && _value.Kind == value.Kind)
                {
                    return;
                }

                _asUtc = DateTime.MinValue;
                _value = value;
            }
        }

        public bool IsUtc => _value.Kind == DateTimeKind.Utc;

        public bool HasDate
        {
            get => _hasDate;
            set => _hasDate = value;
        }

        public bool HasTime
        {
            get => _hasTime;
            set => _hasTime = value;
        }

		public bool HasTimeZone { get; }

		public TimeZoneInfo TimeZoneInfo { get; set; }
        private string _tzId = string.Empty;

        /// <summary>
        /// Setting the TzId to a local time zone will set Value.Kind to Local. Setting TzId to UTC will set Value.Kind to Utc. If the incoming value is null
        /// or whitespace, Value.Kind will be set to Unspecified. Setting the TzId will NOT incur a UTC offset conversion under any circumstances. To convert
        /// to another time zone, use the ToTimeZone() method.
        /// </summary>
        public string TzId
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_tzId))
                {
                    _tzId = Parameters.Get("TZID");
                }
                return _tzId;
            }
            /*
            set
            {
                if (string.Equals(_tzId, value, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                _asUtc = DateTime.MinValue;

                var isEmpty = string.IsNullOrWhiteSpace(value);
                if (isEmpty)
                {
                    Parameters.Remove("TZID");
                    _tzId = null;
                    Value = DateTime.SpecifyKind(Value, DateTimeKind.Local);
                    return;
                }

                var kind = string.Equals(value, "UTC", StringComparison.OrdinalIgnoreCase)
                    ? DateTimeKind.Utc
                    : DateTimeKind.Local;

                Value = DateTime.SpecifyKind(Value, kind);
                Parameters.Set("TZID", value);
                _tzId = value;
            }
			*/
        }

        public string TimeZoneName => TzId;

        public int Year => Value.Year;

        public int Month => Value.Month;

        public int Day => Value.Day;

        public int Hour => Value.Hour;

        public int Minute => Value.Minute;

        public int Second => Value.Second;

        public int Millisecond => Value.Millisecond;

        public long Ticks => Value.Ticks;

        public DayOfWeek DayOfWeek => Value.DayOfWeek;

        public int DayOfYear => Value.DayOfYear;

        public DateTime Date => Value.Date;

        public TimeSpan TimeOfDay => Value.TimeOfDay;

        /// <summary>
        /// Returns a representation of the IDateTime in the specified time zone
        /// <para>
        /// If <see cref="IDateTime"/> instance does not have time zone, then the new <see cref="CalDateTime"/> instance with the same values of the
        /// year, month, day, hours, minutes, and seconds and the specified <paramref name="tzId"/> is returned.
        /// If
        /// </para>
        /// </summary>
        /// <returns>New <see cref="CalDateTime"/> instance</returns>
        public IDateTime ToTimeZone(string tzId)
        {
            if (string.IsNullOrWhiteSpace(tzId))
            {
                throw new ArgumentException("You must provide a time zone id", nameof(tzId));
            }

            // If TzId is empty, it's a system-local datetime, so we should use the system time zone as the starting point.
            var originalTzId = string.IsNullOrWhiteSpace(TzId)
                ? TimeZoneInfo.Local.Id
                : TzId;

            var zonedOriginal = DateUtil.ToZonedDateTimeLeniently(Value, originalTzId);
            var converted = zonedOriginal.WithZone(DateUtil.GetZone(tzId));

            return converted.Zone == DateTimeZone.Utc
                ? new CalDateTime(converted.ToDateTimeUtc(), tzId, TimeZoneInfo.Utc)
                : new CalDateTime(DateTime.SpecifyKind(converted.ToDateTimeUnspecified(), DateTimeKind.Local), tzId, default(TimeZoneInfo));
        }

        /// <summary>
        /// Returns a DateTimeOffset representation of the Value. If a TzId is specified, it will use that time zone's UTC offset, otherwise it will use the
        /// system-local time zone.
        /// </summary>
        public DateTimeOffset AsDateTimeOffset =>
            string.IsNullOrWhiteSpace(TzId)
                ? new DateTimeOffset(AsSystemLocal)
                : DateUtil.ToZonedDateTimeLeniently(Value, TzId).ToDateTimeOffset();

        public IDateTime Add(TimeSpan ts) => this + ts;

        public IDateTime Subtract(TimeSpan ts) => this - ts;

        public TimeSpan Subtract(IDateTime dt) => this - dt;

        public IDateTime AddYears(int years)
        {
            var dt = Copy<IDateTime>();
            dt.Value = Value.AddYears(years);
            return dt;
        }

        public IDateTime AddMonths(int months)
        {
            var dt = Copy<IDateTime>();
            dt.Value = Value.AddMonths(months);
            return dt;
        }

        public IDateTime AddDays(int days)
        {
            var dt = Copy<IDateTime>();
            dt.Value = Value.AddDays(days);
            return dt;
        }

        public IDateTime AddHours(int hours)
        {
            var dt = Copy<IDateTime>();
            if (!dt.HasTime && hours % 24 > 0)
            {
                dt.HasTime = true;
            }
            dt.Value = Value.AddHours(hours);
            return dt;
        }

        public IDateTime AddMinutes(int minutes)
        {
            var dt = Copy<IDateTime>();
            if (!dt.HasTime && minutes % 1440 > 0)
            {
                dt.HasTime = true;
            }
            dt.Value = Value.AddMinutes(minutes);
            return dt;
        }

        public IDateTime AddSeconds(int seconds)
        {
            var dt = Copy<IDateTime>();
            if (!dt.HasTime && seconds % 86400 > 0)
            {
                dt.HasTime = true;
            }
            dt.Value = Value.AddSeconds(seconds);
            return dt;
        }

        public IDateTime AddMilliseconds(int milliseconds)
        {
            var dt = Copy<IDateTime>();
            if (!dt.HasTime && milliseconds % 86400000 > 0)
            {
                dt.HasTime = true;
            }
            dt.Value = Value.AddMilliseconds(milliseconds);
            return dt;
        }

        public IDateTime AddTicks(long ticks)
        {
            var dt = Copy<IDateTime>();
            dt.HasTime = true;
            dt.Value = Value.AddTicks(ticks);
            return dt;
        }

        public bool LessThan(IDateTime dt) => this < dt;

        public bool GreaterThan(IDateTime dt) => this > dt;

        public bool LessThanOrEqual(IDateTime dt) => this <= dt;

        public bool GreaterThanOrEqual(IDateTime dt) => this >= dt;

        public void AssociateWith(IDateTime dt)
        {
            if (AssociatedObject == null && dt.AssociatedObject != null)
            {
                AssociatedObject = dt.AssociatedObject;
            }
            else if (AssociatedObject != null && dt.AssociatedObject == null)
            {
                dt.AssociatedObject = AssociatedObject;
            }
        }

        public int CompareTo(IDateTime dt)
        {
            if (Equals(dt))
            {
                return 0;
            }
            if (this < dt)
            {
                return -1;
            }
            if (this > dt)
            {
                return 1;
            }
            throw new Exception("An error occurred while comparing two IDateTime values.");
        }

        public override string ToString() => ToString(null, null);

        public string ToString(string format) => ToString(format, null);

        public string ToString(string format, IFormatProvider formatProvider)
        {
            var tz = TimeZoneName;
            if (!string.IsNullOrEmpty(tz))
            {
                tz = " " + tz;
            }

            if (format != null)
            {
                return Value.ToString(format, formatProvider) + tz;
            }
            if (HasTime && HasDate)
            {
                return Value + tz;
            }
            if (HasTime)
            {
                return Value.TimeOfDay + tz;
            }
            return Value.ToString("d") + tz;
        }
    }
}
