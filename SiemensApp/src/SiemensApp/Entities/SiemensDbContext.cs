using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace SiemensApp.Entities
{
    public class SiemensDbContext : DbContext
    {
        public SiemensDbContext(DbContextOptions<SiemensDbContext> options) : base(options)
        {
            //this.UseManagedIdentity();
            ChangeTracker.LazyLoadingEnabled = false;
            ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        public DbSet<SiteConfigurationEntity> SiteConfigurations { get; set; }
        public DbSet<ScanRequestEntity> ScanRequests { get; set; }
        public DbSet<SystemObjectEntity> SystemObjects { get; set; }
        public DbQuery<ChildrenExistResult> ChildrenObjects { get; set; }

        public IQueryable<ChildrenExistResult> SystemObjectsChildrenExists => ChildrenObjects.FromSql(
            @"select o.[Id], case when exists(select 1 from [SystemObjects] o2 where o2.[ParentId] = o.[Id]) then cast(1 as bit) else cast(0 as bit) end as [Exist]
                    from [SystemObjects] o");
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<SiteConfigurationEntity>().HasKey(x => x.SiteId);

        }
    }
}
