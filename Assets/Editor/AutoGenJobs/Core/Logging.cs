using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace AutoGenJobs.Core
{
    /// <summary>
    /// AutoGen Job System 的日志系统
    /// 支持控制台输出和文件日志
    /// </summary>
    public class JobLogger
    {
        private readonly string _jobId;
        private readonly List<LogEntry> _entries = new List<LogEntry>();
        private int _currentCommandIndex = -1;

        public string JobId => _jobId;
        public IReadOnlyList<LogEntry> Entries => _entries;

        public JobLogger(string jobId)
        {
            _jobId = jobId;
        }

        /// <summary>
        /// 设置当前正在执行的命令索引
        /// </summary>
        public void SetCurrentCommand(int index)
        {
            _currentCommandIndex = index;
        }

        /// <summary>
        /// 记录信息日志
        /// </summary>
        public void Info(string message)
        {
            Log(LogLevel.Info, message);
        }

        /// <summary>
        /// 记录警告日志
        /// </summary>
        public void Warning(string message)
        {
            Log(LogLevel.Warning, message);
        }

        /// <summary>
        /// 记录错误日志
        /// </summary>
        public void Error(string message)
        {
            Log(LogLevel.Error, message);
        }

        /// <summary>
        /// 记录调试日志（仅在 VerboseLogging 启用时输出到控制台）
        /// </summary>
        public void Debug(string message)
        {
            Log(LogLevel.Debug, message);
        }

        private void Log(LogLevel level, string message)
        {
            var entry = new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = level,
                JobId = _jobId,
                CommandIndex = _currentCommandIndex,
                Message = message
            };
            
            _entries.Add(entry);
            
            // 输出到 Unity 控制台
            var prefix = _currentCommandIndex >= 0 
                ? $"[AutoGenJobs][{_jobId}][Cmd:{_currentCommandIndex}]" 
                : $"[AutoGenJobs][{_jobId}]";
            
            var fullMessage = $"{prefix} {message}";
            
            switch (level)
            {
                case LogLevel.Debug:
                    if (AutoGenSettings.VerboseLogging)
                        UnityEngine.Debug.Log(fullMessage);
                    break;
                case LogLevel.Info:
                    UnityEngine.Debug.Log(fullMessage);
                    break;
                case LogLevel.Warning:
                    UnityEngine.Debug.LogWarning(fullMessage);
                    break;
                case LogLevel.Error:
                    UnityEngine.Debug.LogError(fullMessage);
                    break;
            }
        }

        /// <summary>
        /// 将日志写入文件
        /// </summary>
        public void WriteToFile(string filePath)
        {
            try
            {
                PathUtil.EnsureDirectoryExists(filePath);
                
                var sb = new StringBuilder();
                foreach (var entry in _entries)
                {
                    sb.AppendLine(entry.ToString());
                }
                
                File.WriteAllText(filePath, sb.ToString());
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[AutoGenJobs] Failed to write log file: {e.Message}");
            }
        }

        /// <summary>
        /// 写入 JSONL 格式日志文件
        /// </summary>
        public void WriteToJsonlFile(string filePath)
        {
            try
            {
                PathUtil.EnsureDirectoryExists(filePath);
                
                var sb = new StringBuilder();
                foreach (var entry in _entries)
                {
                    sb.AppendLine(JsonUtility.ToJson(entry.ToSerializable()));
                }
                
                File.WriteAllText(filePath, sb.ToString());
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[AutoGenJobs] Failed to write jsonl log file: {e.Message}");
            }
        }
    }

    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }

    public class LogEntry
    {
        public DateTime Timestamp;
        public LogLevel Level;
        public string JobId;
        public int CommandIndex;
        public string Message;

        public override string ToString()
        {
            var cmdPart = CommandIndex >= 0 ? $"[Cmd:{CommandIndex}]" : "";
            return $"[{Timestamp:yyyy-MM-ddTHH:mm:ss.fffZ}][{Level}]{cmdPart} {Message}";
        }

        public SerializableLogEntry ToSerializable()
        {
            return new SerializableLogEntry
            {
                timestamp = Timestamp.ToString("o"),
                level = Level.ToString(),
                jobId = JobId,
                commandIndex = CommandIndex,
                message = Message
            };
        }
    }

    [Serializable]
    public class SerializableLogEntry
    {
        public string timestamp;
        public string level;
        public string jobId;
        public int commandIndex;
        public string message;
    }

    /// <summary>
    /// 全局日志工具（用于非 Job 上下文的日志）
    /// </summary>
    public static class AutoGenLog
    {
        public static void Info(string message)
        {
            if (AutoGenSettings.VerboseLogging || true) // Info 始终输出
                UnityEngine.Debug.Log($"[AutoGenJobs] {message}");
        }

        public static void Warning(string message)
        {
            UnityEngine.Debug.LogWarning($"[AutoGenJobs] {message}");
        }

        public static void Error(string message)
        {
            UnityEngine.Debug.LogError($"[AutoGenJobs] {message}");
        }

        public static void Debug(string message)
        {
            if (AutoGenSettings.VerboseLogging)
                UnityEngine.Debug.Log($"[AutoGenJobs][DEBUG] {message}");
        }
    }
}
