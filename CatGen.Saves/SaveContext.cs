using CatGen.DTOs;

using Microsoft.EntityFrameworkCore;

namespace CatGen.Saves;

public sealed class SaveContext : DbContext
{
    public DbSet<ModelOnDisk> ModelsOnDisk => Set<ModelOnDisk>();

    public DbSet<SpawnedEntityMetadata> SpawnedEntities => Set<SpawnedEntityMetadata>();

    public SaveContext()
    {
        Database.EnsureCreated();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=catgen.db");
    }
}
