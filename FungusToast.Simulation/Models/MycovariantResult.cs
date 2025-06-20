using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FungusToast.Simulation.Models
{
    public class MycovariantResult
    {
        public int MycovariantId { get; set; }
        public string MycovariantName { get; set; } = "";
        public string MycovariantType { get; set; } = "";
        public bool Triggered { get; set; }
        public string EffectSummary { get; set; } = "";
    }
}
