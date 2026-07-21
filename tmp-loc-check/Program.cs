using System.Globalization;
using Qalam.Data.Commons;
CultureInfo.CurrentCulture = new CultureInfo("ar-EG");
Console.WriteLine("ar=" + LocalizableEntity.GetLocalizedValue("зяхМ", "English"));
CultureInfo.CurrentCulture = new CultureInfo("en-US");
Console.WriteLine("en=" + LocalizableEntity.GetLocalizedValue("зяхМ", "English"));
