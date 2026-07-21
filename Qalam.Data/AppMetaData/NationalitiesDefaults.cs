using Qalam.Data.Entity.Common;

namespace Qalam.Data.AppMetaData;

/// <summary>
/// Default nationality rows. Arabic countries first (SortOrder 10+), then common others (1000+).
/// </summary>
public static class NationalitiesDefaults
{
    public static IReadOnlyList<Nationality> Create(DateTime? createdAt = null)
    {
        var now = createdAt ?? DateTime.UtcNow;
        var list = new List<Nationality>();

        // Arabic / MENA countries first
        Add(list, now, "SA", "المملكة العربية السعودية", "Saudi Arabia", "🇸🇦", 10);
        Add(list, now, "EG", "مصر", "Egypt", "🇪🇬", 20);
        Add(list, now, "AE", "الإمارات العربية المتحدة", "United Arab Emirates", "🇦🇪", 30);
        Add(list, now, "KW", "الكويت", "Kuwait", "🇰🇼", 40);
        Add(list, now, "QA", "قطر", "Qatar", "🇶🇦", 50);
        Add(list, now, "BH", "البحرين", "Bahrain", "🇧🇭", 60);
        Add(list, now, "OM", "عُمان", "Oman", "🇴🇲", 70);
        Add(list, now, "JO", "الأردن", "Jordan", "🇯🇴", 80);
        Add(list, now, "SY", "سوريا", "Syria", "🇸🇾", 90);
        Add(list, now, "LB", "لبنان", "Lebanon", "🇱🇧", 100);
        Add(list, now, "PS", "فلسطين", "Palestine", "🇵🇸", 110);
        Add(list, now, "IQ", "العراق", "Iraq", "🇮🇶", 120);
        Add(list, now, "YE", "اليمن", "Yemen", "🇾🇪", 130);
        Add(list, now, "SD", "السودان", "Sudan", "🇸🇩", 140);
        Add(list, now, "LY", "ليبيا", "Libya", "🇱🇾", 150);
        Add(list, now, "TN", "تونس", "Tunisia", "🇹🇳", 160);
        Add(list, now, "DZ", "الجزائر", "Algeria", "🇩🇿", 170);
        Add(list, now, "MA", "المغرب", "Morocco", "🇲🇦", 180);
        Add(list, now, "MR", "موريتانيا", "Mauritania", "🇲🇷", 190);
        Add(list, now, "SO", "الصومال", "Somalia", "🇸🇴", 200);
        Add(list, now, "DJ", "جيبوتي", "Djibouti", "🇩🇯", 210);
        Add(list, now, "KM", "جزر القمر", "Comoros", "🇰🇲", 220);

        // Common others
        Add(list, now, "US", "الولايات المتحدة", "United States", "🇺🇸", 1000);
        Add(list, now, "GB", "المملكة المتحدة", "United Kingdom", "🇬🇧", 1010);
        Add(list, now, "CA", "كندا", "Canada", "🇨🇦", 1020);
        Add(list, now, "AU", "أستراليا", "Australia", "🇦🇺", 1030);
        Add(list, now, "FR", "فرنسا", "France", "🇫🇷", 1040);
        Add(list, now, "DE", "ألمانيا", "Germany", "🇩🇪", 1050);
        Add(list, now, "TR", "تركيا", "Turkey", "🇹🇷", 1060);
        Add(list, now, "PK", "باكستان", "Pakistan", "🇵🇰", 1070);
        Add(list, now, "IN", "الهند", "India", "🇮🇳", 1080);
        Add(list, now, "ID", "إندونيسيا", "Indonesia", "🇮🇩", 1090);
        Add(list, now, "MY", "ماليزيا", "Malaysia", "🇲🇾", 1100);
        Add(list, now, "BD", "بنغلاديش", "Bangladesh", "🇧🇩", 1110);
        Add(list, now, "NG", "نيجيريا", "Nigeria", "🇳🇬", 1120);
        Add(list, now, "ZA", "جنوب أفريقيا", "South Africa", "🇿🇦", 1130);
        Add(list, now, "BR", "البرازيل", "Brazil", "🇧🇷", 1140);
        Add(list, now, "CN", "الصين", "China", "🇨🇳", 1150);
        Add(list, now, "JP", "اليابان", "Japan", "🇯🇵", 1160);
        Add(list, now, "KR", "كوريا الجنوبية", "South Korea", "🇰🇷", 1170);
        Add(list, now, "RU", "روسيا", "Russia", "🇷🇺", 1180);
        Add(list, now, "IT", "إيطاليا", "Italy", "🇮🇹", 1190);
        Add(list, now, "ES", "إسبانيا", "Spain", "🇪🇸", 1200);
        Add(list, now, "NL", "هولندا", "Netherlands", "🇳🇱", 1210);
        Add(list, now, "SE", "السويد", "Sweden", "🇸🇪", 1220);
        Add(list, now, "NO", "النرويج", "Norway", "🇳🇴", 1230);
        Add(list, now, "DK", "الدنمارك", "Denmark", "🇩🇰", 1240);
        Add(list, now, "FI", "فنلندا", "Finland", "🇫🇮", 1250);
        Add(list, now, "CH", "سويسرا", "Switzerland", "🇨🇭", 1260);
        Add(list, now, "AT", "النمسا", "Austria", "🇦🇹", 1270);
        Add(list, now, "BE", "بلجيكا", "Belgium", "🇧🇪", 1280);
        Add(list, now, "IE", "أيرلندا", "Ireland", "🇮🇪", 1290);
        Add(list, now, "NZ", "نيوزيلندا", "New Zealand", "🇳🇿", 1300);
        Add(list, now, "SG", "سنغافورة", "Singapore", "🇸🇬", 1310);
        Add(list, now, "PH", "الفلبين", "Philippines", "🇵🇭", 1320);
        Add(list, now, "TH", "تايلاند", "Thailand", "🇹🇭", 1330);
        Add(list, now, "VN", "فيتنام", "Vietnam", "🇻🇳", 1340);
        Add(list, now, "AF", "أفغانستان", "Afghanistan", "🇦🇫", 1350);
        Add(list, now, "IR", "إيران", "Iran", "🇮🇷", 1360);
        Add(list, now, "ET", "إثيوبيا", "Ethiopia", "🇪🇹", 1370);
        Add(list, now, "KE", "كينيا", "Kenya", "🇰🇪", 1380);
        Add(list, now, "GH", "غانا", "Ghana", "🇬🇭", 1390);
        Add(list, now, "MX", "المكسيك", "Mexico", "🇲🇽", 1400);
        Add(list, now, "AR", "الأرجنتين", "Argentina", "🇦🇷", 1410);
        Add(list, now, "CL", "تشيلي", "Chile", "🇨🇱", 1420);
        Add(list, now, "CO", "كولومبيا", "Colombia", "🇨🇴", 1430);
        Add(list, now, "PE", "بيرو", "Peru", "🇵🇪", 1440);
        Add(list, now, "UA", "أوكرانيا", "Ukraine", "🇺🇦", 1450);
        Add(list, now, "PL", "بولندا", "Poland", "🇵🇱", 1460);
        Add(list, now, "PT", "البرتغال", "Portugal", "🇵🇹", 1470);
        Add(list, now, "GR", "اليونان", "Greece", "🇬🇷", 1480);
        Add(list, now, "CZ", "التشيك", "Czechia", "🇨🇿", 1490);
        Add(list, now, "RO", "رومانيا", "Romania", "🇷🇴", 1500);
        Add(list, now, "HU", "المجر", "Hungary", "🇭🇺", 1510);
        Add(list, now, "UZ", "أوزبكستان", "Uzbekistan", "🇺🇿", 1520);
        Add(list, now, "KZ", "كازاخستان", "Kazakhstan", "🇰🇿", 1530);
        Add(list, now, "AZ", "أذربيجان", "Azerbaijan", "🇦🇿", 1540);
        Add(list, now, "GE", "جورجيا", "Georgia", "🇬🇪", 1550);
        Add(list, now, "AM", "أرمينيا", "Armenia", "🇦🇲", 1560);
        Add(list, now, "NP", "نيبال", "Nepal", "🇳🇵", 1570);
        Add(list, now, "LK", "سريلانكا", "Sri Lanka", "🇱🇰", 1580);
        Add(list, now, "MM", "ميانمار", "Myanmar", "🇲🇲", 1590);
        Add(list, now, "KH", "كمبوديا", "Cambodia", "🇰🇭", 1600);
        Add(list, now, "SN", "السنغال", "Senegal", "🇸🇳", 1610);
        Add(list, now, "CI", "ساحل العاج", "Côte d'Ivoire", "🇨🇮", 1620);
        Add(list, now, "CM", "الكاميرون", "Cameroon", "🇨🇲", 1630);
        Add(list, now, "UG", "أوغندا", "Uganda", "🇺🇬", 1640);
        Add(list, now, "TZ", "تنزانيا", "Tanzania", "🇹🇿", 1650);
        Add(list, now, "ZW", "زيمبابوي", "Zimbabwe", "🇿🇼", 1660);
        Add(list, now, "AO", "أنغولا", "Angola", "🇦🇴", 1670);
        Add(list, now, "MZ", "موزمبيق", "Mozambique", "🇲🇿", 1680);
        Add(list, now, "ER", "إريتريا", "Eritrea", "🇪🇷", 1690);
        Add(list, now, "TD", "تشاد", "Chad", "🇹🇩", 1700);
        Add(list, now, "NE", "النيجر", "Niger", "🇳🇪", 1710);
        Add(list, now, "ML", "مالي", "Mali", "🇲🇱", 1720);
        Add(list, now, "GM", "غامبيا", "Gambia", "🇬🇲", 1730);
        Add(list, now, "SL", "سيراليون", "Sierra Leone", "🇸🇱", 1740);
        Add(list, now, "LR", "ليبيريا", "Liberia", "🇱🇷", 1750);
        Add(list, now, "CF", "جمهورية أفريقيا الوسطى", "Central African Republic", "🇨🇫", 1760);
        Add(list, now, "SS", "جنوب السودان", "South Sudan", "🇸🇸", 1770);
        Add(list, now, "AL", "ألبانيا", "Albania", "🇦🇱", 1780);
        Add(list, now, "BA", "البوسنة والهرسك", "Bosnia and Herzegovina", "🇧🇦", 1790);
        Add(list, now, "XK", "كوسوفو", "Kosovo", "🇽🇰", 1800);
        Add(list, now, "MK", "مقدونيا الشمالية", "North Macedonia", "🇲🇰", 1810);
        Add(list, now, "MD", "مولدوفا", "Moldova", "🇲🇩", 1820);
        Add(list, now, "BY", "بيلاروس", "Belarus", "🇧🇾", 1830);
        Add(list, now, "BG", "بلغاريا", "Bulgaria", "🇧🇬", 1840);
        Add(list, now, "HR", "كرواتيا", "Croatia", "🇭🇷", 1850);
        Add(list, now, "RS", "صربيا", "Serbia", "🇷🇸", 1860);
        Add(list, now, "SI", "سلوفينيا", "Slovenia", "🇸🇮", 1870);
        Add(list, now, "SK", "سلوفاكيا", "Slovakia", "🇸🇰", 1880);
        Add(list, now, "LT", "ليتوانيا", "Lithuania", "🇱🇹", 1890);
        Add(list, now, "LV", "لاتفيا", "Latvia", "🇱🇻", 1900);
        Add(list, now, "EE", "إستونيا", "Estonia", "🇪🇪", 1910);
        Add(list, now, "IS", "آيسلندا", "Iceland", "🇮🇸", 1920);
        Add(list, now, "LU", "لوكسمبورغ", "Luxembourg", "🇱🇺", 1930);
        Add(list, now, "MT", "مالطا", "Malta", "🇲🇹", 1940);
        Add(list, now, "CY", "قبرص", "Cyprus", "🇨🇾", 1950);
        Add(list, now, "HK", "هونغ كونغ", "Hong Kong", "🇭🇰", 1960);
        Add(list, now, "TW", "تايوان", "Taiwan", "🇹🇼", 1970);
        Add(list, now, "IL", "إسرائيل", "Israel", "🇮🇱", 1980);

        return list;
    }

    private static void Add(
        List<Nationality> list,
        DateTime now,
        string code,
        string nameAr,
        string nameEn,
        string flagEmoji,
        int sortOrder)
    {
        list.Add(new Nationality
        {
            Code = code,
            NameAr = nameAr,
            NameEn = nameEn,
            FlagEmoji = flagEmoji,
            IsActive = true,
            SortOrder = sortOrder,
            CreatedAt = now
        });
    }
}
