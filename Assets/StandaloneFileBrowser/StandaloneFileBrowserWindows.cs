#if UNITY_STANDALONE_WIN

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace SFB
{
    // For fullscreen support
    // - WindowWrapper class and GetActiveWindow() are required for modal file dialog.
    // - "PlayerSettings/Visible In Background" should be enabled, otherwise when file dialog opened app window minimizes automatically.

    public class StandaloneFileBrowserWindows : IStandaloneFileBrowser
    {
        public string[] OpenFilePanel(string title, string directory, ExtensionFilter[] extensions, bool multiselect)
        {
            var ofn = new OpenFileName();
            try
            {
                ofn.structSize = Marshal.SizeOf(ofn);
                ofn.dlgOwner = LibWrap.GetActiveWindow();
                ofn.filter = GetWindowsFilterFromFileExtensionList(extensions);
                ofn.maxFile = 2048;
                ofn.file = Marshal.StringToHGlobalUni(new string(new char[ofn.maxFile]));
                ofn.maxFileTitle = 64;
                ofn.fileTitle = new string(new char[ofn.maxFileTitle]);
                ofn.initialDir = GetDirectoryPath(directory);
                ofn.title = title;
                ofn.flags =
                    OpenFileName.OFN_NOCHANGEDIR |
                    OpenFileName.OFN_EXPLORER |
                    OpenFileName.OFN_PATHMUSTEXIST |
                    OpenFileName.OFN_FILEMUSTEXIST;
                if (multiselect) ofn.flags |= OpenFileName.OFN_ALLOWMULTISELECT;

                if (LibWrap.GetOpenFileName(ofn))
                {
                    return GetFilenamesUni(ofn.file).ToArray();
                }
                return new string[] { };
            }
            finally
            {
                if (ofn.file != IntPtr.Zero) Marshal.FreeHGlobal(ofn.file);
            }
        }

        public void OpenFilePanelAsync(string title, string directory, ExtensionFilter[] extensions, bool multiselect, Action<string[]> cb)
        {
            cb.Invoke(OpenFilePanel(title, directory, extensions, multiselect));
        }

        public string[] OpenFolderPanel(string title, string directory, bool multiselect)
        {
            throw new NotImplementedException();
        }

        public void OpenFolderPanelAsync(string title, string directory, bool multiselect, Action<string[]> cb)
        {
            cb.Invoke(OpenFolderPanel(title, directory, multiselect));
        }

        public string SaveFilePanel(string title, string directory, string defaultName, ExtensionFilter[] extensions)
        {
            var ofn = new OpenFileName();
            try
            {
                ofn.structSize = Marshal.SizeOf(ofn);
                ofn.dlgOwner = LibWrap.GetActiveWindow();
                ofn.filter = GetWindowsFilterFromFileExtensionList(extensions);
                ofn.maxFile = 2048;
                ofn.file = Marshal.StringToHGlobalUni(new string(new char[ofn.maxFile]));
                ofn.maxFileTitle = 64;
                ofn.fileTitle = new string(new char[ofn.maxFileTitle]);
                ofn.initialDir = GetDirectoryPath(directory);
                ofn.title = title;
                // Alternatively could change after, but that means the overwrite prompt doesn't work
                if (extensions != null && extensions.Length > 0 && extensions[0].Extensions.Length > 0)
                {
                    ofn.defExt = extensions[0].Extensions[0];
                }
                ofn.flags =
                    OpenFileName.OFN_NOCHANGEDIR
                    | OpenFileName.OFN_EXPLORER
                    | OpenFileName.OFN_PATHMUSTEXIST
                    | OpenFileName.OFN_OVERWRITEPROMPT;

                if (LibWrap.GetSaveFileName(ofn))
                {
                    var filenames = GetFilenamesUni(ofn.file);
                    if (filenames.Count == 1 && filenames[0] != null)
                    {
                        var rc = filenames[0];
                        //if (ofn.fileExtension == 0)
                        //{
                        //    if (extensions != null &&
                        //        ofn.filterIndex > 0 &&
                        //        ofn.filterIndex <= extensions.Length)
                        //    {
                        //        var extensionList = extensions[ofn.filterIndex - 1];
                        //        if (extensionList.Extensions.Length > 0)
                        //        {
                        //            rc = Path.ChangeExtension(rc, "." + extensionList.Extensions[0]);
                        //        }
                        //    }
                        //}
                        return rc;
                    }
                }
                return string.Empty;
            }
            finally
            {
                if (ofn.file != IntPtr.Zero) Marshal.FreeHGlobal(ofn.file);
            }
        }

        public void SaveFilePanelAsync(string title, string directory, string defaultName, ExtensionFilter[] extensions, Action<string> cb)
        {
            cb.Invoke(SaveFilePanel(title, directory, defaultName, extensions));
        }

        // .NET Framework FileDialog Filter format
        // "Log files\0*.log\0Batch files\0*.bat\0"
        private static string GetWindowsFilterFromFileExtensionList(ExtensionFilter[] extensions)
        {
            if (extensions == null)
            {
                return "All Files\0*.*\0\0";
            }
            var filterString = "";
            foreach (var filter in extensions)
            {
                filterString += filter.Name + " (";
                foreach (var ext in filter.Extensions)
                {
                    filterString += "*." + ext + ",";
                }
                filterString = filterString.Remove(filterString.Length - 1);
                filterString += ")";
                filterString += "\0";
                foreach (var ext in filter.Extensions)
                {
                    filterString += "*." + ext + ";";
                }
                filterString = filterString.Remove(filterString.Length - 1);
                filterString += "\0";
            }
            filterString += "\0";
            return filterString;
        }

        static readonly string separatorString = Path.DirectorySeparatorChar.ToString();
        static readonly string altSeparatorString = Path.AltDirectorySeparatorChar.ToString();

        private static string GetDirectoryPath(string directory)
        {
            if (string.IsNullOrEmpty(directory)) return null;
            var directoryPath = Path.GetFullPath(directory);
            if (directoryPath.EndsWith(separatorString)) return directoryPath;
            if (directoryPath.EndsWith(altSeparatorString)) return directoryPath;
            if (directoryPath.Contains(separatorString)) return directoryPath + separatorString;
            return directoryPath + altSeparatorString;
        }

        private List<string> GetFilenamesUni(IntPtr file)
        {
            var filenames = PtrToStringArrayUni(file);
            if (filenames.Count <= 1) return filenames;
            var rc = new List<string>();
            var root = filenames[0];
            for (int i = 1; i < filenames.Count; ++i)
            {
                rc.Add(Path.Combine(root, filenames[i]));
            }
            return rc;
        }

        private List<string> PtrToStringArrayUni(IntPtr ptr)
        {
            var rc = new List<string>();
            while (true)
            {
                var item = Marshal.PtrToStringUni(ptr);
                if (!string.IsNullOrEmpty(item))
                {
                    rc.Add(item);
                    ptr = new IntPtr(ptr.ToInt64() + (item.Length + 1) * 2);
                }
                else
                {
                    break;
                }
            }
            return rc;
        }

    }
}

#endif