using Blog.Models;
using Microsoft.EntityFrameworkCore;

namespace Blog
{
    public class MDBlogDbContext : DbContext
    {
        public MDBlogDbContext(DbContextOptions<MDBlogDbContext> options) : base(options) { }

        public DbSet<MDBlogPost> Posts => Set<MDBlogPost>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MDBlogPost>()
                .HasIndex(p => p.Title);

            modelBuilder.Entity<MDBlogPost>()
                .HasIndex(p => p.Date);

            modelBuilder.Entity<MDBlogPost>()
                .HasIndex(p => p.Tags);

        }
        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Properties<string>().UseCollation("NOCASE");
        }
    }
}
