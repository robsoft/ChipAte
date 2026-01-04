using ChipAte.Console;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using MonoGameGum;
using Gum.Forms.Controls;
using MonoGameGum.GueDeriving;

using System;

namespace ChipAte;

public partial class Chip8Wrapper
{
    private void QuitButton_Click(object sender, System.EventArgs e)
    {
        StopBeep();
        Gum.Root.Children.Clear();
        Exit();
    }

    private void ReturnButton_Click(object sender, System.EventArgs e)
    {
        SetScene(Scene.Main);
    }
    private void OptionsButton_Click(object sender, System.EventArgs e)
    {
        SetScene(Scene.Options);
    }
    private void SelectFileButton_Click(object sender, System.EventArgs e)
    {
        SetScene(Scene.FileSelect);
    }
    private void StartButton_Click(object sender, System.EventArgs e)
    {
        if (!chip8.ROMLoaded)
        {
            var file = "Brix [Andreas Gustafsson, 1990].ch8";
            file = "c:\\dev\\chipate\\roms\\" + file;
            if (!chip8.LoadRom(file))
            {
                throw new System.Exception($"Failed to load ROM {file}");
            }
        }
        SetScene(Scene.Game);
    }

    private void SetupOptionsPanel()
    {
        optionsPanel = new StackPanel();

        optionsPanel.Spacing = 3;
        optionsPanel.Anchor(Anchor.Center);

        var titleLabel = new Label();
        titleLabel.Text = "Emulation Options";
        titleLabel.Anchor(Anchor.Top);
        optionsPanel.AddChild(titleLabel);

        var returnButton = new Button();
        returnButton.Text = "Return";
        returnButton.Visual.Width = 200;
        returnButton.Click += ReturnButton_Click;
        returnButton.Anchor(Anchor.Top);
        optionsPanel.AddChild(returnButton);

    }

    private void SetupFileSelectPanel()
    {
        fileSelectPanel = new StackPanel();

        fileSelectPanel.Spacing = 3;
        fileSelectPanel.Anchor(Anchor.Center);

        var titleLabel = new Label();
        titleLabel.Text = "Select ROM file";
        titleLabel.Anchor(Anchor.Top);
        fileSelectPanel.AddChild(titleLabel);

        var returnButton = new Button();
        returnButton.Text = "Cancel";
        returnButton.Visual.Width = 200;
        returnButton.Click += ReturnButton_Click;
        returnButton.Anchor(Anchor.Top);
        fileSelectPanel.AddChild(returnButton);
    }

    private void SetupMainPanel()
    {
        mainPanel = new StackPanel();
        mainPanel.Visual.AddToRoot();

        mainPanel.Spacing = 3;
        mainPanel.Anchor(Anchor.Center);

        var titleLabel = new Label();
        titleLabel.Text = "ChipATE";
        titleLabel.Anchor(Anchor.Top);
        mainPanel.AddChild(titleLabel);

        var versionLabel = new Label();
        versionLabel.Text = "v 0.01";
        versionLabel.Anchor(Anchor.Top);
        mainPanel.AddChild(versionLabel);

        var currentRomLabel = new Label();
        currentRomLabel.Text = "No ROM loaded";
        currentRomLabel.Anchor(Anchor.Top);
        mainPanel.AddChild(currentRomLabel);

        var startButton = new Button();
        startButton.Text = "Start/Resume";
        startButton.Visual.Width = 200;
        startButton.Click += StartButton_Click;
        startButton.Anchor(Anchor.Top);
        mainPanel.AddChild(startButton);

        var selectFileButton = new Button();
        selectFileButton.Text = "Load ROM";
        selectFileButton.Visual.Width = 200;
        selectFileButton.Click += SelectFileButton_Click;
        selectFileButton.Anchor(Anchor.Top);
        mainPanel.AddChild(selectFileButton);

        var optionsButton = new Button();
        optionsButton.Text = "Options";
        optionsButton.Visual.Width = 200;
        optionsButton.Click += OptionsButton_Click;
        optionsButton.Anchor(Anchor.Top);
        mainPanel.AddChild(optionsButton);

        var quitButton = new Button();
        quitButton.Text = "Quit";
        quitButton.Visual.Width = 200;
        quitButton.Click += QuitButton_Click;
        quitButton.Anchor(Anchor.Top);
        mainPanel.AddChild(quitButton);

        var keysLabel = new Label();
        keysLabel.Text = "F1 - Keys    Esc - Menu    F8 - Reset    F12 - Debugger";
        keysLabel.Anchor(Anchor.Top);
        mainPanel.AddChild(keysLabel);

    }




}
