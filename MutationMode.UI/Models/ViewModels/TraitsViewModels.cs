using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MutationMode.UI.Models.ViewModels
{
    
    public class TraitCheckListViewModel
    {
        public TraitViewModel Trait { get; set; }
        public bool Checking { get; set; }

        public override string ToString()
        {
            return $"{this.Trait.NameLookup} ({this.Trait.TraitType})";
        }

    }

}
