using ChipAte;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChipAte;

public class PlaceDisplay : ListBoxItem
{
    public PlaceDisplay() : base()
    {
        var text = ((ListBoxItemVisual)this.Visual).TextInstance;
        text.Anchor(Gum.Wireframe.Anchor.TopLeft);
    }

    public override void UpdateToObject(object o)
    {
        var placeItem = o as PlaceItem;
        if (placeItem != null)
        {
            coreText.RawText = placeItem.DisplayName;
        }
    }
}

public static class PlacesProvider
{
    // return a list of 'places' - special folders and drives
    public static List<PlaceItem> GetPlaces()
    {
        var places = new List<PlaceItem>();
        // a few “special folders”
        AddIfValid(places, "Home", GetHomePath());
        AddIfValid(places, "Desktop", Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
        AddIfValid(places, "Downloads", GetDownloadsPathBestEffort());
        AddIfValid(places, "Documents", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        AddIfValid(places, "AppData Local", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));

        // drives / mounted volumes
        foreach (var d in DriveInfo.GetDrives().OrderBy(d => d.Name))
        {
            try
            {
                // on some platforms IsReady can throw or be slow; guard with try/catch.
                if (!d.IsReady) continue;

                string label = string.IsNullOrWhiteSpace(d.VolumeLabel)
                    ? d.Name
                    : $"{d.Name} ({d.VolumeLabel})";

                places.Add(new PlaceItem(label, d.RootDirectory.FullName));
            }
            catch
            {
                // ignore drives we can’t query
            }
        }

        return places;
    }

    private static bool CanAdd(List<PlaceItem> list, string name, string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;

        // normalize and ensure it exists (avoids dead entries on some platforms)
        try
        {
            path = Path.GetFullPath(path);
        }
        catch
        {
            return false;
        }

        if (Directory.Exists(path))
        {
            // only add if not already in the list
            if (list.Any(p => string.Equals(p.Path, path, StringComparison.OrdinalIgnoreCase)))
                return false;
        }
        return true;
    }
    
    
    public static void AddIfValid(List<PlaceItem> list, string name, string? path)
    {
        if (!string.IsNullOrWhiteSpace(path))
        {
            if (CanAdd(list, name, path))
            {
                list.Add(new PlaceItem(name, path));
            }
        }
    }


    public static void InsertIfValid(List<PlaceItem> list, string name, string? path)
    {
        if (!string.IsNullOrWhiteSpace(path))
        {
            if (CanAdd(list, name, path))
            {
                list.Insert(0, new PlaceItem(name, path));
            }
        }
    }


    private static string GetHomePath()
        => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    // best-effort: there isn’t a perfect cross-platform “Downloads” API everywhere.
    private static string? GetDownloadsPathBestEffort()
    {
        // Windows usually supports this; on Linux/macOS it often returns empty.
        var sf = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrWhiteSpace(sf)) return null;

        // Try typical “Downloads” under home.
        var candidate = Path.Combine(sf, "Downloads");
        return Directory.Exists(candidate) ? candidate : null;
    }
}
