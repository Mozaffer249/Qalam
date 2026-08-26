using Microsoft.EntityFrameworkCore;
using Qalam.Data.AppMetaData;
using Qalam.Data.Entity.Education;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Seeding;

/// <summary>
/// Wave-1 catalog: six domains split from skills, plus writable-filter slots and example values.
/// </summary>
public static class SkillsSplitCatalogSeeder
{
    public static async Task SeedAsync(ApplicationDBContext context)
    {
        var domains = await context.EducationDomains
            .Where(d => EducationDomainCodes.Wave1SplitFromSkills.Contains(d.Code))
            .ToListAsync();
        if (domains.Count == 0)
            return;

        foreach (var domain in domains)
        {
            await SeedLevelsAsync(context, domain);
            await SeedSubjectsAsync(context, domain);
            await SeedWritableSlotsAsync(context, domain);
            await EnsureMissingOtherCatalogAsync(context, domain);
        }
    }

    /// <summary>
    /// Idempotent backfill for «أخرى» / empty parents and their write-in slots on existing DBs
    /// (initial seed returns early once any subject/slot rows exist).
    /// </summary>
    private static async Task EnsureMissingOtherCatalogAsync(ApplicationDBContext context, EducationDomain domain)
    {
        await EnsureMissingSubjectsAsync(context, domain);
        await EnsureMissingWritableSlotsAsync(context, domain);
    }

    private static async Task EnsureMissingSubjectsAsync(ApplicationDBContext context, EducationDomain domain)
    {
        var needed = domain.Code switch
        {
            EducationDomainCodes.LifeSkills => new (string Code, string Ar, string En)[]
            {
                ("life.other", "أخرى", "Other")
            },
            EducationDomainCodes.SoftSkills =>
            [
                ("soft.other", "أخرى", "Other")
            ],
            EducationDomainCodes.Hobbies =>
            [
                ("hobby.interest", "مجموعات الاهتمام المشترك", "Shared interest groups"),
                ("hobby.other", "مهارات وهوايات أخرى", "Other skills and hobbies")
            ],
            EducationDomainCodes.Knowledge =>
            [
                ("know.other", "أخرى", "Other")
            ],
            EducationDomainCodes.TechSkills =>
            [
                ("tech.other", "أخرى", "Other")
            ],
            _ => []
        };

        if (needed.Length == 0)
            return;

        var codes = needed.Select(n => n.Code).ToList();
        var existing = await context.Subjects
            .Where(s => s.DomainId == domain.Id && codes.Contains(s.Code!))
            .Select(s => s.Code!)
            .ToListAsync();
        var have = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);

        var added = false;
        foreach (var (code, ar, en) in needed)
        {
            if (have.Contains(code))
                continue;
            context.Subjects.Add(new Subject
            {
                DomainId = domain.Id,
                Code = code,
                NameAr = ar,
                NameEn = en,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            added = true;
        }

        if (added)
            await context.SaveChangesAsync();
    }

    private static async Task EnsureMissingWritableSlotsAsync(ApplicationDBContext context, EducationDomain domain)
    {
        var specs = SlotSpecsForDomain(domain.Code);
        if (specs.Length == 0)
            return;

        var existingCodes = await context.WritableFilterSlots
            .Where(s => s.DomainId == domain.Id)
            .Select(s => s.Code)
            .ToListAsync();
        var have = new HashSet<string>(existingCodes, StringComparer.OrdinalIgnoreCase);

        foreach (var spec in specs)
        {
            if (have.Contains(spec.Code))
                continue;

            var slot = new WritableFilterSlot
            {
                DomainId = domain.Id,
                Code = spec.Code,
                NameAr = spec.NameAr,
                NameEn = spec.NameEn,
                AfterStep = spec.AfterStep,
                OrderIndex = spec.OrderIndex,
                IsRequired = spec.IsRequired,
                RequiredWhenSubjectCodeContains = spec.RequiredWhen,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.WritableFilterSlots.Add(slot);
            await context.SaveChangesAsync();

            foreach (var value in spec.Values)
            {
                context.WritableFilterValues.Add(new WritableFilterValue
                {
                    SlotId = slot.Id,
                    Code = value.Code,
                    NameAr = value.Ar,
                    NameEn = value.En,
                    NormalizedText = WritableFilterTextNormalizer.Normalize(value.Ar),
                    IsSeeded = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedLevelsAsync(ApplicationDBContext context, EducationDomain domain)
    {
        if (await SeederHelper.HasAnyDataAsync(context.EducationLevels, l => l.DomainId == domain.Id))
            return;

        var levels = domain.Code switch
        {
            EducationDomainCodes.LifeSkills => new (string Ar, string En)[]
            {
                ("أطفال", "Children"),
                ("شباب", "Youth"),
                ("كبار", "Adults"),
                ("الوالدان والأسرة", "Parents and family"),
                ("الأزواج والمقبلون على الزواج", "Couples and soon-to-marry")
            },
            EducationDomainCodes.Hobbies =>
            [
                ("مبتدئ", "Beginner"),
                ("متوسط", "Intermediate"),
                ("متقدم", "Advanced"),
                ("هاو", "Amateur"),
                ("محترف", "Professional")
            ],
            EducationDomainCodes.TechSkills or EducationDomainCodes.Finance or EducationDomainCodes.Knowledge =>
            [
                ("مبتدئ", "Beginner"),
                ("متوسط", "Intermediate"),
                ("متقدم", "Advanced")
            ],
            _ => []
        };

        for (var i = 0; i < levels.Length; i++)
        {
            context.EducationLevels.Add(new EducationLevel
            {
                DomainId = domain.Id,
                NameAr = levels[i].Ar,
                NameEn = levels[i].En,
                OrderIndex = i + 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (levels.Length > 0)
            await context.SaveChangesAsync();
    }

    private static async Task SeedSubjectsAsync(ApplicationDBContext context, EducationDomain domain)
    {
        switch (domain.Code)
        {
            case EducationDomainCodes.SoftSkills:
                await EnsureTreeAsync(context, domain.Id, SoftCategories());
                break;
            case EducationDomainCodes.LifeSkills:
                await EnsureTreeAsync(context, domain.Id, LifeCategories());
                break;
            case EducationDomainCodes.Hobbies:
                await EnsureTreeAsync(context, domain.Id, HobbyCategories());
                break;
            case EducationDomainCodes.TechSkills:
                await EnsureFlatAsync(context, domain.Id, TechPaths());
                break;
            case EducationDomainCodes.Finance:
                await EnsureFlatAsync(context, domain.Id,
                    [("finance.root", "المال والاستثمار", "Money and Investment")]);
                break;
            case EducationDomainCodes.Knowledge:
                await EnsureFlatAsync(context, domain.Id, KnowledgeFields());
                break;
        }
    }

    private static async Task EnsureTreeAsync(
        ApplicationDBContext context,
        int domainId,
        IReadOnlyList<(string Code, string Ar, string En, (string Code, string Ar, string En)[] Children)> categories)
    {
        var existingCodes = await context.Subjects
            .Where(s => s.DomainId == domainId && s.Code != null)
            .Select(s => s.Code!)
            .ToListAsync();
        var have = new HashSet<string>(existingCodes, StringComparer.OrdinalIgnoreCase);
        var addedAny = false;

        foreach (var (code, ar, en, children) in categories)
        {
            Subject parent;
            if (have.Contains(code))
            {
                parent = await context.Subjects
                    .FirstAsync(s => s.DomainId == domainId && s.Code == code);
            }
            else
            {
                parent = new Subject
                {
                    DomainId = domainId,
                    Code = code,
                    NameAr = ar,
                    NameEn = en,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                context.Subjects.Add(parent);
                await context.SaveChangesAsync();
                have.Add(code);
                addedAny = true;
            }

            foreach (var child in children)
            {
                if (have.Contains(child.Code))
                    continue;

                context.Subjects.Add(new Subject
                {
                    DomainId = domainId,
                    ParentSubjectId = parent.Id,
                    Code = child.Code,
                    NameAr = child.Ar,
                    NameEn = child.En,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
                have.Add(child.Code);
                addedAny = true;
            }
        }

        if (addedAny)
            await context.SaveChangesAsync();
    }

    private static async Task EnsureFlatAsync(
        ApplicationDBContext context,
        int domainId,
        IReadOnlyList<(string Code, string Ar, string En)> items)
    {
        var existingCodes = await context.Subjects
            .Where(s => s.DomainId == domainId && s.Code != null)
            .Select(s => s.Code!)
            .ToListAsync();
        var have = new HashSet<string>(existingCodes, StringComparer.OrdinalIgnoreCase);
        var addedAny = false;

        foreach (var item in items)
        {
            if (have.Contains(item.Code))
                continue;

            context.Subjects.Add(new Subject
            {
                DomainId = domainId,
                Code = item.Code,
                NameAr = item.Ar,
                NameEn = item.En,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            have.Add(item.Code);
            addedAny = true;
        }

        if (addedAny)
            await context.SaveChangesAsync();
    }

    private static async Task SeedWritableSlotsAsync(ApplicationDBContext context, EducationDomain domain)
    {
        if (await SeederHelper.HasAnyDataAsync(context.WritableFilterSlots, s => s.DomainId == domain.Id))
            return;

        foreach (var spec in SlotSpecsForDomain(domain.Code))
        {
            var slot = new WritableFilterSlot
            {
                DomainId = domain.Id,
                Code = spec.Code,
                NameAr = spec.NameAr,
                NameEn = spec.NameEn,
                AfterStep = spec.AfterStep,
                OrderIndex = spec.OrderIndex,
                IsRequired = spec.IsRequired,
                RequiredWhenSubjectCodeContains = spec.RequiredWhen,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.WritableFilterSlots.Add(slot);
            await context.SaveChangesAsync();

            foreach (var value in spec.Values)
            {
                context.WritableFilterValues.Add(new WritableFilterValue
                {
                    SlotId = slot.Id,
                    Code = value.Code,
                    NameAr = value.Ar,
                    NameEn = value.En,
                    NormalizedText = WritableFilterTextNormalizer.Normalize(value.Ar),
                    IsSeeded = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await context.SaveChangesAsync();
        }
    }

    private static SlotSpec[] SlotSpecsForDomain(string domainCode) =>
        domainCode switch
        {
            EducationDomainCodes.SoftSkills =>
            [
                new(WritableFilterSlotCodes.SoftOtherSkill, "مهارة أخرى", "Other skill",
                    WritableFilterAfterSteps.Subject, 1, false, ".other",
                    [("change-mgmt", "إدارة التغيير", "Change management"),
                     ("coaching", "الكوتشينغ المهني", "Career coaching")])
            ],
            EducationDomainCodes.LifeSkills =>
            [
                new(WritableFilterSlotCodes.LifeOtherSkill, "مهارة أخرى", "Other skill",
                    WritableFilterAfterSteps.Subject, 1, false, ".other",
                    [("sleep", "تنظيم النوم", "Sleep routine"),
                     ("digital-habits", "العادات الرقمية", "Digital habits")])
            ],
            EducationDomainCodes.TechSkills =>
            [
                new(WritableFilterSlotCodes.TechSpecialty, "التخصص", "Specialty",
                    WritableFilterAfterSteps.Level, 1, false, null,
                    [("5g", "5G الجيل الخامس", "5G"),
                     ("react", "React", "React"),
                     ("flutter", "Flutter", "Flutter"),
                     ("k8s", "Kubernetes", "Kubernetes"),
                     ("pentest", "اختبار الاختراق", "Pentesting")]),
                new(WritableFilterSlotCodes.TechTool, "التقنية / البرنامج", "Tool / program",
                    WritableFilterAfterSteps.Level, 2, false, null,
                    [("huawei", "HUAWEI", "HUAWEI"),
                     ("packet-tracer", "Cisco Packet Tracer", "Cisco Packet Tracer"),
                     ("vscode", "VS Code", "VS Code"),
                     ("figma", "Figma", "Figma"),
                     ("premiere", "Adobe Premiere", "Adobe Premiere"),
                     ("aws", "AWS", "AWS")]),
                new(WritableFilterSlotCodes.TechCurriculum, "المنهج", "Curriculum",
                    WritableFilterAfterSteps.Level, 3, false, null,
                    [("ccna", "CCNA", "CCNA"),
                     ("aws-cp", "AWS Cloud Practitioner", "AWS Cloud Practitioner")]),
                new(WritableFilterSlotCodes.TechOtherPath, "مسار آخر", "Other path",
                    WritableFilterAfterSteps.Subject, 4, false, ".other",
                    [("blockchain", "البلوك تشين", "Blockchain")])
            ],
            EducationDomainCodes.Hobbies =>
            [
                new(WritableFilterSlotCodes.HobbiesInterestGroup, "مجموعة اهتمام مشترك", "Shared interest group",
                    WritableFilterAfterSteps.Subject, 1, false, null,
                    [("book-club", "نادي القراءة", "Book club"),
                     ("chess-club", "نادي الشطرنج", "Chess club"),
                     ("astro-club", "الفلك للهواة", "Amateur astronomy")]),
                new(WritableFilterSlotCodes.HobbiesOther, "مهارة أو هواية أخرى", "Other skill or hobby",
                    WritableFilterAfterSteps.Subject, 2, false, ".other",
                    [("oud", "العود", "Oud"),
                     ("improv", "الخطابة الارتجالية", "Improvisational speaking")])
            ],
            EducationDomainCodes.Finance =>
            [
                new(WritableFilterSlotCodes.FinanceField, "المجال", "Field",
                    WritableFilterAfterSteps.Start, 1, true, null,
                    [("investing", "الاستثمار", "Investing"),
                     ("saving", "الادخار", "Saving"),
                     ("stocks", "الأسهم", "Stocks"),
                     ("real-estate", "العقار", "Real estate"),
                     ("fx", "العملات", "Currencies"),
                     ("planning", "التخطيط المالي الشخصي", "Personal financial planning"),
                     ("zakat", "الزكاة والوقف", "Zakat and waqf")])
            ],
            EducationDomainCodes.Knowledge =>
            [
                new(WritableFilterSlotCodes.KnowledgePreciseTopic, "المنهج والمجال الدقيق", "Precise topic or curriculum",
                    WritableFilterAfterSteps.Level, 1, false, null,
                    [("umayyad-bidaya", "الدولة الأموية — البداية والنهاية", "Umayyad period — Al-Bidaya wa-l-Nihaya"),
                     ("newton-mechanics", "ميكانيكا نيوتن", "Newtonian mechanics")]),
                new(WritableFilterSlotCodes.KnowledgeOtherField, "مجال معرفي آخر", "Other knowledge field",
                    WritableFilterAfterSteps.Subject, 2, false, ".other",
                    [("comparative-religion", "مقارنة الأديان", "Comparative religion")])
            ],
            _ => []
        };

    private sealed record SlotSpec(
        string Code,
        string NameAr,
        string NameEn,
        string AfterStep,
        int OrderIndex,
        bool IsRequired,
        string? RequiredWhen,
        (string Code, string Ar, string En)[] Values);

    private static IReadOnlyList<(string Code, string Ar, string En, (string Code, string Ar, string En)[] Children)> SoftCategories() =>
    [
        ("soft.career", "التوظيف والتطوير المهني", "Career and professional development",
        [
            ("soft.career.resume", "السيرة الذاتية", "Resume"),
            ("soft.career.linkedin", "LinkedIn", "LinkedIn"),
            ("soft.career.interview", "المقابلات الشخصية", "Interviews"),
            ("soft.career.jobsearch", "البحث عن وظيفة", "Job search"),
            ("soft.career.path", "بناء المسار المهني", "Career path")
        ]),
        ("soft.comm", "التواصل المهني", "Professional communication",
        [
            ("soft.comm.verbal", "التواصل", "Communication"),
            ("soft.comm.writing", "التواصل الكتابي", "Written communication"),
            ("soft.comm.email", "كتابة البريد", "Email writing"),
            ("soft.comm.present", "مهارات العرض والتقديم", "Presentation"),
            ("soft.comm.public", "التحدث أمام الجمهور", "Public speaking"),
            ("soft.comm.listen", "الإنصات", "Listening")
        ]),
        ("soft.lead", "القيادة والإدارة", "Leadership and management",
        [
            ("soft.lead.lead", "القيادة", "Leadership"),
            ("soft.lead.teams", "إدارة الفرق", "Team management"),
            ("soft.lead.delegate", "التفويض", "Delegation"),
            ("soft.lead.motivate", "التحفيز", "Motivation"),
            ("soft.lead.decide", "اتخاذ القرار", "Decision making")
        ]),
        ("soft.pm", "إدارة المشاريع والعمل", "Project and work management",
        [
            ("soft.pm.projects", "إدارة المشاريع", "Project management"),
            ("soft.pm.tasks", "إدارة المهام", "Task management"),
            ("soft.pm.time", "إدارة الوقت", "Time management"),
            ("soft.pm.priority", "تحديد الأولويات", "Prioritization"),
            ("soft.pm.org", "تنظيم العمل", "Work organization")
        ]),
        ("soft.team", "العمل الجماعي والعلاقات المهنية", "Teamwork and professional relations",
        [
            ("soft.team.work", "العمل الجماعي", "Teamwork"),
            ("soft.team.relations", "بناء العلاقات", "Relationship building"),
            ("soft.team.conflict", "إدارة الخلافات", "Conflict management"),
            ("soft.team.collab", "التعاون", "Collaboration"),
            ("soft.team.sq", "الذكاء الاجتماعي في بيئة العمل", "Workplace social intelligence")
        ]),
        ("soft.nego", "التفاوض والإقناع", "Negotiation and persuasion",
        [
            ("soft.nego.nego", "التفاوض", "Negotiation"),
            ("soft.nego.persuade", "الإقناع", "Persuasion"),
            ("soft.nego.influence", "التأثير", "Influence"),
            ("soft.nego.objections", "إدارة الاعتراضات", "Handling objections")
        ]),
        ("soft.think", "التفكير وحل المشكلات", "Thinking and problem solving",
        [
            ("soft.think.critical", "التفكير الناقد", "Critical thinking"),
            ("soft.think.creative", "التفكير الإبداعي", "Creative thinking"),
            ("soft.think.solve", "حل المشكلات", "Problem solving"),
            ("soft.think.decide", "اتخاذ القرار", "Decision making"),
            ("soft.think.analyze", "التحليل", "Analysis")
        ]),
        ("soft.cx", "خدمة العملاء وتجربة العميل", "Customer service and experience",
        [
            ("soft.cx.service", "خدمة العملاء", "Customer service"),
            ("soft.cx.complaints", "التعامل مع الشكاوى", "Complaints"),
            ("soft.cx.comms", "التواصل مع العملاء", "Customer communication"),
            ("soft.cx.exp", "تجربة العميل", "Customer experience")
        ]),
        ("soft.sales", "المبيعات والتسويق", "Sales and marketing",
        [
            ("soft.sales.sell", "مهارات البيع", "Selling"),
            ("soft.sales.nego", "التفاوض البيعي", "Sales negotiation"),
            ("soft.sales.persuade", "الإقناع", "Persuasion"),
            ("soft.sales.comms", "التواصل مع العملاء", "Customer communication"),
            ("soft.sales.offers", "العروض البيعية", "Sales pitches")
        ]),
        ("soft.research", "البحث والكتابة المهنية", "Research and professional writing",
        [
            ("soft.research.research", "البحث وجمع المعلومات", "Research"),
            ("soft.research.reports", "كتابة التقارير", "Report writing"),
            ("soft.research.writing", "الكتابة المهنية", "Professional writing"),
            ("soft.research.summary", "التلخيص", "Summarizing"),
            ("soft.research.letters", "إعداد المراسلات", "Correspondence")
        ]),
        ("soft.prod", "الإنتاجية والكفاءة المهنية", "Productivity and professional efficiency",
        [
            ("soft.prod.time", "إدارة الوقت", "Time management"),
            ("soft.prod.tasks", "تنظيم المهام", "Task organization"),
            ("soft.prod.priority", "تحديد الأولويات", "Prioritization"),
            ("soft.prod.meetings", "إدارة الاجتماعات", "Meeting management"),
            ("soft.prod.personal", "الإنتاجية الشخصية", "Personal productivity")
        ]),
        ("soft.freelance", "مهارات العمل الحر وريادة الأعمال", "Freelancing and entrepreneurship",
        [
            ("soft.freelance.free", "العمل الحر", "Freelancing"),
            ("soft.freelance.clients", "التعامل مع العملاء", "Client management"),
            ("soft.freelance.pricing", "تسعير الخدمات", "Pricing"),
            ("soft.freelance.proposals", "تقديم العروض", "Proposals"),
            ("soft.freelance.projects", "إدارة المشاريع المستقلة", "Independent projects")
        ]),
        ("soft.other", "أخرى", "Other", [])
    ];

    private static IReadOnlyList<(string Code, string Ar, string En, (string Code, string Ar, string En)[] Children)> LifeCategories() =>
    [
        ("life.self", "تطوير الذات وبناء الشخصية", "Self-development and character",
        [
            ("life.self.confidence", "الثقة بالنفس", "Self-confidence"),
            ("life.self.esteem", "تقدير الذات", "Self-esteem"),
            ("life.self.character", "بناء الشخصية", "Character building"),
            ("life.self.discipline", "الانضباط", "Discipline"),
            ("life.self.responsibility", "تحمل المسؤولية", "Responsibility"),
            ("life.self.goals", "تحديد الأهداف", "Goal setting"),
            ("life.self.achieve", "تحقيق الأهداف", "Goal achievement")
        ]),
        ("life.psych", "الجودة والمهارات النفسية", "Psychological skills",
        [
            ("life.psych.emotions", "إدارة المشاعر", "Emotion management"),
            ("life.psych.stress", "التعامل مع الضغوط", "Stress"),
            ("life.psych.resilience", "المرونة النفسية", "Resilience"),
            ("life.psych.positive", "التفكير الإيجابي", "Positive thinking"),
            ("life.psych.awareness", "الوعي بالذات", "Self-awareness")
        ]),
        ("life.social", "التواصل والعلاقات الاجتماعية", "Communication and social relations",
        [
            ("life.social.effective", "التواصل الفعال", "Effective communication"),
            ("life.social.dialogue", "فن الحوار", "Dialogue"),
            ("life.social.listen", "الإنصات", "Listening"),
            ("life.social.sq", "الذكاء الاجتماعي", "Social intelligence"),
            ("life.social.relations", "بناء العلاقات", "Building relationships"),
            ("life.social.conflict", "حل الخلافات", "Conflict resolution")
        ]),
        ("life.marriage", "العلاقات الزوجية والأسرية", "Marital and family relations",
        [
            ("life.marriage.couple", "العلاقات الزوجية", "Marital relations"),
            ("life.marriage.comms", "التواصل بين الزوجين", "Couple communication"),
            ("life.marriage.conflict", "حل الخلافات الزوجية", "Marital conflict"),
            ("life.marriage.family", "بناء العلاقة الأسرية", "Family relationship")
        ]),
        ("life.parent", "التربية والوالدية", "Parenting",
        [
            ("life.parent.skills", "مهارات التربية", "Parenting skills"),
            ("life.parent.children", "التعامل مع الأطفال", "Working with children"),
            ("life.parent.teens", "التعامل مع المراهقين", "Working with teens"),
            ("life.parent.comms", "التواصل مع الأبناء", "Communicating with children"),
            ("life.parent.positive", "التربية الإيجابية", "Positive parenting")
        ]),
        ("life.guide", "الإرشاد والتوجيه", "Guidance and counseling",
        [
            ("life.guide.personal", "الإرشاد الشخصي", "Personal counseling"),
            ("life.guide.family", "الإرشاد الأسري", "Family counseling"),
            ("life.guide.direction", "التوجيه", "Guidance"),
            ("life.guide.decisions", "اتخاذ القرارات الحياتية", "Life decisions")
        ]),
        ("life.eq", "الذكاء العاطفي", "Emotional intelligence",
        [
            ("life.eq.aware", "الوعي بالمشاعر", "Emotional awareness"),
            ("life.eq.manage", "إدارة المشاعر", "Managing emotions"),
            ("life.eq.empathy", "التعاطف", "Empathy"),
            ("life.eq.regulate", "تنظيم الانفعالات", "Emotional regulation"),
            ("life.eq.relations", "العلاقات", "Relationships")
        ]),
        ("life.habits", "العادات ونمط الحياة", "Habits and lifestyle",
        [
            ("life.habits.build", "بناء العادات", "Building habits"),
            ("life.habits.break", "التخلص من العادات السلبية", "Breaking habits"),
            ("life.habits.organize", "تنظيم الحياة", "Life organization"),
            ("life.habits.time", "إدارة الوقت الشخصي", "Personal time"),
            ("life.habits.balance", "التوازن الحياتي", "Life balance")
        ]),
        ("life.speak", "فن التحدث والإلقاء", "Speaking and delivery",
        [
            ("life.speak.fluency", "التحدث بطلاقة", "Fluent speaking"),
            ("life.speak.delivery", "فن الإلقاء", "Delivery"),
            ("life.speak.public", "التحدث أمام الجمهور", "Public speaking"),
            ("life.speak.body", "لغة الجسد", "Body language"),
            ("life.speak.fear", "التغلب على رهبة التحدث", "Speaking anxiety")
        ]),
        ("life.other", "أخرى", "Other", [])
    ];

    private static IReadOnlyList<(string Code, string Ar, string En, (string Code, string Ar, string En)[] Children)> HobbyCategories() =>
    [
        ("hobby.cook", "الطبخ والمخبوزات", "Cooking and baking",
        [
            ("hobby.cook.cook", "الطبخ", "Cooking"),
            ("hobby.cook.sweets", "الحلويات", "Desserts"),
            ("hobby.cook.bakery", "المخبوزات", "Baking"),
            ("hobby.cook.coffee", "القهوة", "Coffee"),
            ("hobby.cook.drinks", "إعداد المشروبات", "Drinks"),
            ("hobby.cook.cake", "تزيين الكيك", "Cake decorating")
        ]),
        ("hobby.art", "الفنون والرسم", "Arts and drawing",
        [
            ("hobby.art.draw", "الرسم", "Drawing"),
            ("hobby.art.color", "التلوين", "Coloring"),
            ("hobby.art.digital", "الرسم الرقمي", "Digital drawing"),
            ("hobby.art.calligraphy", "الخط", "Calligraphy"),
            ("hobby.art.sculpt", "النحت", "Sculpture"),
            ("hobby.art.plastic", "الفنون التشكيلية", "Fine arts")
        ]),
        ("hobby.photo", "التصوير وصناعة المحتوى", "Photography and content",
        [
            ("hobby.photo.photo", "التصوير الفوتوغرافي", "Photography"),
            ("hobby.photo.video", "تصوير الفيديو", "Videography"),
            ("hobby.photo.content", "صناعة المحتوى", "Content creation")
        ]),
        ("hobby.fashion", "الخياطة والأزياء", "Sewing and fashion",
        [
            ("hobby.fashion.sew", "الخياطة", "Sewing"),
            ("hobby.fashion.emb", "التطريز", "Embroidery"),
            ("hobby.fashion.knit", "الحياكة", "Knitting"),
            ("hobby.fashion.crochet", "الكروشيه", "Crochet"),
            ("hobby.fashion.design", "تصميم الأزياء", "Fashion design")
        ]),
        ("hobby.craft", "الحرف والأشغال اليدوية", "Crafts",
        [
            ("hobby.craft.hand", "الأشغال اليدوية", "Handicrafts"),
            ("hobby.craft.accessories", "صناعة الإكسسوارات", "Accessories"),
            ("hobby.craft.pottery", "الفخار", "Pottery"),
            ("hobby.craft.wood", "الأعمال الخشبية", "Woodwork"),
            ("hobby.craft.flowers", "تنسيق الزهور", "Floral design")
        ]),
        ("hobby.write", "الكتابة والمهارات الأدبية", "Writing and literary skills",
        [
            ("hobby.write.creative", "الكتابة الإبداعية", "Creative writing"),
            ("hobby.write.poetry", "الشعر", "Poetry"),
            ("hobby.write.stories", "كتابة القصص", "Story writing"),
            ("hobby.write.read", "القراءة", "Reading")
        ]),
        ("hobby.mind", "الألعاب الذهنية والاستراتيجية", "Mind and strategy games",
        [
            ("hobby.mind.chess", "الشطرنج", "Chess"),
            ("hobby.mind.iq", "ألعاب الذكاء", "Brain games"),
            ("hobby.mind.puzzles", "الألغاز", "Puzzles"),
            ("hobby.mind.cube", "حل المكعبات", "Cube solving")
        ]),
        ("hobby.outdoor", "الهوايات الخارجية والطبيعة", "Outdoors and nature",
        [
            ("hobby.outdoor.home-garden", "الزراعة المنزلية", "Home gardening"),
            ("hobby.outdoor.garden", "البستنة", "Gardening"),
            ("hobby.outdoor.camp", "التخييم", "Camping"),
            ("hobby.outdoor.hike", "المشي والهايكنج", "Hiking")
        ]),
        ("hobby.home", "المهارات المنزلية", "Home skills",
        [
            ("hobby.home.org", "التنظيم المنزلي", "Home organization"),
            ("hobby.home.decor", "الديكور", "Decor"),
            ("hobby.home.plants", "العناية بالنباتات", "Plant care"),
            ("hobby.home.practical", "أعمال منزلية تطبيقية", "Practical home work")
        ]),
        ("hobby.trade", "الحرف المهنية", "Trade crafts",
        [
            ("hobby.trade.carpentry", "النجارة", "Carpentry"),
            ("hobby.trade.blacksmith", "الحدادة", "Blacksmithing"),
            ("hobby.trade.plumb", "السباكة", "Plumbing"),
            ("hobby.trade.elec", "الكهرباء", "Electrical"),
            ("hobby.trade.hvac", "التكييف والتبريد", "HVAC"),
            ("hobby.trade.maintain", "صيانة منزلية", "Home maintenance")
        ]),
        ("hobby.interest", "مجموعات الاهتمام المشترك", "Shared interest groups", []),
        ("hobby.other", "مهارات وهوايات أخرى", "Other skills and hobbies", [])
    ];

    private static IReadOnlyList<(string Code, string Ar, string En)> TechPaths() =>
    [
        ("tech.desktop", "برامج وتطبيقات الحاسب", "Computer applications"),
        ("tech.programming", "البرمجة وتطوير البرمجيات", "Programming"),
        ("tech.web", "تطوير المواقع", "Web development"),
        ("tech.mobile", "تطوير تطبيقات الجوال", "Mobile development"),
        ("tech.ai", "الذكاء الاصطناعي وتعلم الآلة", "AI and machine learning"),
        ("tech.data", "علوم وتحليل البيانات", "Data science"),
        ("tech.cyber", "الأمن السيبراني", "Cybersecurity"),
        ("tech.networks", "الشبكات", "Networks"),
        ("tech.cloud", "الحوسبة السحابية", "Cloud computing"),
        ("tech.db", "قواعد البيانات", "Databases"),
        ("tech.devops", "DevOps", "DevOps"),
        ("tech.uiux", "تصميم تجربة وواجهة المستخدم UI/UX", "UI/UX"),
        ("tech.graphic", "التصميم والجرافيك", "Graphic design"),
        ("tech.video", "المونتاج وصناعة المحتوى الرقمي", "Video and digital content"),
        ("tech.iot", "إنترنت الأشياء IoT", "IoT"),
        ("tech.robotics", "الروبوتات", "Robotics"),
        ("tech.word", "Microsoft Word", "Microsoft Word"),
        ("tech.excel", "Microsoft Excel", "Microsoft Excel"),
        ("tech.ppt", "Microsoft PowerPoint", "Microsoft PowerPoint"),
        ("tech.sql", "SQL", "SQL"),
        ("tech.powerbi", "Power BI", "Power BI"),
        ("tech.other", "أخرى", "Other")
    ];

    private static IReadOnlyList<(string Code, string Ar, string En)> KnowledgeFields() =>
    [
        ("know.physics", "الفيزياء", "Physics"),
        ("know.chem", "الكيمياء", "Chemistry"),
        ("know.bio", "الأحياء", "Biology"),
        ("know.geology", "علوم الأرض والجيولوجيا", "Geology"),
        ("know.marine", "علوم البحار", "Marine science"),
        ("know.astro", "علم الفلك", "Astronomy"),
        ("know.space", "علوم الفضاء", "Space science"),
        ("know.cosmo", "علم الكونيات", "Cosmology"),
        ("know.math", "الرياضيات", "Mathematics"),
        ("know.stats", "الإحصاء", "Statistics"),
        ("know.prob", "الاحتمالات", "Probability"),
        ("know.hist.islamic", "التاريخ الإسلامي", "Islamic history"),
        ("know.hist.saudi", "التاريخ السعودي", "Saudi history"),
        ("know.hist.arab", "التاريخ العربي", "Arab history"),
        ("know.hist.world", "التاريخ العالمي", "World history"),
        ("know.hist.ancient", "الحضارات القديمة", "Ancient civilizations"),
        ("know.hist.modern", "التاريخ الحديث والمعاصر", "Modern history"),
        ("know.geo.physical", "الجغرافيا الطبيعية", "Physical geography"),
        ("know.geo.human", "الجغرافيا البشرية", "Human geography"),
        ("know.geo.nations", "جغرافيا الدول والشعوب", "Geography of nations"),
        ("know.phil", "الفلسفة", "Philosophy"),
        ("know.logic", "المنطق", "Logic"),
        ("know.phil.sci", "فلسفة العلوم", "Philosophy of science"),
        ("know.ethics", "الأخلاق", "Ethics"),
        ("know.schools", "الفكر والمدارس الفكرية", "Schools of thought"),
        ("know.psych", "علم النفس", "Psychology"),
        ("know.socio", "علم الاجتماع", "Sociology"),
        ("know.anthro", "الأنثروبولوجيا", "Anthropology"),
        ("know.demo", "علم السكان", "Demography"),
        ("know.cultural", "الدراسات الثقافية", "Cultural studies"),
        ("know.econ", "الاقتصاد", "Economics"),
        ("know.econ.world", "الاقتصاد العالمي", "World economy"),
        ("know.econ.behav", "الاقتصاد السلوكي", "Behavioral economics"),
        ("know.econ.hist", "تاريخ الاقتصاد", "Economic history"),
        ("know.lit.ar", "الأدب العربي", "Arabic literature"),
        ("know.lit.world", "الأدب العالمي", "World literature"),
        ("know.poetry", "الشعر", "Poetry"),
        ("know.crit", "النقد الأدبي", "Literary criticism"),
        ("know.ling", "اللسانيات وعلم اللغة", "Linguistics"),
        ("know.culture.gen", "الثقافة العامة", "General culture"),
        ("know.culture.sa", "الثقافة السعودية", "Saudi culture"),
        ("know.culture.ar", "الثقافة العربية", "Arab culture"),
        ("know.culture.peoples", "ثقافات الشعوب", "World cultures"),
        ("know.heritage", "التراث", "Heritage"),
        ("know.customs", "العادات والتقاليد", "Customs and traditions"),
        ("know.visual", "الفنون البصرية", "Visual arts"),
        ("know.arch", "العمارة", "Architecture"),
        ("know.media", "الإعلام", "Media"),
        ("know.comms", "الاتصال", "Communication"),
        ("know.digital-media", "الإعلام الرقمي", "Digital media"),
        ("know.journalism", "الصحافة", "Journalism"),
        ("know.env", "البيئة", "Environment"),
        ("know.climate", "المناخ", "Climate"),
        ("know.biodiversity", "التنوع الحيوي", "Biodiversity"),
        ("know.sustain", "الاستدامة", "Sustainability"),
        ("know.resources", "الموارد الطبيعية", "Natural resources"),
        ("know.ai", "الذكاء الاصطناعي", "Artificial intelligence"),
        ("know.robotics", "الروبوتات", "Robotics"),
        ("know.emerging", "التقنيات الناشئة", "Emerging tech"),
        ("know.future", "علوم المستقبل", "Future studies"),
        ("know.space-explore", "استكشاف الفضاء", "Space exploration"),
        ("know.research", "البحث العلمي", "Scientific research"),
        ("know.methods", "مناهج البحث", "Research methods"),
        ("know.critical", "التفكير النقدي", "Critical thinking"),
        ("know.scientific", "التفكير العلمي", "Scientific thinking"),
        ("know.reasoning", "المنطق والاستدلال", "Logic and reasoning"),
        ("know.other", "أخرى", "Other")
    ];
}
