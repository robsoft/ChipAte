using ChipAte.Console;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace ChipAte;

public class Chip8Wrapper : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Texture2D _pixelTex;
    private SoundEffect _beepEffect;
    private SoundEffectInstance _beepInstance;
    private Rectangle _playfieldRect;
   
    private Chip8 chip8;

    private int scale = 16; // TODO: make adjustable
    private int offset;

    private Color BackgroundColor = Color.Black; // TODO: make adjustable
    private Color PixelColor = Color.White; // TODO: make adjustable
    private Color BorderColor = Color.CornflowerBlue; // TODO: make adjustable

    private bool beepPlaying = false;
    private const int SampleRate = 44100;
    private const int BeepFrequency = 440; // A4 — classic, pleasant

    private const int CpuHz = 700;   // reasonable default
    private const int TimerHz = 60;
    private double _cpuAccumulator = 0.0;


    public Chip8Wrapper()
    {

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

        // SAMPLE ROMS
        //var file = "ibm logo.ch8";
        //var file = "Keypad Test [Hap, 2006].ch8";
        //var file = "Delay Timer Test [Matthew Mikolay, 2010].ch8";
        var file = "Brix [Andreas Gustafsson, 1990].ch8";
        //var file = "Pong (alt).ch8";
        //var file = "Random Number Test [Matthew Mikolay, 2010].ch8";
        //var file = "Breakout [Carmelo Cortez, 1979].ch8";
        //var file = "Zero Demo [zeroZshadow, 2007].ch8";
        //var file = "Trip8 Demo (2008) [Revival Studios].ch8";
        //var file = "Tetris [Fran Dachille, 1991].ch8";
        //var file = "Space Invaders [David Winter].ch8";
        //var file = "Particle Demo [zeroZshadow, 2008].ch8";
        //var file = "Maze (alt) [David Winter, 199x].ch8";
        //var file = "Clock Program [Bill Fisher, 1981].ch8";
        //var file = "Chip8 emulator Logo [Garstyciuks].ch8";
        //var file = "Maze [David Winter, 199x].ch8";
        //var file = "Sierpinski [Sergey Naydenov, 2010].ch8";
        //var file = "Stars [Sergey Naydenov, 2010].ch8";
        file = "c:\\dev\\chipate\\roms\\" + file;

        // TEST SUITE ROMS
        //var file = "1-chip8-logo.ch8";
        //var file = "2-ibm-logo.ch8";
        //var file = "3-corax+.ch8";
        //var file = "4-flags.ch8";
        //var file = "5-quirks.ch8";
        //var file = "6-keypad.ch8";
        //var file = "7-beep.ch8";
        //var file = "8-scrolling.ch8"; - xo/super only, ignore for Chip-8 only
        //var file = "oob_test_7.ch8"; // the oob - out of bounds - rom test - brutal!
        //file = "c:\\dev\\chipate\\roms\\testsuite\\" + file;


        //TODO: command args and a ui file selector!

        if (!chip8.LoadRom(file))
        {
            throw new System.Exception($"Failed to load ROM {file}");
        }

    }

    protected override void Initialize()
    {
        base.Initialize();
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
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // setup our pixel texture
        _pixelTex = new Texture2D(GraphicsDevice, 1, 1);
        _pixelTex.SetData(new[] { PixelColor });

        // prepare beep sound
        CreateBeepSound();
    }

    protected override void Update(GameTime gameTime)
    {
        // how much real time has passed since last frame
        double elapsedSeconds = gameTime.ElapsedGameTime.TotalSeconds;

        // accumulate the CPU time
        _cpuAccumulator += elapsedSeconds * CpuHz;

        // check keys (just once per frame!)
        chip8.SaveKeypad();
        HandleKeypad(chip8.Keypad);

        // now execute as many opcodes as need to fit in a frame
        while (_cpuAccumulator >= 1.0)
        {
            chip8.Fetch();
            chip8.Decode();
            chip8.Execute();
            _cpuAccumulator -= 1.0;

            // this is the 'Display Wait' quirk - if we've just performed a draw, that's it for this frame, no more opcodes.
            if (chip8.DidDXYN) break;
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


        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(BorderColor);

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        // PointClamp avoids smoothing if we ever scale textures.
        _spriteBatch.Draw(_pixelTex, _playfieldRect, BackgroundColor);

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
                _spriteBatch.Draw(_pixelTex, dest, PixelColor);
            }
        }

        _spriteBatch.End();

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
        if (_beepInstance.State != SoundState.Playing)
            _beepInstance.Play();
    }

    // stop the beep sound effect, if necessary
    private void StopBeep()
    {
        if (_beepInstance.State == SoundState.Playing)
            _beepInstance.Stop();
        beepPlaying = false;
    }
}

