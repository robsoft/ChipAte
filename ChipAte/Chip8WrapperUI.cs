using ChipAte.Console;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Mvvm;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Framework.Utilities;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using System;

namespace ChipAte;

public partial class Chip8Wrapper
{
    private void QuitButton_Click(object sender, System.EventArgs e)
    {
        StopBeep();
        GumUI.Root.Children.Clear();
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
    private void NoEvent_Click(object sender, System.EventArgs e)
    {
    }

    private void SetupOptionsPanel(OptionsViewModel optionsViewModel)
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

    void HandleItemClicked(object? sender, EventArgs args)
    {
        if (GumService.Default.Cursor.PrimaryDoubleClick)
        {
            // it was a double-click
            if (fileSelectViewModel != null)
            {
                if (sender is BrowseFileListBoxItem listitem)
                {
                    if (fileSelectViewModel.ConfirmFile((BrowserEntry)listitem.BindingContext))
                    {
                        if (chip8.LoadRom(fileSelectViewModel.SelectedFilePath))
                        {
                            SetScene(Scene.Game);
                        }
                    }
                }
            }
        }
    }

    private void SetupFileSelectPanel(FileSelectViewModel fileSelectViewModel)
    {
        fileSelectPanel = new StackPanel();
        fileSelectPanel.BindingContext = fileSelectViewModel;

        fileSelectPanel.Visual.StackSpacing = 3;
        fileSelectPanel.Anchor(Anchor.Center);
        fileSelectPanel.Visual.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        var titleLabel = new Label();
        titleLabel.Text = "Select ROM file";
        titleLabel.Anchor(Anchor.Top);
        fileSelectPanel.AddChild(titleLabel);

        var testLabel = new Label();
        testLabel.Anchor(Anchor.Top);
        testLabel.SetBinding(nameof(testLabel.Text), nameof(fileSelectViewModel.SelectedFilePath));
        fileSelectPanel.AddChild(testLabel);

        var drivesComboBox = new ComboBox();
        drivesComboBox.Visual.Width = 400;
        drivesComboBox.Anchor(Anchor.Top);
        fileSelectPanel.AddChild(drivesComboBox);

        var filesListBox = new ListBox();
        filesListBox.Visual.Width = 400;
        filesListBox.Anchor(Anchor.Top);
        filesListBox.ItemClicked += HandleItemClicked;
        fileSelectPanel.AddChild(filesListBox);

        var buttonsPanel = new StackPanel();
        buttonsPanel.Orientation = Orientation.Horizontal;
        buttonsPanel.Visual.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        buttonsPanel.Spacing = 2;
        buttonsPanel.Anchor(Anchor.Top);

        var okButton = new Button();
        okButton.Text = "Select";
        okButton.Visual.Width = 150;
        okButton.Click += (s, e) =>
        {
            if (fileSelectViewModel.ConfirmFile(fileSelectViewModel.SelectedBrowserEntry))
            {
                if (chip8.LoadRom(fileSelectViewModel.SelectedFilePath))
                {
                    SetScene(Scene.Game);
                }
            }
        };

        var returnButton = new Button();
        returnButton.Text = "Cancel";
        returnButton.Visual.Width = 150;
        returnButton.Click += ReturnButton_Click;

        fileSelectPanel.AddChild(buttonsPanel);
        buttonsPanel.AddChild(okButton);
        buttonsPanel.AddChild(returnButton);

        drivesComboBox.SetBinding(nameof(drivesComboBox.Items), nameof(fileSelectViewModel.AvailablePlaces));
        drivesComboBox.SetBinding(nameof(drivesComboBox.SelectedObject), nameof(fileSelectViewModel.SelectedPlace));
        drivesComboBox.FrameworkElementTemplate =
            new Gum.Forms.FrameworkElementTemplate(typeof(PlaceDisplay));
        drivesComboBox.DisplayMemberPath = nameof(PlaceItem.DisplayName);

        filesListBox.SetBinding(nameof(filesListBox.Items), nameof(fileSelectViewModel.AvailableFiles));
        // dont do this, we are going to bind this ourselves
        // filesListBox.SetBinding(nameof(filesListBox.SelectedObject), nameof(fileSelectViewModel.SelectedBrowserEntry));
        filesListBox.FrameworkElementTemplate =
            new Gum.Forms.FrameworkElementTemplate(typeof(BrowseFileListBoxItem));
    }

    private void SetupMainPanel()
    {
        mainPanel = new StackPanel();
        mainPanel.Visual.AddToRoot();

        mainPanel.Visual.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        mainPanel.Visual.StackSpacing = 3;
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
