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
    }
}

