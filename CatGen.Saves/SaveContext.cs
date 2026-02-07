using CatGen.DTOs;

using Microsoft.EntityFrameworkCore;

namespace CatGen.Saves;

public class SaveContext : DbContext
{
    public DbSet<ModelOnDisk> ModelsOnDisk => Set<ModelOnDisk>();

    public DbSet<SpawnedObjectMetadata> SpawnedObjects => Set<SpawnedObjectMetadata>();

    public SaveContext()
    {
        Database.EnsureCreated();
        Database.Migrate();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=catgen.db");
    }
}
