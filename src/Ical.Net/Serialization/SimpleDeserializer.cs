using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Ical.Net.CalendarComponents;

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit {}
}

namespace Ical.Net.Serialization
{
    internal record ParsedLine(string Name, string Value, CaptureCollection ParamNames, CaptureCollection ParamValues);
    public class SimpleDeserializer
    {
        internal SimpleDeserializer(
            DataTypeMapper dataTypeMapper,
            ISerializerFactory serializerFactory,
            CalendarComponentFactory componentFactory)
        {
            _dataTypeMapper = dataTypeMapper;
            _serializerFactory = serializerFactory;
            _componentFactory = componentFactory;
        }

        public static readonly SimpleDeserializer Default = new SimpleDeserializer(
            new DataTypeMapper(),
            new SerializerFactory(),
            new CalendarComponentFactory());

        private const string _nameGroup = "name";
        private const string _valueGroup = "value";
        private const string _paramNameGroup = "paramName";
        private const string _paramValueGroup = "paramValue";

        private static readonly Regex _contentLineRegex = new Regex(BuildContentLineRegex(), RegexOptions.Compiled);

        private readonly DataTypeMapper _dataTypeMapper;
        private readonly ISerializerFactory _serializerFactory;
        private readonly CalendarComponentFactory _componentFactory;

        private static string BuildContentLineRegex()
        {
            // name          = iana-token / x-name
            // iana-token    = 1*(ALPHA / DIGIT / "-")
            // x-name        = "X-" [vendorid "-"] 1*(ALPHA / DIGIT / "-")
            // vendorid      = 3*(ALPHA / DIGIT)
            // Add underscore to match behavior of bug 2033495
            const string identifier = "[-A-Za-z0-9_]+";

            // param-value   = paramtext / quoted-string
            // paramtext     = *SAFE-CHAR
            // quoted-string = DQUOTE *QSAFE-CHAR DQUOTE
            // QSAFE-CHAR    = WSP / %x21 / %x23-7E / NON-US-ASCII
            // ; Any character except CONTROL and DQUOTE
            // SAFE-CHAR     = WSP / %x21 / %x23-2B / %x2D-39 / %x3C-7E
            //               / NON-US-ASCII
            // ; Any character except CONTROL, DQUOTE, ";", ":", ","
            var paramValue = $"((?<{_paramValueGroup}>[^\\x00-\\x08\\x0A-\\x1F\\x7F\";:,]*)|\"(?<{_paramValueGroup}>[^\\x00-\\x08\\x0A-\\x1F\\x7F\"]*)\")";

            // param         = param-name "=" param-value *("," param-value)
            // param-name    = iana-token / x-name
            var paramName = $"(?<{_paramNameGroup}>{identifier})";
            var param = $"{paramName}={paramValue}(,{paramValue})*";

            // contentline   = name *(";" param ) ":" value CRLF
            var name = $"(?<{_nameGroup}>{identifier})";
            // value         = *VALUE-CHAR
            var value = $"(?<{_valueGroup}>[^\\x00-\\x08\\x0E-\\x1F\\x7F]*)";
            var contentLine = $"^{name}(;{param})*:{value}$";
            return contentLine;
        }

        private static ParsedLine ParseLine(string input)
        {
            var match = _contentLineRegex.Match(input);
            if (!match.Success)
            {
                throw new SerializationException($"Could not parse line: '{input}'");
            }
            var name = match.Groups[_nameGroup].Value;
            var value = match.Groups[_valueGroup].Value;
            var paramNames = match.Groups[_paramNameGroup].Captures;
            var paramValues = match.Groups[_paramValueGroup].Captures;
            
            return new ParsedLine(name, value, paramNames, paramValues);
        }
        
        public IEnumerable<ICalendarComponent> Deserialize(TextReader reader)
        {
            var context = new SerializationContext();
            var stack = new Stack<ICalendarComponent>();
            var current = default(ICalendarComponent);
            
            foreach (ParsedLine parsedLine in GetContentLines(reader))
            {
                var contentLine = ParseContentLine(context, parsedLine);
                if (string.Equals(contentLine.Name, Scope.Begin, StringComparison.OrdinalIgnoreCase))
                {
                    stack.Push(current);
                    current = _componentFactory.Build((string)contentLine.Value);
                    SerializationUtil.OnDeserializing(current);
                }
                else
                {
                    if (current == null)
                    {
                        throw new SerializationException($"Expected '{Scope.Begin}', found '{contentLine.Name}'");
                    }
                    if (string.Equals(contentLine.Name, Scope.End, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!string.Equals((string)contentLine.Value, current.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            throw new SerializationException($"Expected '{Scope.End}:{current.Name}', found 'END:{contentLine.Value}'");
                        }
                        SerializationUtil.OnDeserialized(current);
                        var finished = current;
                        current = stack.Pop();
                        if (current == null)
                        {
                            yield return finished;
                        }
                        else
                        {
                            current.Children.Add(finished);
                        }
                    }
                    else
                    {
                        current.Properties.Add(contentLine);
                    }
                }
            }
            if (current != null)
            {
                throw new SerializationException($"Unclosed component {current.Name}");
            }
        }
        
        private CalendarProperty ParseContentLine(SerializationContext context, ParsedLine parsedLine)
        {
            var property = new CalendarProperty(parsedLine.Name.ToUpperInvariant());
            context.Push(property);
            SetPropertyParameters(property, parsedLine.ParamNames, parsedLine.ParamValues);
            SetPropertyValue(context, property, parsedLine.Value);
            context.Pop();
            return property;
        }

        private static void SetPropertyParameters(CalendarProperty property, CaptureCollection paramNames, CaptureCollection paramValues)
        {
            var paramValueIndex = 0;
            for (var paramNameIndex = 0; paramNameIndex < paramNames.Count; paramNameIndex++)
            {
                var paramName = paramNames[paramNameIndex].Value;
                var parameter = new CalendarParameter(paramName);
                var nextParamIndex = paramNameIndex + 1 < paramNames.Count ? paramNames[paramNameIndex + 1].Index : int.MaxValue;
                while (paramValueIndex < paramValues.Count && paramValues[paramValueIndex].Index < nextParamIndex)
                {
                    var paramValue = paramValues[paramValueIndex].Value;
                    parameter.AddValue(paramValue);
                    paramValueIndex++;
                }
                property.AddParameter(parameter);
            }
        }

        private void SetPropertyValue(SerializationContext context, CalendarProperty property, string value)
        {
            var type = _dataTypeMapper.GetPropertyMapping(property) ?? typeof(string);
            var serializer = (SerializerBase)_serializerFactory.Build(type, context);
            using (var valueReader = new StringReader(value))
            {
                var propertyValue = serializer.Deserialize(valueReader);
                var propertyValues = propertyValue as IEnumerable<string>;
                if (propertyValues != null)
                {
                    foreach (var singlePropertyValue in propertyValues)
                    {
                        property.AddValue(singlePropertyValue);
                    }
                }
                else
                {
                    property.AddValue(propertyValue);
                }
            }
        }

        /// <summary>
        /// Parses iCalendar file but puts all VTIMEZONE occurrences first
        /// in order to use time zone(s) to convert date time to UTC later during the parsing
        /// </summary>
        /// <param name="reader">content of iCalendar in stream format</param>
        /// <returns><see cref="IEnumerable{T}">IEnumerable&lt;ParsedLine&gt;</see> where <see cref="ParsedLine"/> is a token of the parsed string</returns>
        private static IEnumerable<ParsedLine> GetContentLines(TextReader reader)
        {
            var lines = new List<ParsedLine>();
            bool isvTimeZone = false;
            while (true)
            {
                #region Get next line -> break / continue / process

                var nextLine = reader.ReadLine();
                if (nextLine == null)
                {
                    break;
                }

                nextLine = nextLine.Trim();

                if (nextLine.Length <= 0)
                {
                    continue;
                }

                #endregion

                ParsedLine parsedLine = ParseLine(nextLine);

                switch (parsedLine.Name, parsedLine.Value, isvTimeZone)
                {
                    case(Scope.Begin, Components.Calendar,false):
                        yield return parsedLine;
                        break;
                    
                    case(Scope.Begin, Components.Calendar,true):
                    case(Scope.End, Components.Calendar,true):
                        throw new SerializationException($"'{parsedLine.Name}:VCALENDAR was encountered while parsing VTIMEZONE'");
                   
                    case(Scope.End, Components.Calendar,false):
                        lines.Add(parsedLine);
                        foreach (ParsedLine token in lines)
                        {
                            yield return token;
                        }
                        lines.Clear();
                        break;

                    case (Scope.Begin, Components.Timezone, false):
                        isvTimeZone = true;
                        yield return parsedLine;
                        break;
                    
                    case (Scope.Begin, Components.Timezone, true):
                        throw new SerializationException($"'BEGIN:VTIMEZONE appeared the second time before END:VTIMEZONE'");
                    
                    case (Scope.End, Components.Timezone, true):
                        isvTimeZone = false;
                        yield return parsedLine;
                        break;
                    
                    case (_, _, true):
                        yield return parsedLine;
                        break;
                    
                    default:
                        lines.Add(parsedLine);
                        break;
                }
            }

        }
    }
}
