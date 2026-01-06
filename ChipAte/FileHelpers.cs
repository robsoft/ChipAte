using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChipAte;

public enum EntryKind { Parent, Folder, File }

public sealed record PlaceItem(string DisplayName, string Path);

public sealed record BrowserEntry(EntryKind Kind, string EntryName, string FullPath);
