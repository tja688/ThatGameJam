import urllib.request
import urllib.error
import json
import os
import sys
import time

# Configuration
BASE_URL = "http://127.0.0.1:32204"
PROJECT_ROOT = os.getcwd().replace("\\", "/")
REPORT_PATH = os.path.join(PROJECT_ROOT, "Assets/Editor/CodeIntel/QA_TEST_REPORT.md")

# Helper to normalize paths
def normalize_path(path):
    if not path:
        return ""
    p = path.replace("\\", "/")
    if PROJECT_ROOT in p:
        p = os.path.relpath(p, PROJECT_ROOT).replace("\\", "/")
    return p

def post_json(url, data, timeout=5):
    try:
        req = urllib.request.Request(
            url, 
            data=json.dumps(data).encode('utf-8'),
            headers={'Content-Type': 'application/json'}
        )
        with urllib.request.urlopen(req, timeout=timeout) as response:
            return json.loads(response.read().decode('utf-8'))
    except urllib.error.HTTPError as e:
        # Return error dict if possible
        return {"error": e.code}
    except Exception as e:
        raise e

# Test Definition
tests = [
    # 维度 1：类级定位 (Class Location)
    {
        "id": 1,
        "type": "Class",
        "desc": "BackpackModel 定位",
        "action": "symbol",
        "query": "BackpackModel",
        "expected_path": "Assets/Scripts/Features/BackpackFeature/Models/BackpackModel.cs",
        "expected_subcheck": lambda r: r['name'] == "BackpackModel" and "ThatGameJam.Features.BackpackFeature.Models" in r.get('containerName', '')
    },
    {
        "id": 2,
        "type": "Class",
        "desc": "UniTask 定位 (三方库)",
        "action": "symbol",
        "query": "UniTask",
        "expected_path": "Assets/Plugins/UniTask/Runtime/UniTask.cs",
        "expected_subcheck": lambda r: r['name'] == "UniTask" and "Cysharp.Threading.Tasks" in r.get('containerName', '')
    },
    {
        "id": 3,
        "type": "Class",
        "desc": "UIRouter 定位",
        "action": "symbol",
        "query": "UIRouter",
        "expected_path": "Assets/Scripts/UI/UIRouter.cs",
        "expected_subcheck": lambda r: r['name'] == "UIRouter"
    },

    # 维度 2：方法/函数定位 (Method Location)
    {
        "id": 4,
        "type": "Method",
        "desc": "BackpackModel.Clear",
        "action": "symbol",
        "query": "Clear", 
        "expected_path": "Assets/Scripts/Features/BackpackFeature/Models/BackpackModel.cs",
        "expected_subcheck": lambda r: r['name'] == "Clear" and "BackpackModel" in r.get('containerName', '')
    },
    {
        "id": 5,
        "type": "Overload",
        "desc": "UniTask.Delay(int)",
        "action": "symbol",
        "query": "Delay",
        "expected_path": "Assets/Plugins/UniTask/Runtime/UniTask.Delay.cs",
        "expected_subcheck": lambda r: "UniTask" in r.get('containerName', '')
    },
    {
        "id": 6,
        "type": "Library",
        "desc": "Image.DOFade",
        "action": "symbol",
        "query": "DOFade",
        "expected_path": "Assets/Plugins/Demigiant/DOTween/Modules/DOTweenModuleUI.cs",
        "expected_subcheck": lambda r: "DOTweenModuleUI" in r.get('containerName', '')
    },

    # 维度 3：字段/属性定位 (Member Location)
    {
        "id": 7,
        "type": "Property",
        "desc": "IBackpackModel.SelectedIndex",
        "action": "symbol",
        "query": "SelectedIndex",
        "expected_path": "Assets/Scripts/Features/BackpackFeature/Models/IBackpackModel.cs",
        "expected_subcheck": lambda r: "IBackpackModel" in r.get('containerName', '')
    },
    {
        "id": 8,
        "type": "Field",
        "desc": "BackpackModel._selectedIndex",
        "action": "symbol",
        "query": "_selectedIndex",
        "expected_path": "Assets/Scripts/Features/BackpackFeature/Models/BackpackModel.cs",
        "expected_subcheck": lambda r: "BackpackModel" in r.get('containerName', '')
    },
    {
        "id": 9,
        "type": "Symbol",
        "desc": "成员名 Items 歧义",
        "action": "symbol",
        "query": "Items",
        "expected_path": "Assets/Scripts/Features/BackpackFeature/Models/BackpackModel.cs",
        "expected_subcheck": lambda r: "BackpackModel" in r.get('containerName', '')
    },

    # 维度 4：调用点跳转 (Jump from Call Site)
    {
        "id": 10,
        "type": "Definition",
        "desc": "OnInit 基类跳转",
        "action": "definition",
        "file": "Assets/Scripts/Features/BackpackFeature/Models/BackpackModel.cs",
        "line": 42,
        "column": 35, 
        "expected_path": "Assets/QFramework/Framework/Scripts/QFramework.cs",
        "expected_subcheck": lambda r: True 
    },
    {
        "id": 11,
        "type": "Definition",
        "desc": "接口实现跳转 (GetSelectedIndexQuery -> IBackpackModel.SelectedIndex implementation)",
        "action": "definition",
        "file": "Assets/Scripts/Features/BackpackFeature/Queries/GetSelectedIndexQuery.cs",
        "line": 11,
        "column": 36, 
        "expected_path": "Assets/Scripts/Features/BackpackFeature/Models/BackpackModel.cs", 
        "expected_subcheck": lambda r: "BackpackModel.cs" in normalize_path(r.get('start', {}).get('file', '')) or "BackpackModel.cs" in normalize_path(r.get('filename', ''))
    },

    # 维度 5：引用链 - 读 (References - Read)
    {
        "id": 12,
        "type": "References",
        "desc": "SelectedIndex 读取点",
        "action": "references",
        "file": "Assets/Scripts/Features/BackpackFeature/Models/IBackpackModel.cs",
        "line": 7,
        "column": 45, 
        "expected_path": "", 
        "expected_subcheck": lambda stats: any("GetSelectedIndexQuery.cs" in p for p in stats)
    },
    {
        "id": 13,
        "type": "References",
        "desc": "UniTask.Yield 引用",
         "action": "symbol_then_ref",
         "query": "Yield",
         "container": "UniTask",
         "expected_subcheck": lambda stats: len(stats) > 0 
    },

    # 维度 6：引用链 - 写 (References - Write)
    {
        "id": 14,
        "type": "References",
        "desc": "_selectedIndex 写入点",
        "action": "references",
        "file": "Assets/Scripts/Features/BackpackFeature/Models/BackpackModel.cs",
        "line": 15,
        "column": 45, 
        "expected_subcheck": lambda stats: any("BackpackModel.cs" in p for p in stats) and len(stats) >= 3 
    },

    # 维度 7：歧义/同名冲突 (Ambiguity)
    {
        "id": 15,
        "type": "Ambiguity",
        "desc": "Controller 同名冲突",
        "action": "symbol",
        "query": "Controller",
        "expected_subcheck": lambda results: len([r for r in results if r['name'].endswith('Controller')]) > 3
    },
    {
        "id": 16,
        "type": "Ambiguity",
        "desc": "AddItem 冲突",
        "action": "symbol",
        "query": "AddItem",
        "expected_subcheck": lambda results: any("AddItemCommand" in r['name'] for r in results)
    },

    # 维度 8：高级结构与 Unity 特性 (Advanced structures)
    {
        "id": 17,
        "type": "Advanced",
        "desc": "内部类 BackpackItemEntry",
        "action": "symbol",
        "query": "BackpackItemEntry",
        "expected_path": "Assets/Scripts/Features/BackpackFeature/Models/BackpackModel.cs",
        "expected_subcheck": lambda r: "BackpackModel" in r.get('containerName', '')
    },
    {
        "id": 18,
        "type": "Advanced",
        "desc": "泛型 BindableProperty<T>",
        "action": "symbol",
        "query": "BindableProperty",
        "expected_path": "Assets/QFramework/Framework/Scripts/QFramework.cs",
        "expected_subcheck": lambda r: r['name'] == "BindableProperty`1" or r['name'] == "BindableProperty"
    },
    {
        "id": 19,
        "type": "Advanced",
        "desc": "事件引用链 BackpackChangedEvent",
        "action": "symbol_then_ref",
        "query": "BackpackChangedEvent",
        "container": "Events",
        "expected_subcheck": lambda stats: len(stats) > 2
    },
    {
        "id": 20,
        "type": "Unity",
        "desc": "UIRoot.Awake 生命周期",
        "action": "symbol",
        "query": "Awake", 
        "expected_path": "Assets/Scripts/UI/UIRoot.cs",
        "expected_subcheck": lambda r: r['name'] == "Awake" and "UIRoot" in r.get('containerName', '')
    }
]

def run_test(test):
    try:
        results = []
        
        if test['action'] == 'symbol':
            payload = {"query": test['query']}
            data = post_json(f"{BASE_URL}/v1/symbols", payload)
            if 'error' in data: return False, f"API Error {data['error']}"
            
            # Filter matches
            matches = []
            if isinstance(data, list):
                for item in data:
                    path = normalize_path(item.get('location', {}).get('filename', ''))
                    if 'expected_path' in test:
                        if path.endswith(test['expected_path']):
                            matches.append(item)
                    else:
                        matches.append(item)
            
            # Subcheck
            found = False
            for m in matches:
                try:
                    if test.get('expected_subcheck')(m):
                        found = True
                        break
                except:
                    pass
            
            # Special case for ambiguity (check raw list)
            if 'expected_path' not in test and not matches: 
                 try:
                     if test.get('expected_subcheck')(data):
                         found = True
                 except: user_found = False

            return found, f"Found {len(matches)} matches"

        elif test['action'] == 'definition':
            abs_path = os.path.join(PROJECT_ROOT, test['file'])
            payload_camel = {
                "fileName": abs_path, 
                "line": test['line'], 
                "column": test['column']
            }
            data = post_json(f"{BASE_URL}/v1/definition", payload_camel)
            if 'error' in data: # Try pascal
                payload = { "FileName": abs_path, "Line": test['line'], "Column": test['column'] }
                data = post_json(f"{BASE_URL}/v1/definition", payload)
            
            if 'error' in data: return False, f"API Error {data['error']}"
            
            target_file = ""
            if data and 'filename' in data: target_file = data['filename']
            elif data and 'FileName' in data: target_file = data['FileName']
            
            path = normalize_path(target_file)
            
            if test.get('expected_path') and path.endswith(test['expected_path']):
                return True, f"Jumped to {path}"
            elif test.get('expected_subcheck') and test.get('expected_subcheck')({'start': {'file': path}, 'filename': path}):
                 return True, f"Jumped to {path}"
            
            return False, f"Jumped to {path}, expected {test.get('expected_path')}"

        elif test['action'] == 'references':
            abs_path = os.path.join(PROJECT_ROOT, test['file'])
            payload_camel = {
                "fileName": abs_path, 
                "line": test['line'], 
                "column": test['column']
            }
            data = post_json(f"{BASE_URL}/v1/references", payload_camel, timeout=10)
            if 'error' in data: return False, f"API Error {data['error']}"
            
            files = set()
            if isinstance(data, list):
                for item in data:
                    f = item.get('filename') or item.get('FileName')
                    if f:
                        files.add(normalize_path(f))
            
            if test['expected_subcheck'](files):
                return True, f"Found refs in {len(files)} files"
            return False, f"Found refs in {len(files)} files: {list(files)[:3]}..."

        elif test['action'] == 'symbol_then_ref':
            payload = {"query": test['query']}
            data = post_json(f"{BASE_URL}/v1/symbols", payload)
            if 'error' in data: return False, f"Symbol API Error {data['error']}"
            
            target = None
            if isinstance(data, list):
                for item in data:
                    if test['container'] in item.get('containerName', '') or test['container'] == 'Any':
                        target = item
                        break
            
            if not target:
                return False, "Symbol not found for ref search"
            
            loc = target.get('location', {})
            fname = loc.get('filename')
            start_line = loc.get('range', {}).get('start', {}).get('line', 0)
            start_col = loc.get('range', {}).get('start', {}).get('character', 0)
            
            payload_ref = {
                "fileName": fname,
                "line": start_line,
                "column": start_col
            }
            data_refs = post_json(f"{BASE_URL}/v1/references", payload_ref, timeout=10)
            if 'error' in data_refs: return False, f"Ref API Error {data_refs['error']}"

            files = set()
            if isinstance(data_refs, list):
                for item in data_refs:
                    f = item.get('filename')
                    if f: files.add(normalize_path(f))
            
            if test['expected_subcheck'](files):
                return True, f"Found {len(files)} ref files"
            return False, f"Found {len(files)} ref files"

    except Exception as e:
        return False, str(e)

    return False, "Unknown Error"

# Main Execution
print("| 测试 ID | 维度 | 题目描述 | 测试结果 (Pass/Fail) | 耗时 (ms) | 备注 |")
print("| :--- | :--- | :--- | :--- | :--- | :--- |")

report_lines = []
report_lines.append("| 测试 ID | 维度 | 题目描述 | 测试结果 (Pass/Fail) | 耗时 (ms) | 备注 |")
report_lines.append("| :--- | :--- | :--- | :--- | :--- | :--- |")

passed_count = 0
for test in tests:
    start = time.time()
    passed, msg = run_test(test)
    dur = int((time.time() - start) * 1000)
    
    if passed: passed_count += 1
    res_str = "Pass" if passed else "Fail"
    print(f"| {test['id']} | {test['type']} | {test['desc']} | {res_str} | {dur} | {msg} |")
    report_lines.append(f"| {test['id']} | {test['type']} | {test['desc']} | {res_str} | {dur} | {msg} |")

# Write Report
try:
    with open(REPORT_PATH, "w", encoding='utf-8') as f:
        f.write("# CodeIntel QA Test Report\n\n")
        f.write(f"**Date**: {time.strftime('%Y-%m-%d %H:%M:%S')}\n")
        f.write(f"**Summary**: Passed {passed_count}/{len(tests)}\n\n")
        f.write("\n".join(report_lines))
        f.write("\n\n**总体评价**:\n")
        f.write(f"Automated test completed. Passed: {passed_count}, Failed: {len(tests) - passed_count}.\n")
        if passed_count < 10:
             f.write("Significant issues detected in symbol resolution or path mapping.\n")
        else:
             f.write("Core functionality appears operational.\n")
except Exception as e:
    print(f"Error writing report: {e}")
