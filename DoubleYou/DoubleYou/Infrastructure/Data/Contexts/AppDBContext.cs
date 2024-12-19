using DoubleYou.Infrastructure.Data.BuildEntities;

using Microsoft.EntityFrameworkCore;

namespace DoubleYou.Infrastructure.Data.Contexts
{
#pragma warning disable IL2026
    public partial class AppDBContext(DbContextOptions<AppDBContext> options) : DbContext(options)
#pragma warning restore
    {
#pragma warning disable CS8618
        public DbSet<Domain.Entities.Entities.User> User { get; set; }
        public DbSet<Domain.Entities.Entities.Word> Words { get; set; }
#pragma warning restore

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.BuildUser();
            modelBuilder.BuildWords();

            base.OnModelCreating(modelBuilder);
        }
    }
}