using System;
using System.Collections.Generic;

namespace SpaceMaintenance.Core
{
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> _listeners = new Dictionary<Type, List<Delegate>>();

        public static void Subscribe<T>(Action<T> listener)
        {
            var type = typeof(T);
            if (!_listeners.ContainsKey(type))
            {
                _listeners[type] = new List<Delegate>();
            }
            _listeners[type].Add(listener);
        }

        public static void Unsubscribe<T>(Action<T> listener)
        {
            var type = typeof(T);
            if (_listeners.ContainsKey(type))
            {
                _listeners[type].Remove(listener);
            }
        }

        public static void Publish<T>(T eventMessage)
        {
            var type = typeof(T);
            if (_listeners.ContainsKey(type))
            {
                var listeners = new List<Delegate>(_listeners[type]);
                foreach (var listener in listeners)
                {
                    (listener as Action<T>)?.Invoke(eventMessage);
                }
            }
        }
    }
}
