using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MutationMode.UI.Models.Entities
{
    public class GameplayContext : DbContext
    {

        public string ConnectionString { get; private set; }

        public DbSet<Trait> Traits { get; set; }
        public DbSet<Leader> Leaders { get; set; }
        public DbSet<LeaderTrait> LeaderTraits { get; set; }
        public DbSet<Civilization> Civilizations { get; set; }
        public DbSet<CivilizationTrait> CivilizationTraits { get; set; }
        public DbSet<CivilizationLeader> CivilizationLeaders { get; set; }

        public GameplayContext(string connectionString)
        {
            this.ConnectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(this.ConnectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Trait>()
                .HasKey(q => q.TraitType);

            modelBuilder.Entity<Leader>()
                .HasKey(q => q.LeaderType);

            modelBuilder.Entity<LeaderTrait>()
                .HasKey(q => new { q.LeaderType, q.TraitType });

            modelBuilder.Entity<Civilization>()
                .HasKey(q => q.CivilizationType);

            modelBuilder.Entity<CivilizationTrait>()
                .HasKey(q => new { q.CivilizationType, q.TraitType });

            modelBuilder.Entity<CivilizationLeader>()
                .HasKey(q => new { q.LeaderType, q.CivilizationType });
        }

    }

    public class Trait
    {
        public string TraitType { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        
        public ICollection<LeaderTrait> LeaderTraits { get; set; }
        public ICollection<CivilizationTrait> CivilizationTraits { get; set; }

    }

    public class Leader
    {
        public string LeaderType { get; set; }
        public string Name { get; set; }
        public string InheritFrom { get; set; }

        public ICollection<LeaderTrait> LeaderTraits { get; set; }
        public ICollection<CivilizationLeader> CivilizationLeaders { get; set; }
    }

    public class LeaderTrait
    {
        public string LeaderType { get; set; }
        public string TraitType { get; set; }

        public Leader Leader { get; set; }
        public Trait Trait { get; set; }
    }

    public class Civilization
    {
        public string CivilizationType { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string StartingCivilizationLevelType { get; set; }

        public ICollection<CivilizationTrait> CivilizationTraits { get; set; }
        public ICollection<CivilizationLeader> CivilizationLeaders { get; set; }
    }

    public class CivilizationTrait
    {
        public string CivilizationType { get; set; }
        public string TraitType { get; set; }

        public Civilization Civilization { get; set; }
        public Trait Trait { get; set; }

    }

    public class CivilizationLeader
    {
        public string LeaderType { get; set; }
        public string CivilizationType { get; set; }

        public Leader Leader { get; set; }
        public Civilization Civilization { get; set; }

    }

}
