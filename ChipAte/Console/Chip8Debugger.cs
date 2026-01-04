using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChipAte.Console;

public class Chip8Debugger
{
    private Chip8 chip8Instance;

    public Chip8Debugger(Chip8 chip8)
    {
        chip8Instance = chip8;
    }

    private bool enabled = false;
    public bool Enabled
    {
        get => enabled;
        set
        {
            if (enabled == value) return;
            enabled = value;
            HandleEnabledChanged();
        }
    }

    private bool singleStepping = false;
    public bool SingleStepping
    {
        get => singleStepping;
        set
        {
            if (singleStepping == value) return;
            singleStepping = value;
            HandleSingleSteppingChanged();
        }
    }

    private void HandleEnabledChanged()
    {
        if (enabled)
        {
            // Activate debugger UI
        }
        else
        {
            // Deactivate debugger UI
        }
    }
    private void HandleSingleSteppingChanged()
    {
        if (singleStepping)
        {
            // Activate single stepping mode
        }
        else
        {
            // Deactivate single stepping mode
        }
    }

}