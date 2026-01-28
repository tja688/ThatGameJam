# CodeIntel QA Test Report

**Date**: 2026-01-28 20:45:56
**Summary**: Passed 0/20

| 测试 ID | 维度 | 题目描述 | 测试结果 (Pass/Fail) | 耗时 (ms) | 备注 |
| :--- | :--- | :--- | :--- | :--- | :--- |
| 1 | Class | BackpackModel 定位 | Fail | 136 | API Error {'code': '', 'message': ''} |
| 2 | Class | UniTask 定位 (三方库) | Fail | 152 | API Error {'code': '', 'message': ''} |
| 3 | Class | UIRouter 定位 | Fail | 132 | API Error {'code': '', 'message': ''} |
| 4 | Method | BackpackModel.Clear | Fail | 225 | API Error {'code': '', 'message': ''} |
| 5 | Overload | UniTask.Delay(int) | Fail | 225 | API Error {'code': '', 'message': ''} |
| 6 | Library | Image.DOFade | Fail | 170 | API Error {'code': '', 'message': ''} |
| 7 | Property | IBackpackModel.SelectedIndex | Fail | 184 | API Error {'code': '', 'message': ''} |
| 8 | Field | BackpackModel._selectedIndex | Fail | 189 | API Error {'code': '', 'message': ''} |
| 9 | Symbol | 成员名 Items 歧义 | Fail | 229 | API Error {'code': '', 'message': ''} |
| 10 | Definition | OnInit 基类跳转 | Fail | 3 | API Error 500 |
| 11 | Definition | 接口实现跳转 (GetSelectedIndexQuery -> IBackpackModel.SelectedIndex implementation) | Fail | 1 | API Error 500 |
| 12 | References | SelectedIndex 读取点 | Fail | 14 | API Error 500 |
| 13 | References | UniTask.Yield 引用 | Fail | 142 | Symbol API Error {'code': '', 'message': ''} |
| 14 | References | _selectedIndex 写入点 | Fail | 23 | API Error 500 |
| 15 | Ambiguity | Controller 同名冲突 | Fail | 225 | API Error {'code': '', 'message': ''} |
| 16 | Ambiguity | AddItem 冲突 | Fail | 155 | API Error {'code': '', 'message': ''} |
| 17 | Advanced | 内部类 BackpackItemEntry | Fail | 149 | API Error {'code': '', 'message': ''} |
| 18 | Advanced | 泛型 BindableProperty<T> | Fail | 139 | API Error {'code': '', 'message': ''} |
| 19 | Advanced | 事件引用链 BackpackChangedEvent | Fail | 162 | Symbol API Error {'code': '', 'message': ''} |
| 20 | Unity | UIRoot.Awake 生命周期 | Fail | 246 | API Error {'code': '', 'message': ''} |

**总体评价**:
Automated test completed. Passed: 0, Failed: 20.
Significant issues detected in symbol resolution or path mapping.
