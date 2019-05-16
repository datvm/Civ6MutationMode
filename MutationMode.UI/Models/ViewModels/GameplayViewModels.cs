using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MutationMode.UI.Models.ViewModels
{

    public class CivilizationViewModel
    {
        public string CivilizationType { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string StartingCivilizationLevelType { get; set; }

        public string NameLookup { get; set; }
        public string DescriptionLookup { get; set; }
    }

    public class LeaderViewModel
    {

        public string LeaderType { get; set; }
        public string Name { get; set; }
        public string InheritFrom { get; set; }

        public string NameLookup { get; set; }
    }

    public class TraitViewModel
    {
        public string TraitType { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public string NameLookup { get; set; }
        public string DescriptionLookup { get; set; }

        public override string ToString()
        {
            return $"{this.NameLookup} ({this.TraitType})";
        }
    }

    public class LeaderWithTraitsViewModel : LeaderViewModel
    {
        public List<TraitViewModel> LeaderTraits { get; set; }
    }

    public class CivWithTraitsViewModel : CivilizationViewModel
    {
        public List<TraitViewModel> CivilizationTraits { get; set; }
    }

}
