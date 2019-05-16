using AutoMapper;
using MutationMode.UI.Models.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace MutationMode.UI.Models.ViewModels
{
    public class MainViewModel : IDisposable, INotifyPropertyChanged
    {
        AppOptions options = AppOptions.Instance;
        DataConnector connector;

        public event EventHandler<RequestOperationEventArgs> OperationRequested = delegate { };
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public ObservableCollection<LeaderTraitSwap> Leaders { get; private set; }
        public ObservableCollection<CivTraitSwap> Civs { get; private set; }

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

            this.connector = new DataConnector();
            await this.connector.LoadGameTextAsync();

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

        private void ValidateCacheLocation()
        {
            var cacheLocation = this.options.CacheLocation;

            if (!Directory.Exists(cacheLocation))
            {
                this.OperationRequested(this, new RequestOperationEventArgs(Operation.RequestCacheLocation));
            }
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

        public void AddTraitsToSelectingItem(List<TraitViewModel> traits)
        {
            this.SelectingItem.SwappingTraits = this.SelectingItem.SwappingTraits ??
                new ObservableCollection<TraitViewModelWithCheck>();

            foreach (var trait in traits)
            {
                // Skip duplicate
                if (this.SelectingItem.SwappingTraits.Any(q => q.TraitType == trait.TraitType))
                {
                    continue;
                }

                var traitInfo = Mapper.Map<TraitViewModelWithCheck>(trait);
                traitInfo.Checking = true;

                this.SelectingItem.SwappingTraits.Add(traitInfo);
            }

            this.SelectingItem.NotifyChange(nameof(this.SelectingItem.ForegroundColor));
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

        private void NotifyChange(string name)
        {
            this.PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        public void Dispose()
        {
            this.options.Save();
        }
    }

    public class BaseTraitSwap : INotifyPropertyChanged
    {
        public List<TraitViewModel> OriginalTraits { get; set; }
        public ObservableCollection<TraitViewModelWithCheck> SwappingTraits { get; set; }

        public Brush ForegroundColor
        {
            get
            {
                return this.SwappingTraits.Count > 0 ?
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

        public RequestOperationEventArgs(Operation operation)
        {
            this.Operation = operation;
        }

    }

    public enum Operation
    {
        RequestCacheLocation,
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

}
