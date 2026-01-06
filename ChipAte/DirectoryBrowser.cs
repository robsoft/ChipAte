using Gum.Forms.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChipAte;

public class BrowseFileListBoxItem : ListBoxItem
{

    public override void UpdateToObject(object o)
    {
        if (o is BrowserEntry entry)
        {
            if (entry != null)
            {
                base.UpdateToObject(entry.EntryName);
            }
            else
            {
                base.UpdateToObject("null");
            }
        }
        else
        {
            base.UpdateToObject("bad type");
        }
    }
}

public static class DirectoryBrowser
{
    // return a list of folders and files from the given path
    public static IReadOnlyList<BrowserEntry> ListDirectory(string path, string filePattern = "*.ch8")
    {
        var results = new List<BrowserEntry>();
        if (path.Trim()==string.Empty)
        {
            System.Diagnostics.Debug.WriteLine("DirectoryBrowser: Empty path provided.");
            return results;
        }
        // normalize path
        var fullPath = System.IO.Path.GetFullPath(path);

        // can we add a parent entry
        var parent = System.IO.Directory.GetParent(fullPath);
        if (parent != null)
        {
            results.Add(new BrowserEntry(EntryKind.Parent, "[ .. ]", parent.FullName));
        }

        try
        {
            // folders
            foreach (var dir in System.IO.Directory.EnumerateDirectories(fullPath).OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
            {
                var name = System.IO.Path.GetFileName(dir.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar));
                var dirinfo = new System.IO.DirectoryInfo(dir);
                if (name.StartsWith(".") || 
                    dirinfo.Attributes.HasFlag(System.IO.FileAttributes.Hidden) || 
                    dirinfo.Attributes.HasFlag(System.IO.FileAttributes.System))
                {
                    // skip hidden/system folders
                    continue;
                }
                results.Add(new BrowserEntry(EntryKind.Folder, $"[ {name} ]", dir));
            }

            // files (using the filepattern)
            foreach (var file in System.IO.Directory.EnumerateFiles(fullPath, filePattern)
                                          .OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
            {
                var name = System.IO.Path.GetFileName(file);
                results.Add(new BrowserEntry(EntryKind.File, name, file));
            }
        }

        catch (UnauthorizedAccessException)
        {
            // skip this - but interesting to log what we're hitting?
            System.Diagnostics.Debug.WriteLine($"DirectoryBrowser: Access denied to path '{fullPath}'.");
        }
        catch (System.IO.DirectoryNotFoundException)
        {
            // path no longer exists; return parent only (or empty)
        }
        catch (System.IO.IOException)
        {
            // network share hiccups etc.
        }

        return results;
    }
}
