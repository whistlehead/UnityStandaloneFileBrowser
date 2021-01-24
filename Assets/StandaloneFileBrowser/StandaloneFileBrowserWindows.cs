#if UNITY_STANDALONE_WIN

using System;
using System.Collections.Generic;
using System.IO;
//using System.Windows.Forms;
using System.Runtime.InteropServices;
//using Ookii.Dialogs;

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

                var rc = new List<string>();
                if (LibWrap.GetOpenFileName(ofn))
                {
                    var filenames = PtrToStringArrayUni(ofn.file);
                    if (filenames.Count == 1)
                    {
                        rc = filenames;
                    }
                    else if (filenames.Count > 1)
                    {
                        var root = filenames[0];
                        for (int i = 1; i < filenames.Count; ++i)
                        {
                            rc.Add(Path.Combine(root, filenames[i]));
                        }
                    }
                }
                return rc.ToArray();
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
                ofn.flags =
                    OpenFileName.OFN_NOCHANGEDIR |
                    OpenFileName.OFN_EXPLORER |
                    OpenFileName.OFN_PATHMUSTEXIST |
                    OpenFileName.OFN_FILEMUSTEXIST;

                string rc = null;
                if (LibWrap.GetSaveFileName(ofn))
                {
                    var filenames = PtrToStringArrayUni(ofn.file);
                    if (filenames.Count == 1) rc = filenames[0];
                }
                return rc;
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
            //filterString = filterString.Remove(filterString.Length - 1);
            return filterString;
        }

        private static string GetDirectoryPath(string directory)
        {
            if (string.IsNullOrEmpty(directory)) return null;
            var directoryPath = Path.GetFullPath(directory);
            if (!directoryPath.EndsWith("\\"))
            {
                directoryPath += "\\";
            }
            if (Path.GetPathRoot(directoryPath) == directoryPath)
            {
                return directory;
            }
            return Path.GetDirectoryName(directoryPath) + Path.DirectorySeparatorChar;
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