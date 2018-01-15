﻿using System;
using System.Data.SQLite;
using System.IO;
namespace SunSync.Models
{
    public class SyncSetting
    {
        //local dir to sync
        public string SyncLocalDir { set; get; }
        //target bucket
        public string SyncTargetBucket { set; get; }
        // file type
        public int FileType { set; get; }
        //check remote duplicate
        public bool CheckRemoteDuplicate { set; get; }
        //prefix
        public string SyncPrefix { set; get; }
        //check new files
        public bool CheckNewFiles { set; get; }
        //ignore dir
        public bool IgnoreDir { set; get; }
        //skip prefixes
        public string SkipPrefixes { set; get; }
        //skip suffixes
        public string SkipSuffixes { set; get; }
        //overwrite same file
        public bool OverwriteFile { set; get; }
        //default chunk size
        public int DefaultChunkSize { set; get; }
        //upload threshold
        public int ChunkUploadThreshold { set; get; }
        //sync thread count
        public int SyncThreadCount { set; get; }
        //upload entry domain
        public int UploadEntryDomain { set; get; }

        //计划任务相关配置
        public bool EnableSchedule { get; set; }
        public TimeSpanType TimeSpanType { get; set; }
        public int TimeSpan { get; set; }

        /// <summary>
        /// load sync settings from the database by job id
        /// </summary>
        /// <param name="syncId">job id</param>
        /// <returns>
        /// return null if not exist
        /// </returns>
        public static SyncSetting LoadSyncSettingByJobId(string syncId)
        {
            SyncSetting setting = null;
            string myDocPath = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string jobsDb = System.IO.Path.Combine(myDocPath, "qsunsync", "jobs.db");

            if (File.Exists(jobsDb))
            {
                string conStr = new SQLiteConnectionStringBuilder { DataSource = jobsDb }.ToString();
                string query = "SELECT * FROM [sync_jobs]  WHERE [sync_id]=@sync_id";
                using (SQLiteConnection sqlCon = new SQLiteConnection(conStr))
                {
                    sqlCon.Open();
                    using (SQLiteCommand sqlCmd = new SQLiteCommand(sqlCon))
                    {
                        sqlCmd.CommandText = query;
                        sqlCmd.Parameters.Add("@sync_id", System.Data.DbType.String);
                        sqlCmd.Parameters["@sync_id"].Value = syncId;
                        using (SQLiteDataReader dr = sqlCmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                setting = new SyncSetting();
                                setting.SyncLocalDir = Convert.ToString(dr["sync_local_dir"]);
                                setting.SyncTargetBucket = Convert.ToString(dr["sync_target_bucket"]);
                                setting.FileType = Convert.ToInt32(dr["file_type"]);
                                setting.CheckRemoteDuplicate = Convert.ToBoolean(dr["check_remote_duplicate"]);
                                setting.SyncPrefix = Convert.ToString(dr["sync_prefix"]);
                                setting.CheckNewFiles = Convert.ToBoolean(dr["check_new_files"]);
                                setting.IgnoreDir = Convert.ToBoolean(dr["ignore_dir"]);
                                setting.SkipPrefixes = Convert.ToString(dr["skip_prefixes"]);
                                setting.SkipSuffixes = Convert.ToString(dr["skip_suffixes"]);
                                setting.OverwriteFile = Convert.ToBoolean(dr["overwrite_file"]);
                                setting.DefaultChunkSize = Convert.ToInt32(dr["default_chunk_size"]);
                                setting.ChunkUploadThreshold = Convert.ToInt32(dr["chunk_upload_threshold"]);
                                setting.SyncThreadCount = Convert.ToInt32(dr["sync_thread_count"]);
                                setting.UploadEntryDomain = Convert.ToInt32(dr["upload_entry_domain"]);
                            }
                        }
                    }
                }
            }
            return setting;
        }
    }
}
