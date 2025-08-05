using Microsoft.EntityFrameworkCore;
using TimeScaleAPI.Models;

namespace TimeScaleAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Value> Values { get; set; }
    public DbSet<Result> Results { get; set; }
}
