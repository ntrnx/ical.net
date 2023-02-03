using System;
using Ical.Net.DataTypes;

namespace Ical.Net.Serialization.DataTypes
{
    public abstract class DataTypeSerializer : SerializerBase
    {
        protected DataTypeSerializer() {}

        protected DataTypeSerializer(SerializationContext ctx) : base(ctx) {}

		protected DataTypeSerializer(SerializationContext ctx, CalendarProperty property)
			: base(ctx)
		{
			CalendarProperty = property;
		}

		// Keeps time zone information for the serialized datetime object
		protected CalendarProperty CalendarProperty { get; set; }
        protected virtual ICalendarDataType CreateAndAssociate()
        {
            // Create an instance of the object
            if (!(Activator.CreateInstance(TargetType) is ICalendarDataType dt))
            {
                return null;
            }

            if (SerializationContext.Peek() is ICalendarObject associatedObject)
            {
                dt.AssociatedObject = associatedObject;
            }

            return dt;
        }
    }
}
