using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Consignee> Consignees { get; set; }
    public DbSet<ConsigneeUnit> ConsigneeUnits { get; set; }
    public DbSet<Configuration> Configurations { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<UserRoleMapper> UserRoleMappers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}