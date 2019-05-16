using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using MutationMode.UI.Models.Entities;
using MutationMode.UI.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MutationMode.UI.Models
{

    public class DataConnector
    {

        AppOptions options = AppOptions.Instance;

        IDictionary<string, string> gameTexts;

        public async Task LoadGameTextAsync()
        {
            using (var dc = new LocalizationContext(
                this.GetConnectionStringFromFile(this.options.CacheDebugLocalization)))
            {
                this.gameTexts = (await dc.BaseGameText
                    .ToListAsync())
                    .ToDictionary(q => q.Tag, q => q.Text);
            }
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
            where T: LeaderViewModel
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
