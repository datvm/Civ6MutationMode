using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MutationMode.UI.Models.Entities
{

    public class LocalizationContext : DbContext
    {
        public string ConnectionString { get; private set; }
        public DbSet<BaseGameText> BaseGameText { get; set; }

        public LocalizationContext(string connectionString)
        {
            this.ConnectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(this.ConnectionString);
        }
    }

    public class BaseGameText
    {

        [Key]
        public string Tag { get; set; }
        public string Text { get; set; }
    }

}
