using Gum.Mvvm;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChipAte;

public static class DisplaySizes
{
    private const int SmallWidth = 640;
    private const int SmallHeight = 480;
    private const int DefaultWidth = 800;
    private const int DefaultHeight = 600;
    private const int LargeWidth = 1024;
    private const int LargeHeight = 768;
    private const int FullScreenWidth = 0;
    private const int FullScreenHeight = 0;

    public static (int width, int height) GetDimensions(Options.ScreenSize screenSize)
    {
        return screenSize switch
        {
            Options.ScreenSize.Small => (SmallWidth, SmallHeight),
            Options.ScreenSize.Default => (DefaultWidth, DefaultHeight),
            Options.ScreenSize.Large => (LargeWidth, LargeHeight),
            Options.ScreenSize.FullScreen => (FullScreenWidth, FullScreenHeight),
            _ => (DefaultWidth, DefaultHeight),
        };
    }

}


public class Options : ViewModel
{
    public enum ScreenSize { Small, Default, Large, FullScreen };
    public bool SoundEnabled { get => Get<bool>(); set => Set(value); }
    public float SoundVolume { get => Get<float>(); set => Set(value); }
    public Color ForegroundColor { get => Get<Color>(); set => Set(value); }
    public Color BackgroundColor { get => Get<Color>(); set => Set(value); }
    public ScreenSize DisplaySize { get => Get<ScreenSize>(); set => Set(value); }

    public string LastLoadFromFolder { get => Get<string>(); set => Set(value); }

    public Options()
    {
        // TODO: these need persisting to some kind of settings file
        SoundEnabled = true;
        SoundVolume = 1.0f;
        ForegroundColor = Color.White;
        BackgroundColor = Color.Black;
        DisplaySize = ScreenSize.Default;
        //LastLoadFromFolder = AppContext.BaseDirectory;
        LastLoadFromFolder = @"c:\dev\chipate\roms";
    }

    public void LoadOptions()
    { }

    public void SaveOptions()
    { }

}
