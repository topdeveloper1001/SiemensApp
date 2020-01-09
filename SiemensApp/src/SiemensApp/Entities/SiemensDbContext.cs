using Microsoft.EntityFrameworkCore;

namespace SiemensApp.Entities
{
    public class SiemensDbContext : DbContext
    {
        public SiemensDbContext(DbContextOptions<SiemensDbContext> options) : base(options)
        {
            this.UseManagedIdentity();
            ChangeTracker.LazyLoadingEnabled = false;
            ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        public DbSet<SiteConfigurationEntity> SiteConfigurations { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<SiteConfigurationEntity>().HasKey(x => x.SiteId);
        }
    }
}
