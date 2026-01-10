using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Seeding;

public class QuranSubjectsSeeder
{
    public static async Task SeedAsync(ApplicationDBContext context)
    {
        var quranDomainId = 2; // Quran domain
        
        // Get Quran levels by NameEn
        var beginnerLevel = await context.QuranLevels.FirstOrDefaultAsync(ql => ql.NameEn == "Beginner");
        var intermediateLevel = await context.QuranLevels.FirstOrDefaultAsync(ql => ql.NameEn == "Intermediate");
        var advancedLevel = await context.QuranLevels.FirstOrDefaultAsync(ql => ql.NameEn == "Advanced");

        if (beginnerLevel == null || intermediateLevel == null || advancedLevel == null)
        {
            throw new Exception("Quran levels must be seeded before Quran subjects");
        }

        if (!await context.Subjects.AnyAsync(s => s.DomainId == quranDomainId))
        {
            var subjects = new List<Subject>();

            // ====== BEGINNER LEVEL QURAN SUBJECTS ======
            // Memorization subjects for common short Surahs (Juz 30)
            subjects.Add(new Subject
            {
                DomainId = quranDomainId,
                NameAr = "حفظ جزء عم",
                NameEn = "Memorization of Juz Amma (Part 30)",
                DescriptionAr = "حفظ وتسميع السور القصيرة من جزء عم",
                DescriptionEn = "Memorization and recitation of short Surahs from Juz Amma",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            subjects.Add(new Subject
            {
                DomainId = quranDomainId,
                NameAr = "التلاوة الصحيحة - مستوى مبتدئ",
                NameEn = "Proper Recitation - Beginner Level",
                DescriptionAr = "تعلم القراءة الصحيحة للقرآن الكريم",
                DescriptionEn = "Learning proper Quranic recitation",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            subjects.Add(new Subject
            {
                DomainId = quranDomainId,
                NameAr = "أحكام التجويد - المستوى الأول",
                NameEn = "Tajweed Rules - Level 1",
                DescriptionAr = "تعلم أحكام التجويد الأساسية",
                DescriptionEn = "Learning basic Tajweed rules",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            subjects.Add(new Subject
            {
                DomainId = quranDomainId,
                NameAr = "حفظ السور المشهورة",
                NameEn = "Memorization of Popular Surahs",
                DescriptionAr = "حفظ السور المشهورة (الكهف، يس، الملك، الواقعة)",
                DescriptionEn = "Memorizing popular Surahs (Al-Kahf, Yasin, Al-Mulk, Al-Waqi'ah)",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            subjects.Add(new Subject
            {
                DomainId = quranDomainId,
                NameAr = "تفسير السور القصيرة",
                NameEn = "Tafsir of Short Surahs",
                DescriptionAr = "فهم معاني ودروس السور القصيرة",
                DescriptionEn = "Understanding meanings and lessons of short Surahs",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            // ====== INTERMEDIATE LEVEL QURAN SUBJECTS ======
            subjects.Add(new Subject
            {
                DomainId = quranDomainId,
                NameAr = "حفظ الأجزاء (1-10)",
                NameEn = "Memorization of Parts (1-10)",
                DescriptionAr = "حفظ الأجزاء من الأول إلى العاشر من القرآن الكريم",
                DescriptionEn = "Memorizing parts 1-10 of the Holy Quran",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            subjects.Add(new Subject
            {
                DomainId = quranDomainId,
                NameAr = "حفظ الأجزاء (11-20)",
                NameEn = "Memorization of Parts (11-20)",
                DescriptionAr = "حفظ الأجزاء من الحادي عشر إلى العشرين من القرآن الكريم",
                DescriptionEn = "Memorizing parts 11-20 of the Holy Quran",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            subjects.Add(new Subject
            {
                DomainId = quranDomainId,
                NameAr = "التلاوة الصحيحة - مستوى متوسط",
                NameEn = "Proper Recitation - Intermediate Level",
                DescriptionAr = "تحسين التلاوة وتطبيق أحكام التجويد",
                DescriptionEn = "Improving recitation and applying Tajweed rules",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            subjects.Add(new Subject
            {
                DomainId = quranDomainId,
                NameAr = "أحكام التجويد - المستوى الثاني",
                NameEn = "Tajweed Rules - Level 2",
                DescriptionAr = "دراسة متقدمة لأحكام التجويد والمخارج",
                DescriptionEn = "Advanced study of Tajweed rules and articulation points",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            subjects.Add(new Subject
            {
                DomainId = quranDomainId,
                NameAr = "المراجعة والتثبيت",
                NameEn = "Review and Retention",
                DescriptionAr = "مراجعة وتثبيت الحفظ السابق",
                DescriptionEn = "Reviewing and strengthening previous memorization",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            subjects.Add(new Subject
            {
                DomainId = quranDomainId,
                NameAr = "تفسير متوسط",
                NameEn = "Intermediate Tafsir",
                DescriptionAr = "فهم معاني وتفسير السور المختارة",
                DescriptionEn = "Understanding meanings and interpretation of selected Surahs",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            // ====== ADVANCED LEVEL QURAN SUBJECTS ======
            subjects.Add(new Subject
            {
                DomainId = quranDomainId,
                NameAr = "حفظ الأجزاء (21-30)",
                NameEn = "Memorization of Parts (21-30)",
                DescriptionAr = "حفظ الأجزاء من الحادي والعشرين إلى الثلاثين",
                DescriptionEn = "Memorizing parts 21-30 of the Holy Quran",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            subjects.Add(new Subject
            {
                DomainId = quranDomainId,
                NameAr = "إتمام حفظ القرآن الكريم",
                NameEn = "Complete Quran Memorization",
                DescriptionAr = "إتمام حفظ القرآن الكريم كاملاً",
                DescriptionEn = "Completing the memorization of the entire Holy Quran",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            subjects.Add(new Subject
            {
                DomainId = quranDomainId,
                NameAr = "التلاوة المتقنة",
                NameEn = "Mastered Recitation",
                DescriptionAr = "إتقان التلاوة بالأحكام والمقامات",
                DescriptionEn = "Mastering recitation with rules and melodic modes",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            subjects.Add(new Subject
            {
                DomainId = quranDomainId,
                NameAr = "أحكام التجويد - المستوى المتقدم",
                NameEn = "Tajweed Rules - Advanced Level",
                DescriptionAr = "دراسة شاملة ومتقدمة لجميع أحكام التجويد",
                DescriptionEn = "Comprehensive and advanced study of all Tajweed rules",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            subjects.Add(new Subject
            {
                DomainId = quranDomainId,
                NameAr = "القراءات العشر",
                NameEn = "The Ten Qira'at",
                DescriptionAr = "دراسة القراءات القرآنية العشر المتواترة",
                DescriptionEn = "Study of the ten authentic Quranic readings",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            subjects.Add(new Subject
            {
                DomainId = quranDomainId,
                NameAr = "تفسير متقدم",
                NameEn = "Advanced Tafsir",
                DescriptionAr = "دراسة متعمقة لتفسير القرآن الكريم",
                DescriptionEn = "In-depth study of Quranic interpretation",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            subjects.Add(new Subject
            {
                DomainId = quranDomainId,
                NameAr = "علوم القرآن",
                NameEn = "Quranic Sciences",
                DescriptionAr = "دراسة علوم القرآن (أسباب النزول، الناسخ والمنسوخ، المكي والمدني)",
                DescriptionEn = "Study of Quranic sciences (reasons of revelation, abrogation, Makki and Madani)",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            subjects.Add(new Subject
            {
                DomainId = quranDomainId,
                NameAr = "الإجازة القرآنية",
                NameEn = "Quranic Ijazah",
                DescriptionAr = "التحضير للحصول على الإجازة في القرآن الكريم",
                DescriptionEn = "Preparation for obtaining Quran Ijazah certification",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            await context.Subjects.AddRangeAsync(subjects);
            await context.SaveChangesAsync();
        }
    }
}

