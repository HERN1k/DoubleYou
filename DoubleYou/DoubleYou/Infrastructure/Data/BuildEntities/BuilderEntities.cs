using Microsoft.EntityFrameworkCore;

namespace DoubleYou.Infrastructure.Data.BuildEntities
{
    internal static class BuilderEntities
    {
        public static void BuildUser(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Domain.Entities.Entities.User>(entity =>
            {
                entity.HasIndex(e => e.Id)
                    .IsUnique();
            });
        }

        public static void BuildWords(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Domain.Entities.Entities.Word>(entity =>
            {
                entity.HasIndex(e => e.Id)
                    .IsUnique();

                entity.HasIndex(e => e.Data);

                entity.Property(e => e.Topic)
                    .HasConversion<string>();

                entity.HasIndex(e => e.Topic);

                entity.HasIndex(e => e.LearnedDate);
            });
        }
    }
}