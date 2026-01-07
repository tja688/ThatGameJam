#!/usr/bin/env python3
"""
Unity AutoGen Job System - Python SDK

这个脚本提供了与 Unity AutoGen Job System 交互的 Python API。
AI Agent 可以直接导入和使用这些函数来执行 Unity 操作。

使用示例:
    from autogen_unity import UnityJobClient
    
    client = UnityJobClient()
    
    # 创建 GameObject
    result = client.create_gameobject("MyObject", position=[0, 5, 0])
    
    # 创建 Prefab
    result = client.create_prefab("Enemy", [
        client.cmd_add_component("$prefabRoot", "SpriteRenderer")
    ])
"""

import os
import json
import time
import uuid
from datetime import datetime
from pathlib import Path
from typing import Any, Dict, List, Optional, Union


class UnityJobClient:
    """Unity AutoGen Job System 客户端"""
    
    def __init__(self, project_root: str = "."):
        """
        初始化客户端
        
        Args:
            project_root: Unity 项目根目录
        """
        self.project_root = Path(project_root).resolve()
        self.jobs_root = self.project_root / "AutoGenJobs"
        self.inbox_path = self.jobs_root / "inbox"
        self.results_path = self.jobs_root / "results"
        
        # 确保目录存在
        self.inbox_path.mkdir(parents=True, exist_ok=True)
        self.results_path.mkdir(parents=True, exist_ok=True)
    
    def generate_job_id(self, prefix: str = "job") -> str:
        """生成唯一的 Job ID"""
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        short_uuid = str(uuid.uuid4())[:8]
        return f"{prefix}_{timestamp}_{short_uuid}"
    
    def submit_job(self, job_data: Dict[str, Any]) -> str:
        """
        提交 Job 到 Unity
        
        Args:
            job_data: Job 数据字典
            
        Returns:
            Job ID
        """
        job_id = job_data.get("jobId") or self.generate_job_id()
        job_data["jobId"] = job_id
        job_data.setdefault("schemaVersion", 1)
        job_data.setdefault("projectWriteRoot", "Assets/AutoGen")
        job_data.setdefault("createdAtUtc", datetime.utcnow().isoformat() + "Z")
        
        pending_file = self.inbox_path / f"{job_id}.job.json.pending"
        final_file = self.inbox_path / f"{job_id}.job.json"
        
        # 原子写入
        with open(pending_file, 'w', encoding='utf-8') as f:
            json.dump(job_data, f, indent=2, ensure_ascii=False)
            f.flush()
            os.fsync(f.fileno())
        
        os.rename(pending_file, final_file)
        
        return job_id
    
    def wait_for_result(self, job_id: str, timeout: float = 30.0, 
                        poll_interval: float = 0.5) -> Optional[Dict[str, Any]]:
        """
        等待 Job 完成并返回结果
        
        Args:
            job_id: Job ID
            timeout: 超时时间（秒）
            poll_interval: 轮询间隔（秒）
            
        Returns:
            结果字典，超时返回 None
        """
        result_file = self.results_path / f"{job_id}.result.json"
        start_time = time.time()
        
        while time.time() - start_time < timeout:
            if result_file.exists():
                with open(result_file, 'r', encoding='utf-8') as f:
                    result = json.load(f)
                
                status = result.get("status", "UNKNOWN")
                if status in ("DONE", "FAILED"):
                    return result
            
            time.sleep(poll_interval)
        
        return None
    
    def execute(self, commands: List[Dict[str, Any]], 
                requires_types: Optional[List[str]] = None,
                timeout: float = 30.0) -> Dict[str, Any]:
        """
        执行命令并等待结果
        
        Args:
            commands: 命令列表
            requires_types: 需要的类型列表
            timeout: 超时时间
            
        Returns:
            执行结果
        """
        job_data = {
            "commands": commands
        }
        if requires_types:
            job_data["requiresTypes"] = requires_types
        
        job_id = self.submit_job(job_data)
        result = self.wait_for_result(job_id, timeout)
        
        if result is None:
            return {
                "status": "TIMEOUT",
                "jobId": job_id,
                "message": f"Timeout after {timeout}s"
            }
        
        return result
    
    # ==================== 命令构建器 ====================
    
    @staticmethod
    def cmd_create_gameobject(name: str, 
                               parent_path: Optional[str] = None,
                               position: Optional[List[float]] = None,
                               rotation: Optional[List[float]] = None,
                               scale: Optional[List[float]] = None,
                               ensure: bool = True,
                               out_var: str = "$go") -> Dict[str, Any]:
        """创建 CreateGameObject 命令"""
        args = {"name": name, "ensure": ensure}
        if parent_path:
            args["parentPath"] = parent_path
        if position:
            args["position"] = position
        if rotation:
            args["rotation"] = rotation
        if scale:
            args["scale"] = scale
        
        return {
            "cmd": "CreateGameObject",
            "args": args,
            "out": {"go": out_var}
        }
    
    @staticmethod
    def cmd_add_component(target_ref: str, 
                          component_type: str,
                          if_missing: bool = True,
                          out_var: str = "$component") -> Dict[str, Any]:
        """创建 AddComponent 命令"""
        return {
            "cmd": "AddComponent",
            "args": {
                "target": {"ref": target_ref},
                "type": component_type,
                "ifMissing": if_missing
            },
            "out": {"component": out_var}
        }
    
    @staticmethod
    def cmd_set_property(target_ref: str, 
                         property_path: str, 
                         value: Any) -> Dict[str, Any]:
        """创建 SetSerializedProperty 命令"""
        return {
            "cmd": "SetSerializedProperty",
            "args": {
                "target": {"ref": target_ref},
                "propertyPath": property_path,
                "value": value
            }
        }
    
    @staticmethod
    def cmd_set_transform(target_ref: str,
                          position: Optional[List[float]] = None,
                          rotation: Optional[List[float]] = None,
                          scale: Optional[List[float]] = None,
                          space: str = "local") -> Dict[str, Any]:
        """创建 SetTransform 命令"""
        args = {"target": {"ref": target_ref}, "space": space}
        if position:
            args["position"] = position
        if rotation:
            args["rotation"] = rotation
        if scale:
            args["scale"] = scale
        
        return {"cmd": "SetTransform", "args": args}
    
    @staticmethod
    def cmd_create_so(so_type: str,
                      asset_path: str,
                      init: Optional[Dict[str, Any]] = None,
                      overwrite: bool = False,
                      out_var: str = "$asset") -> Dict[str, Any]:
        """创建 CreateScriptableObject 命令"""
        args = {
            "type": so_type,
            "assetPath": asset_path,
            "overwrite": overwrite
        }
        if init:
            args["init"] = init
        
        return {
            "cmd": "CreateScriptableObject",
            "args": args,
            "out": {"asset": out_var}
        }
    
    @staticmethod
    def cmd_create_prefab(prefab_path: str,
                          root_name: Optional[str] = None,
                          edits: Optional[List[Dict[str, Any]]] = None,
                          out_var: str = "$prefab") -> Dict[str, Any]:
        """创建 CreateOrEditPrefab 命令"""
        args = {"prefabPath": prefab_path}
        if root_name:
            args["rootName"] = root_name
        if edits:
            args["edits"] = edits
        
        return {
            "cmd": "CreateOrEditPrefab",
            "args": args,
            "out": {"prefab": out_var}
        }
    
    @staticmethod
    def cmd_instantiate(prefab_path: str,
                        name_override: Optional[str] = None,
                        parent_path: Optional[str] = None,
                        position: Optional[List[float]] = None,
                        ensure: bool = True,
                        out_var: str = "$instance") -> Dict[str, Any]:
        """创建 InstantiatePrefabInScene 命令"""
        args = {"prefabPath": prefab_path, "ensure": ensure}
        if name_override:
            args["nameOverride"] = name_override
        if parent_path:
            args["parentPath"] = parent_path
        if position:
            args["position"] = position
        
        return {
            "cmd": "InstantiatePrefabInScene",
            "args": args,
            "out": {"instance": out_var}
        }
    
    @staticmethod
    def cmd_save_assets(refresh: bool = True) -> Dict[str, Any]:
        """创建 SaveAssets 命令"""
        return {"cmd": "SaveAssets", "args": {"refresh": refresh}}
    
    # ==================== 便捷方法 ====================
    
    def create_gameobject(self, name: str, **kwargs) -> Dict[str, Any]:
        """创建 GameObject 并等待结果"""
        cmd = self.cmd_create_gameobject(name, **kwargs)
        return self.execute([cmd])
    
    def create_prefab(self, prefab_name: str, 
                      edits: Optional[List[Dict[str, Any]]] = None,
                      **kwargs) -> Dict[str, Any]:
        """创建 Prefab 并等待结果"""
        prefab_path = f"Assets/AutoGen/Prefabs/{prefab_name}.prefab"
        cmd = self.cmd_create_prefab(prefab_path, prefab_name, edits, **kwargs)
        return self.execute([cmd, self.cmd_save_assets()])
    
    def create_config(self, so_type: str, config_name: str,
                      init: Optional[Dict[str, Any]] = None) -> Dict[str, Any]:
        """创建 ScriptableObject 配置并等待结果"""
        asset_path = f"Assets/AutoGen/Configs/{config_name}.asset"
        cmd = self.cmd_create_so(so_type, asset_path, init)
        return self.execute([cmd, self.cmd_save_assets()], 
                           requires_types=[so_type])
    
    def instantiate_in_scene(self, prefab_path: str, 
                              name: Optional[str] = None,
                              position: List[float] = None) -> Dict[str, Any]:
        """实例化 Prefab 到场景"""
        cmd = self.cmd_instantiate(prefab_path, name, position=position)
        return self.execute([cmd])


# ==================== 值构建器 ====================

class Value:
    """值类型构建器"""
    
    @staticmethod
    def ref(var_name: str) -> Dict[str, str]:
        """变量引用"""
        return {"ref": var_name}
    
    @staticmethod
    def asset_path(path: str) -> Dict[str, str]:
        """资产路径引用"""
        return {"assetPath": path}
    
    @staticmethod
    def asset_guid(guid: str) -> Dict[str, str]:
        """资产 GUID 引用"""
        return {"assetGuid": guid}
    
    @staticmethod
    def null() -> Dict[str, bool]:
        """空值"""
        return {"null": True}
    
    @staticmethod
    def enum(enum_type: str, value_name: str) -> Dict[str, str]:
        """枚举值"""
        return {"enum": enum_type, "name": value_name}
    
    @staticmethod
    def color(r: float, g: float, b: float, a: float = 1.0) -> List[float]:
        """颜色值"""
        return [r, g, b, a]
    
    @staticmethod
    def vector3(x: float, y: float, z: float) -> List[float]:
        """Vector3 值"""
        return [x, y, z]
    
    @staticmethod
    def vector2(x: float, y: float) -> List[float]:
        """Vector2 值"""
        return [x, y]


# ==================== CLI 入口 ====================

def main():
    import argparse
    
    parser = argparse.ArgumentParser(description="Unity AutoGen Job CLI")
    parser.add_argument("--project", "-p", default=".", 
                        help="Unity project root path")
    
    subparsers = parser.add_subparsers(dest="command")
    
    # status 命令
    subparsers.add_parser("status", help="Show runner status")
    
    # submit 命令
    submit_parser = subparsers.add_parser("submit", help="Submit job file")
    submit_parser.add_argument("file", help="Job JSON file")
    
    # check 命令
    check_parser = subparsers.add_parser("check", help="Check job result")
    check_parser.add_argument("job_id", help="Job ID")
    
    args = parser.parse_args()
    client = UnityJobClient(args.project)
    
    if args.command == "status":
        print(f"Project: {client.project_root}")
        print(f"Inbox:   {client.inbox_path}")
        print(f"Results: {client.results_path}")
        
        inbox_count = len(list(client.inbox_path.glob("*.job.json")))
        print(f"\nPending jobs: {inbox_count}")
    
    elif args.command == "submit":
        with open(args.file, 'r', encoding='utf-8') as f:
            job_data = json.load(f)
        job_id = client.submit_job(job_data)
        print(f"Submitted: {job_id}")
    
    elif args.command == "check":
        result_file = client.results_path / f"{args.job_id}.result.json"
        if result_file.exists():
            with open(result_file, 'r', encoding='utf-8') as f:
                result = json.load(f)
            print(json.dumps(result, indent=2))
        else:
            print(f"Result not found: {args.job_id}")
    
    else:
        parser.print_help()


if __name__ == "__main__":
    main()
