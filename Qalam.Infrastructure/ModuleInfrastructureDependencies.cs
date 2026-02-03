using Microsoft.Extensions.DependencyInjection;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.InfrastructureBases;
using Qalam.Infrastructure.Repositories;
using Qalam.Infrastructure.Seeder;

namespace Qalam.Infrastructure
{
    public static class ModuleInfrastructureDependencies
    {
        public static IServiceCollection AddInfrastructureDependencies(this IServiceCollection services)
        {
            // Generic Repository
            services.AddTransient(typeof(IGenericRepositoryAsync<>), typeof(GenericRepositoryAsync<>));

            // Identity Repositories
            services.AddTransient<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddTransient<IPhoneOtpRepository, PhoneOtpRepository>();

            // Teacher Repositories
            services.AddTransient<ITeacherRepository, TeacherRepository>();
            services.AddTransient<ITeacherDocumentRepository, TeacherDocumentRepository>();
            services.AddTransient<ITeacherSubjectRepository, TeacherSubjectRepository>();
            services.AddTransient<ITeacherAvailabilityRepository, TeacherAvailabilityRepository>();

            // Education Repositories
            services.AddTransient<IEducationDomainRepository, EducationDomainRepository>();
            services.AddTransient<ICurriculumRepository, CurriculumRepository>();
            services.AddTransient<IEducationLevelRepository, EducationLevelRepository>();
            services.AddTransient<IGradeRepository, GradeRepository>();
            services.AddTransient<IAcademicTermRepository, AcademicTermRepository>();
            services.AddTransient<ISubjectRepository, SubjectRepository>();
            services.AddTransient<IContentUnitRepository, ContentUnitRepository>();
            services.AddTransient<ILessonRepository, LessonRepository>();

            // Quran Repositories
            services.AddTransient<IQuranLevelRepository, QuranLevelRepository>();
            services.AddTransient<IQuranContentTypeRepository, QuranContentTypeRepository>();

            // Teaching Repositories
            services.AddTransient<ITeachingModeRepository, TeachingModeRepository>();
            services.AddTransient<ISessionTypeRepository, SessionTypeRepository>();

            // Common Repositories
            services.AddTransient<ITimeSlotRepository, TimeSlotRepository>();

            // Database Seeder
            services.AddTransient<DatabaseSeeder>();
            services.AddTransient<RoleSeeder>();
            services.AddTransient<UserSeeder>();

            return services;
        }
    }
}

