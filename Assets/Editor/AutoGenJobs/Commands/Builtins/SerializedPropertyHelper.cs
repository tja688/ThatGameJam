using System;
using AutoGenJobs.Core;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AutoGenJobs.Commands.Builtins
{
    /// <summary>
    /// SerializedProperty 值设置辅助类
    /// 支持多种值类型：基本类型、枚举、Asset 引用、变量引用、数组等
    /// </summary>
    public static class SerializedPropertyHelper
    {
        /// <summary>
        /// 设置 SerializedProperty 的值
        /// </summary>
        public static bool SetValue(SerializedProperty property, ValueUnion value, CommandContext ctx)
        {
            if (property == null)
            {
                ctx?.Logger.Error("Property is null");
                return false;
            }

            if (value.IsNull)
            {
                return SetNullValue(property, ctx);
            }

            // 变量引用
            if (value.IsRef)
            {
                return SetObjectReference(property, ctx.GetVar(value.GetRef()), ctx);
            }

            // Asset 引用
            if (value.IsAssetRef)
            {
                var asset = Core.AssetRef.Load<Object>(value.GetAssetGuid(), value.GetAssetPath());
                if (asset == null)
                {
                    ctx?.Logger.Warning($"Asset not found: guid={value.GetAssetGuid()}, path={value.GetAssetPath()}");
                    return false;
                }
                return SetObjectReference(property, asset, ctx);
            }

            // 枚举值
            if (value.IsEnum)
            {
                return SetEnumValue(property, value.GetEnumType(), value.GetEnumName(), ctx);
            }

            // 数组
            if (value.IsArray)
            {
                return SetArrayValue(property, value.RawValue as JArray, ctx);
            }

            // 基本类型
            return SetPrimitiveValue(property, value, ctx);
        }

        /// <summary>
        /// 设置 null 值
        /// </summary>
        private static bool SetNullValue(SerializedProperty property, CommandContext ctx)
        {
            if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                property.objectReferenceValue = null;
                return true;
            }

            if (property.propertyType == SerializedPropertyType.String)
            {
                property.stringValue = null;
                return true;
            }

            ctx?.Logger.Warning($"Cannot set null for property type: {property.propertyType}");
            return false;
        }

        /// <summary>
        /// 设置对象引用
        /// </summary>
        private static bool SetObjectReference(SerializedProperty property, Object obj, CommandContext ctx)
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                ctx?.Logger.Error($"Property {property.propertyPath} is not ObjectReference type");
                return false;
            }

            property.objectReferenceValue = obj;
            ctx?.Logger.Debug($"Set {property.propertyPath} = {obj}");
            return true;
        }

        /// <summary>
        /// 设置枚举值
        /// </summary>
        private static bool SetEnumValue(SerializedProperty property, string enumTypeName, string valueName, CommandContext ctx)
        {
            if (property.propertyType != SerializedPropertyType.Enum)
            {
                ctx?.Logger.Error($"Property {property.propertyPath} is not Enum type");
                return false;
            }

            // 尝试通过名称设置
            var names = property.enumNames;
            for (int i = 0; i < names.Length; i++)
            {
                if (string.Equals(names[i], valueName, StringComparison.OrdinalIgnoreCase))
                {
                    property.enumValueIndex = i;
                    ctx?.Logger.Debug($"Set {property.propertyPath} = {valueName}");
                    return true;
                }
            }

            // 如果提供了类型名，尝试解析
            if (!string.IsNullOrEmpty(enumTypeName))
            {
                var enumType = TypeResolver.ResolveType(enumTypeName);
                if (enumType != null && TypeResolver.TryParseEnum(enumType, valueName, out var enumValue))
                {
                    property.intValue = (int)enumValue;
                    ctx?.Logger.Debug($"Set {property.propertyPath} = {valueName} (via type)");
                    return true;
                }
            }

            ctx?.Logger.Warning($"Enum value not found: {valueName}");
            return false;
        }

        /// <summary>
        /// 设置数组值
        /// </summary>
        private static bool SetArrayValue(SerializedProperty property, JArray array, CommandContext ctx)
        {
            if (!property.isArray)
            {
                ctx?.Logger.Error($"Property {property.propertyPath} is not an array");
                return false;
            }

            property.arraySize = array.Count;

            for (int i = 0; i < array.Count; i++)
            {
                var element = property.GetArrayElementAtIndex(i);
                if (!SetValue(element, new ValueUnion(array[i]), ctx))
                {
                    ctx?.Logger.Warning($"Failed to set array element {i}");
                }
            }

            ctx?.Logger.Debug($"Set {property.propertyPath} array with {array.Count} elements");
            return true;
        }

        /// <summary>
        /// 设置基本类型值
        /// </summary>
        private static bool SetPrimitiveValue(SerializedProperty property, ValueUnion value, CommandContext ctx)
        {
            try
            {
                switch (property.propertyType)
                {
                    case SerializedPropertyType.Integer:
                        property.intValue = value.AsInt();
                        break;

                    case SerializedPropertyType.Boolean:
                        property.boolValue = value.AsBool();
                        break;

                    case SerializedPropertyType.Float:
                        property.floatValue = value.AsFloat();
                        break;

                    case SerializedPropertyType.String:
                        property.stringValue = value.AsString();
                        break;

                    case SerializedPropertyType.Color:
                        SetColorValue(property, value);
                        break;

                    case SerializedPropertyType.Vector2:
                        SetVector2Value(property, value);
                        break;

                    case SerializedPropertyType.Vector3:
                        SetVector3Value(property, value);
                        break;

                    case SerializedPropertyType.Vector4:
                        SetVector4Value(property, value);
                        break;

                    case SerializedPropertyType.Rect:
                        SetRectValue(property, value);
                        break;

                    case SerializedPropertyType.Bounds:
                        SetBoundsValue(property, value);
                        break;

                    case SerializedPropertyType.Quaternion:
                        SetQuaternionValue(property, value);
                        break;

                    case SerializedPropertyType.Vector2Int:
                        SetVector2IntValue(property, value);
                        break;

                    case SerializedPropertyType.Vector3Int:
                        SetVector3IntValue(property, value);
                        break;

                    case SerializedPropertyType.LayerMask:
                        property.intValue = value.AsInt();
                        break;

                    // 嵌套对象：递归处理子属性
                    case SerializedPropertyType.Generic:
                        if (value.IsObject)
                        {
                            return SetNestedObject(property, value.RawValue as JObject, ctx);
                        }
                        ctx?.Logger.Warning($"Unsupported value for Generic property: {property.propertyPath}");
                        return false;

                    default:
                        ctx?.Logger.Warning($"Unsupported property type: {property.propertyType} for {property.propertyPath}");
                        return false;
                }

                ctx?.Logger.Debug($"Set {property.propertyPath} = {value.AsString()}");
                return true;
            }
            catch (Exception e)
            {
                ctx?.Logger.Error($"Failed to set property {property.propertyPath}: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 设置嵌套对象的值
        /// </summary>
        private static bool SetNestedObject(SerializedProperty property, JObject obj, CommandContext ctx)
        {
            if (obj == null) return false;

            foreach (var prop in obj.Properties())
            {
                var childProp = property.FindPropertyRelative(prop.Name);
                if (childProp == null)
                {
                    ctx?.Logger.Warning($"Child property not found: {prop.Name}");
                    continue;
                }

                if (!SetValue(childProp, new ValueUnion(prop.Value), ctx))
                {
                    ctx?.Logger.Warning($"Failed to set child property: {prop.Name}");
                }
            }

            return true;
        }

        // Vector/Color/Rect 等复合类型的设置方法
        private static void SetColorValue(SerializedProperty property, ValueUnion value)
        {
            if (value.RawValue.Type == JTokenType.Array)
            {
                var arr = value.As<float[]>();
                property.colorValue = new Color(
                    arr.Length > 0 ? arr[0] : 0,
                    arr.Length > 1 ? arr[1] : 0,
                    arr.Length > 2 ? arr[2] : 0,
                    arr.Length > 3 ? arr[3] : 1
                );
            }
            else if (value.RawValue.Type == JTokenType.Object)
            {
                var obj = value.RawValue as JObject;
                property.colorValue = new Color(
                    obj["r"]?.ToObject<float>() ?? 0,
                    obj["g"]?.ToObject<float>() ?? 0,
                    obj["b"]?.ToObject<float>() ?? 0,
                    obj["a"]?.ToObject<float>() ?? 1
                );
            }
        }

        private static void SetVector2Value(SerializedProperty property, ValueUnion value)
        {
            if (value.RawValue.Type == JTokenType.Array)
            {
                var arr = value.As<float[]>();
                property.vector2Value = new Vector2(
                    arr.Length > 0 ? arr[0] : 0,
                    arr.Length > 1 ? arr[1] : 0
                );
            }
            else if (value.RawValue.Type == JTokenType.Object)
            {
                var obj = value.RawValue as JObject;
                property.vector2Value = new Vector2(
                    obj["x"]?.ToObject<float>() ?? 0,
                    obj["y"]?.ToObject<float>() ?? 0
                );
            }
        }

        private static void SetVector3Value(SerializedProperty property, ValueUnion value)
        {
            if (value.RawValue.Type == JTokenType.Array)
            {
                var arr = value.As<float[]>();
                property.vector3Value = new Vector3(
                    arr.Length > 0 ? arr[0] : 0,
                    arr.Length > 1 ? arr[1] : 0,
                    arr.Length > 2 ? arr[2] : 0
                );
            }
            else if (value.RawValue.Type == JTokenType.Object)
            {
                var obj = value.RawValue as JObject;
                property.vector3Value = new Vector3(
                    obj["x"]?.ToObject<float>() ?? 0,
                    obj["y"]?.ToObject<float>() ?? 0,
                    obj["z"]?.ToObject<float>() ?? 0
                );
            }
        }

        private static void SetVector4Value(SerializedProperty property, ValueUnion value)
        {
            if (value.RawValue.Type == JTokenType.Array)
            {
                var arr = value.As<float[]>();
                property.vector4Value = new Vector4(
                    arr.Length > 0 ? arr[0] : 0,
                    arr.Length > 1 ? arr[1] : 0,
                    arr.Length > 2 ? arr[2] : 0,
                    arr.Length > 3 ? arr[3] : 0
                );
            }
            else if (value.RawValue.Type == JTokenType.Object)
            {
                var obj = value.RawValue as JObject;
                property.vector4Value = new Vector4(
                    obj["x"]?.ToObject<float>() ?? 0,
                    obj["y"]?.ToObject<float>() ?? 0,
                    obj["z"]?.ToObject<float>() ?? 0,
                    obj["w"]?.ToObject<float>() ?? 0
                );
            }
        }

        private static void SetRectValue(SerializedProperty property, ValueUnion value)
        {
            if (value.RawValue.Type == JTokenType.Array)
            {
                var arr = value.As<float[]>();
                property.rectValue = new Rect(
                    arr.Length > 0 ? arr[0] : 0,
                    arr.Length > 1 ? arr[1] : 0,
                    arr.Length > 2 ? arr[2] : 0,
                    arr.Length > 3 ? arr[3] : 0
                );
            }
            else if (value.RawValue.Type == JTokenType.Object)
            {
                var obj = value.RawValue as JObject;
                property.rectValue = new Rect(
                    obj["x"]?.ToObject<float>() ?? 0,
                    obj["y"]?.ToObject<float>() ?? 0,
                    obj["width"]?.ToObject<float>() ?? 0,
                    obj["height"]?.ToObject<float>() ?? 0
                );
            }
        }

        private static void SetBoundsValue(SerializedProperty property, ValueUnion value)
        {
            if (value.RawValue.Type == JTokenType.Object)
            {
                var obj = value.RawValue as JObject;
                var centerArr = obj["center"]?.ToObject<float[]>() ?? new float[3];
                var sizeArr = obj["size"]?.ToObject<float[]>() ?? new float[3];

                property.boundsValue = new Bounds(
                    new Vector3(centerArr[0], centerArr[1], centerArr[2]),
                    new Vector3(sizeArr[0], sizeArr[1], sizeArr[2])
                );
            }
        }

        private static void SetQuaternionValue(SerializedProperty property, ValueUnion value)
        {
            if (value.RawValue.Type == JTokenType.Array)
            {
                var arr = value.As<float[]>();
                property.quaternionValue = new Quaternion(
                    arr.Length > 0 ? arr[0] : 0,
                    arr.Length > 1 ? arr[1] : 0,
                    arr.Length > 2 ? arr[2] : 0,
                    arr.Length > 3 ? arr[3] : 1
                );
            }
            else if (value.RawValue.Type == JTokenType.Object)
            {
                var obj = value.RawValue as JObject;
                property.quaternionValue = new Quaternion(
                    obj["x"]?.ToObject<float>() ?? 0,
                    obj["y"]?.ToObject<float>() ?? 0,
                    obj["z"]?.ToObject<float>() ?? 0,
                    obj["w"]?.ToObject<float>() ?? 1
                );
            }
        }

        private static void SetVector2IntValue(SerializedProperty property, ValueUnion value)
        {
            if (value.RawValue.Type == JTokenType.Array)
            {
                var arr = value.As<int[]>();
                property.vector2IntValue = new Vector2Int(
                    arr.Length > 0 ? arr[0] : 0,
                    arr.Length > 1 ? arr[1] : 0
                );
            }
            else if (value.RawValue.Type == JTokenType.Object)
            {
                var obj = value.RawValue as JObject;
                property.vector2IntValue = new Vector2Int(
                    obj["x"]?.ToObject<int>() ?? 0,
                    obj["y"]?.ToObject<int>() ?? 0
                );
            }
        }

        private static void SetVector3IntValue(SerializedProperty property, ValueUnion value)
        {
            if (value.RawValue.Type == JTokenType.Array)
            {
                var arr = value.As<int[]>();
                property.vector3IntValue = new Vector3Int(
                    arr.Length > 0 ? arr[0] : 0,
                    arr.Length > 1 ? arr[1] : 0,
                    arr.Length > 2 ? arr[2] : 0
                );
            }
            else if (value.RawValue.Type == JTokenType.Object)
            {
                var obj = value.RawValue as JObject;
                property.vector3IntValue = new Vector3Int(
                    obj["x"]?.ToObject<int>() ?? 0,
                    obj["y"]?.ToObject<int>() ?? 0,
                    obj["z"]?.ToObject<int>() ?? 0
                );
            }
        }
    }
}
