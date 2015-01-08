
using System;
using Dokan;
using System.Collections.Generic;
using System.Diagnostics;


namespace AdvancedFS
{
    public class TraceFS : Dokan.DokanOperations
    {
        private Dokan.DokanOperations _wrappedSubject;

        public TraceFS(DokanOperations wrapee)
        {
            this._wrappedSubject = wrapee;
            this.AttachToEvents();
        }

        private Dokan.DokanOperations WrappedSubject
        {
            get { return this._wrappedSubject; }
            set { this._wrappedSubject = value; }
        }

        private void AttachToEvents()
        {
        }

        public void TraceOperation(string op, params object[] p)
        {
            Trace.Write(op);
            Trace.Write("(");
            foreach (var par in p)
            {
                Trace.Write("[");
                Trace.Write(par);
                Trace.Write("], ");
            }
            Trace.Write(")");
            Trace.WriteLine("");
        }

        public int CreateFile(string filename, System.IO.FileAccess access, System.IO.FileShare share, System.IO.FileMode mode, System.IO.FileOptions options, Dokan.DokanFileInfo info)
        {
            TraceOperation("CreateFile", filename, access, share, mode, options, info);
            return this._wrappedSubject.CreateFile(filename, access, share, mode, options, info);
        }

        public int OpenDirectory(string filename, Dokan.DokanFileInfo info)
        {
            TraceOperation("OpenDirectory", filename, info);
            return this._wrappedSubject.OpenDirectory(filename, info);
        }

        public int CreateDirectory(string filename, Dokan.DokanFileInfo info)
        {
            TraceOperation("CreateDirectory", filename, info);
            return this._wrappedSubject.CreateDirectory(filename, info);
        }

        public int Cleanup(string filename, Dokan.DokanFileInfo info)
        {
            TraceOperation("Cleanup", filename, info);
            return this._wrappedSubject.Cleanup(filename, info);
        }

        public int CloseFile(string filename, Dokan.DokanFileInfo info)
        {
            TraceOperation("CloseFile", filename, info);
            return this._wrappedSubject.CloseFile(filename, info);
        }

        public int ReadFile(string filename, byte[] buffer, ref uint readBytes, long offset, DokanFileInfo info)
        {
            TraceOperation("ReadFile", filename, buffer, readBytes, offset, info);
            return this._wrappedSubject.ReadFile(filename, buffer, ref readBytes, offset, info);
        }

        public int WriteFile(string filename, byte[] buffer, ref uint writtenBytes, long offset, DokanFileInfo info)
        {
            TraceOperation("WriteFile", filename, buffer, writtenBytes, offset, info);
            return this._wrappedSubject.WriteFile(filename, buffer, ref writtenBytes, offset, info);
        }

        public int FlushFileBuffers(string filename, Dokan.DokanFileInfo info)
        {
            TraceOperation("FlushFileBuffers", filename, info);
            return this._wrappedSubject.FlushFileBuffers(filename, info);
        }

        public int GetFileInformation(string filename, Dokan.FileInformation fileinfo, Dokan.DokanFileInfo info)
        {
            TraceOperation("GetFileInformation", filename, fileinfo, info);
            return this._wrappedSubject.GetFileInformation(filename, fileinfo, info);
        }

        public int FindFiles(string filename, List<FileInformation> files, Dokan.DokanFileInfo info)
        {
            TraceOperation("FindFiles", filename, files, info);
            System.Collections.ArrayList arrayList = new System.Collections.ArrayList(files);
            return this._wrappedSubject.FindFiles(filename, arrayList, info);
        }

        public int SetFileAttributes(string filename, System.IO.FileAttributes attr, Dokan.DokanFileInfo info)
        {
            TraceOperation("SetFileAttributes", filename, attr, info);
            return this._wrappedSubject.SetFileAttributes(filename, attr, info);
        }

        public int SetFileTime(string filename, System.DateTime ctime, System.DateTime atime, System.DateTime mtime, Dokan.DokanFileInfo info)
        {
            TraceOperation("SetFileTime", filename, ctime, atime, mtime, info);
            return this._wrappedSubject.SetFileTime(filename, ctime, atime, mtime, info);
        }

        public int DeleteFile(string filename, Dokan.DokanFileInfo info)
        {
            TraceOperation("DeleteFile", filename, info);
            return this._wrappedSubject.DeleteFile(filename, info);
        }

        public int DeleteDirectory(string filename, Dokan.DokanFileInfo info)
        {
            TraceOperation("DeleteDirectory", filename, info);
            return this._wrappedSubject.DeleteDirectory(filename, info);
        }

        public int MoveFile(string filename, string newname, bool replace, Dokan.DokanFileInfo info)
        {
            TraceOperation("MoveFile", filename, newname, replace, info);
            return this._wrappedSubject.MoveFile(filename, newname, replace, info);
        }

        public int SetEndOfFile(string filename, long length, Dokan.DokanFileInfo info)
        {
            TraceOperation("SetEndOfFile", filename, length, info);
            return this._wrappedSubject.SetEndOfFile(filename, length, info);
        }

        public int LockFile(string filename, long offset, long length, Dokan.DokanFileInfo info)
        {
            TraceOperation("LockFile", filename, offset, length, info);
            return this._wrappedSubject.LockFile(filename, offset, length, info);
        }

        public int UnlockFile(string filename, long offset, long length, Dokan.DokanFileInfo info)
        {
            TraceOperation("UnlockFile", filename, offset, length, info);
            return this._wrappedSubject.UnlockFile(filename, offset, length, info);
        }

        public int GetDiskFreeSpace(ref ulong freeBytesAvailable, ref ulong totalBytes, ref ulong totalFreeBytes, DokanFileInfo info)
        {
            TraceOperation("GetDiskFreeSpace", freeBytesAvailable, totalBytes, totalFreeBytes, info);
            return this._wrappedSubject.GetDiskFreeSpace(ref freeBytesAvailable, ref totalBytes, ref totalFreeBytes, info);
        }

        public int Unmount(Dokan.DokanFileInfo info)
        {
            TraceOperation("Unmount", info);
            return this._wrappedSubject.Unmount(info);
        }
    }
}
