using System.IO;

using DoubleYou.Utilities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DoubleYou.Infrastructure.Data.Contexts
{
    public class AppDBContextFactory : IDesignTimeDbContextFactory<AppDBContext>
    {
        public AppDBContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDBContext>();

            optionsBuilder.UseSqlite(string.Concat("Data Source=", Path.Combine(Path.GetTempPath(), Constants.SQL_DB_FILE_NAME)));

            return new AppDBContext(optionsBuilder.Options);
        }
    }
}