using System;
using System.Collections.Generic;
using Ical.Net.CalendarComponents;

namespace Ical.Net.Serialization
{
    public class SerializationContext
    {
        private static SerializationContext _default;

        /// <summary>
        /// Gets the Singleton instance of the SerializationContext class.
        /// </summary>
        public static SerializationContext Default
        {
            get
            {
                if (_default == null)
                {
                    _default = new SerializationContext();
                }

                // Create a new serialization context that doesn't contain any objects
                // (and is non-static).  That way, if any objects get pushed onto
                // the serialization stack when the Default serialization context is used,
                // and something goes wrong and the objects don't get popped off the stack,
                // we don't need to worry (as much) about a memory leak, because the
                // objects weren't pushed onto a stack referenced by a static variable.
                var ctx = new SerializationContext
                {
                    _mServiceProvider = _default._mServiceProvider
                };
                return ctx;
            }
        }


        private readonly Stack<WeakReference> _mStack = new ();
        private ServiceProvider _mServiceProvider = new ();
		private readonly Dictionary<string, TimeZoneInfo> _vTimeZones;

        public SerializationContext()
        {
            // Add some services by default
            SetService(new SerializerFactory());
            SetService(new CalendarComponentFactory());
            SetService(new DataTypeMapper());
            SetService(new EncodingStack());
            SetService(new EncodingProvider(this));
			_vTimeZones = new Dictionary<string, TimeZoneInfo>();
		}

        public virtual void Push(object item)
        {
            if (item != null)
            {
                _mStack.Push(new WeakReference(item));
            }
        }

        public virtual object Pop()
        {
            if (_mStack.Count > 0)
            {
                var r = _mStack.Pop();
                if (r.IsAlive)
                {
                    return r.Target;
                }
            }
            return null;
        }

        public virtual object Peek()
        {
            if (_mStack.Count > 0)
            {
                var r = _mStack.Peek();
                if (r.IsAlive)
                {
                    return r.Target;
                }
            }
            return null;
        }

        public virtual object GetService(Type serviceType) => _mServiceProvider.GetService(serviceType);

        public virtual object GetService(string name) => _mServiceProvider.GetService(name);

        public virtual T GetService<T>() => _mServiceProvider.GetService<T>();

        public virtual T GetService<T>(string name) => _mServiceProvider.GetService<T>(name);

        public virtual void SetService(string name, object obj)
        {
            _mServiceProvider.SetService(name, obj);
        }

        public virtual void SetService(object obj)
        {
            _mServiceProvider.SetService(obj);
        }

        public virtual void RemoveService(Type type)
        {
            _mServiceProvider.RemoveService(type);
        }

        public virtual void RemoveService(string name)
        {
            _mServiceProvider.RemoveService(name);
        }

		/// <summary>
		/// Adds <see cref="TimeZoneInfo"/> object taken from <paramref name="vTimeZone"/>.
		/// If <see cref="TimeZoneInfo"/> object is <see cref="null"/> nothing is added.
		/// </summary>
		/// <param name="vTimeZone"><see cref="VTimeZone"/> object calculated after parsing VTIMEZONE section</param>
		public virtual void AddTimeZone(VTimeZone vTimeZone)
		{
			if (vTimeZone.TimeZoneInfo != null)
			{
				_vTimeZones.Add(vTimeZone.TzId, vTimeZone.TimeZoneInfo);
			}
		}

		/// <summary>
		/// Gets <see cref="TimeZoneInfo"/> according to <paramref name="tzId"/> value.
		/// </summary>
		/// <param name="tzId">TZID</param>
		/// <returns>
		/// <see cref="TimeZoneInfo"/> corresponding to <paramref name="tzId"/>.
		/// <see cref="null"/> if there is no time zone ifo with such <paramref name="tzId"/>
		/// </returns>
		public virtual TimeZoneInfo GetTimeZone(string tzId)
		{
			if (_vTimeZones.ContainsKey(tzId))
			{
				return _vTimeZones[tzId];
			}

			return default;
		}

		public virtual void ClearTimeZones() => _vTimeZones.Clear();
	}
}
