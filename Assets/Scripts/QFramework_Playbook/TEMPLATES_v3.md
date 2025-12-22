# TEMPLATES — Code Skeletons (v3)

> Read `PLAYBOOK.md` first.Copy/paste templates for this project.
>
> - **Single Root Architecture**: `GameRootApp : Architecture<GameRootApp>`
> - **Vertical Slice**: `Assets/Scripts/Features/[FeatureName]/...`
> - Do NOT write logic in generated `*.Designer.cs` / `*.Generated.cs`.

---

## 1. GameRootApp (Single Root Architecture)

**File (recommended):**

- `Assets/Scripts/Root/GameRootApp.cs`

```csharp
using QFramework;

public class GameRootApp : Architecture<GameRootApp>
{
    protected override void Init()
    {
        // Register global Systems/Models/Utilities here.
        //
        // Example:
        // this.RegisterModel<IMyModel>(new MyModel());
        // this.RegisterSystem<IMySystem>(new MySystem());
        // this.RegisterUtility<IMyUtility>(new MyUtility());
    }
}
```

---

## 2. ProjectToolkitBootstrap (Startup Bootstrap / Deterministic Init)

**Purpose**

- Run project-side startup wiring **without touching** `Assets/QFramework/**`.
- Optionally force the Root Architecture to initialize early
  (`Architecture<T>.Interface` triggers `Init()` once, lazily).

**File (recommended):**

- `Assets/Scripts/Root/ProjectToolkitBootstrap.cs`

```csharp
using QFramework;

public static class ProjectToolkitBootstrap
{
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void BeforeSceneLoad()
    {
        // Force Root Architecture initialization early.
        _ = GameRootApp.Interface;

        // Optional project-side initialization (only if required):
        // ResKit.Init();
        //
        // Optional toolkit overrides (only if required):
        // UIKit.Config.PanelLoaderPool = ...;
        // AudioKit.Config.AudioLoaderPool = ...;
    }
}
```

### 2.1 Event: “System requests a write” (recommended)

**File (recommended):**

* `Assets/Scripts/Features/[FeatureName]/Events/Request[Verb][Noun]CommandEvent.cs`

<pre class="overflow-visible! px-0!" data-start="2519" data-end="2643"><div class="contain-inline-size rounded-2xl corner-superellipse/1.1 relative bg-token-sidebar-surface-primary"><div class="@w-xl/main:top-9 sticky top-[calc(--spacing(9)+var(--header-height))]"><div class="absolute end-0 bottom-0 flex h-9 items-center pe-2"><div class="bg-token-bg-elevated-secondary text-token-text-secondary flex items-center gap-4 rounded-sm px-2 font-sans text-xs"></div></div></div><div class="overflow-y-auto p-4" dir="ltr"><code class="whitespace-pre! language-csharp"><span><span>// Prefer struct for TypeEventSystem style.</span><span>
</span><span>public</span><span></span><span>struct</span><span> RequestDoThingCommandEvent
{
    </span><span>public</span><span></span><span>int</span><span> Value;
}
</span></span></code></div></div></pre>

---

### 2.2 System: listen → decide → emit request event (NO SendCommand by default)

**File (recommended):**

* `Assets/Scripts/Features/[FeatureName]/Systems/[FeatureName]System.cs`

<pre class="overflow-visible! px-0!" data-start="2829" data-end="3732"><div class="contain-inline-size rounded-2xl corner-superellipse/1.1 relative bg-token-sidebar-surface-primary"><div class="@w-xl/main:top-9 sticky top-[calc(--spacing(9)+var(--header-height))]"><div class="absolute end-0 bottom-0 flex h-9 items-center pe-2"><div class="bg-token-bg-elevated-secondary text-token-text-secondary flex items-center gap-4 rounded-sm px-2 font-sans text-xs"></div></div></div><div class="overflow-y-auto p-4" dir="ltr"><code class="whitespace-pre! language-csharp"><span><span>using</span><span> QFramework;

</span><span>public</span><span></span><span>interface</span><span></span><span>IFeatureNameSystem</span><span> : </span><span>ISystem</span><span>
{
}

</span><span>public</span><span></span><span>class</span><span></span><span>FeatureNameSystem</span><span> : </span><span>AbstractSystem</span><span>, </span><span>IFeatureNameSystem</span><span>
{
    </span><span>protected</span><span></span><span>override</span><span></span><span>void</span><span></span><span>OnInit</span><span>()
    {
        </span><span>// Long-lived: System lives with the Root Architecture.</span><span>
        </span><span>// Register events here.</span><span>
        </span><span>this</span><span>.RegisterEvent<SomeTriggerEvent>(OnSomeTriggerEvent);
    }

    </span><span>private</span><span></span><span>void</span><span></span><span>OnSomeTriggerEvent</span><span>(</span><span>SomeTriggerEvent e</span><span>)
    {
        </span><span>// IMPORTANT:</span><span>
        </span><span>// Default ISystem does NOT implement ICanSendCommand.</span><span>
        </span><span>// So: do NOT call this.SendCommand(...) here.</span><span>
        </span><span>//</span><span>
        </span><span>// Instead, emit a request event to be handled by a Controller.</span><span>

        </span><span>this</span><span>.SendEvent(</span><span>new</span><span> RequestDoThingCommandEvent
        {
            Value = e.Value
        });
    }
}

</span><span>// Example trigger event (could be emitted by another feature/system/controller)</span><span>
</span><span>public</span><span></span><span>struct</span><span> SomeTriggerEvent
{
    </span><span>public</span><span></span><span>int</span><span> Value;
}
</span></span></code></div></div></pre>

---

### 2.3 Controller: receive request → SendCommand (write path)

**File (recommended):**

* `Assets/Scripts/Features/[FeatureName]/Controllers/[FeatureName]Controller.cs`

<pre class="overflow-visible! px-0!" data-start="3987" data-end="4877"><div class="contain-inline-size rounded-2xl corner-superellipse/1.1 relative bg-token-sidebar-surface-primary"><div class="@w-xl/main:top-9 sticky top-[calc(--spacing(9)+var(--header-height))]"><div class="absolute end-0 bottom-0 flex h-9 items-center pe-2"><div class="bg-token-bg-elevated-secondary text-token-text-secondary flex items-center gap-4 rounded-sm px-2 font-sans text-xs"></div></div></div><div class="overflow-y-auto p-4" dir="ltr"><code class="whitespace-pre! language-csharp"><span><span>using</span><span> QFramework;
</span><span>using</span><span> UnityEngine;

</span><span>public</span><span></span><span>class</span><span></span><span>FeatureNameController</span><span> : </span><span>MonoBehaviour</span><span>, </span><span>IController</span><span>
{
    </span><span>public</span><span> IArchitecture </span><span>GetArchitecture</span><span>() => GameRootApp.Interface;

    </span><span>private</span><span></span><span>void</span><span></span><span>OnEnable</span><span>()
    {
        </span><span>this</span><span>.RegisterEvent<RequestDoThingCommandEvent>(OnRequestDoThingCommand)
            .UnRegisterWhenDisabled(gameObject);
    }

    </span><span>private</span><span></span><span>void</span><span></span><span>OnRequestDoThingCommand</span><span>(</span><span>RequestDoThingCommandEvent e</span><span>)
    {
        </span><span>// Controller is the safe/default place to SendCommand.</span><span>
        </span><span>this</span><span>.SendCommand(</span><span>new</span><span> DoThingCommand(e.Value));
    }
}

</span><span>public</span><span></span><span>class</span><span></span><span>DoThingCommand</span><span> : </span><span>AbstractCommand</span><span>
{
    </span><span>private</span><span></span><span>readonly</span><span></span><span>int</span><span> _value;

    </span><span>public</span><span></span><span>DoThingCommand</span><span>(</span><span>int</span><span></span><span>value</span><span>) => _value = </span><span>value</span><span>;

    </span><span>protected</span><span></span><span>override</span><span></span><span>void</span><span></span><span>OnExecute</span><span>()
    {
        </span><span>// State mutation ONLY (write path)</span><span>
        </span><span>// var model = this.GetModel<IFeatureNameModel>();</span><span>
        </span><span>// model.Apply(_value);</span><span>
    }
}
</span></span></code></div></div></pre>

## 3. Feature Skeleton (Folder Structure)

```text
Assets/Scripts/Features/[FeatureName]/
  Commands/
  Queries/
  Events/
  Models/
  Systems/
  Utilities/
  Controllers/
  Views/

Assets/Scripts/Features/_Shared/   (optional: shared cross-feature types)
```

---

## 4. Controller Skeleton (MonoBehaviour Entry)

**File (recommended):**

- `Assets/Scripts/Features/[FeatureName]/Controllers/[FeatureName]Controller.cs`

```csharp
using QFramework;
using UnityEngine;

public class FeatureNameController : MonoBehaviour, IController
{
    public IArchitecture GetArchitecture() => GameRootApp.Interface;

    private void OnEnable()
    {
        // Example: event subscription with auto-cleanup
        // this.RegisterEvent<SomeEvent>(OnSomeEvent)
        //     .UnRegisterWhenDisabled(gameObject);
    }

    private void OnSomeEvent(SomeEvent e)
    {
    }
}
```

---

## 5. Command Templates

### 5.1 Command (No Return)

**File (recommended):**

- `Assets/Scripts/Features/[FeatureName]/Commands/[Verb][Noun]Command.cs`

```csharp
using QFramework;

public class DoThingCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        // State mutation ONLY (write path)
        // var model = this.GetModel<IMyModel>();
        // model.DoSomething();
    }
}
```

### 5.2 Command (Return Value)

```csharp
using QFramework;

public class DoThingCommand : AbstractCommand<int>
{
    protected override int OnExecute()
    {
        // Perform mutation and return a result if needed.
        return 0;
    }
}
```

---

## 6. Query Templates

### 6.1 Query (Return Value)

**File (recommended):**

- `Assets/Scripts/Features/[FeatureName]/Queries/Get[Something]Query.cs`

```csharp
using QFramework;

public class GetSomethingQuery : AbstractQuery<int>
{
    protected override int OnGet()
    {
        // Read-only logic (no side effects)
        return 0;
    }
}
```

### 6.2 Calling Command/Query from a Controller / UI Panel

```csharp
// Command:
this.SendCommand<DoThingCommand>();

// Query:
var value = this.SendQuery(new GetSomethingQuery());
```

---

## 7. Model & System Skeleton

### 7.1 Model

**Files (recommended):**

- `Assets/Scripts/Features/[FeatureName]/Models/I[FeatureName]Model.cs`
- `Assets/Scripts/Features/[FeatureName]/Models/[FeatureName]Model.cs`

```csharp
using QFramework;

public interface IFeatureNameModel : IModel
{
    // Expose readonly state + Bindables
    // BindableProperty<int> Count { get; }
}

public class FeatureNameModel : AbstractModel, IFeatureNameModel
{
    // Example:
    // public BindableProperty<int> Count { get; } = new BindableProperty<int>(0);

    protected override void OnInit()
    {
        // Initialize state here
    }
}
```

### 7.2 System

**Files (recommended):**

- `Assets/Scripts/Features/[FeatureName]/Systems/I[FeatureName]System.cs`
- `Assets/Scripts/Features/[FeatureName]/Systems/[FeatureName]System.cs`

```csharp
using QFramework;

public interface IFeatureNameSystem : ISystem
{
}

public class FeatureNameSystem : AbstractSystem, IFeatureNameSystem
{
    protected override void OnInit()
    {
        // Orchestration here. Avoid holding Controller references.
    }
}
```

---

## 8. Utility Skeleton (Infrastructure Only)

**Files (recommended):**

- `Assets/Scripts/Features/[FeatureName]/Utilities/I[FeatureName]Utility.cs`
- `Assets/Scripts/Features/[FeatureName]/Utilities/[FeatureName]Utility.cs`

```csharp
using QFramework;

public interface IFeatureNameUtility : IUtility
{
}

public class FeatureNameUtility : IFeatureNameUtility
{
    // Infrastructure only: storage/network/sdk wrappers, etc.
}
```

### 8.1 SingletonKit Utility (Allowed only for low-level infra)

```csharp
using QFramework;

public class MyInfraSingleton : Singleton<MyInfraSingleton>
{
    // Infrastructure only; must NOT become a service locator across layers.
    public void Setup() { }
}
```

---

## 9. Events & Bindables

### 9.1 Event (Prefer `struct` for TypeEventSystem style)

**File (recommended):**

- `Assets/Scripts/Features/[FeatureName]/Events/[Something]ChangedEvent.cs`

```csharp
public struct SomethingChangedEvent
{
    public int Value;
}
```

### 9.2 BindableProperty (Recommended for UI binding)

```csharp
using QFramework;

// In Model:
public BindableProperty<int> Count { get; } = new BindableProperty<int>(0);

// In Controller/UI:
Count.Register(v => { /* update UI */ })
     .UnRegisterWhenGameObjectDestroyed(gameObject);
```

---

## 10. UIKit Panel Logic Skeleton (Partial Class Only)

**Files created by UIKit generator:**

- `[PanelName].cs` (write logic here)
- `[PanelName].Designer.cs` (generated; do NOT edit)

```csharp
using QFramework;

public partial class MyUIPanel : UIPanel, IController
{
    public IArchitecture GetArchitecture() => GameRootApp.Interface;

    protected override void OnInit(IUIData uiData = null)
    {
        // Use generated fields (from Designer):
        // BtnClose.onClick.AddListener(() => CloseSelf());

        // Send command:
        // this.SendCommand<DoThingCommand>();

        // Subscribe event (auto cleanup):
        // this.RegisterEvent<SomethingChangedEvent>(e => { /* refresh */ })
        //     .UnRegisterWhenGameObjectDestroyed(gameObject);
    }

    protected override void OnOpen(IUIData uiData = null) { }
    protected override void OnShow() { }
    protected override void OnHide() { }
    protected override void OnClose() { }
}
```

---

## 11. RootApp Registration Snippet (Integrate a Feature)

```csharp
protected override void Init()
{
    // Model
    this.RegisterModel<IFeatureNameModel>(new FeatureNameModel());

    // System
    this.RegisterSystem<IFeatureNameSystem>(new FeatureNameSystem());

    // Utility (optional)
    // this.RegisterUtility<IFeatureNameUtility>(new FeatureNameUtility());
}
```
