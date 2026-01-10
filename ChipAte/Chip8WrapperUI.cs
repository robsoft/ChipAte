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
        //TODO: need this to copy into options class and 'save'
        returnButton.Click += (s, e) => SetScene(Scene.Main);
        returnButton.Anchor(Anchor.Top);
        optionsPanel.AddChild(returnButton);

    }


    //TODO: push this into the viewmodel
    private void HandleItemClicked(object? sender, EventArgs args)
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
                            RunConsole();
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

        var drivesComboBox = new ComboBox();
        drivesComboBox.Visual.Width = 500;
        drivesComboBox.Anchor(Anchor.Top);
        fileSelectPanel.AddChild(drivesComboBox);

        var filesListBox = new ListBox();
        filesListBox.Visual.Width = 500;
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
                    RunConsole();
                }
            }
        };

        var returnButton = new Button();
        returnButton.Text = "Cancel";
        returnButton.Visual.Width = 150;
        returnButton.Click += (s, e) => SetScene(Scene.Main);

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
        filesListBox.SetBinding(nameof(filesListBox.SelectedObject), nameof(fileSelectViewModel.SelectedBrowserEntry));
        filesListBox.FrameworkElementTemplate =
            new Gum.Forms.FrameworkElementTemplate(typeof(BrowseFileListBoxItem));
    }

    private void RunConsole()
    {
            if (chip8.ROMLoaded)
            {
                SetScene(Scene.Game);
            }
    }

    private void SetupMainPanel(MainViewModel mainViewModel)
    {
        mainPanel = new StackPanel();
        mainPanel.BindingContext = mainViewModel;
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
        //TODO: need to get this back from FileSelect etc
        currentRomLabel.SetBinding(nameof(currentRomLabel.Text), nameof(mainViewModel.FilePath));
        currentRomLabel.Anchor(Anchor.Top);
        mainPanel.AddChild(currentRomLabel);

        var startButton = new Button();
        startButton.Text = "Start/Resume";
        startButton.Visual.Width = 200;
        startButton.Click += (s, e) => RunConsole();

        startButton.Anchor(Anchor.Top);
        mainPanel.AddChild(startButton);

        var selectFileButton = new Button();
        selectFileButton.Text = "Load ROM";
        selectFileButton.Visual.Width = 200;
        selectFileButton.Click += (s, e) => SetScene(Scene.FileSelect);
        selectFileButton.Anchor(Anchor.Top);
        mainPanel.AddChild(selectFileButton);

        var optionsButton = new Button();
        optionsButton.Text = "Options";
        optionsButton.Visual.Width = 200;
        optionsButton.Click += (s, e) => SetScene(Scene.Options);
        optionsButton.Anchor(Anchor.Top);
        mainPanel.AddChild(optionsButton);

        var quitButton = new Button();
        quitButton.Text = "Quit";
        quitButton.Visual.Width = 200;
        quitButton.Click += (s, e) =>
        {
            StopBeep();
            GumUI.Root.Children.Clear();
            Exit();
        };
        quitButton.Anchor(Anchor.Top);
        mainPanel.AddChild(quitButton);

        var keysLabel = new Label();
        keysLabel.Text = "F1 - Keys    Esc - Menu    F8 - Reset    F12 - Debugger";
        keysLabel.Anchor(Anchor.Top);
        mainPanel.AddChild(keysLabel);

    }




}
