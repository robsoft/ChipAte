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
        optionsPanel = new Panel();
        optionsPanel.BindingContext = mainViewModel;
        optionsPanel.Visual.AddToRoot();

        optionsPanel.Width = 620;
        optionsPanel.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        optionsPanel.Height = 460;
        optionsPanel.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        optionsPanel.Anchor(Anchor.Center);

        var background = new ColoredRectangleRuntime();
        optionsPanel.AddChild(background);
        background.Color = Color.DarkBlue;
        background.Dock(Gum.Wireframe.Dock.Fill);

        var topPanel = new StackPanel();
        optionsPanel.AddChild(topPanel);
        topPanel.Visual.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        topPanel.Visual.StackSpacing = 8;
        topPanel.Dock(Dock.FillHorizontally);

        var titleLabel = new Label();
        topPanel.AddChild(titleLabel);
        titleLabel.Dock(Dock.Top);
        titleLabel.Text = "Emulation Options";
        titleLabel.Y = 10;

        var bottomPanel = new StackPanel();
        optionsPanel.AddChild(bottomPanel);
        bottomPanel.Visual.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        bottomPanel.Visual.StackSpacing = 8;
        bottomPanel.Dock(Dock.Bottom);
        bottomPanel.Width = -250;
        bottomPanel.Y = -20;

        var returnButton = new Button();
        bottomPanel.AddChild(returnButton);
        returnButton.Text = "Return";
        returnButton.Click += (s, e) => SaveOptionsAndClose();
        returnButton.Dock(Dock.Bottom);

        var subPanel = new StackPanel();
        optionsPanel.AddChild(subPanel);
        subPanel.Visual.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        subPanel.Visual.StackSpacing = 8;
        subPanel.Anchor(Anchor.Center);
        subPanel.Width = 200;

        var displayLabel = new Label();
        subPanel.AddChild(displayLabel);
        displayLabel.Text = "Display";
        displayLabel.Dock(Dock.Top);
        
        var smallDisplay = new RadioButton();
        subPanel.AddChild(smallDisplay);
        smallDisplay.Text = "Small (800 x 600)";
        smallDisplay.Dock(Dock.Top);
        if (optionsViewModel.Options.DisplaySize == Options.ScreenSize.Small)
        {
            smallDisplay.IsChecked = true;
        }
        smallDisplay.Checked += (s, e) =>
        {
            optionsViewModel.Options.DisplaySize = Options.ScreenSize.Small;
            ApplyDisplaySize();
        };

        var defaultDisplay = new RadioButton();
        subPanel.AddChild(defaultDisplay);
        defaultDisplay.Text = "Default (1024 x 768)";
        defaultDisplay.Dock(Dock.Top);
        if (optionsViewModel.Options.DisplaySize == Options.ScreenSize.Default)
        {
            defaultDisplay.IsChecked = true;
        }
        defaultDisplay.Checked += (s, e) =>
        {
            optionsViewModel.Options.DisplaySize = Options.ScreenSize.Default;
            ApplyDisplaySize();
        };

        var largeDisplay = new RadioButton();
        subPanel.AddChild(largeDisplay);
        largeDisplay.Text = "Large (1280 x 1024)";
        largeDisplay.Dock(Dock.Top);
        if (optionsViewModel.Options.DisplaySize == Options.ScreenSize.Large)
        {
            largeDisplay.IsChecked = true;
        }
        largeDisplay.Checked += (s, e) =>
        {
            optionsViewModel.Options.DisplaySize = Options.ScreenSize.Large;
            ApplyDisplaySize();
        };

        var fullScreenDisplay = new RadioButton();
        subPanel.AddChild(fullScreenDisplay);
        fullScreenDisplay.Text = "Full Screen";
        fullScreenDisplay.Dock(Dock.Top);
        if (optionsViewModel.Options.DisplaySize == Options.ScreenSize.FullScreen)
        {
            fullScreenDisplay.IsChecked = true;
        }
        fullScreenDisplay.Checked += (s, e) =>
        {
            optionsViewModel.Options.DisplaySize = Options.ScreenSize.FullScreen;
            ApplyDisplaySize();
        };
    }

    private void SaveOptionsAndClose()
    {
        //TODO: need this to copy into options class and 'save'
        SetScene(Scene.Main);
    }


    private void ApplyDisplaySize()
    {
        var (width, height) = DisplaySizes.GetDimensions(optionsViewModel.Options.DisplaySize);
        if (width == 0 || height == 0)
        {
            // full screen
            _graphics.IsFullScreen = true;
            _graphics.ApplyChanges();
        }
        else
        {
            _graphics.IsFullScreen = false;
            _graphics.PreferredBackBufferWidth = width;
            _graphics.PreferredBackBufferHeight = height;
            _graphics.ApplyChanges();
        }
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
        fileSelectPanel = new Panel();
        fileSelectPanel.BindingContext = fileSelectViewModel;
        fileSelectPanel.Visual.AddToRoot();

        fileSelectPanel.Width = 620;
        fileSelectPanel.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        fileSelectPanel.Height = 460;
        fileSelectPanel.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        fileSelectPanel.Anchor(Anchor.Center);

        var background = new ColoredRectangleRuntime();
        fileSelectPanel.AddChild(background);
        background.Color = Color.DarkBlue;
        background.Dock(Gum.Wireframe.Dock.Fill);

        var topPanel = new StackPanel();
        fileSelectPanel.AddChild(topPanel);
        topPanel.Visual.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        topPanel.Visual.StackSpacing = 8;
        topPanel.Dock(Dock.FillHorizontally);

        var titleLabel = new Label();
        topPanel.AddChild(titleLabel);
        titleLabel.Dock(Dock.Top);
        titleLabel.Text = "Select ROM file";
        titleLabel.Y = 10;

        var bottomPanel = new StackPanel();
        fileSelectPanel.AddChild(bottomPanel);
        bottomPanel.Visual.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        bottomPanel.Visual.StackSpacing = 40;
        bottomPanel.Dock(Dock.Bottom);
        bottomPanel.Width = -180;
        bottomPanel.Y = -20;

        var okButton = new Button();
        bottomPanel.AddChild(okButton);
        okButton.Text = "Select";
        okButton.Width = 200;
        okButton.Click += (s, e) =>
        {
            if (fileSelectViewModel.ConfirmFile(fileSelectViewModel.SelectedBrowserEntry))
            {
                if (chip8.LoadRom(fileSelectViewModel.SelectedFilePath))
                {
                    mainViewModel.FilePath = fileSelectViewModel.SelectedFilePath;
                    RunConsole();
                }
            }
        };

        var returnButton = new Button();
        bottomPanel.AddChild(returnButton);
        returnButton.Text = "Return";
        returnButton.Width = 200;
        returnButton.Click += (s, e) => SetScene(Scene.Main);

        var subPanel = new StackPanel();
        fileSelectPanel.AddChild(subPanel);
        subPanel.Visual.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        subPanel.Visual.StackSpacing = 8;
        subPanel.Anchor(Anchor.Center);
        subPanel.Width = 500;

        var drivesComboBox = new ComboBox();
        subPanel.AddChild(drivesComboBox);
        drivesComboBox.Dock(Dock.Top);
        drivesComboBox.Visual.Width = 10;

        var filesListBox = new ListBox();
        subPanel.AddChild(filesListBox);
        filesListBox.Dock(Dock.Top);
        filesListBox.Visual.Width = 10;
        filesListBox.ItemClicked += HandleItemClicked;

        drivesComboBox.SetBinding(nameof(drivesComboBox.Items), nameof(fileSelectViewModel.AvailablePlaces));
        drivesComboBox.SetBinding(nameof(drivesComboBox.SelectedObject), nameof(fileSelectViewModel.SelectedPlace));
        drivesComboBox.FrameworkElementTemplate =
            new Gum.Forms.FrameworkElementTemplate(typeof(PlaceDisplay));
        drivesComboBox.DisplayMemberPath = nameof(PlaceItem.DisplayName);
         
        drivesComboBox.ListBox.Height = filesListBox.Visual.Height + 5; //spacing fudge factor

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
        mainPanel = new Panel();
        mainPanel.BindingContext = mainViewModel;
        mainPanel.Visual.AddToRoot();

        mainPanel.Width = 620;
        mainPanel.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        mainPanel.Height = 460;
        mainPanel.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;      
        mainPanel.Anchor(Anchor.Center);

        var background = new ColoredRectangleRuntime();
        mainPanel.AddChild(background);
        background.Color = Color.DarkBlue;
        background.Dock(Gum.Wireframe.Dock.Fill);

        var topPanel = new StackPanel();
        mainPanel.AddChild(topPanel);
        topPanel.Visual.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        topPanel.Visual.StackSpacing = 8;
        topPanel.Dock(Dock.FillHorizontally);

        var titleLabel = new Label();
        topPanel.AddChild(titleLabel);
        titleLabel.Dock(Dock.Top);
        titleLabel.Text = APP_NAME;
        titleLabel.Y = 10;

        var versionLabel = new Label();
        topPanel.AddChild(versionLabel);
        versionLabel.Dock(Dock.Top);
        versionLabel.Text = APP_VERSION;
        versionLabel.Y = -5;

        var bottomPanel = new StackPanel();
        mainPanel.AddChild(bottomPanel);
        bottomPanel.Visual.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        bottomPanel.Visual.StackSpacing = 8;
        bottomPanel.Dock(Dock.Bottom);
        bottomPanel.Y = -20;

        var keysLabel = new Label();
        bottomPanel.AddChild(keysLabel);
        keysLabel.Text = "(in emulation: 1-4,Q-R,A-F,Z-V    Esc - Menu    F8 - Reset)";
        keysLabel.Dock(Dock.Bottom);

        var subPanel = new StackPanel();
        mainPanel.AddChild(subPanel);
        subPanel.Visual.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        subPanel.Visual.StackSpacing = 8;
        subPanel.Anchor(Anchor.Center);
        subPanel.Width = 200;

        var currentRomLabel = new Label();
        subPanel.AddChild(currentRomLabel);
        currentRomLabel.SetBinding(nameof(currentRomLabel.Text), nameof(mainViewModel.FilePath));
        currentRomLabel.Dock(Dock.Top);
        currentRomLabel.Y = -15;
        currentRomLabel.Width = 620;

        var startButton = new Button();
        subPanel.AddChild(startButton);
        startButton.Text = "Start/Resume";
        startButton.Click += (s, e) => RunConsole();
        startButton.Dock(Dock.Top);
        startButton.Y = 10;

        var selectFileButton = new Button();
        subPanel.AddChild(selectFileButton);
        selectFileButton.Text = "Load ROM";
        selectFileButton.Click += (s, e) => SetScene(Scene.FileSelect);
        selectFileButton.Dock(Dock.Top);

        var optionsButton = new Button();
        subPanel.AddChild(optionsButton);
        optionsButton.Text = "Options";
        optionsButton.Click += (s, e) => SetScene(Scene.Options);
        optionsButton.Dock(Dock.Top);

        var quitButton = new Button();
        subPanel.AddChild(quitButton);
        quitButton.Text = "Quit";
        quitButton.Click += (s, e) =>
        {
            StopBeep();
            GumUI.Root.Children.Clear();
            Exit();
        };

        quitButton.Dock(Dock.Top);

    }




}
