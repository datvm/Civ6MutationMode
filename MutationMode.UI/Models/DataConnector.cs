using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using MutationMode.UI.Models.Entities;
using MutationMode.UI.Models.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace MutationMode.UI.Models
{

    public class DataConnector
    {
        public static readonly string ProgramFolder = Path.GetDirectoryName(typeof(DataConnector).Assembly.Location);

        AppOptions options = AppOptions.Instance;

        IDictionary<string, string> gameTexts;

        public void CompressTextData(List<LeaderWithTraitsViewModel> leaders, List<CivWithTraitsViewModel> civs)
        {
            var fullTexts = new Dictionary<string, string>();

            var texts = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                File.ReadAllText(@"D:\Temp\texts1.json"));
            foreach (var text in texts)
            {
                fullTexts[text.Key] = text.Value;
            }

            texts = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                File.ReadAllText(@"D:\Temp\texts2.json"));
            foreach (var text in texts)
            {
                fullTexts[text.Key] = text.Value;
            }

            var neededValues = new Dictionary<string, string>();
            Action<string> tryAdd = key =>
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    return;
                }

                if (!fullTexts.TryGetValue(key, out var result))
                {
                    Debug.WriteLine("Missing: " + key);
                    return;
                }

                neededValues[key] = result;
            };
            
            foreach (var leader in leaders)
            {
                tryAdd(leader.Name);

                foreach (var trait in leader.LeaderTraits)
                {
                    tryAdd(trait.Name);
                    tryAdd(trait.Description);
                }
            }

            foreach (var civ in civs)
            {
                tryAdd(civ.Name);
                tryAdd(civ.Description);

                foreach (var trait in civ.CivilizationTraits)
                {
                    tryAdd(trait.Name);
                    tryAdd(trait.Description);
                }
            }

            File.WriteAllText(@"D:\Temp\texts-cache.json", JsonConvert.SerializeObject(neededValues));
        }

        public void LoadGameTexts()
        {
            var textFile = Path.Combine(ProgramFolder, "texts-cache.json");
            this.gameTexts = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                File.ReadAllText(textFile));
        }

        public async Task LoadGameText1Async()
        {
            using (var dc = new LocalizationContext(
                this.GetConnectionStringFromFile(this.options.CacheDebugLocalization)))
            {
                this.gameTexts = (await dc.BaseGameText
                    .ToListAsync())
                    .ToDictionary(q => q.Tag, q => q.Text);
            }

            File.WriteAllText(@"D:\Temp\texts1.json", JsonConvert.SerializeObject(this.gameTexts));
        }

        public void LoadGameText2()
        {
            this.gameTexts = new Dictionary<string, string>();

            const string Folder = @"F:\Game\SteamLibrary\steamapps\common\Sid Meier's Civilization VI\";

            var counter = 0;
            var files = Directory.GetFiles(Folder, "*.xml", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                try
                {
                    Debug.WriteLine($"{++counter}/{files.Length}: {file}");

                    var xmlDoc = new XmlDocument();
                    xmlDoc.Load(file);

                    Action<string> scanTag = tagName =>
                    {
                        var englishTexts = xmlDoc.DocumentElement.GetElementsByTagName(tagName);

                        foreach (XmlElement englishText in englishTexts)
                        {
                            var rows = englishText.GetElementsByTagName("Row");

                            foreach (XmlElement row in rows)
                            {
                                this.gameTexts[row.GetAttribute("Tag")] = row.GetElementsByTagName("Text")[0].InnerText;
                            }
                        }
                    };

                    scanTag("BaseGameText");
                    scanTag("EnglishText");
                    scanTag("LocalizedText");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }

            }

            File.WriteAllText(@"D:\Temp\texts2.json", JsonConvert.SerializeObject(this.gameTexts));
        }

        public async Task<List<LeaderWithTraitsViewModel>> GetLeadersWithTraitsAsync(bool getValidOnly)
        {
            using (var dc = this.GetGameplayContext())
            {
                IQueryable<Leader> query = dc.Leaders
                    .AsNoTracking()
                    .Include(q => q.LeaderTraits);

                if (getValidOnly)
                {
                    query = query.Where(q => q.InheritFrom == "LEADER_DEFAULT");
                }

                var result = await query
                    .ProjectTo<LeaderWithTraitsViewModel>()
                    .ToListAsync();

                this.LookupLeaderTexts(result);
                this.LookupTraitTexts(result.SelectMany(q => q.LeaderTraits).ToList());

                return result;
            }
        }

        public async Task<List<CivWithTraitsViewModel>> GetCivsWithTraitsAsync(bool getValidOnly)
        {
            using (var dc = this.GetGameplayContext())
            {
                IQueryable<Civilization> query = dc.Civilizations
                    .AsNoTracking()
                    .Include(q => q.CivilizationTraits);

                if (getValidOnly)
                {
                    query = query.Where(q => q.StartingCivilizationLevelType == "CIVILIZATION_LEVEL_FULL_CIV");
                }

                var result = await query
                    .ProjectTo<CivWithTraitsViewModel>()
                    .ToListAsync();

                this.LookupCivTexts(result);
                this.LookupTraitTexts(result.SelectMany(q => q.CivilizationTraits).ToList());

                return result;
            }
        }

        public string LookupText(string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                return "";
            }

            if (this.gameTexts == null)
            {
                throw new InvalidOperationException("Game Text not loaded");
            }

            return this.gameTexts.TryGetValue(tag, out var result) ?
                result :
                tag;
        }

        public List<T> LookupCivTexts<T>(List<T> civs)
            where T : CivilizationViewModel
        {
            foreach (var civ in civs)
            {
                civ.NameLookup = this.LookupText(civ.Name);
                civ.DescriptionLookup = this.LookupText(civ.Description);
            }

            return civs;
        }

        public List<T> LookupLeaderTexts<T>(List<T> leaders)
            where T : LeaderViewModel
        {
            foreach (var leader in leaders)
            {
                leader.NameLookup = this.LookupText(leader.Name);
            }

            return leaders;
        }

        public List<T> LookupTraitTexts<T>(List<T> traits)
            where T : TraitViewModel
        {
            foreach (var trait in traits)
            {
                trait.NameLookup = this.LookupText(trait.Name);
                trait.DescriptionLookup = this.LookupText(trait.Description);
            }

            return traits;
        }

        private string GetConnectionStringFromFile(string filePath)
        {
            return "Data Source=" + filePath;
        }

        private GameplayContext GetGameplayContext()
        {
            return new GameplayContext(
                this.GetConnectionStringFromFile(this.options.CacheDebugGameplay));
        }

    }

}
