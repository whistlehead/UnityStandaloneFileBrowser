using System;
using System.Runtime.InteropServices;

namespace SFB
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public class OpenFileName
    {
        public static int OFN_OVERWRITEPROMPT = 0x00000002;
        public static int OFN_NOCHANGEDIR = 0x00000008;
        public static int OFN_ALLOWMULTISELECT = 0x00000200;
        public static int OFN_PATHMUSTEXIST = 0x00000800;
        public static int OFN_FILEMUSTEXIST = 0x00001000;
        public static int OFN_EXPLORER = 0x00080000;

        public int structSize = 0;
        public IntPtr dlgOwner = IntPtr.Zero;
        public IntPtr instance = IntPtr.Zero;

        public string filter = null;
        public string customFilter = null;
        public int maxCustFilter = 0;
        public int filterIndex = 0;

        // To allow for multistrings
        public IntPtr file = IntPtr.Zero;
        public int maxFile = 0;

        public string fileTitle = null;
        public int maxFileTitle = 0;

        public string initialDir = null;

        public string title = null;

        public int flags = 0;
        public short fileOffset = 0;
        public short fileExtension = 0;

        public string defExt = null;

        public IntPtr custData = IntPtr.Zero;
        public IntPtr hook = IntPtr.Zero;

        public string templateName = null;

        public IntPtr reservedPtr = IntPtr.Zero;
        public int reservedInt = 0;
        public int flagsEx = 0;
    }

    public class LibWrap
    {
        [DllImport("Comdlg32.dll", CharSet = CharSet.Unicode)]
        public static extern bool GetOpenFileName([In, Out] OpenFileName ofn);

        [DllImport("Comdlg32.dll", CharSet = CharSet.Unicode)]
        public static extern bool GetSaveFileName([In, Out] OpenFileName ofn);

        [DllImport("user32.dll")]
        public static extern IntPtr GetActiveWindow();

    }
}
