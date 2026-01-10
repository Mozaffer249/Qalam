# Qalam Platform - Database Seeding Guide

## 📋 Overview

This document provides comprehensive information about the database seeding system for the Qalam educational platform. The seeding system populates the database with initial data for all education domains: School Education (Saudi Curriculum), Quran, Languages, and General Skills.

---

## 🗂️ Seeding Structure

### Directory Location
All seeder files are located in: `Qalam.Infrastructure/Seeding/`

### Seeder Files

#### 1. Infrastructure Seeders
- **EducationDomainsSeeder.cs** - Seeds 4 education domains
- **CurriculumsSeeder.cs** - Seeds curricula (Saudi, Egyptian, American)
- **TeachingModesSeeder.cs** - Seeds teaching modes (In-Person, Online)
- **SessionTypesSeeder.cs** - Seeds session types (Individual, Group)
- **QuranLevelsSeeder.cs** - Seeds Quran proficiency levels
- **QuranContentTypesSeeder.cs** - Seeds Quran content types
- **TimeSlotsSeeder.cs** - Seeds common time slots

#### 2. Saudi Education System Seeders
- **SaudiEducationLevelsSeeder.cs** - Seeds 3 education levels (Elementary, Intermediate, Secondary)
- **SaudiGradesSeeder.cs** - Seeds 12 grades across all levels
- **SaudiAcademicTermsSeeder.cs** - Seeds 3 academic terms
- **SaudiSubjectsSeeder.cs** - Seeds ~120 subjects for all Saudi grades

#### 3. Quran Domain Seeders
- **QuranSubjectsSeeder.cs** - Seeds 18 Quran subjects across 3 levels

#### 4. Languages Domain Seeders
- **LanguageLevelsSeeder.cs** - Seeds language proficiency levels and grades
- **LanguageSubjectsSeeder.cs** - Seeds subjects for 8 languages

#### 5. General Skills Domain Seeders
- **GeneralSkillsSubjectsSeeder.cs** - Seeds 35 skill subjects

#### 6. Master Seeder
- **DatabaseSeeder.cs** - Orchestrates all seeders in correct dependency order

---

## 📊 Data Breakdown

### Education Domains (4 domains)
1. **School Education** (تعليم مدرسي) - Academic school education
2. **Quran** (قرآن كريم) - Quran education and memorization
3. **Languages** (لغات) - Foreign and Arabic language education
4. **General Skills** (مهارات عامة) - Life, professional, and technical skills

### Saudi Education System

#### Levels (3)
- **Elementary** (المرحلة الابتدائية) - Grades 1-6
- **Intermediate** (المرحلة المتوسطة) - Grades 1-3
- **Secondary** (المرحلة الثانوية) - Grades 1-3

#### Academic Terms (3)
- First Term (الفصل الدراسي الأول)
- Second Term (الفصل الدراسي الثاني)
- Third Term (الفصل الدراسي الثالث)

#### Subjects by Level

**Elementary (Grades 1-6):**
- Common subjects for all grades:
  - Arabic Language (اللغة العربية)
  - Islamic Education (التربية الإسلامية)
  - Mathematics (الرياضيات)
  - Science (العلوم)
  - Art Education (التربية الفنية)
  - Physical Education (التربية البدنية)
- Additional subjects from Grade 4:
  - English Language (اللغة الإنجليزية)
  - Digital Skills (المهارات الرقمية)

**Intermediate (Grades 1-3):**
- All 10 subjects including: Arabic, Islamic Ed, Math, Science, English, Social Studies, Art, Physical Ed, Digital Skills, Critical Thinking

**Secondary (Grades 1-3):**
- All 11 subjects including: Arabic, Islamic Ed, Math, English, Physics, Chemistry, Biology, History, Geography, Computer Science, Physical Ed

**Total Saudi Subjects:** ~120 subjects

### Quran Domain

#### Levels
- Preparatory (تمهيدي)
- Beginner (مبتدئ)
- Intermediate (متوسط)
- Advanced (متقدم)

#### Subjects (18 total)

**Beginner Level (5 subjects):**
- Memorization of Juz Amma (Part 30)
- Proper Recitation - Beginner Level
- Tajweed Rules - Level 1
- Memorization of Popular Surahs
- Tafsir of Short Surahs

**Intermediate Level (6 subjects):**
- Memorization of Parts (1-10)
- Memorization of Parts (11-20)
- Proper Recitation - Intermediate Level
- Tajweed Rules - Level 2
- Review and Retention
- Intermediate Tafsir

**Advanced Level (7 subjects):**
- Memorization of Parts (21-30)
- Complete Quran Memorization
- Mastered Recitation
- Tajweed Rules - Advanced Level
- The Ten Qira'at (القراءات العشر)
- Advanced Tafsir
- Quranic Sciences
- Quranic Ijazah

### Languages Domain

#### Supported Languages (8)
1. English (الإنجليزية)
2. French (الفرنسية)
3. German (الألمانية)
4. Turkish (التركية)
5. Spanish (الإسبانية)
6. Chinese - Mandarin (الصينية)
7. Japanese (اليابانية)
8. Korean (الكورية)

#### Proficiency Levels (6 grades)
- **A1** - Basic Beginner
- **A2** - Elementary
- **B1** - Intermediate
- **B2** - Upper Intermediate
- **C1** - Advanced
- **C2** - Proficiency

#### Subject Types (per language per level)
- Grammar (القواعد)
- Conversation (المحادثة)
- Reading & Writing (القراءة والكتابة)
- Listening Comprehension (الاستماع والفهم)
- Vocabulary (المفردات)

**Advanced Level Additional Subjects:**
- Literature & Texts (الأدب والنصوص)
- Business Language (اللغة المهنية)

**Arabic for Non-Native Speakers:**
- 3 levels (Beginner, Intermediate, Advanced)

**Total Language Subjects:** ~250+ subjects

### General Skills Domain

#### Categories

**Life Skills (8 subjects):**
- Communication Skills (مهارات التواصل)
- Critical Thinking (التفكير النقدي)
- Problem Solving (حل المشكلات)
- Time Management (إدارة الوقت)
- Financial Literacy (الثقافة المالية)
- Leadership Skills (مهارات القيادة)
- Emotional Intelligence (الذكاء العاطفي)
- Personal Planning (التخطيط الشخصي)

**Professional Skills (7 subjects):**
- Project Management (إدارة المشاريع)
- Business Communication (التواصل المهني)
- Entrepreneurship (ريادة الأعمال)
- Marketing Basics (أساسيات التسويق)
- Public Speaking & Presentation (الخطابة والعرض)
- Resume Writing & Interview Skills (كتابة السيرة الذاتية ومهارات المقابلات)
- Negotiation & Conflict Resolution (التفاوض وحل النزاعات)

**Technical Skills (20 subjects):**
- Programming: Python, JavaScript
- Development: Web Development, Mobile App Development
- Design: Graphic Design, Video Editing, 3D Modeling
- Data: Data Analysis, Database Management
- Modern Tech: AI & Machine Learning, Cloud Computing, IoT
- Marketing: Digital Marketing, SEO
- Security: Cybersecurity Basics

**Total Skills Subjects:** 35 subjects

---

## 🚀 How to Use the Seeders

### Option 1: Seed on Application Startup (Recommended for Development)

Add this code in `Program.cs` before `app.Run()`:

```csharp
// Seed database with initial data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
    
    // Apply any pending migrations
    await context.Database.MigrateAsync();
    
    // Seed all data
    await DatabaseSeeder.SeedAllAsync(context);
    
    Console.WriteLine("Database seeding completed successfully!");
}

app.Run();
```

### Option 2: Create an Admin API Endpoint

Create a controller method for manual seeding:

```csharp
[HttpPost("api/admin/seed-database")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> SeedDatabase()
{
    try
    {
        await DatabaseSeeder.SeedAllAsync(_context);
        return Ok(new 
        { 
            Success = true, 
            Message = "Database seeded successfully!",
            Timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        return BadRequest(new 
        { 
            Success = false, 
            Message = "Seeding failed", 
            Error = ex.Message 
        });
    }
}
```

### Option 3: Run Individual Seeders

You can run seeders individually:

```csharp
// Seed only Saudi subjects
await SaudiSubjectsSeeder.SeedAsync(context);

// Seed only Quran subjects
await QuranSubjectsSeeder.SeedAsync(context);

// Seed only Language subjects
await LanguageLevelsSeeder.SeedAsync(context);
await LanguageSubjectsSeeder.SeedAsync(context);

// Seed only Skills subjects
await GeneralSkillsSubjectsSeeder.SeedAsync(context);
```

---

## 📐 Seeding Execution Order

The `DatabaseSeeder.SeedAllAsync()` method executes seeders in the following order to respect foreign key dependencies:

```
1. Basic Infrastructure
   ├── EducationDomainsSeeder
   ├── CurriculumsSeeder
   ├── TeachingModesSeeder
   ├── SessionTypesSeeder
   ├── QuranLevelsSeeder
   ├── QuranContentTypesSeeder
   └── TimeSlotsSeeder

2. Saudi Education System
   ├── SaudiEducationLevelsSeeder (depends on: Domains, Curriculums)
   ├── SaudiGradesSeeder (depends on: EducationLevels)
   ├── SaudiAcademicTermsSeeder (depends on: Curriculums)
   └── SaudiSubjectsSeeder (depends on: Levels, Grades, Terms)

3. Quran Domain
   └── QuranSubjectsSeeder (depends on: Domains, QuranLevels)

4. Languages Domain
   ├── LanguageLevelsSeeder (depends on: Domains)
   └── LanguageSubjectsSeeder (depends on: LanguageLevels, Grades)

5. General Skills Domain
   └── GeneralSkillsSubjectsSeeder (depends on: Domains)
```

---

## ✅ Features

### Idempotent Seeding
All seeders are **idempotent**, meaning they are safe to run multiple times:
- Checks if data already exists before inserting
- Will not create duplicates
- Safe to run on every application startup

### Bilingual Support
All seeded data includes both Arabic and English:
- NameAr (Arabic name)
- NameEn (English name)
- DescriptionAr (Arabic description)
- DescriptionEn (English description)

### Audit Trail
All seeded entities include:
- `CreatedAt` - Timestamp of creation
- `IsActive` - Active status flag

---

## 📈 Statistics

| Category | Count |
|----------|-------|
| Education Domains | 4 |
| Curriculums | 3 |
| Saudi Education Levels | 3 |
| Saudi Grades | 12 |
| Saudi Academic Terms | 3 |
| Saudi Subjects | ~120 |
| Quran Levels | 4 |
| Quran Subjects | 18 |
| Language Proficiency Grades | 6 |
| Supported Languages | 8 |
| Language Subjects | ~250+ |
| General Skills Subjects | 35 |
| **Total Subjects** | **~420+** |

---

## 🔧 Troubleshooting

### Issue: Seeding fails with foreign key constraint error
**Solution:** Ensure seeders run in the correct order. Use `DatabaseSeeder.SeedAllAsync()` which handles dependencies automatically.

### Issue: Duplicate data being inserted
**Solution:** Check the conditional logic in each seeder (e.g., `if (!await context.Subjects.AnyAsync(...))`). Each seeder should check for existing data before inserting.

### Issue: Some subjects not appearing for certain grades
**Solution:** Verify that the education levels and grades were seeded before subjects. Run seeders in dependency order.

---

## 🎯 Next Steps After Seeding

1. **Verify Data**: Query the database to confirm all data was seeded correctly
2. **Test APIs**: Create endpoints to retrieve subjects, levels, and grades
3. **Build UI**: Create frontend components to display available subjects
4. **User Management**: Allow teachers to select their subjects
5. **Course Creation**: Enable teachers to create courses from seeded subjects

---

## 📝 Notes

- All timestamps use UTC
- All subjects are marked as active (`IsActive = true`) by default
- Language subjects follow CEFR standards (A1-C2)
- Saudi curriculum follows the 3-term system
- Quran subjects can be expanded to link specific Juz and Surahs

---

## 🔄 Updating Seed Data

To update existing seed data:

1. Modify the seeder file
2. Delete the specific data from the database (or drop and recreate)
3. Run the seeder again

Or create a new migration with update scripts.

---

## 📞 Support

For questions or issues with seeding:
- Check the seeder files in `Qalam.Infrastructure/Seeding/`
- Review the `DatabaseSeeder.cs` for execution order
- Ensure migrations are applied before seeding

---

**Last Updated:** January 2026  
**Version:** 1.0

