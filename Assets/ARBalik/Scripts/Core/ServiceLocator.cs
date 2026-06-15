using System;
using System.Collections.Generic;

namespace ARFishing.Core
{
    public static class ServiceLocator
    {
        static readonly Dictionary<Type, object> s_Services = new();

        public static void Register<T>(T service) where T : class
        {
            s_Services[typeof(T)] = service;
        }

        public static T Get<T>() where T : class
        {
            return s_Services.TryGetValue(typeof(T), out var svc) ? (T)svc : null;
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            if (s_Services.TryGetValue(typeof(T), out var svc))
            {
                service = (T)svc;
                return true;
            }
            service = null;
            return false;
        }

        public static void Unregister<T>(T service) where T : class
        {
            if (s_Services.TryGetValue(typeof(T), out var existing) && ReferenceEquals(existing, service))
            {
                s_Services.Remove(typeof(T));
            }
        }

        public static void Clear()
        {
            s_Services.Clear();
        }
    }
}
