using Microsoft.EntityFrameworkCore;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Seeding;

public static class SeederHelper
{
    /// <summary>
    /// Checks if a table exists in the database
    /// </summary>
    public static async Task<bool> TableExistsAsync(ApplicationDBContext context, string schemaName, string tableName)
    {
        try
        {
            var result = await context.Database
                .SqlQueryRaw<int>($@"
                    SELECT CAST(COUNT(*) AS INT) AS Value 
                    FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_SCHEMA = '{schemaName}' 
                    AND TABLE_NAME = '{tableName}'")
                .FirstOrDefaultAsync();
            
            return result > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if a schema exists in the database
    /// </summary>
    public static async Task<bool> SchemaExistsAsync(ApplicationDBContext context, string schemaName)
    {
        try
        {
            var result = await context.Database
                .SqlQueryRaw<int>($@"
                    SELECT CAST(COUNT(*) AS INT) AS Value 
                    FROM INFORMATION_SCHEMA.SCHEMATA 
                    WHERE SCHEMA_NAME = '{schemaName}'")
                .FirstOrDefaultAsync();
            
            return result > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Safely checks if any records exist in a DbSet
    /// Returns false if table doesn't exist or on any error
    /// </summary>
    public static async Task<bool> HasAnyDataAsync<T>(DbSet<T> dbSet) where T : class
    {
        try
        {
            return await dbSet.AnyAsync();
        }
        catch
        {
            // Table doesn't exist or other error - return false to trigger seeding
            return false;
        }
    }

    /// <summary>
    /// Safely checks if any records matching a condition exist in a DbSet
    /// Returns false if table doesn't exist or on any error
    /// </summary>
    public static async Task<bool> HasAnyDataAsync<T>(DbSet<T> dbSet, System.Linq.Expressions.Expression<Func<T, bool>> predicate) where T : class
    {
        try
        {
            return await dbSet.AnyAsync(predicate);
        }
        catch
        {
            // Table doesn't exist or other error - return false to trigger seeding
            return false;
        }
    }
}
