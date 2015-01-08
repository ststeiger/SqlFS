using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dokan;
using System.IO;
using System.Collections;
//using FileObject = System.IO.Stream;
using System.Diagnostics;
using ContextType = System.UInt32;

using System.Runtime.InteropServices;


using Windows;


namespace AdvancedFS
{


    public class MirrorFS : Dokan.DokanOperations
    {
        #region DokanOperations Members

        private string _root;
        private uint _openFilesNextKey;
        private uint _openFoldersNextKey;

        class FileObject
        {
            public Stream fileStream;
        }

        private Dictionary<ContextType, FileObject> _openFiles = new Dictionary<ContextType, FileObject>();

        public MirrorFS(string root)
        {
            _root = Path.GetFullPath(root);

            if (!_root.EndsWith(@"\"))
                _root = _root + @"\";

            _openFilesNextKey = 1;
            _openFoldersNextKey = 0x80000000;
        }

        private string GetPath(string filename)
        {
            return _root + filename;
        }

        public int CreateFile(String filename, FileAccess access, FileShare share,
            FileMode mode, FileOptions options, DokanFileInfo info)
        {
            string path = GetPath(filename);
            ContextType contextKey = _openFilesNextKey++;
            info.Context = contextKey;

            if (Directory.Exists(path))
            {
                //info.IsDirectory = true;
                return OpenDirectory(filename, info);
            }
            else if (!File.Exists(path))
            {
                if (mode == FileMode.Open || mode == FileMode.Truncate)
                    return -DokanNet.ERROR_FILE_NOT_FOUND;
            }

            // If we got here:
            // mode != FileMode.Open &&
            // mode != FileMode.Truncate &&
            // File.Exists(path)

            try
            {
                FileStream file = File.Open(path, mode, access, share);
                _openFiles.Add(contextKey, new FileObject() { fileStream = file });
                return 0;
            }
            catch (System.Exception e)
            {
                Trace.WriteLine("Got exception:");
                Trace.WriteLine(e.ToString());
                return -DokanNet.ERROR_ACCESS_DENIED;
            }
        }

        public int OpenDirectory(String filename, DokanFileInfo info)
        {
            info.Context = _openFoldersNextKey++;
            if (Directory.Exists(GetPath(filename)))
                return 0;
            else
                return -DokanNet.ERROR_PATH_NOT_FOUND;
        }

        public int CreateDirectory(String filename, DokanFileInfo info)
        {
            string path = GetPath(filename);
            if (Directory.Exists(path))
                return -DokanNet.ERROR_ALREADY_EXISTS;
            else
            {
                try
                {
                    info.Context = _openFoldersNextKey++;
                    Directory.CreateDirectory(path);
                    return 0;
                }
                catch (System.Exception e)
                {
                    Trace.WriteLine("Got exception:");
                    Trace.WriteLine(e.ToString());
                    return -1;
                }
            }
        }

        public int Cleanup(String filename, DokanFileInfo info)
        {
            //Console.WriteLine("%%%%%% count = {0}", info.Context);
            return 0;
        }

        public int CloseFile(String filename, DokanFileInfo info)
        {
            try
            {
                ContextType contextKey = (ContextType)info.Context;

                // An open folder?
                if ((contextKey & 0x80000000) != 0)
                    return 0;

                Stream file = _openFiles[contextKey].fileStream;
                file.Flush();
                file.Close();
                _openFiles.Remove(contextKey);
            }
            catch { return -1; }

            return 0;
        }

        public int ReadFile(String filename, Byte[] buffer, ref uint readBytes,
            long offset, DokanFileInfo info)
        {
            try
            {
                ContextType contextKey = (ContextType)info.Context;
                Stream file = _openFiles[contextKey].fileStream;
                file.Seek(offset, SeekOrigin.Begin);
                readBytes = (uint)file.Read(buffer, 0, buffer.Length);
            }
            catch { return -1; }

            return 0;
        }

        public int WriteFile(String filename, Byte[] buffer,
            ref uint writtenBytes, long offset, DokanFileInfo info)
        {
            try
            {
                ContextType contextKey = (ContextType)info.Context;
                Stream file = _openFiles[contextKey].fileStream;
                file.Seek(offset, SeekOrigin.Begin);
                file.Write(buffer, 0, buffer.Length);
                writtenBytes = (uint) buffer.Length;
            }
            catch(NotSupportedException)
            {
                // TODO: Change error code, this one causes no error on the writing client
                return DokanNet.ERROR_ACCESS_DENIED;
            }
            catch { return -1; }

            return 0;
        }

        public int FlushFileBuffers(String filename, DokanFileInfo info)
        {
            try
            {
                ContextType contextKey = (ContextType)info.Context;
                Stream file = _openFiles[contextKey].fileStream;
                file.Flush();
            }
            catch { return -1; }

            return 0;
        }

        public int GetFileInformation(String filename, FileInformation fileinfo, DokanFileInfo info)
        {
            string path = GetPath(filename);
            if (File.Exists(path))
            {
                FileInfo f = new FileInfo(path);

                fileinfo.Attributes = f.Attributes;
                fileinfo.CreationTime = f.CreationTime;
                fileinfo.LastAccessTime = f.LastAccessTime;
                fileinfo.LastWriteTime = f.LastWriteTime;
                fileinfo.Length = f.Length;

                return 0;
            }
            else if (Directory.Exists(path))
            {
                DirectoryInfo f = new DirectoryInfo(path);

                fileinfo.Attributes = f.Attributes;
                fileinfo.CreationTime = f.CreationTime;
                fileinfo.LastAccessTime = f.LastAccessTime;
                fileinfo.LastWriteTime = f.LastWriteTime;
                fileinfo.Length = 0;
                return 0;
            }
            else
            {
                return -1;
            }
        }


        public int SetAllocationSize(string filename, long length, DokanFileInfo info)
        {
            throw new NotImplementedException();
        }


        public int FindFiles(string filename, System.Collections.ArrayList files, DokanFileInfo info)
        {
            List<FileInformation> newFiles = new List<FileInformation>(files.Count);
            foreach (FileInformation file in files)
            {
                newFiles.Add(file);
            } // Next file

            return FindFiles(filename, newFiles, info);
        }


        // Old one
        public int FindFiles(String filename, List<FileInformation> files, DokanFileInfo info)
        {
            string path = GetPath(filename);
            if (Directory.Exists(path))
            {
                DirectoryInfo d = new DirectoryInfo(path);
                FileSystemInfo[] entries = d.GetFileSystemInfos();
                foreach (FileSystemInfo f in entries)
                {
                    FileInformation fi = new FileInformation();
                    fi.Attributes = f.Attributes;
                    fi.CreationTime = f.CreationTime;
                    fi.LastAccessTime = f.LastAccessTime;
                    fi.LastWriteTime = f.LastWriteTime;
                    fi.Length = (f is DirectoryInfo) ? 0 : ((FileInfo)f).Length;
                    fi.FileName = f.Name;
                    files.Add(fi);
                }
                return 0;
            }
            else
            {
                return -1;
            }
        }

        public int SetFileAttributes(String filename, FileAttributes attr, DokanFileInfo info)
        {
            string path = GetPath(filename);

            try
            {
                File.SetAttributes(path, attr);
            }
            catch (Exception)
            {
                return -1;
            }

            return 0;
        }

        public int SetFileTime(String filename, DateTime ctime,
                DateTime atime, DateTime mtime, DokanFileInfo info)
        {
            string path = GetPath(filename);

            try
            {
                FileInfo fileObject = new FileInfo(path);

                // Update times only if there is a valid time value

                if (ctime != DateTime.MinValue)
                    fileObject.CreationTime = ctime;
                if (mtime != DateTime.MinValue)
                    fileObject.LastWriteTime = mtime;

                // Avoid updating the last-accessed time
                //if (atime != DateTime.MinValue)
                //    fileObject.LastAccessTime = atime;
            }
            catch (Exception)
            {
                return -1;
            }

            return 0;
        }

        public int DeleteFile(String filename, DokanFileInfo info)
        {
            string realPath = GetPath(filename);

            if (!File.Exists(realPath))
                return -DokanNet.ERROR_FILE_NOT_FOUND;

            try
            {
                File.Delete(realPath);
            }
            catch (System.Exception e)
            {
                Trace.WriteLine("Got exception: \n" + e.ToString());
                return -DokanNet.ERROR_ACCESS_DENIED;
            }

            return 0;
        }

        public int DeleteDirectory(String filename, DokanFileInfo info)
        {
            string realPath = GetPath(filename);

            if (!Directory.Exists(realPath))
                return -DokanNet.ERROR_FILE_NOT_FOUND;

            try
            {
                Directory.Delete(realPath, false);
            }
            catch (System.Exception e)
            {
                Trace.WriteLine("Got exception: \n" + e.ToString());


                // TODO: Find a more elegant way 
                if (e.Message.Contains("The directory is not empty"))
                    return -DokanNet.ERROR_DIR_NOT_EMPTY;

                return -DokanNet.ERROR_ACCESS_DENIED;
            }

            return 0;
        }



        public int MoveFile(String filename, String newname, bool replace, DokanFileInfo info)
        {
            string oldPath, newPath;
            bool status;

            oldPath = GetPath(filename);
            newPath = GetPath(newname);

            if (info.Context != null)
            {
		        // should close? or rename at closing?
               
                WinBase.CloseHandle(new IntPtr((int)info.Context));
		        info.Context = null;
	        }

            if (replace)
                status = WinBase.MoveFileEx(oldPath, newPath, MoveFileFlags.REPLACE_EXISTING);
            else
                status = WinBase.MoveFile(oldPath, newPath);

            if (!status)
            {
                int error = WinBase.GetLastErrorInt();
                //DbgPrint("\tMoveFile failed status = %d, code = %d\n", status, error);
                return -error;
            }

            return 0;
        }

        public int SetEndOfFile(String filename, long length, DokanFileInfo info)
        {
            string path = GetPath(filename);

            try
            {
                using (Stream s = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    s.SetLength(length);
                    s.Close();
                }
            }
            catch (Exception)
            {
                return -1;
            }

            return 0;
        }

        public int LockFile(String filename, long offset, long length, DokanFileInfo info)
        {
            return 0;
        }

        public int UnlockFile(String filename, long offset, long length, DokanFileInfo info)
        {
            return 0;
        }

        public int GetDiskFreeSpace(ref ulong freeBytesAvailable, ref ulong totalBytes,
            ref ulong totalFreeBytes, DokanFileInfo info)
        {
            if (!WinBase.GetDiskFreeSpaceEx(_root, out freeBytesAvailable,
                out totalBytes, out totalFreeBytes))
            {
                throw Marshal.GetExceptionForHR(Marshal.GetLastWin32Error());
            }

            return 0;
        }

        public int Unmount(DokanFileInfo info)
        {
            foreach (FileObject file in _openFiles.Values)
            {
                try
                {
                    if (file.fileStream != null)
                        file.fileStream.Close();
                }
                catch {}
            }

            return 0;
        }
        #endregion
    }
}
