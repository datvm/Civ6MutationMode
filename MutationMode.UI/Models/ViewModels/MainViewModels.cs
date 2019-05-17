using AutoMapper;
using MutationMode.UI.Models.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace MutationMode.UI.Models.ViewModels
{
    public class MainViewModel : IDisposable, INotifyPropertyChanged
    {
        const string ModFolderName = "1742519440";
        const string ModSqlFile = "mutationmodeabilitieschooser.sql";
        const string DefaultProfilePath = "profile.mpf";

        AppOptions options = AppOptions.Instance;
        DataConnector connector;

        public event EventHandler<RequestOperationEventArgs> OperationRequested = delegate { };
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public ObservableCollection<LeaderTraitSwap> Leaders { get; private set; }
        public ObservableCollection<CivTraitSwap> Civs { get; private set; }

        Dictionary<string, TraitViewModel> allTraits;

        public BaseTraitSwap SelectingItem { get; set; }
        public bool IsSelectingCiv
        {
            get
            {
                return this.SelectingItem is CivTraitSwap;
            }
        }

        public bool ShouldEnableEdit
        {
            get
            {
                return this.SelectingItem != null;
            }
        }

        public MainViewModel()
        {

        }

        public async Task InitializeAsync()
        {
            this.InitializeMapper();

            this.ValidateCacheLocation();
            this.ValidateModLocation();
            this.ValidateModFileWriting();

            this.connector = new DataConnector();
            this.connector.LoadGameTexts();

            this.Leaders = new ObservableCollection<LeaderTraitSwap>((await this.connector.GetLeadersWithTraitsAsync(true))
                .Select(q => new LeaderTraitSwap()
                {
                    Leader = q,
                    OriginalTraits = q.LeaderTraits,
                    SwappingTraits = new ObservableCollection<TraitViewModelWithCheck>(),
                }));

            this.Civs = new ObservableCollection<CivTraitSwap>((await this.connector.GetCivsWithTraitsAsync(true))
                .Select(q => new CivTraitSwap()
                {
                    Civilization = q,
                    OriginalTraits = q.CivilizationTraits,
                    SwappingTraits = new ObservableCollection<TraitViewModelWithCheck>(),
                }));

            this.allTraits = (await this.connector.GetAllTraitsAsync())
                .ToDictionary(q => q.TraitType);

            this.ImportProfile();

            //this.connector.CompressTextData(
            //    this.Leaders.Select(q => q.Leader).Cast<LeaderWithTraitsViewModel>().ToList(),
            //    this.Civs.Select(q => q.Civilization).Cast<CivWithTraitsViewModel>().ToList());
        }

        private void InitializeMapper()
        {
            Mapper.Initialize(config =>
            {
                config.CreateMap<Leader, LeaderWithTraitsViewModel>()
                    .ForMember(q => q.LeaderTraits, options => options.MapFrom(
                        q => q.LeaderTraits.Select(p => p.Trait)));

                config.CreateMap<Civilization, CivWithTraitsViewModel>()
                    .ForMember(q => q.CivilizationTraits, options => options.MapFrom(
                        q => q.CivilizationTraits.Select(p => p.Trait)));

                config.CreateMap<TraitViewModel, TraitViewModelWithCheck>();
            });
        }

        private void ValidateModLocation()
        {
            var location = this.options.ModLocation;
            if (!Directory.Exists(location) || Path.GetFileName(location) != ModFolderName)
            {
                var tryLocation = this.TryLookingForGameLocation();

                if (tryLocation == null)
                {
                    this.OperationRequested(this, new RequestOperationEventArgs(Operation.RequestModLocation));
                }
                else
                {
                    this.SetModLocationOverride(tryLocation);
                    this.OperationRequested(this, new RequestOperationEventArgs(Operation.AnnounceModAutoLocation, tryLocation));
                }
            }
        }

        private void ValidateModFileWriting()
        {
            try
            {
                var tempFile = Path.Combine(this.options.ModLocation, "test.file");
                File.WriteAllText(tempFile, "abc");
                File.Delete(tempFile);
            }
            catch (Exception ex)
            {
                this.OperationRequested(this, new RequestOperationEventArgs(Operation.ModFolderNotWritable, ex.Message, this.options.ModLocation));
            }
        }

        public void SetModLocationOverride(string location)
        {
            this.options.ModLocationOverride = location;
            this.ValidateModLocation();
        }

        private void ValidateCacheLocation()
        {
            var cacheLocation = this.options.CacheLocation;

            if (!Directory.Exists(cacheLocation))
            {
                this.OperationRequested(this, new RequestOperationEventArgs(Operation.RequestCacheLocation));
            }
        }

        private string TryLookingForGameLocation()
        {
            try
            {
                var configFile = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    @"Steam\config\config.vdf");

                if (File.Exists(configFile))
                {
                    var fileContent = File.ReadAllText(configFile);
                    var position = -1;

                    do
                    {
                        position = fileContent.IndexOf("BaseInstallFolder", position + 1);

                        if (position > -1)
                        {
                            var openDQ = fileContent.IndexOf('"', position);
                            openDQ = fileContent.IndexOf('"', openDQ + 1);
                            var closingDQ = fileContent.IndexOf('"', openDQ + 1);

                            var path = fileContent.Substring(openDQ + 1, closingDQ - openDQ - 1)
                                .Replace("\\\\", "\\");

                            // Look for mod path
                            var potentialModPath = Path.Combine(
                                path,
                                @"steamapps\workshop\content\289070\1742519440");

                            if (Directory.Exists(potentialModPath))
                            {
                                return potentialModPath;
                            }
                        }
                    } while (position > -1);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            return null;
        }

        public void SetCacheLocationOverride(string location)
        {
            this.options.CacheLocationOverride = location;
            this.ValidateCacheLocation();
        }

        public IEnumerable<BaseTraitSwap> GetItemTraitListForSelectingItem()
        {
            if (this.IsSelectingCiv)
            {
                return this.Civs;
            }
            else
            {
                return this.Leaders;
            }
        }

        public void AddTraitsToItem(List<TraitViewModel> traits, BaseTraitSwap item)
        {
            item.SwappingTraits = item.SwappingTraits ?? new ObservableCollection<TraitViewModelWithCheck>();

            foreach (var trait in traits)
            {
                // Skip duplicate
                if (item.SwappingTraits.Any(q => q.TraitType == trait.TraitType))
                {
                    continue;
                }

                var traitInfo = Mapper.Map<TraitViewModelWithCheck>(trait);
                traitInfo.Checking = true;

                item.SwappingTraits.Add(traitInfo);
            }

            item.NotifyChange(nameof(item.ForegroundColor));
        }

        public void AddTraitsToSelectingItem(List<TraitViewModel> traits)
        {
            this.AddTraitsToItem(traits, this.SelectingItem);
        }

        public void SelectItem(BaseTraitSwap item)
        {
            if (item == null)
            {
                return;
            }

            this.SelectingItem = item;
            this.NotifyChange(nameof(this.SelectingItem));
            this.NotifyChange(nameof(this.ShouldEnableEdit));
        }

        public void RemoveSelectedTraits()
        {
            var checkingItems = this.SelectingItem.SwappingTraits
                .Where(q => q.Checking)
                .ToList();

            foreach (var checkingItem in checkingItems)
            {
                this.SelectingItem.SwappingTraits.Remove(checkingItem);
            }

            this.SelectingItem.NotifyChange(nameof(this.SelectingItem.ForegroundColor));
        }

        public void AddOriginalToSelecting()
        {
            this.AddTraitsToSelectingItem(this.SelectingItem.OriginalTraits);
        }

        public void SetSelectAllChanges(bool selecting)
        {
            foreach (var item in this.SelectingItem.SwappingTraits)
            {
                item.Checking = selecting;
            }
        }

        public void SetSelectInvert()
        {
            foreach (var item in this.SelectingItem.SwappingTraits)
            {
                item.Checking = !item.Checking;
            }
        }

        public void ApplyModData()
        {
            var content = new StringBuilder();

            var changingLeaders = this.Leaders.Where(q => q.IsChanging).ToList();
            var changingCivs = this.Civs.Where(q => q.IsChanging).ToList();

            content.AppendLine("DROP TABLE IF EXISTS OriginalLeaderTraits;");
            content.AppendLine("DROP TABLE IF EXISTS OriginalCivilizationTraits;");
            content.AppendLine("CREATE TABLE OriginalLeaderTraits(LeaderType TEXT NOT NULL, TraitType TEXT NOT NULL, PRIMARY KEY(LeaderType, TraitType));");
            content.AppendLine("CREATE TABLE OriginalCivilizationTraits(CivilizationType TEXT NOT NULL, TraitType TEXT NOT NULL, PRIMARY KEY(CivilizationType, TraitType));");
            content.AppendLine("INSERT INTO OriginalLeaderTraits SELECT LeaderType, TraitType FROM LeaderTraits;");
            content.AppendLine("INSERT INTO OriginalCivilizationTraits SELECT CivilizationType, TraitType FROM CivilizationTraits;");

            content.AppendLine("DROP TABLE IF EXISTS MutationModeLeaders;");
            content.AppendLine("DROP TABLE IF EXISTS MutationModeCivs;");

            content.AppendLine("CREATE TABLE MutationModeLeaders(LeaderType TEXT NOT NULL, PRIMARY KEY(LeaderType));");
            content.AppendLine("CREATE TABLE MutationModeCivs(CivilizationType TEXT NOT NULL, PRIMARY KEY(CivilizationType));");

            if (changingLeaders.Count > 0)
            {
                content.AppendLine("INSERT OR IGNORE INTO MutationModeLeaders(LeaderType) VALUES ");
                foreach (var leader in changingLeaders)
                {
                    content.Append($"('{leader.Leader.LeaderType}'),");
                }
                this.RemoveLastOccurence(",", content);
                content.AppendLine(";");

                content.AppendLine("DELETE FROM LeaderTraits WHERE LeaderType IN (SELECT LeaderType FROM MutationModeLeaders);");

                content.Append("INSERT OR IGNORE INTO LeaderTraits(LeaderType, TraitType) VALUES ");
                foreach (var leader in changingLeaders)
                {
                    foreach (var trait in leader.SwappingTraits)
                    {
                        content.Append($"('{leader.Leader.LeaderType}', '{trait.TraitType}'),");
                    }
                }
                this.RemoveLastOccurence(",", content);
                content.AppendLine(";");
            }

            if (changingCivs.Count > 0)
            {
                content.AppendLine("INSERT OR IGNORE INTO MutationModeCivs(CivilizationType) VALUES ");
                foreach (var civ in changingCivs)
                {
                    content.Append($"('{civ.Civilization.CivilizationType}'),");
                }
                this.RemoveLastOccurence(",", content);
                content.AppendLine(";");

                content.AppendLine("DELETE FROM CivilizationTraits WHERE CivilizationType IN (SELECT CivilizationType FROM MutationModeCivs);");

                content.Append("INSERT OR IGNORE INTO CivilizationTraits(CivilizationType, TraitType) VALUES ");
                foreach (var civ in changingCivs)
                {
                    foreach (var trait in civ.SwappingTraits)
                    {
                        content.Append($"('{civ.Civilization.CivilizationType}', '{trait.TraitType}'),");
                    }
                }
                this.RemoveLastOccurence(",", content);
                content.AppendLine(";");
            }

            var filePath = GetModSqlFileLocation();
            File.WriteAllText(filePath, content.ToString());
        }

        private string GetModSqlFileLocation()
        {
            return Path.Combine(this.options.ModLocation, ModSqlFile);
        }

        public void RemoveAllCivChanges()
        {
            foreach (var civ in this.Civs)
            {
                if (civ.IsChanging)
                {
                    civ.SwappingTraits.Clear();
                    civ.NotifyChange(nameof(civ.ForegroundColor));
                }
            }
        }

        public void RemoveAllLeaderChanges()
        {
            foreach (var leader in this.Leaders)
            {
                if (leader.IsChanging)
                {
                    leader.SwappingTraits.Clear();
                    leader.NotifyChange(nameof(leader.ForegroundColor));
                }
            }
        }

        public void ImportProfile(string filePath = null)
        {
            filePath = filePath ?? DefaultProfilePath;

            if (!File.Exists(filePath))
            {
                return;
            }

            var fileContent = File.ReadAllText(filePath);
            var changes = JsonConvert.DeserializeObject<ProfileFileViewModel>(fileContent);

            var warning = new StringBuilder();

            foreach (var civ in this.Civs)
            {
                civ.SwappingTraits.Clear();
            }

            foreach (var leader in this.Leaders)
            {
                leader.SwappingTraits.Clear();
            }

            foreach (var civChange in changes.CivChanges)
            {
                var civ = this.Civs.FirstOrDefault(q => q.Civilization.CivilizationType == civChange.Key);

                if (civ == null)
                {
                    warning.AppendLine($"Civilization {civChange.Key} does not exist in current data.");
                    continue;
                }

                foreach (var trait in civChange.Value.Traits)
                {
                    if (!this.allTraits.TryGetValue(trait, out var traitInfo))
                    {
                        warning.AppendLine($"Trait {trait} does not exist in current data.");
                        continue;
                    }

                    var traitWithCheck = Mapper.Map<TraitViewModelWithCheck>(traitInfo);
                    traitWithCheck.Checking = true;

                    civ.SwappingTraits.Add(traitWithCheck);
                }
            }

            foreach (var leaderChange in changes.LeaderChanges)
            {
                var leader = this.Leaders.FirstOrDefault(q => q.Leader.LeaderType == leaderChange.Key);

                if (leader == null)
                {
                    warning.AppendLine($"Leader {leaderChange.Key} does not exist in current data.");
                    continue;
                }

                foreach (var trait in leaderChange.Value.Traits)
                {
                    if (!this.allTraits.TryGetValue(trait, out var traitInfo))
                    {
                        warning.AppendLine($"Trait {trait} does not exist in current data.");
                        continue;
                    }

                    var traitWithCheck = Mapper.Map<TraitViewModelWithCheck>(traitInfo);
                    traitWithCheck.Checking = true;

                    leader.SwappingTraits.Add(traitWithCheck);
                }
            }

            if (warning.Length > 0)
            {
                this.OperationRequested(this, new RequestOperationEventArgs(Operation.ImportProfileWarning, warning.ToString()));
            }
        }

        public void ExportProfile(string filePath = null)
        {
            filePath = filePath ?? DefaultProfilePath;

            var changes = new ProfileFileViewModel();

            changes.CivChanges = this.Civs
                .Where(q => q.IsChanging)
                .ToDictionary(
                    q => q.Civilization.CivilizationType,
                    q => new ProfileChangeItemViewModel()
                    {
                        Traits = q.SwappingTraits.Select(p => p.TraitType).ToList(),
                    });

            changes.LeaderChanges = this.Leaders
                .Where(q => q.IsChanging)
                .ToDictionary(
                    q => q.Leader.LeaderType,
                    q => new ProfileChangeItemViewModel()
                    {
                        Traits = q.SwappingTraits.Select(p => p.TraitType).ToList(),
                    });

            File.WriteAllText(filePath, JsonConvert.SerializeObject(changes));
        }

        public void RevertToDefault()
        {
            File.Copy(
                Path.Combine(DataConnector.ProgramFolder, "default-mod-file.sql"),
                this.GetModSqlFileLocation(), true);
        }

        public void Randomize(bool swapCivs, bool swapLeaders, int seed, bool addOrigins)
        {
            this.RemoveAllCivChanges();
            this.RemoveAllLeaderChanges();

            if (swapCivs)
            {
                var random = new Random(seed);
                this.SwapTraits(this.Civs, addOrigins, random);
            }

            if (swapLeaders)
            {
                var random = new Random(seed);
                this.SwapTraits(this.Leaders, addOrigins, random);
            }
        }

        private void SwapTraits(IEnumerable<BaseTraitSwap> items, bool addOrigins, Random random)
        {
            var position = Enumerable.Range(0, items.Count()).ToList();

            while (true)
            {
                for (int i = 0; i < position.Count; i++)
                {
                    var swapPos = random.Next(position.Count);

                    var temp = position[i];
                    position[i] = position[swapPos];
                    position[swapPos] = temp;
                }

                // Check to make sure everyone is swapped
                var ok = true;
                for (int i = 0; i < position.Count; i++)
                {
                    if (position[i] == i)
                    {
                        ok = false;
                        break;
                    }
                }

                if (ok)
                {
                    break;
                }
            }

            for (int i = 0; i < position.Count; i++)
            {
                var item = items.ElementAt(i);

                if (addOrigins)
                {
                    this.AddTraitsToItem(item.OriginalTraits, item);
                }

                var swappingItem = items.ElementAt(position[i]);
                this.AddTraitsToItem(swappingItem.OriginalTraits, item);
            }
        }

        private void NotifyChange(string name)
        {
            this.PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        private void RemoveLastOccurence(string content, StringBuilder stringBuilder)
        {
            var pos = stringBuilder.ToString().LastIndexOf(content);
            if (pos > -1)
            {
                stringBuilder.Remove(pos, content.Length);
            }
        }

        public void Dispose()
        {
            this.ExportProfile();
            this.options.Save();
        }
    }

    public class BaseTraitSwap : INotifyPropertyChanged
    {
        public List<TraitViewModel> OriginalTraits { get; set; }
        public ObservableCollection<TraitViewModelWithCheck> SwappingTraits { get; set; }

        public bool IsChanging
        {
            get
            {
                return this.SwappingTraits != null && this.SwappingTraits.Count > 0;
            }
        }

        public Brush ForegroundColor
        {
            get
            {
                return this.IsChanging ?
                    Brushes.DarkGreen :
                    Brushes.Black;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public void NotifyChange(string name)
        {
            this.PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
    }

    public class LeaderTraitSwap : BaseTraitSwap
    {
        public LeaderViewModel Leader { get; set; }

        public override string ToString()
        {
            return this.Leader.NameLookup;
        }
    }

    public class CivTraitSwap : BaseTraitSwap
    {
        public CivilizationViewModel Civilization { get; set; }

        public override string ToString()
        {
            return this.Civilization.NameLookup;
        }
    }

    public class RequestOperationEventArgs : EventArgs
    {
        public Operation Operation { get; private set; }
        public object[] Params { get; private set; }

        public RequestOperationEventArgs(Operation operation)
        {
            this.Operation = operation;
        }

        public RequestOperationEventArgs(Operation operation, params object[] @params)
            : this(operation)
        {
            this.Params = @params;
        }

    }

    public enum Operation
    {
        RequestCacheLocation,
        RequestModLocation,
        AnnounceModAutoLocation,
        ModFolderNotWritable,
        ImportProfileWarning,
    }

    public class TraitViewModelWithCheck : TraitViewModel, INotifyPropertyChanged
    {

        bool checkingField;
        public bool Checking
        {
            get
            {
                return this.checkingField;
            }
            set
            {
                this.checkingField = value;
                this.NotifyChange(nameof(this.Checking));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public void NotifyChange(string name)
        {
            this.PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
    }

    public class ProfileFileViewModel
    {
        public Dictionary<string, ProfileChangeItemViewModel> LeaderChanges { get; set; }
        public Dictionary<string, ProfileChangeItemViewModel> CivChanges { get; set; }
    }

    public class ProfileChangeItemViewModel
    {
        public List<string> Traits { get; set; }
    }

}
