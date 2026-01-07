using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace AutoGenJobs.Core
{
    /// <summary>
    /// Job 执行状态
    /// </summary>
    public enum JobStatus
    {
        DONE,
        FAILED,
        WAITING
    }

    /// <summary>
    /// 等待原因
    /// </summary>
    public enum WaitingReason
    {
        None,
        WAITING_COMPILING,
        WAITING_RUNNER_VERSION,
        WAITING_TYPES_MISSING
    }

    /// <summary>
    /// Job 执行结果
    /// </summary>
    [Serializable]
    public class JobResult
    {
        public int schemaVersion = 1;
        public string jobId;
        public string status;
        public string startedAtUtc;
        public string finishedAtUtc;
        public int runnerVersion = AutoGenSettings.RUNNER_VERSION;
        public string unityVersion = Application.unityVersion;
        public string message;
        public List<CommandResult> commandResults = new List<CommandResult>();
        public JobError error;
        public string waitingReason;

        public JobResult() { }

        public JobResult(string jobId)
        {
            this.jobId = jobId;
            this.startedAtUtc = DateTime.UtcNow.ToString("o");
        }

        /// <summary>
        /// 设置为完成状态
        /// </summary>
        public void SetDone(string msg = "ok")
        {
            status = JobStatus.DONE.ToString();
            message = msg;
            finishedAtUtc = DateTime.UtcNow.ToString("o");
        }

        /// <summary>
        /// 设置为失败状态
        /// </summary>
        public void SetFailed(string errorCode, string errorMessage, string stack = null)
        {
            status = JobStatus.FAILED.ToString();
            message = errorMessage;
            error = new JobError
            {
                code = errorCode,
                message = errorMessage,
                stack = stack
            };
            finishedAtUtc = DateTime.UtcNow.ToString("o");
        }

        /// <summary>
        /// 设置为等待状态
        /// </summary>
        public void SetWaiting(WaitingReason reason, string msg = null)
        {
            status = JobStatus.WAITING.ToString();
            waitingReason = reason.ToString();
            message = msg ?? reason.ToString();
            finishedAtUtc = DateTime.UtcNow.ToString("o");
        }

        /// <summary>
        /// 添加命令结果
        /// </summary>
        public void AddCommandResult(CommandResult result)
        {
            commandResults.Add(result);
        }

        /// <summary>
        /// 转换为 JSON 字符串
        /// </summary>
        public string ToJson(bool prettyPrint = true)
        {
            return JsonConvert.SerializeObject(this, prettyPrint ? Formatting.Indented : Formatting.None,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        }

        /// <summary>
        /// 从 JSON 字符串解析
        /// </summary>
        public static JobResult FromJson(string json)
        {
            return JsonConvert.DeserializeObject<JobResult>(json);
        }
    }

    /// <summary>
    /// 单个命令的执行结果
    /// </summary>
    [Serializable]
    public class CommandResult
    {
        public int index;
        public string cmd;
        public string status;
        public string message;
        public Dictionary<string, string> outputs;

        public CommandResult() { }

        public CommandResult(int index, string cmd)
        {
            this.index = index;
            this.cmd = cmd;
        }

        /// <summary>
        /// 设置为成功
        /// </summary>
        public void SetDone(Dictionary<string, string> outputVars = null, string msg = null)
        {
            status = "DONE";
            message = msg;
            outputs = outputVars;
        }

        /// <summary>
        /// 设置为失败
        /// </summary>
        public void SetFailed(string msg)
        {
            status = "FAILED";
            message = msg;
        }

        /// <summary>
        /// 设置为跳过
        /// </summary>
        public void SetSkipped(string msg = null)
        {
            status = "SKIPPED";
            message = msg;
        }
    }

    /// <summary>
    /// Job 错误信息
    /// </summary>
    [Serializable]
    public class JobError
    {
        public string code;
        public string message;
        public string stack;
    }

    /// <summary>
    /// 命令执行结果（内部使用）
    /// </summary>
    public class CommandExecResult
    {
        public bool Success { get; private set; }
        public string Message { get; private set; }
        public Dictionary<string, UnityEngine.Object> Outputs { get; private set; }
        public Exception Exception { get; private set; }

        private CommandExecResult() { }

        /// <summary>
        /// 创建成功结果
        /// </summary>
        public static CommandExecResult Ok(string message = null, Dictionary<string, UnityEngine.Object> outputs = null)
        {
            return new CommandExecResult
            {
                Success = true,
                Message = message,
                Outputs = outputs ?? new Dictionary<string, UnityEngine.Object>()
            };
        }

        /// <summary>
        /// 创建失败结果
        /// </summary>
        public static CommandExecResult Fail(string message, Exception ex = null)
        {
            return new CommandExecResult
            {
                Success = false,
                Message = message,
                Exception = ex,
                Outputs = new Dictionary<string, UnityEngine.Object>()
            };
        }

        /// <summary>
        /// 将输出转换为字符串表示
        /// </summary>
        public Dictionary<string, string> GetOutputStrings()
        {
            var result = new Dictionary<string, string>();
            if (Outputs != null)
            {
                foreach (var kvp in Outputs)
                {
                    if (kvp.Value != null)
                    {
                        if (kvp.Value is GameObject go)
                        {
                            result[kvp.Key] = $"SceneObject:{go.name}";
                        }
                        else if (kvp.Value is Component comp)
                        {
                            result[kvp.Key] = $"Component:{comp.GetType().Name}@{comp.gameObject.name}";
                        }
                        else
                        {
                            var path = UnityEditor.AssetDatabase.GetAssetPath(kvp.Value);
                            if (!string.IsNullOrEmpty(path))
                            {
                                result[kvp.Key] = $"Asset:{path}";
                            }
                            else
                            {
                                result[kvp.Key] = kvp.Value.ToString();
                            }
                        }
                    }
                }
            }
            return result;
        }
    }
}
