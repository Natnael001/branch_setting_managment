using Microsoft.EntityFrameworkCore;
using E_Invoice_Parameter.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {

    }
    public DbSet<SystemConstants> SystemConstants { get; set; }
    public DbSet<Article> Articles { get; set; }
    public DbSet<Device> Device { get; set; }
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