using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AutoGenJobs.Core
{
    /// <summary>
    /// Job 的顶层结构
    /// </summary>
    [Serializable]
    public class JobData
    {
        public int schemaVersion = 1;
        public string jobType = "AutoGen";
        public string jobId;
        public string createdAtUtc;
        public int runnerMinVersion = 1;
        public List<string> requiresTypes = new List<string>();
        public string projectWriteRoot = "Assets/AutoGen";
        public bool dryRun = false;
        public List<CommandData> commands = new List<CommandData>();
        public JobMeta meta;

        /// <summary>
        /// 从 JSON 字符串解析 JobData
        /// </summary>
        public static JobData FromJson(string json)
        {
            return JsonConvert.DeserializeObject<JobData>(json, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
        }

        /// <summary>
        /// 转换为 JSON 字符串
        /// </summary>
        public string ToJson(bool prettyPrint = true)
        {
            return JsonConvert.SerializeObject(this, prettyPrint ? Formatting.Indented : Formatting.None);
        }
    }

    /// <summary>
    /// Job 元数据
    /// </summary>
    [Serializable]
    public class JobMeta
    {
        public string author;
        public string note;
    }

    /// <summary>
    /// 单个命令的数据结构
    /// </summary>
    [Serializable]
    public class CommandData
    {
        public string cmd;

        [JsonConverter(typeof(JObjectConverter))]
        public JObject args;

        [JsonConverter(typeof(JObjectConverter))]
        public JObject @out;

        /// <summary>
        /// 获取输出变量名（如果有）
        /// </summary>
        public Dictionary<string, string> GetOutputVars()
        {
            var result = new Dictionary<string, string>();
            if (@out != null)
            {
                foreach (var prop in @out.Properties())
                {
                    result[prop.Name] = prop.Value.ToString();
                }
            }
            return result;
        }
    }

    /// <summary>
    /// 目标引用（用于定位 GameObject、Component 等）
    /// </summary>
    [Serializable]
    public class TargetRef
    {
        public string @ref;           // 变量引用，如 "$go1"
        public string scenePath;      // 场景层级路径
        public string assetGuid;      // Asset GUID
        public string assetPath;      // Asset 路径

        public static TargetRef FromJToken(JToken token)
        {
            if (token == null) return null;

            if (token.Type == JTokenType.Object)
            {
                return token.ToObject<TargetRef>();
            }
            else if (token.Type == JTokenType.String)
            {
                // 如果是字符串，尝试判断是变量引用还是路径
                var str = token.ToString();
                if (str.StartsWith("$"))
                {
                    return new TargetRef { @ref = str };
                }
                else if (str.StartsWith("Assets/"))
                {
                    return new TargetRef { assetPath = str };
                }
                else
                {
                    return new TargetRef { scenePath = str };
                }
            }

            return null;
        }

        public bool IsVariableRef => !string.IsNullOrEmpty(@ref);
        public bool IsScenePath => !string.IsNullOrEmpty(scenePath);
        public bool IsAssetRef => !string.IsNullOrEmpty(assetGuid) || !string.IsNullOrEmpty(assetPath);
    }

    /// <summary>
    /// 值的联合类型（支持多种值格式）
    /// </summary>
    public class ValueUnion
    {
        public JToken RawValue { get; private set; }

        public ValueUnion(JToken value)
        {
            RawValue = value;
        }

        public bool IsNull => RawValue == null || RawValue.Type == JTokenType.Null;
        public bool IsPrimitive => RawValue != null && (RawValue.Type == JTokenType.Integer
                                                         || RawValue.Type == JTokenType.Float
                                                         || RawValue.Type == JTokenType.Boolean
                                                         || RawValue.Type == JTokenType.String);
        public bool IsArray => RawValue?.Type == JTokenType.Array;
        public bool IsObject => RawValue?.Type == JTokenType.Object;

        /// <summary>
        /// 检查是否是变量引用
        /// </summary>
        public bool IsRef => IsObject && RawValue["ref"] != null;

        /// <summary>
        /// 检查是否是 Asset 引用（GUID 或 Path）
        /// </summary>
        public bool IsAssetRef => IsObject && (RawValue["assetGuid"] != null || RawValue["assetPath"] != null);

        /// <summary>
        /// 检查是否是枚举值
        /// </summary>
        public bool IsEnum => IsObject && RawValue["enum"] != null;

        /// <summary>
        /// 获取变量引用名
        /// </summary>
        public string GetRef() => RawValue?["ref"]?.ToString();

        /// <summary>
        /// 获取 Asset GUID
        /// </summary>
        public string GetAssetGuid() => RawValue?["assetGuid"]?.ToString();

        /// <summary>
        /// 获取 Asset Path
        /// </summary>
        public string GetAssetPath() => RawValue?["assetPath"]?.ToString();

        /// <summary>
        /// 获取枚举类型名
        /// </summary>
        public string GetEnumType() => RawValue?["enum"]?.ToString();

        /// <summary>
        /// 获取枚举值名
        /// </summary>
        public string GetEnumName() => RawValue?["name"]?.ToString();

        /// <summary>
        /// 转换为指定类型
        /// </summary>
        public T As<T>() => RawValue.ToObject<T>();

        /// <summary>
        /// 获取为 int
        /// </summary>
        public int AsInt() => RawValue.ToObject<int>();

        /// <summary>
        /// 获取为 float
        /// </summary>
        public float AsFloat() => RawValue.ToObject<float>();

        /// <summary>
        /// 获取为 bool
        /// </summary>
        public bool AsBool() => RawValue.ToObject<bool>();

        /// <summary>
        /// 获取为 string
        /// </summary>
        public string AsString() => RawValue?.ToString();
    }

    /// <summary>
    /// JObject 的自定义 JSON 转换器
    /// </summary>
    public class JObjectConverter : JsonConverter<JObject>
    {
        public override JObject ReadJson(JsonReader reader, Type objectType, JObject existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;
            return JObject.Load(reader);
        }

        public override void WriteJson(JsonWriter writer, JObject value, JsonSerializer serializer)
        {
            value?.WriteTo(writer);
        }
    }
}
