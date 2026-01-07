using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutoGenJobs.Core;

namespace AutoGenJobs.Core
{
    /// <summary>
    /// Job 队列管理
    /// 负责扫描、排序和移动 Job 文件
    /// </summary>
    public class JobQueue
    {
        /// <summary>
        /// 获取 inbox 中待处理的 Job 文件列表（按创建时间排序）
        /// </summary>
        public List<string> GetPendingJobs()
        {
            var inboxPath = AutoGenSettings.InboxPath;
            if (!Directory.Exists(inboxPath))
            {
                return new List<string>();
            }

            try
            {
                var files = Directory.GetFiles(inboxPath, "*.job.json")
                    .OrderBy(f => File.GetCreationTimeUtc(f))
                    .ToList();

                return files;
            }
            catch (Exception e)
            {
                AutoGenLog.Error($"Failed to scan inbox: {e.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// 将 Job 移动到 working 目录
        /// </summary>
        public bool MoveToWorking(string jobFilePath, out string workingPath)
        {
            workingPath = null;

            try
            {
                var fileName = Path.GetFileName(jobFilePath);
                workingPath = Path.Combine(AutoGenSettings.WorkingPath, fileName);

                return PathUtil.SafeMoveFile(jobFilePath, workingPath);
            }
            catch (Exception e)
            {
                AutoGenLog.Error($"Failed to move job to working: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 将 Job 移动到 done 目录
        /// </summary>
        public bool MoveToDone(string workingPath)
        {
            try
            {
                var fileName = Path.GetFileName(workingPath);
                var donePath = Path.Combine(AutoGenSettings.DonePath, fileName);

                return PathUtil.SafeMoveFile(workingPath, donePath);
            }
            catch (Exception e)
            {
                AutoGenLog.Error($"Failed to move job to done: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 将 Job 移动到 dead 目录（死信）
        /// </summary>
        public bool MoveToDead(string filePath)
        {
            try
            {
                var fileName = Path.GetFileName(filePath);
                var deadPath = Path.Combine(AutoGenSettings.DeadPath, fileName);

                return PathUtil.SafeMoveFile(filePath, deadPath);
            }
            catch (Exception e)
            {
                AutoGenLog.Error($"Failed to move job to dead: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 将 Job 移回 inbox（用于 WAITING 状态）
        /// </summary>
        public bool MoveBackToInbox(string workingPath)
        {
            try
            {
                var fileName = Path.GetFileName(workingPath);
                var inboxPath = Path.Combine(AutoGenSettings.InboxPath, fileName);

                return PathUtil.SafeMoveFile(workingPath, inboxPath);
            }
            catch (Exception e)
            {
                AutoGenLog.Error($"Failed to move job back to inbox: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 写入 Job 结果
        /// </summary>
        public void WriteResult(JobResult result)
        {
            if (result == null || string.IsNullOrEmpty(result.jobId))
            {
                AutoGenLog.Error("Cannot write result: null or missing jobId");
                return;
            }

            try
            {
                var resultPath = Path.Combine(AutoGenSettings.ResultsPath, $"{result.jobId}.result.json");
                PathUtil.EnsureDirectoryExists(resultPath);

                File.WriteAllText(resultPath, result.ToJson());
                AutoGenLog.Debug($"Wrote result: {resultPath}");
            }
            catch (Exception e)
            {
                AutoGenLog.Error($"Failed to write result: {e.Message}");
            }
        }

        /// <summary>
        /// 写入 Job 日志
        /// </summary>
        public void WriteLog(JobLogger logger)
        {
            if (logger == null || string.IsNullOrEmpty(logger.JobId))
            {
                return;
            }

            try
            {
                var logPath = Path.Combine(AutoGenSettings.ResultsPath, $"{logger.JobId}.log.txt");
                logger.WriteToFile(logPath);
            }
            catch (Exception e)
            {
                AutoGenLog.Error($"Failed to write log: {e.Message}");
            }
        }

        /// <summary>
        /// 检查 Job 是否已完成（结果文件存在且状态为 DONE）
        /// </summary>
        public bool IsJobCompleted(string jobId)
        {
            if (string.IsNullOrEmpty(jobId)) return false;

            var resultPath = Path.Combine(AutoGenSettings.ResultsPath, $"{jobId}.result.json");
            if (!File.Exists(resultPath)) return false;

            try
            {
                var json = File.ReadAllText(resultPath);
                var result = JobResult.FromJson(json);
                return result?.status == JobStatus.DONE.ToString();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取 working 目录中的 Job 文件（用于崩溃恢复）
        /// </summary>
        public List<string> GetWorkingJobs()
        {
            var workingPath = AutoGenSettings.WorkingPath;
            if (!Directory.Exists(workingPath))
            {
                return new List<string>();
            }

            try
            {
                return Directory.GetFiles(workingPath, "*.job.json").ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// 恢复 working 目录中的 Job 到 inbox
        /// </summary>
        public int RecoverWorkingJobs()
        {
            var workingJobs = GetWorkingJobs();
            int recovered = 0;

            foreach (var job in workingJobs)
            {
                if (MoveBackToInbox(job))
                {
                    recovered++;
                    AutoGenLog.Info($"Recovered job: {Path.GetFileName(job)}");
                }
            }

            return recovered;
        }
    }
}
