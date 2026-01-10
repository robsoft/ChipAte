using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ChipAte;


public partial class FileSelectViewModel : ViewModel
{
    private bool loading = false;
    private Options _options;

    public void BrowseFiles(string folder)
    {
        var browse = DirectoryBrowser.ListDirectory(folder, "*.ch8");

        AvailableFiles.Clear();
        foreach (var file in browse)
        {
            AvailableFiles.Add(file);
        }

        SelectedBrowserEntry = null;
    }

    public ObservableCollection<PlaceItem> AvailablePlaces
    {
        get; private set;
    } = new ObservableCollection<PlaceItem>();

    public PlaceItem? SelectedPlace
    {
        get => Get<PlaceItem?>();
        set
        {
            if (value == SelectedPlace) return;
            Set(value);
            if (loading || value == null) return; // dont trigger an update for this
            BrowseFiles(value.Path);
        }
    }

    public ObservableCollection<BrowserEntry> AvailableFiles
    {
        get; private set;
    } = new ObservableCollection<BrowserEntry>();


    public BrowserEntry? SelectedBrowserEntry
    {
        get => Get<BrowserEntry?>();
        set
        {
            if (value == SelectedBrowserEntry) return;

            Set(value);
            if (value != null && value.Kind == EntryKind.File)
            {
                SelectedFilePath = value.FullPath;
            }
            else
            {
                SelectedFilePath = string.Empty;
            }
        }
    }


    public string SelectedFilePath
    {
        get => Get<string>();
        set => Set(value);
    }

    private void AddSelectedPlace(BrowserEntry entry)
    {
        if (entry == null || string.IsNullOrEmpty(entry.FullPath)) return;

        loading = true; // indicate we don't want to trigger any updates
        
        var existingPlace = AvailablePlaces.FirstOrDefault(item => item.Path == entry.FullPath);
        if (existingPlace == null)
        {
            var newPlace = new PlaceItem(entry.FullPath, entry.FullPath); // want to see the full path in the place for these
            AvailablePlaces.Add(newPlace);
            SelectedPlace = newPlace;
        }
        else
        {
            SelectedPlace = existingPlace;
        }
        loading = false;
    }

    public bool ConfirmFile(BrowserEntry entry)
    {
        if (entry == null || string.IsNullOrEmpty(entry.FullPath)) return false;

        if (entry.Kind == EntryKind.File)
        {
            SelectedFilePath = entry.FullPath;
            return true;
        }
        else if (entry.Kind == EntryKind.Folder || entry.Kind == EntryKind.Parent)
        {
            System.Diagnostics.Debug.WriteLine($"Browsing into folder: {entry.FullPath}");
            AddSelectedPlace(entry);
            BrowseFiles(entry.FullPath);
        }
        return false;
    }

    public FileSelectViewModel(Options options)
    {
        _options = options;

        SelectedFilePath = string.Empty;

        var places = PlacesProvider.GetPlaces();
        PlacesProvider.InsertIfValid(places, "Application Folder", AppContext.BaseDirectory);
        PlacesProvider.InsertIfValid(places, options.LastLoadFromFolder, options.LastLoadFromFolder);

        AvailablePlaces = new ObservableCollection<PlaceItem>(places);
        SelectedPlace = AvailablePlaces.FirstOrDefault();
    }
}
