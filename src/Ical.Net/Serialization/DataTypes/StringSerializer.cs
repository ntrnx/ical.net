using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Ical.Net.DataTypes;

namespace Ical.Net.Serialization.DataTypes
{
    public class StringSerializer : EncodableDataTypeSerializer
    {
        public StringSerializer() {}

        public StringSerializer(SerializationContext ctx) : base(ctx) {}

        protected virtual string Unescape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            StringBuilder sb = new StringBuilder(value.Length);

            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];

                if (c != '\\')
                {
                    sb.Append(c);
                    continue;
                }

                // Trailing backslash — keep as-is
                if (i == value.Length - 1)
                {
                    sb.Append('\\');
                    break;
                }

                char next = value[++i];

                switch (next)
                {
                    case 'n':
                    case 'N':
                        sb.Append('\n');
                        break;
                    case '\\':
                        sb.Append('\\');
                        break;
                    case ';':
                        sb.Append(';');
                        break;
                    case ',':
                        sb.Append(',');
                        break;
                    case '"':
                        // NOTE: double quotes aren't escaped in RFC 5545, but are in Mozilla Sunbird (0.5-)
                        sb.Append('"');
                        break;
                    default:
                        // Unknown escape sequence — preserve backslash and character
                        sb.Append('\\');
                        sb.Append(next);
                        break;
                }
            }

            return sb.ToString();
        }

        protected virtual string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            // NOTE: backslash must be escaped first to avoid double-escaping subsequent replacements.
            // SEE unit test SERIALIZE25().
            return value
                .Replace("\\", "\\\\")
                .Replace(SerializationConstants.LineBreak, @"\n")
                .Replace("\r", @"\n")
                .Replace("\n", @"\n")
                .Replace(";", @"\;")
                .Replace(",", @"\,");
        }

        public override Type TargetType => typeof (string);

        public override string SerializeToString(object obj)
        {
            if (obj == null)
            {
                return null;
            }

            var values = new List<string>();
            if (obj is string)
            {
                values.Add((string) obj);
            }
            else if (obj is IEnumerable)
            {
                values.AddRange(from object child in (IEnumerable) obj select child.ToString());
            }

            var co = SerializationContext.Peek() as ICalendarObject;
            if (co != null)
            {
                // Encode the string as needed.
                var dt = new EncodableDataType
                {
                    AssociatedObject = co
                };
                for (var i = 0; i < values.Count; i++)
                {
                    values[i] = Encode(dt, Escape(values[i]));
                }

                return string.Join(",", values);
            }

            for (var i = 0; i < values.Count; i++)
            {
                values[i] = Escape(values[i]);
            }
            return string.Join(",", values);
        }

        internal static readonly Regex UnescapedCommas = new Regex(@"(?<!\\),", RegexOptions.Compiled);
        public override object Deserialize(TextReader tr)
        {
            if (tr == null)
            {
                return null;
            }

            var value = tr.ReadToEnd();

            // NOTE: this can deserialize into an IList<string> or simply a string,
            // depending on the input text.  Anything that uses this serializer should
            // be prepared to receive either a string, or an IList<string>.

            var serializeAsList = false;

            // Determine if we can serialize this property
            // with multiple values per line.
            var co = SerializationContext.Peek() as ICalendarObject;
            if (co is ICalendarProperty)
            {
                serializeAsList = GetService<DataTypeMapper>().GetPropertyAllowsMultipleValues(co);
            }

            // Try to decode the string
            EncodableDataType dt = null;
            if (co != null)
            {
                dt = new EncodableDataType
                {
                    AssociatedObject = co
                };
            }

            var encodedValues = serializeAsList ? UnescapedCommas.Split(value) : new[] { value };
            var escapedValues = encodedValues.Select(v => Decode(dt, v)).ToList();
            var values = escapedValues.Select(Unescape).ToList();

            if (co is ICalendarProperty)
            {
                // Is this necessary?
                co.SetService("EscapedValue", escapedValues.Count == 1 ? escapedValues[0] : (object)escapedValues);
            }

            // Return either a single value, or the entire list.
            if (values.Count == 1)
            {
                return values[0];
            }
            return values;
        }
    }
}
