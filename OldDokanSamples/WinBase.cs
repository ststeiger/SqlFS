using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using HANDLE = System.IntPtr;

namespace Windows
{
    #region NTFileAccess
    [Flags]
    public enum NTAccessFlags : long
    {
        FILE_READ_DATA = (0x0001),    // file & pipe
        FILE_LIST_DIRECTORY = (0x0001),    // directory
        FILE_WRITE_DATA = (0x0002),    // file & pipe
        FILE_ADD_FILE = (0x0002),    // directory
        FILE_APPEND_DATA = (0x0004),    // file
        FILE_ADD_SUBDIRECTORY = (0x0004),    // directory
        FILE_CREATE_PIPE_INSTANCE = (0x0004),    // named pipe
        FILE_READ_EA = (0x0008),    // file & directory
        FILE_WRITE_EA = (0x0010),    // file & directory
        FILE_EXECUTE = (0x0020),    // file
        FILE_TRAVERSE = (0x0020),    // directory
        FILE_DELETE_CHILD = (0x0040),    // directory
        FILE_READ_ATTRIBUTES = (0x0080),    // all
        FILE_WRITE_ATTRIBUTES = (0x0100),    // all
        FILE_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED | SYNCHRONIZE | 0x1FF),
        FILE_GENERIC_READ = (STANDARD_RIGHTS_READ |
                                           FILE_READ_DATA |
                                           FILE_READ_ATTRIBUTES |
                                           FILE_READ_EA |
                                           SYNCHRONIZE),
        FILE_GENERIC_WRITE = (STANDARD_RIGHTS_WRITE |
                                           FILE_WRITE_DATA |
                                           FILE_WRITE_ATTRIBUTES |
                                           FILE_WRITE_EA |
                                           FILE_APPEND_DATA |
                                           SYNCHRONIZE),
        FILE_GENERIC_EXECUTE = (STANDARD_RIGHTS_EXECUTE |
                                           FILE_READ_ATTRIBUTES |
                                           FILE_EXECUTE |
                                           SYNCHRONIZE),

        DELETE = (0x00010000L),
        READ_CONTROL = (0x00020000L),
        WRITE_DAC = (0x00040000L),
        WRITE_OWNER = (0x00080000L),
        SYNCHRONIZE = (0x00100000L),
        STANDARD_RIGHTS_REQUIRED = (0x000F0000L),
        STANDARD_RIGHTS_READ = (READ_CONTROL),
        STANDARD_RIGHTS_WRITE = (READ_CONTROL),
        STANDARD_RIGHTS_EXECUTE = (READ_CONTROL),
        STANDARD_RIGHTS_ALL = (0x001F0000L),
        SPECIFIC_RIGHTS_ALL = (0x0000FFFFL),
    }
    #endregion // NTFileAccess

    [Flags]
    public enum MoveFileFlags // Originally MOVEFILE_* defines in winbase.h
    {
        REPLACE_EXISTING = 0x00000001,
        COPY_ALLOWED = 0x00000002,
        DELAY_UNTIL_REBOOT = 0x00000004,
        WRITE_THROUGH = 0x00000008,
        CREATE_HARDLINK = 0x00000010,
        FAIL_IF_NOT_TRACKABLE = 0x00000020,
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class SECURITY_ATTRIBUTES
    {
        uint nLength;
        IntPtr lpSecurityDescriptor;
        bool bInheritHandle;
    }

    public static class WinBase
    {
        public static readonly HANDLE INVALID_HANDLE_VALUE = new HANDLE(-1);

        [DllImport("kernel32.dll", EntryPoint = "CloseHandle")]
        public extern static bool CloseHandle([In] HANDLE hObject);

        [DllImport("kernel32.dll", EntryPoint = "MoveFileW", CharSet = CharSet.Unicode)]
        public extern static bool MoveFile(
            [In] string lpExistingFileName,
            [In] string lpNewFileName);

        [DllImport("kernel32.dll", EntryPoint = "MoveFileExW", CharSet = CharSet.Unicode)]
        public extern static bool MoveFileEx(
            [In] string lpExistingFileName,
            [In, Optional] string lpNewFileName,
            [In] [MarshalAs(UnmanagedType.U4)] MoveFileFlags dwFlags);

        [DllImport("kernel32.dll")]
        public extern static uint GetLastError();

        [DllImport("kernel32.dll", EntryPoint = "GetLastError")]
        public extern static int GetLastErrorInt();

        [DllImport("kernel32.dll", EntryPoint = "CreateFileW", CharSet=CharSet.Unicode)]
        public extern static HANDLE CreateFile(
          [In] string lpFileName,
          [In] NTAccessFlags dwDesiredAccess,
          [In] uint dwShareMode,
          [In, Optional] SECURITY_ATTRIBUTES lpSecurityAttributes,
          [In] uint dwCreationDisposition,
          [In] uint dwFlagsAndAttributes,
          [In, Optional] HANDLE hTemplateFile
        );


        // http://www.pinvoke.net/default.aspx/kernel32.getdiskfreespaceex
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetDiskFreeSpaceEx(string lpDirectoryName,
        out ulong lpFreeBytesAvailable,
        out ulong lpTotalNumberOfBytes,
        out ulong lpTotalNumberOfFreeBytes);



    }
}
