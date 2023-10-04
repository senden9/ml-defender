using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;

namespace Common
{
    /// <summary>
    ///     Collects statistics from various Simulation Models by using the StatsEventSystem
    /// </summary>
    public class StatisticsCollector : MonoBehaviour
    {
        /// <summary>
        /// Sets the storage mode of the log-files.
        /// </summary>
        public enum StorageMode
        {
            /// <summary>
            /// Stores the log in the project source folder
            /// </summary>
            InAssets,
            /// <summary>
            /// Use a given `externalStoragePath`. Can be external.
            /// </summary>
            ExternalStoragePath,
        }
        
        private StreamWriter logWriter;
        private int writeCounter = 0;
        private const int flushXWrites = 3;

        public StorageMode storageMode = StorageMode.InAssets;
        /// <summary>
        /// Path without file-name. Must not end in a path-separator.
        /// </summary>
        public string externalStoragePath;

        // Start is called before the first frame update
        void Start()
        {
            Assert.IsTrue(logWriter is null);
            string logPath = GenLogPath();
            if (File.Exists(logPath))
            {
                Debug.LogError($"File `{logPath}` already exists.");
                return;
            }

            logWriter = File.CreateText(logPath);
            Debug.Log($"Write Statistic information at `{logPath}`");

            StatsEventSystem.current.RoundFinished += OnRoundFinished;
        }

        private void OnRoundFinished(RoundStatisticDto obj)
        {
            string jsonString = JsonConvert.SerializeObject(obj, Formatting.None);
            if (IsAnyFieldNull(obj))
            {
                Debug.LogWarning($"Logging object with null field: {jsonString}");
            }

            logWriter.WriteLine(jsonString);

            // Flush from time to time for easier debugging.
            writeCounter++;
            if (writeCounter > flushXWrites)
            {
                writeCounter = 0;
                logWriter.Flush();
            }
        }

        private void OnDestroy()
        {
            Debug.Log("Start to destroy statistics collector");
            StatsEventSystem.current.RoundFinished -= OnRoundFinished;
            logWriter.Close();
            logWriter = null;
            Debug.Log("Successful destroyed statistics collector");
        }

        /// <summary>
        ///     Check if any public field of an object is null.
        ///     Modified version of https://www.phind.com/search?cache=228f6b4d-99ef-49ab-b382-0e716808e7de AI search engine.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private static bool IsAnyFieldNull(object obj)
        {
            Type type = obj.GetType();
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (FieldInfo field in fields)
            {
                if (field.GetValue(obj) is null)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Defines where the statistics logfile should be placed.
        /// </summary>
        private string GenLogPath()
        {
            string storagePath;
            switch (storageMode)
            {
                case StorageMode.InAssets:
                    storagePath = new StackTrace(true).GetFrame(0).GetFileName();
                    storagePath = Path.GetFullPath(storagePath);
                    storagePath = Path.GetDirectoryName(storagePath);
                    break;
                case StorageMode.ExternalStoragePath:
                    if (string.IsNullOrEmpty(externalStoragePath))
                    {
                        Debug.LogError("Storage Mode set to external but no path given");
                        throw new ArgumentException();
                    }
                    storagePath = Path.GetFullPath(externalStoragePath);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            Assert.IsTrue(Path.IsPathRooted(storagePath));
            Assert.IsTrue(Path.IsPathFullyQualified(storagePath));

            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss");

            return storagePath + Path.DirectorySeparatorChar + $"statistics-{timestamp}.jsonl";
        }
    }
}