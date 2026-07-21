using System;
using System.Globalization;
using System.Threading;

namespace Qalam.Data.Commons
{
    public class LocalizableEntity
    {
        public string NameAr { get; set; } = default!;
        public string NameEn { get; set; } = default!;

        public string GetLocalized()
        {
            CultureInfo culture = Thread.CurrentThread.CurrentCulture;
            if (culture.TwoLetterISOLanguageName.ToLower().Equals("ar"))
                return NameAr;
            return NameEn;
        }

        /// <summary>
        /// Gets the localized value based on current culture.
        /// Falls back to the other language when the preferred value is null/empty.
        /// </summary>
        public static string? GetLocalizedValue(string? arabicValue, string? englishValue)
        {
            CultureInfo culture = Thread.CurrentThread.CurrentCulture;
            if (culture.TwoLetterISOLanguageName.Equals("ar", StringComparison.OrdinalIgnoreCase))
                return !string.IsNullOrWhiteSpace(arabicValue) ? arabicValue : englishValue;
            return !string.IsNullOrWhiteSpace(englishValue) ? englishValue : arabicValue;
        }
    }
}

