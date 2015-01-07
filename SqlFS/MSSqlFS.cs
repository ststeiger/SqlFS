
using System.Data.Sql;
using System.Data.SqlClient;
using System.Data.SqlTypes;


// using System.Text.RegularExpressions;
// using System.Data;

// using System.IO.Compression;
// using Dokan;


//TODO: Memorystream - dispose ? 
namespace MSSQLFS
{


    class FileCaching
    {
        public System.IO.MemoryStream MemStream;
        public Dokan.FileInformation FileInfo;
    }


    class MSSQLFS : Dokan.DokanOperations
    {

        private System.Collections.Generic.Dictionary<string, FileCaching> FileCache = 
            new System.Collections.Generic.Dictionary<string, FileCaching>();

        public string ZippedExtension = " .zip .gzip .tar .arj .7z .7zip .rar .gif .jpg .jpeg ";

        string ConnectionString;

        // #region DokanOperations member


        public MSSQLFS()
        {
            ConnectionString = "Data Source=10.29.144.51;Initial Catalog=ekvelb2;Integrated Security=False;User ID=nedopilm;Password=**********;Pooling=true;Min Pool Size=1;Max Pool Size=5;Connect Timeout=500";
        }

        public MSSQLFS(string ConnString)
        {
            ConnectionString = ConnString;
        }





        // #region Directory function
        public int CreateDirectory(string filename, Dokan.DokanFileInfo info)
        {
            //create directory in database
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                SqlCommand SP = new SqlCommand();
                SP.Connection = conn;
                SP.CommandType = System.Data.CommandType.StoredProcedure;
                SP.CommandText = "CreateDirectory";
                SP.Parameters.AddWithValue("@filename", filename);
                conn.Open();
                try
                {
                    SP.ExecuteNonQuery(); //on MoveFile can raise error due directory is exists
                }
                catch
                {
                }
            }
            return Dokan.DokanNet.DOKAN_SUCCESS;
        }

        public int DeleteDirectory(string filename, Dokan.DokanFileInfo info)
        {
            return DeleteFile(filename, info);
        }

        public int OpenDirectory(string filename, Dokan.DokanFileInfo info)
        {
            return Dokan.DokanNet.DOKAN_SUCCESS;
        }

        // #endregion


        private static void Decompress(System.IO.MemoryStream zipped, System.IO.MemoryStream Output)
        {
            zipped.Seek(0, System.IO.SeekOrigin.Begin);
            System.IO.Compression.GZipStream gzip = new System.IO.Compression.GZipStream(zipped, System.IO.Compression.CompressionMode.Decompress, true);

            byte[] bytes = new byte[4096];
            int n;
            while ((n = gzip.Read(bytes, 0, bytes.Length)) != 0)
            {
                Output.Write(bytes, 0, n);
            }
            gzip.Close();
        }

        private static void Compress(System.IO.MemoryStream raw, System.IO.MemoryStream Output)
        {
            System.IO.Compression.GZipStream gzip = new System.IO.Compression.GZipStream(Output, System.IO.Compression.CompressionMode.Compress, true);
            raw.Seek(0, System.IO.SeekOrigin.Begin);
            byte[] bytes = new byte[4096];
            int n;
            while ((n = raw.Read(bytes, 0, bytes.Length)) != 0)
            {
                gzip.Write(bytes, 0, n);
            }
            gzip.Close();
        }




        public int Cleanup(string filename, Dokan.DokanFileInfo info)
        {
            lock (FileCache)
            {
                if ((FileCache.ContainsKey(filename) == true) && (FileCache[filename].MemStream.Length > 0))
                {
                    using (SqlConnection conn = new SqlConnection(ConnectionString))
                    {
                        using (SqlCommand Cmd = new SqlCommand())
                        {
                            System.IO.MemoryStream mem = ((FileCaching)FileCache[filename]).MemStream;
                            Cmd.CommandText = "WriteFile";
                            Cmd.Parameters.Add("@iszipped", System.Data.SqlDbType.Bit, 1);
                            Cmd.Parameters["@iszipped"].Value = 0;
                            Cmd.Parameters.Add("@OriginalSize", System.Data.SqlDbType.BigInt);
                            Cmd.Parameters["@OriginalSize"].Value = mem.Length;


                            if (this.ZippedExtension.ToLower().IndexOf(System.IO.Path.GetExtension(System.Text.RegularExpressions.Regex.Split(filename.ToLower(), ".version")[0])) == -1)
                            {
                                if (FileCache[filename].MemStream.Length > 256)
                                {
                                    Cmd.Parameters["@iszipped"].Value = 1;
                                    System.IO.MemoryStream dummy = new System.IO.MemoryStream();
                                    Compress(mem, dummy);
                                    mem.SetLength(0);
                                    dummy.WriteTo(mem);
                                }
                            }

                            mem.Seek(0, System.IO.SeekOrigin.Begin);
                            Cmd.Parameters.Add("@data", System.Data.SqlDbType.VarBinary, (int)mem.Length);
                            Cmd.Parameters["@data"].SqlValue = mem.ToArray();

                            Cmd.Parameters.AddWithValue("@filename", filename);

                            Cmd.CommandType = System.Data.CommandType.StoredProcedure;
                            Cmd.Connection = conn;
                            conn.Open();

                            Cmd.ExecuteNonQuery();
                            FileCache.Remove(filename);
                        }
                    }
                }
            };
            return Dokan.DokanNet.DOKAN_SUCCESS;
        }

        public int CloseFile(string filename, Dokan.DokanFileInfo info)
        {
            return Dokan.DokanNet.DOKAN_SUCCESS;
        }


        public int CreateFile(string filename, System.IO.FileAccess access, System.IO.FileShare share, System.IO.FileMode mode, System.IO.FileOptions options, Dokan.DokanFileInfo info)
        {
            Dokan.FileInformation fi = new Dokan.FileInformation();
            GetFileInformation(filename, ref fi, info);

            switch (mode)
            {
                case System.IO.FileMode.Append:
                    return Dokan.DokanNet.DOKAN_SUCCESS;
                case System.IO.FileMode.Create:
                    AddToFileCache(filename, FillFileCache(filename));
                    return Dokan.DokanNet.DOKAN_SUCCESS;
                case System.IO.FileMode.CreateNew:
                    AddToFileCache(filename, FillFileCache(filename));
                    return Dokan.DokanNet.DOKAN_SUCCESS;
                case System.IO.FileMode.Open:
                    return Dokan.DokanNet.DOKAN_SUCCESS;
                case System.IO.FileMode.Truncate:
                    return Dokan.DokanNet.DOKAN_SUCCESS;
            }
            return Dokan.DokanNet.DOKAN_ERROR;
        }


        public int DeleteFile(string filename, Dokan.DokanFileInfo info)
        {
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                SqlCommand Cmd = new SqlCommand();
                Cmd.CommandText = "DeleteFile";
                Cmd.Parameters.AddWithValue("@filename", filename);
                Cmd.CommandType = System.Data.CommandType.StoredProcedure;
                Cmd.Connection = conn;
                conn.Open();
                Cmd.ExecuteNonQuery(); //TODO:react on error on SQL side
                FileCache.Remove(filename);
            }
            return Dokan.DokanNet.DOKAN_SUCCESS;
        }


        public int FlushFileBuffers(string filename, Dokan.DokanFileInfo info)
        {
            return CloseFile(filename, info);
        }


        public int FindFiles(string filename, System.Collections.ArrayList files, Dokan.DokanFileInfo info)
        {
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                SqlCommand Cmd = new SqlCommand();
                Cmd.CommandText = "FindFiles";
                Cmd.Parameters.AddWithValue("@filename", filename);
                Cmd.CommandType = System.Data.CommandType.StoredProcedure;
                Cmd.Connection = conn;
                conn.Open();
                using (SqlDataReader reader = Cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Dokan.FileInformation finfo = new Dokan.FileInformation();
                        finfo.FileName = reader[0].ToString();
                        finfo.Attributes = reader[1].ToString() == "True" ? System.IO.FileAttributes.Directory : System.IO.FileAttributes.Normal;
                        lock (FileCache)
                        {
                            if (FileCache.ContainsKey(finfo.FileName) == true)
                            {
                                finfo.LastAccessTime = FileCache[finfo.FileName].FileInfo.LastAccessTime;
                                finfo.CreationTime = FileCache[finfo.FileName].FileInfo.CreationTime;
                                finfo.LastWriteTime = FileCache[finfo.FileName].FileInfo.LastWriteTime;
                                finfo.Length = FileCache[finfo.FileName].FileInfo.Length;
                            }
                            else
                            {
                                System.DateTime.TryParse(reader[4].ToString(), out finfo.LastAccessTime);
                                System.DateTime.TryParse(reader[5].ToString(), out finfo.LastWriteTime);
                                System.DateTime.TryParse(reader[6].ToString(), out finfo.CreationTime);
                                finfo.Length = (reader[2] is System.DBNull) ? 0 : int.Parse(reader[2].ToString());
                            }
                        }
                        files.Add(finfo);
                    }
                }
                conn.Close();
            }
            return Dokan.DokanNet.DOKAN_SUCCESS;
        }


        public int GetFileInformation(string filename, ref Dokan.FileInformation fileinfo, Dokan.DokanFileInfo info)
        {
            lock (FileCache)
            {
                if (FileCache.ContainsKey(filename) == false)
                {
                    int RetVal = AddToFileCache(filename);
                    if (RetVal == Dokan.DokanNet.DOKAN_SUCCESS)
                    {
                        fileinfo = FileCache[filename].FileInfo;
                    }
                    return RetVal;
                }
                else
                {
                    fileinfo = FileCache[filename].FileInfo;
                }
            }
            return Dokan.DokanNet.DOKAN_SUCCESS;
        }


        public int LockFile(string filename, long offset, long length, Dokan.DokanFileInfo info)
        {
            return Dokan.DokanNet.DOKAN_SUCCESS;
        }

        public int MoveFile(string filename, string newname, bool replace, Dokan.DokanFileInfo info)
        {
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                SqlCommand Cmd = new SqlCommand();
                Cmd.CommandText = "MoveFile";
                Cmd.Parameters.AddWithValue("@filename", filename);
                Cmd.Parameters.AddWithValue("@newname", newname);
                Cmd.Parameters.AddWithValue("@replace", replace);
                Cmd.CommandType = System.Data.CommandType.StoredProcedure;
                Cmd.Connection = conn;
                conn.Open();
                Cmd.ExecuteNonQuery(); //TODO:react on error
                
                FileCaching fc = FileCache[filename];
                FileCache.Remove(filename);
                FileCache.Remove(newname);
            }
            return Dokan.DokanNet.DOKAN_SUCCESS;

        }

        public int ReadFile(string filename, byte[] buffer, ref uint readBytes, long offset, Dokan.DokanFileInfo info)
        {
            lock (FileCache)
            {
                if (FileCache.ContainsKey(filename) == false)
                {
                    return -1 * Dokan.DokanNet.ERROR_FILE_NOT_FOUND;
                }

                if (FileCache[filename].MemStream.Length == 0)
                {
                    long readed = -1;

                    using (SqlConnection conn = new SqlConnection(ConnectionString))
                    {
                        SqlCommand Cmd = new SqlCommand();
                        Cmd.CommandText = "ReadFile";
                        Cmd.Parameters.AddWithValue("@filename", filename);

                        Cmd.CommandType = System.Data.CommandType.StoredProcedure;
                        Cmd.Connection = conn;
                        conn.Open();
                        using (SqlDataReader reader = Cmd.ExecuteReader())
                        {
                            reader.Read();
                            readed = (long)reader[0];
                            FileCache[filename].MemStream = new System.IO.MemoryStream();
                            FileCache[filename].FileInfo.LastAccessTime = System.DateTime.Now;
                            FileCache[filename].MemStream.Write((reader[2] as byte[]), 0, (int)readed);

                            bool IsZipped = !(reader[1] is System.DBNull) ? (bool)reader[1] : false;
                            if (IsZipped)
                            {
                                System.IO.MemoryStream mem2 = new System.IO.MemoryStream();
                                Decompress(FileCache[filename].MemStream, mem2);
                                FileCache[filename].MemStream.SetLength(0);
                                mem2.WriteTo(FileCache[filename].MemStream);
                            }
                        }
                    }
                }

                FileCache[filename].MemStream.Seek(offset, System.IO.SeekOrigin.Begin);
                readBytes = (uint)FileCache[filename].MemStream.Read(buffer, 0, buffer.Length);
                if ((offset == FileCache[filename].MemStream.Length) && (readBytes == 0))
                {
                    return (-1);
                }
            };
            return Dokan.DokanNet.DOKAN_SUCCESS;
        }

        public int WriteFile(string filename, byte[] buffer, ref uint writtenBytes, long offset, Dokan.DokanFileInfo info)
        {
            lock (FileCache)
            {
                FileCache[filename].MemStream.Seek(offset, System.IO.SeekOrigin.Begin);
                FileCache[filename].MemStream.Write(buffer, 0, (int)buffer.Length);

                writtenBytes = (uint)buffer.Length;
                FileCache[filename].FileInfo.LastWriteTime = System.DateTime.Now;
                FileCache[filename].FileInfo.Length = FileCache[filename].MemStream.Length;
            };
            return Dokan.DokanNet.DOKAN_SUCCESS;
        }

        public int SetEndOfFile(string filename, long length, Dokan.DokanFileInfo info)
        {
            lock (FileCache)
            {
                if (FileCache.ContainsKey(filename) == true)
                {
                    FileCache[filename].MemStream.SetLength(length);
                }
            }
            return Dokan.DokanNet.DOKAN_SUCCESS;
        }

        public int SetAllocationSize(string filename, long length, Dokan.DokanFileInfo info)
        {
            return SetEndOfFile(filename, length, info);
        }

        public int SetFileAttributes(string filename, System.IO.FileAttributes attr, Dokan.DokanFileInfo info)
        {
            return Dokan.DokanNet.DOKAN_SUCCESS;
        }

        public int SetFileTime(string filename, System.DateTime ctime, System.DateTime atime, System.DateTime mtime, Dokan.DokanFileInfo info)
        {
            lock (FileCache)
            {
                if (FileCache.ContainsKey(filename) == true)
                {
                    FileCache[filename].FileInfo.LastAccessTime = atime;
                    FileCache[filename].FileInfo.CreationTime = ctime;
                    FileCache[filename].FileInfo.LastWriteTime = mtime;
                }
            }
            return Dokan.DokanNet.DOKAN_SUCCESS;
        }

        public int UnlockFile(string filename, long offset, long length, Dokan.DokanFileInfo info)
        {
            return Dokan.DokanNet.DOKAN_SUCCESS;
        }

        public int Unmount(Dokan.DokanFileInfo info)
        {
            //TODO: flush all opened files to Sql
            FileCache.Clear();
            return Dokan.DokanNet.DOKAN_SUCCESS;
        }

        public int GetDiskFreeSpace(ref ulong freeBytesAvailable, ref ulong totalBytes, ref ulong totalFreeBytes, Dokan.DokanFileInfo info)
        {
            freeBytesAvailable = 512 * 1024 * 1024;
            totalBytes = 1024 * 1024 * 1024;
            totalFreeBytes = 512 * 1024 * 1024;
            return Dokan.DokanNet.DOKAN_SUCCESS;
        }


        private int AddToFileCache(string filename)
        {
            lock (FileCache)
            {
                if (FileCache.ContainsKey(filename) == false)
                {
                    FileCaching fc = FillFileCache(filename);

                    using (SqlConnection conn = new SqlConnection(ConnectionString))
                    {
                        using (SqlCommand Cmd = new SqlCommand())
                        {
                            Cmd.CommandText = "GetFileInformation";
                            Cmd.Parameters.AddWithValue("@filename", filename);
                            Cmd.Parameters.Add("@IsDirectory", System.Data.SqlDbType.Bit);
                            Cmd.Parameters["@IsDirectory"].Direction = System.Data.ParameterDirection.Output;
                            Cmd.Parameters.Add("@Length", System.Data.SqlDbType.BigInt);
                            Cmd.Parameters["@Length"].Direction = System.Data.ParameterDirection.Output;
                            Cmd.Parameters.Add("@LastAccessTime", System.Data.SqlDbType.DateTime);
                            Cmd.Parameters["@LastAccessTime"].Direction = System.Data.ParameterDirection.Output;
                            Cmd.Parameters.Add("@LastWriteTime", System.Data.SqlDbType.DateTime);
                            Cmd.Parameters["@LastWriteTime"].Direction = System.Data.ParameterDirection.Output;
                            Cmd.Parameters.Add("@CreationTime", System.Data.SqlDbType.DateTime);
                            Cmd.Parameters["@CreationTime"].Direction = System.Data.ParameterDirection.Output;
                            Cmd.CommandType = System.Data.CommandType.StoredProcedure;
                            Cmd.Connection = conn;
                            conn.Open();

                            Cmd.ExecuteNonQuery();
                            if (Cmd.Parameters["@CreationTime"].Value is System.DBNull)
                            {
                                return -1 * Dokan.DokanNet.ERROR_FILE_NOT_FOUND;
                            }
                            
                            fc.FileInfo.FileName = filename;
                            fc.FileInfo.Attributes = (Cmd.Parameters["@IsDirectory"].Value.ToString() == "True") ? System.IO.FileAttributes.Directory : System.IO.FileAttributes.Normal;

                            System.DateTime.TryParse(Cmd.Parameters["@LastAccessTime"].Value.ToString(), out fc.FileInfo.LastAccessTime);
                            System.DateTime.TryParse(Cmd.Parameters["@LastWriteTime"].Value.ToString(), out fc.FileInfo.LastWriteTime);
                            System.DateTime.TryParse(Cmd.Parameters["@CreationTime"].Value.ToString(), out fc.FileInfo.CreationTime);

                            fc.FileInfo.Length = Cmd.Parameters["@Length"].Value is System.DBNull ? 0 : (System.Int64)Cmd.Parameters["@Length"].Value;
                            FileCache.Add(filename, fc);
                        }
                    }
                }
            }
            return Dokan.DokanNet.DOKAN_SUCCESS;
        }


        private void AddToFileCache(string filename, FileCaching fc)
        {
            lock (FileCache)
            {
                if (FileCache.ContainsKey(filename) == false)
                {
                    FileCache.Add(filename, fc);
                }
            }
        }


        private static FileCaching FillFileCache(string filename)
        {
            FileCaching fc = new FileCaching();
            fc.MemStream = new System.IO.MemoryStream();
            fc.FileInfo = new Dokan.FileInformation();
            fc.FileInfo.CreationTime = System.DateTime.Now;
            fc.FileInfo.LastAccessTime = System.DateTime.Now;
            fc.FileInfo.LastWriteTime = System.DateTime.Now;
            fc.FileInfo.Length = 0;
            fc.FileInfo.FileName = filename;
            return fc;
        }


        // #endregion
    }
}
