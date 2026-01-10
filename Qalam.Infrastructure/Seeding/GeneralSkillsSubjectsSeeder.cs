using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Seeding;

public class GeneralSkillsSubjectsSeeder
{
    public static async Task SeedAsync(ApplicationDBContext context)
    {
        var skillsDomainId = 4; // General Skills domain

        if (!await context.Subjects.AnyAsync(s => s.DomainId == skillsDomainId))
        {
            var subjects = new List<Subject>();

            // ====== LIFE SKILLS ======
            subjects.Add(new Subject
            {
                DomainId = skillsDomainId,
                NameAr = "مهارات التواصل",
                NameEn = "Communication Skills",
                DescriptionAr = "تطوير مهارات التواصل الفعال مع الآخرين",
                DescriptionEn = "Developing effective communication skills with others",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            subjects.Add(new Subject
            {
                DomainId = skillsDomainId,
                NameAr = "التفكير النقدي",
                NameEn = "Critical Thinking",
                DescriptionAr = "تنمية مهارات التفكير النقدي والتحليلي",
                DescriptionEn = "Developing critical and analytical thinking skills",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            subjects.Add(new Subject
            {
                DomainId = skillsDomainId,
                NameAr = "حل المشكلات",
                NameEn = "Problem Solving",
                DescriptionAr = "تعلم أساليب وتقنيات حل المشكلات",
                DescriptionEn = "Learning problem-solving methods and techniques",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            subjects.Add(new Subject
            {
                DomainId = skillsDomainId,
                NameAr = "إدارة الوقت",
                NameEn = "Time Management",
                DescriptionAr = "تطوير مهارات تنظيم وإدارة الوقت بفعالية",
                DescriptionEn = "Developing time organization and management skills effectively",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            subjects.Add(new Subject
            {
                DomainId = skillsDomainId,
                NameAr = "الثقافة المالية",
                NameEn = "Financial Literacy",
                DescriptionAr = "فهم أساسيات الإدارة المالية الشخصية والاستثمار",
                DescriptionEn = "Understanding basics of personal financial management and investment",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            subjects.Add(new Subject
            {
                DomainId = skillsDomainId,
                NameAr = "مهارات القيادة",
                NameEn = "Leadership Skills",
                DescriptionAr = "تطوير القدرات القيادية وإدارة الفرق",
                DescriptionEn = "Developing leadership abilities and team management",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            subjects.Add(new Subject
            {
                DomainId = skillsDomainId,
                NameAr = "الذكاء العاطفي",
                NameEn = "Emotional Intelligence",
                DescriptionAr = "تنمية الوعي الذاتي والذكاء العاطفي",
                DescriptionEn = "Developing self-awareness and emotional intelligence",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            subjects.Add(new Subject
            {
                DomainId = skillsDomainId,
                NameAr = "التخطيط الشخصي",
                NameEn = "Personal Planning",
                DescriptionAr = "مهارات التخطيط وتحديد الأهداف الشخصية",
                DescriptionEn = "Planning skills and setting personal goals",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            // ====== PROFESSIONAL SKILLS ======
            subjects.Add(new Subject
            {
                DomainId = skillsDomainId,
                NameAr = "إدارة المشاريع",
                NameEn = "Project Management",
                DescriptionAr = "تعلم أساسيات ومنهجيات إدارة المشاريع",
                DescriptionEn = "Learning project management basics and methodologies",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            subjects.Add(new Subject
            {
                DomainId = skillsDomainId,
                NameAr = "التواصل المهني",
                NameEn = "Business Communication",
                DescriptionAr = "مهارات التواصل في بيئة العمل المهنية",
                DescriptionEn = "Communication skills in professional work environment",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            subjects.Add(new Subject
            {
                DomainId = skillsDomainId,
                NameAr = "ريادة الأعمال",
                NameEn = "Entrepreneurship",
                DescriptionAr = "أساسيات بدء وإدارة المشاريع الريادية",
                DescriptionEn = "Basics of starting and managing entrepreneurial ventures",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            subjects.Add(new Subject
            {
                DomainId = skillsDomainId,
                NameAr = "أساسيات التسويق",
                NameEn = "Marketing Basics",
                DescriptionAr = "مبادئ التسويق والترويج للمنتجات والخدمات",
                DescriptionEn = "Marketing principles and promotion of products and services",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            subjects.Add(new Subject
            {
                DomainId = skillsDomainId,
                NameAr = "الخطابة والعرض",
                NameEn = "Public Speaking & Presentation",
                DescriptionAr = "تطوير مهارات الإلقاء والعرض أمام الجمهور",
                DescriptionEn = "Developing public speaking and presentation skills",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            subjects.Add(new Subject
            {
                DomainId = skillsDomainId,
                NameAr = "كتابة السيرة الذاتية ومهارات المقابلات",
                NameEn = "Resume Writing & Interview Skills",
                DescriptionAr = "تعلم كتابة السيرة الذاتية الاحترافية والتحضير للمقابلات",
                DescriptionEn = "Learning professional resume writing and interview preparation",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            subjects.Add(new Subject
            {
                DomainId = skillsDomainId,
                NameAr = "التفاوض وحل النزاعات",
                NameEn = "Negotiation & Conflict Resolution",
                DescriptionAr = "مهارات التفاوض الفعال وحل النزاعات في بيئة العمل",
                DescriptionEn = "Effective negotiation and conflict resolution skills in workplace",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            // ====== TECHNICAL SKILLS ======
            subjects.Add(new Subject
            {
                DomainId = skillsDomainId,
                NameAr = "البرمجة بلغة بايثون",
                NameEn = "Python Programming",
                DescriptionAr = "تعلم أساسيات البرمجة باستخدام لغة بايثون",
                DescriptionEn = "Learning programming basics using Python language",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            subjects.Add(new Subject
            {
                DomainId = skillsDomainId,
                NameAr = "البرمجة بلغة جافا سكريبت",
                NameEn = "JavaScript Programming",
                DescriptionAr = "تعلم البرمجة بلغة جافا سكريبت لتطوير الويب",
                DescriptionEn = "Learning JavaScript programming for web development",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            subjects.Add(new Subject
            {
                DomainId = skillsDomainId,
                NameAr = "تطوير الويب",
                NameEn = "Web Development",
                DescriptionAr = "تطوير المواقع الإلكترونية باستخدام HTML وCSS وJavaScript",
                DescriptionEn = "Developing websites using HTML, CSS, and JavaScript",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            subjects.Add(new Subject
            {
                DomainId = skillsDomainId,
                NameAr = "تطوير تطبيقات الموبايل",
                NameEn = "Mobile App Development",
                DescriptionAr = "تعلم تطوير تطبيقات الهواتف الذكية",
                DescriptionEn = "Learning mobile smartphone application development",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            subjects.Add(new Subject
            {
                DomainId = skillsDomainId,
                NameAr = "تحليل البيانات",
                NameEn = "Data Analysis",
                DescriptionAr = "تعلم تحليل البيانات واستخراج الرؤى القيمة",
                DescriptionEn = "Learning data analysis and extracting valuable insights",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            subjects.Add(new Subject
            {
                DomainId = skillsDomainId,
                NameAr = "التصميم الجرافيكي",
                NameEn = "Graphic Design",
                DescriptionAr = "تعلم أساسيات التصميم الجرافيكي واستخدام برامج التصميم",
                DescriptionEn = "Learning graphic design basics and using design software",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            subjects.Add(new Subject
            {
                DomainId = skillsDomainId,
                NameAr = "مونتاج الفيديو",
                NameEn = "Video Editing",
                DescriptionAr = "تعلم تحرير ومونتاج مقاطع الفيديو الاحترافية",
                DescriptionEn = "Learning professional video editing and production",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            subjects.Add(new Subject
            {
                DomainId = skillsDomainId,
                NameAr = "النمذجة ثلاثية الأبعاد",
                NameEn = "3D Modeling",
                DescriptionAr = "تعلم إنشاء النماذج والرسوم ثلاثية الأبعاد",
                DescriptionEn = "Learning 3D model and graphics creation",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            subjects.Add(new Subject
            {
                DomainId = skillsDomainId,
                NameAr = "أساسيات الأمن السيبراني",
                NameEn = "Cybersecurity Basics",
                DescriptionAr = "تعلم أساسيات حماية الأنظمة والبيانات الرقمية",
                DescriptionEn = "Learning basics of protecting digital systems and data",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            subjects.Add(new Subject
            {
                DomainId = skillsDomainId,
                NameAr = "أساسيات الذكاء الاصطناعي",
                NameEn = "AI & Machine Learning Basics",
                DescriptionAr = "مقدمة في الذكاء الاصطناعي والتعلم الآلي",
                DescriptionEn = "Introduction to artificial intelligence and machine learning",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            subjects.Add(new Subject
            {
                DomainId = skillsDomainId,
                NameAr = "قواعد البيانات",
                NameEn = "Database Management",
                DescriptionAr = "تعلم إدارة قواعد البيانات ولغة SQL",
                DescriptionEn = "Learning database management and SQL language",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            subjects.Add(new Subject
            {
                DomainId = skillsDomainId,
                NameAr = "الحوسبة السحابية",
                NameEn = "Cloud Computing",
                DescriptionAr = "مقدمة في الحوسبة السحابية والخدمات السحابية",
                DescriptionEn = "Introduction to cloud computing and cloud services",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            subjects.Add(new Subject
            {
                DomainId = skillsDomainId,
                NameAr = "إنترنت الأشياء",
                NameEn = "Internet of Things (IoT)",
                DescriptionAr = "تعلم أساسيات إنترنت الأشياء والأجهزة الذكية",
                DescriptionEn = "Learning IoT basics and smart devices",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            subjects.Add(new Subject
            {
                DomainId = skillsDomainId,
                NameAr = "التسويق الرقمي",
                NameEn = "Digital Marketing",
                DescriptionAr = "تعلم استراتيجيات التسويق عبر الإنترنت ووسائل التواصل",
                DescriptionEn = "Learning online marketing strategies and social media",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            subjects.Add(new Subject
            {
                DomainId = skillsDomainId,
                NameAr = "تحسين محركات البحث (SEO)",
                NameEn = "Search Engine Optimization (SEO)",
                DescriptionAr = "تعلم تحسين ظهور المواقع في نتائج البحث",
                DescriptionEn = "Learning website optimization for search engine results",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            await context.Subjects.AddRangeAsync(subjects);
            await context.SaveChangesAsync();
        }
    }
}

