using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChipAte;

public class OptionsViewModel
{
    public Options Options => _options;
    private Options _options;

    public OptionsViewModel(Options options)
    {
        _options = options;
    }
}
