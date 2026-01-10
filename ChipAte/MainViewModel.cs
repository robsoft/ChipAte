using ChipAte.Console;
using Gum.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChipAte;

public class MainViewModel : ViewModel
{
    private Chip8 _chip8;
    private Chip8Debugger _debugger;
    private Options _options;

    public string FilePath { get; set; }

    public MainViewModel(Chip8 chip8, Chip8Debugger debugger, Options options)
    {
        _chip8 = chip8;
        _debugger = debugger;
        _options = options;
        FilePath = chip8.ROMPath;
    }
}
