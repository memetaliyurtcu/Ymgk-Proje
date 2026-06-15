using System;

namespace ARFishing.Localization
{
    public static class Localizer
    {
        static Locale s_Active = Locale.Turkish;
        static LocalizationTable s_Table;

        public static event Action LocaleChanged;

        public static Locale Active
        {
            get => s_Active;
            set
            {
                if (s_Active == value) return;
                s_Active = value;
                LocaleChanged?.Invoke();
            }
        }

        public static LocalizationTable Table
        {
            get => s_Table;
            set
            {
                if (s_Table == value) return;
                s_Table = value;
                LocaleChanged?.Invoke();
            }
        }

        public static string Get(string key, string fallback = null)
        {
            if (s_Table == null) return fallback ?? key;
            return s_Table.Get(key, s_Active, fallback);
        }

        public static bool TryGet(string key, out string value)
        {
            value = null;
            return s_Table != null && s_Table.TryGet(key, s_Active, out value);
        }
    }
}
