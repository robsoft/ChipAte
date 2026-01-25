using Microsoft.Extensions.Logging;
using ChipAte.Console;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using MonoGameGum;
using Gum.Forms.Controls;
using MonoGameGum.GueDeriving;
using Gum.Forms.DefaultVisuals.V3;

using System;
using System.Diagnostics;
using Gum.Forms;

namespace ChipAte;

public partial class Chip8Wrapper : Game
{

    public const string APP_NAME = "ChipAte";
    public const string APP_VERSION = "v 0.02";

    private enum Scene {
        None,
        Game,
        Main,
        Options,
        FileSelect
    };
    private readonly ILogger<Chip8Wrapper> _log;

    private GraphicsDeviceManager _graphics;
    private SpriteBatch? _spriteBatch;
    private Texture2D? _pixelTex;
    private SoundEffect? _beepEffect;
    private SoundEffectInstance? _beepInstance;
    private Rectangle _playfieldRect;

    GumService GumUI => GumService.Default;
    Panel? mainPanel;
    Panel? optionsPanel;
    Panel? fileSelectPanel;
    private Scene scene = Scene.None;

    private Chip8 chip8;
    private Chip8Debugger debugger;
    private Options options;
    private FileSelectViewModel fileSelectViewModel;
    private OptionsViewModel optionsViewModel;
    private MainViewModel mainViewModel;

    private int scale = 16; // TODO: make adjustable
    private int offset;

    private Color BackgroundColor = Color.Black; // TODO: make adjustable
    private Color PixelColor = Color.White; // TODO: make adjustable
    private Color BorderColor = Color.CornflowerBlue; // TODO: make adjustable

    private bool beepPlaying = false;
    private const int SampleRate = 44100;
    private const int BeepFrequency = 440; // A4 — classic, pleasant

    private const int CpuHz = 700;   // reasonable default - do we want to make this an option?
    private const int TimerHz = 60;
    private double _cpuAccumulator = 0.0;


    public Chip8Wrapper(ILogger<Chip8Wrapper> log)
    {
        _log = log;
        _log.LogInformation("Starting ChipAte...");

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        IsFixedTimeStep = true;
        TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 60.0);

        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = 64 * scale;
        _graphics.PreferredBackBufferHeight = 32 * scale;
        _graphics.SynchronizeWithVerticalRetrace = true;
        _graphics.ApplyChanges();

        chip8 = new Chip8();
        debugger = new Chip8Debugger(chip8);
        //TODO: load this from disk (fallback to sensible defaults)
        options = new Options();

        mainViewModel = new MainViewModel(chip8, debugger, options);
        optionsViewModel = new OptionsViewModel(options);
        fileSelectViewModel = new FileSelectViewModel(options);
    }

    protected override void OnExiting(object sender, ExitingEventArgs args)
    {
        _log.LogInformation("ChipAte exiting");
        base.OnExiting(sender, args);
    }
    protected override void Initialize()
    {
        // prepare beep sound
        CreateBeepSound();

        int borderPixels = 1; // in CHIP-8 pixels
        offset = borderPixels * scale;
        _playfieldRect = new Rectangle(
            offset,
            offset,
            chip8.DisplayWidth * scale,
            chip8.DisplayHeight * scale);

        _graphics.PreferredBackBufferWidth = (chip8.DisplayWidth + borderPixels * 2) * scale;
        _graphics.PreferredBackBufferHeight = (chip8.DisplayHeight + borderPixels * 2) * scale;
        _graphics.ApplyChanges();

        GumUI.Initialize(this, DefaultVisualsVersion.V3);

        SetupMainPanel(mainViewModel);
        SetupOptionsPanel(optionsViewModel);
        SetupFileSelectPanel(fileSelectViewModel);

        SetScene(Scene.Main);

        base.Initialize();
    }

    private void SetScene(Scene newScene)
    {
        if (scene == newScene) return;

        StopBeep();
        GumUI.Root.Children.Clear();

        scene = newScene;
        switch (scene)
        {
            case Scene.Main:
                mainPanel?.Visual.AddToRoot();
                break;

            case Scene.Options:
                optionsPanel?.Visual.AddToRoot();
                break;

            case Scene.FileSelect:
                fileSelectPanel?.Visual.AddToRoot();
                break;

            case Scene.Game:
                break;
        }
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // setup our pixel texture
        _pixelTex = new Texture2D(GraphicsDevice, 1, 1);
        _pixelTex.SetData(new[] { PixelColor });
    }

    protected override void Update(GameTime gameTime)
    {

        if (scene == Scene.Game)
        {
            HandleGameUpdate(gameTime);
        }
        else
        {
            HandlePanelUpdate(gameTime);
        }

        base.Update(gameTime);
    }

    private void HandlePanelUpdate(GameTime gameTime)
    {
        GumUI.Update(gameTime);
        // TODO: want to pick up on Esc here and resume if something is loaded,
        // but need to sort key debouncing first
    }

    private void HandleGameUpdate(GameTime gameTime)
    {

        // UI/Wrapper controller Update code

        //TODO: need to debounce these keys I think

        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
        {
            SetScene(Scene.Main);
            HandlePanelUpdate(gameTime);
            return;
        }

        if (Keyboard.GetState().IsKeyDown(Keys.F8))
        {
            if (chip8.ROMLoaded)
            {
                chip8.LoadRom(chip8.ROMPath);
                // will handle reset, we can't just reset the PC as Chip-8 code can be self-modifying etc
                // LoadRom implicitly does a reset
                return;
            }
        }

        /*
        //TODO: debugger!
        if (Keyboard.GetState().IsKeyDown(Keys.F12))
        {
            debugger.Enabled = !debugger.Enabled;
        }
        */


        // actual CHIP-8 related Update code

        // how much real time has passed since last frame
        double elapsedSeconds = gameTime.ElapsedGameTime.TotalSeconds;

        // accumulate the CPU time
        _cpuAccumulator += elapsedSeconds * CpuHz;

        // check Chip-8 keys (just once per frame!)
        chip8.SaveKeypad();
        HandleKeypad(chip8.Keypad);

        // now execute as many opcodes as need to fit in a frame
        while (_cpuAccumulator >= 1.0)
        {
            chip8.Fetch();
            chip8.Decode();
            chip8.Execute();
            _cpuAccumulator -= 1.0;

            // this is the 'Display Wait' quirk - if we've just performed a draw,
            // that's it for this frame, no more opcodes.
            if (chip8.DidDXYN) break;
            if (debugger.Enabled && debugger.SingleStepping) break;
        }

        // timers tick at 60 Hz (exactly once per frame)
        if (chip8.TimerDelay > 0)
            chip8.TimerDelay--;

        if (chip8.TimerSound > 0)
            chip8.TimerSound--;

        // handle sound
        if (chip8.TimerSound > 0)
        {
            if (!beepPlaying) StartBeep();
        }
        else
        {
            StopBeep();
        }

    }


    private void HandlePanelDraw(GameTime gameTime)
    {
        GumUI.Draw();
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(BorderColor);

        if (scene == Scene.Game)
        {
            HandleGameDraw(gameTime);
        }
        else
        { 
            HandlePanelDraw(gameTime);
        }
        base.Draw(gameTime);
    }

    private void HandleGameDraw(GameTime gameTime)
    { 
        _spriteBatch?.Begin(samplerState: SamplerState.PointClamp);
        // PointClamp avoids smoothing if we ever scale textures.
        _spriteBatch?.Draw(_pixelTex, _playfieldRect, BackgroundColor);

        // skip through our screen buffer, and draw pixels
        // we don't need to consider 'undrawing' because MG clears the screen each frame
        for (int y = 0; y < chip8.DisplayHeight; y++)
        {
            int rowStart = y * chip8.DisplayWidth;

            for (int x = 0; x < chip8.DisplayWidth; x++)
            {
                if (chip8.Screen[rowStart + x] == 0)
                    continue;

                // destination rectangle in backbuffer pixels:
                var dest = new Rectangle(
                    offset + x * scale,
                    offset + y * scale,
                    scale,
                    scale);
                _spriteBatch?.Draw(_pixelTex, dest, PixelColor);
            }
        }

        _spriteBatch?.End();

        base.Draw(gameTime);
    }

    // create a beeper sound effect programmatically - shamelessly lifted from ChatGPT...
    private void CreateBeepSound()
    {
        int durationSeconds = 1;
        int sampleCount = SampleRate * durationSeconds;

        short[] samples = new short[sampleCount];

        double samplesPerCycle = SampleRate / (double)BeepFrequency;
        short amplitude = 8000; // Safe volume (out of 32767)

        for (int i = 0; i < sampleCount; i++)
        {
            samples[i] = (i % samplesPerCycle < samplesPerCycle / 2)
                ? amplitude
                : (short)-amplitude;
        }

        // Convert short[] → byte[]
        byte[] buffer = new byte[samples.Length * sizeof(short)];
        Buffer.BlockCopy(samples, 0, buffer, 0, buffer.Length);

        _beepEffect = new SoundEffect(buffer, SampleRate, AudioChannels.Mono);
        _beepInstance = _beepEffect.CreateInstance();
        _beepInstance.IsLooped = true;
    }

    // map keyboard state to Chip8 keypad - using the layout
    // from https://en.wikipedia.org/wiki/Chip-8#Keypad
    private void HandleKeypad(bool[] keypad)
    {
        KeyboardState state = Keyboard.GetState();
        // Map keys to Chip8 keypad
        keypad[0x0] = state.IsKeyDown(Keys.X);
        keypad[0x1] = state.IsKeyDown(Keys.D1);
        keypad[0x2] = state.IsKeyDown(Keys.D2);
        keypad[0x3] = state.IsKeyDown(Keys.D3);
        keypad[0x4] = state.IsKeyDown(Keys.Q);
        keypad[0x5] = state.IsKeyDown(Keys.W);
        keypad[0x6] = state.IsKeyDown(Keys.E);
        keypad[0x7] = state.IsKeyDown(Keys.A);
        keypad[0x8] = state.IsKeyDown(Keys.S);
        keypad[0x9] = state.IsKeyDown(Keys.D);
        keypad[0xA] = state.IsKeyDown(Keys.Z);
        keypad[0xB] = state.IsKeyDown(Keys.C);
        keypad[0xC] = state.IsKeyDown(Keys.D4);
        keypad[0xD] = state.IsKeyDown(Keys.R);
        keypad[0xE] = state.IsKeyDown(Keys.F);
        keypad[0xF] = state.IsKeyDown(Keys.V);
    }


    // start the beep sound effect, if necessary
    private void StartBeep()
    {
        beepPlaying = true;
        if (_beepInstance?.State != SoundState.Playing)
            _beepInstance?.Play();
    }

    // stop the beep sound effect, if necessary
    private void StopBeep()
    {
        if (_beepInstance?.State == SoundState.Playing)
            _beepInstance?.Stop();
        beepPlaying = false;
    }
}

