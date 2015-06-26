using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedirectService.Models {
    public class NumberConfigContext
    {
        private static List<NumberConfig> _numberConfigs;

        public static List<NumberConfig> Current()
        {
            if (_numberConfigs == null)
                _numberConfigs = new List<NumberConfig>();
            return _numberConfigs;
        }
    }

    

    public class NumberConfig {
        public string From { get; set; }
        public string To { get; set; }
    }
}
