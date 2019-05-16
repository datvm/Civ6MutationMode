using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MutationMode.UI.Models
{

    public class AppOptions
    {
        const string OptionFile = "options.json";

        public static readonly AppOptions Instance = new AppOptions();

        public string CacheLocationOverride { get; set; }
        public int Seed { get; set; }

        public static string DefaultCacheLocation
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    @"my games\Sid Meier's Civilization VI\Cache");
            }
        }

        [JsonIgnore]
        public string CacheLocation
        {
            get
            {
                return this.CacheLocationOverride ?? DefaultCacheLocation;
            }
        }

        [JsonIgnore]
        public string CacheDebugGameplay
        {
            get
            {
                return Path.Combine(this.CacheLocation, "DebugGameplay.sqlite");
            }
        }

        [JsonIgnore]
        public string CacheDebugLocalization
        {
            get
            {
                return Path.Combine(this.CacheLocation, "DebugLocalization.sqlite");
            }
        }

        private AppOptions()
        {
            if (File.Exists(OptionFile))
            {
                var fileContent = File.ReadAllText(OptionFile);
                JsonConvert.PopulateObject(fileContent, this);
            }
            else
            {
                this.Save();
            }

            if (this.Seed == 0)
            {
                this.Seed = new Random().Next();
                this.Save();
            }
        }

        public void Save()
        {
            File.WriteAllText(OptionFile, JsonConvert.SerializeObject(this, new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Include,
                Formatting = Formatting.Indented,
            }));
        }

    }

}
