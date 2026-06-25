# Combat System Course 2 — 开发进度

## 2026-06-18 已完成

### Scripts 整理
- 分析了当前 100+ 个脚本的目录结构，给出了整理方案（待执行）

### 敌人系统
- **DeadState.cs**：修复武器碰撞器残留 bug（Impact 期间死亡→DisableHitboxes 不触发），手动补关
- **DeadState.cs / WolfDeadState.cs / EnemyController.cs**：清理高亮死代码（HighlightMesh 只有关没有开）
- **EnemyTest.cs**：待清理（测试脚本，拼写错误 EnemyHeathBar）

### 敌人战斗 AI
- **CombatMovementState.cs**：修复 Chase 状态下不旋转导致的滑步 bug（updateRotation=false + 无手动旋转）
- **CombatMovementState.cs / RetreatAfterAttackState.cs**：旋转速度开放为 SerializeField（rotationSpeed = 500f）

### 输入系统
- 新增 QuickItemModifier（Tab / LB）和 QuickItemNavigate（滚轮 / DPad）两个 Action
- **InputManager.cs**：添加 OnQuickItemModifierChanged 和 OnQuickItemNavigate 事件
- 手柄键位调整：LB=道具条、RB=疾跑、Y=收拔武器、A=交互、X=空

### 快捷道具栏（初版）
- **QuickItemBar.cs**：三槽展开/收起、Tab 按住放大中间、切换道具、角标动画、道具名显隐
- **QuickItemIndicator.controller**：Enter/Exit Trigger 驱动的倒三角动画
- **QuickItemIndicator_Shows.anim / _Hides.anim**：角标动画 clip
- BG 用 CanvasGroup alpha 控制明暗，侧槽始终显示 alpha 控制显眼度

---

## 2026-06-19 快捷道具栏完成

### QuickItemBar 7槽 + 背包联动
- slots 扩为 7 个，静态 Instance 单例
- GetSlot / HasItem / ClearSlotByItem 对外接口
- ApplySlot 数量显示读 ItemSO.amount，与背包同步

### ItemDetailUI 独立输入
- UI_ItemDetail InputMap：Navigate + Use + Cancel + SetQuickSlot
- Use/Cancel/setQuickSlot 三个按钮支持鼠标点击
- setQuickSlotButton 动态文本（设为快捷道具 / 取消快捷道具）
- 类型过滤：只 Consumable 显示快捷按钮

### SetQuickUseUI
- 7槽选择面板，打开/关闭时 CanvasGroup 导航隔离
- SelectDefault 优先空槽，否则选第一个
- UI_QuickUseBar InputMap 独立处理 Confirm/Cancel
- QuickUseSlotUI 鼠标点击支持（IPointerClickHandler）
- 关闭时刷新 Detail 按钮状态

### F 键使用快捷道具
- InputManager.OnQuickItemUse（F / Gamepad West）
- PlayerController 读取 QuickItemBar.CurrentItem
- 走 UsePotion 动画流程（UseDrag → OnDrinkShowModel → OnDrinkApply → OnDrinkAnimationComplete）
- 堆叠>1 减数量 + 刷 UI，最后一份调 RemoveItem

### 被动取消
- InventoryManager.OnItemRemoved 事件
- RemoveItem 加 updateUI 参数（默认 true）
- InventoryUI.OnItemUse 改为走 RemoveItem(updateUI: false)
- QuickItemBar 订阅事件，物品离包自动清槽

### 清理
- PlayerController 去掉 Alpha1 硬编码喝药测试
- RemoveItem 去掉 MessageUI.Show 调用

---

## 2026-06-20 菜单系统重构

### MenuListController + 多选项篝火
- **MenuListController**：新建通用多选项菜单导航控制器（`WhiteBox/Menu/MenuListController.cs`），用 `InputActionReference` 注入不同菜单的 Navigate/Submit，UnityEvent 通知外部。替代硬编码的 MenuButtonController
- **MenuButton 重构**：引用从 `MenuButtonController` → `MenuListController`，Submit 改为订阅 Controller 事件
- **AnimatorFunctions 更新**：引用换为 `MenuListController`，新增 `ExecuteBonfireOption()` 供篝火 Pressed 动画事件调用
- **InputManager 扩展**：新增 `OnBonfireNavigate(Vector2)`、`OnBonfireSubmit` 事件，转发 `UI_BonfireMenu`
- **BonfireOptionButton**：新建篝火选项按钮（`RPG/UI/BonfireOptionButton.cs`），Animator 用 `BonfireOptions.controller`，参数名 `Selected`/`Pressed`
- **BonfirePanelCtrl 多选项化**：集成 `MenuListController`，支持导航选择 + `HandleOption(index)`：0=Rest, 1=预留, 2=Leave。离开前重置所有按钮 Animator.Selected 防残留
- **MenuButtonController 废弃**：不再被引用，可删除

---

## 2026-06-21 电梯修复 + 描边交互存档系统

### 电梯抽搐修复
- **ElevatorController.cs** 重写：Animator 驱动改为脚本驱动。`FixedUpdate` + `rb.position` 直接赋值替代协程 + `MovePosition`；移除 `elevatorAnimator` 引用
- **Elevator.cs** 废弃（动画事件转发不再需要）
- 场景改动：Elevator GameObject 加 Rigidbody（kinematic/no gravity/no interpolation）、MeshCollider → BoxCollider、禁用 Animator、移除 Elevator.cs 组件

### AddOutlineToRenderer — SubMesh 多材质修复
- `CreateOutline()` 和 `RefreshOutline()`：不再 `new Material[] { _outlineMaterial }`，改为读取父物体 `sharedMaterials.Length` 创建等长数组全部填 outline。修复门+锁等多 SubMesh 物体只渲染第一个子网格的 bug
- **首次交互销毁**：新增 `_removeAfterInteract` 选项（Inspector 勾选）。`Start()` 中 `GetComponentInParent<IInteractable>()` 订阅 `OnInteracted` 事件，回调中 DestroyOutline + 取消订阅。读档时若 `CanInteract` 已为 false 则立即销毁
- 事件驱动，零 Update/LateUpdate 开销

### IInteractable 事件化
- 接口新增 `event System.Action OnInteracted;`
- 9 个实现类全部添加 event 字段 + `Interact()` 中 `OnInteracted?.Invoke();`
  - LockedMachine、InteractableObject、Bonfire、DoorOpener、ElevatorLever、InsideOpenTrigger、MachineTrigger、FountainTrigger、NPC

### SwitchMechanism 读档恢复
- `CheckActivationState()` 从 `Start()` 移到 `Awake()`（解决执行顺序问题）
- 新增 `restoreStateName` 字段（默认 "Open"）：读档时 `anim.Play(stateName, 0, 1f) + anim.Update(0)` 直接跳到最后一帧，不播动画
- 空字符串 = 跳过 Animator 恢复

### LockedMachine 存档联动
- 新增 `public SwitchMechanism switchMechanism;`
- `OpenMachine()` 中调用 `switchMechanism.Activate()` 持久化 + 禁用 `machineCollider`
- `Start()` 读档恢复：检查 `switchMechanism.IsActivated()` → 设置 `isActivated=true` + 禁用 collider
- Animator 由 SwitchMechanism.Awake 先一步处理，LockedMachine 不重复设置

---

## 2026-06-22 输入穿透修复 + 存档面板完善 + 加载优化

### 输入系统根因修复

**问题：三个独立的 PlayerInputActions 实例同时运行**
- `InputManager.Actions` — `new PlayerInputActions()`，管理自己的副本
- `MainMenuState.input` — 又一个 `new PlayerInputActions()`，只为了 AnyKey 检测
- 原始 `.inputactions` 资产 — 被 EventSystem 的 `InputSystemUIInputModule` 全量 Enable
- MenuListController 的 `InputActionReference` 指向原始资产，InputManager 的 SwitchToXxx 管不到

**修复：**
- **MainMenuState.cs** — 删除独立的 `new PlayerInputActions()`，改用 `InputManager.Instance.Actions.UI_MainMenu.AnyKey`；Start 中调用 `SwitchToMainMenuUI()`
- **InputManager.cs** — 移除 `OnEnable` 里的 `Actions.Enable()`（避免与 SwitchToXxx 的 Enable 计数堆叠）；新增 `EnableExclusive(map)` 辅助方法（先 Disable 再 Enable，确保计数恒为 1）；所有 SwitchToXxx 改用 EnableExclusive
- **MenuListController.cs** — 移除独立的 `action.Enable()/Disable()`，只订阅事件不管理生命周期

### 存档面板导航穿透修复

**问题：在存档里按方向键，主菜单的选中也跟着动**
- 原始资产被 InputSystemUIInputModule 全量 Enable → 所有 Map 的 Navigate 同时活着
- 主菜单 MenuListController 和存档 MenuListController 同时收到事件

**修复：ArchiveManager.cs**
- 新增 `public MenuListController mainMenuListController` 字段
- `ShowPanel()` 中 `mainMenuListController.enabled = false`
- `HidePanel()` 中 `mainMenuListController.enabled = true`
- 场景中需将 MenuRoot 的 MenuListController 拖入此字段

### 确认面板 A 键唤醒其他面板修复

**问题：删除确认面板按 A → loadConfirmPanel 也被激活**
- 确认模式下存档的 MenuListController 仍在响应 UI_ArchiveMenu/Submit
- 按 A 同时触发 `OnConfirmSubmitPressed` 和 `OnSubmitPressed` → `SelectLoadArchive`

**修复：ArchiveManager.cs**
- `SelectLoadArchive/SelectDeleteArchive` 中 `menuListController.enabled = false`
- `CancelSelection/CancelDelete/ConfirmDelete/ConfirmLoad` 中 `menuListController.enabled = true`

### 加载界面优化

**问题：新游戏时进度条跑两遍 + 加载迟缓**
- `minLoadTime = 2f` 强控假进度 + 第二个 while 循环 2秒后填充 = 至少 4 秒人为拖延
- 冷启动时假进度与真实进度不同步，两段分裂明显

**修复：LoadingScreen.cs**
- 移除 `minLoadTime` 假进度
- 移除场景激活后第二次填充循环
- 进度条直接反映 `asyncLoad.progress / 0.9f`
- 同步 `LoadScene("LoadingScene")` 保留（场景本身很小）

### 输入系统命名统一
- UI_SaveMenu → UI_ArchiveMenu（Action Map + C# 类 + InputManager 方法）
- 移除 AnyKey Action，新增 Navigate/Submit/Cancel/Delete 四个 Action
- 新增 UI_Confirm 通用确认面板 ActionMap（Submit + Cancel），供所有确认面板复用

---

## 2026-06-23 Static Batching × AddOutlineToRenderer 鬼影建筑

### 现象
WhiteBox_Village 场景打包后出现白色/红色建筑形状的半透明物体（类似 SM_Fortress_Wall_Archway_01、SM_Entrance_01），位于真实建筑附近，Editor 中完全看不到。

### 排查过程
- 怀疑是 AddOutlineToRenderer 创建的 Outline 子物体，用户列出所有挂载该组件的物体，鬼影不匹配
- 将 OutlineOnly_White 颜色改为红色 → 鬼影变红，确认是 Outline shader 渲染
- 将 Outline 子物体改名为 `Outline_父物体名` + 随机色 + 3倍放大 → DebugBuildWhiteObjects 运行时扫描 → 发现 Outline 子物体的 sharedMesh 变成了 `Combined Mesh (root: scene)`，62000+ 顶点

### 根因
**Static Batching**。场景中 `SM_Column_05c_LOD3` 等挂有 AddOutlineToRenderer 的物体同时勾选了 Batching Static。Build 时 Unity 将所有静态物体合并成大 Mesh，`MeshFilter.sharedMesh` 在运行时返回的是合并后的 Combined Mesh（包含整片场景的静态几何体），Outline 子物体用这个 Mesh 渲染 → 整片建筑都出现轮廓。

### 修复
1. **手动**：所有挂 AddOutlineToRenderer 的物体，Inspector 右上角 Static 下拉 → 取消 Batching Static
2. **代码防御**：`CreateOutline()` 中检测 `sharedMesh.name.Contains("Combined Mesh")`，命中则报错跳过并提示关 Batching Static

### 教训
- `MeshFilter.sharedMesh` 在 Static Batching 开启时，Build 后会变成 Combined Mesh，不是原始 Mesh
- 任何需要复制 Mesh 来做视觉效果（描边、高亮、轮廓）的运行时系统，必须确保源物体不参与 Static Batching
- Shader 调试不要先怀疑 Shader，先确认 Mesh 来源是否正确

---

### 武器拔出状态存档
- **GameSaveData** 新增 `bool isWeaponDrawn`
- **SaveManger.SaveGame()** 写入 `WeaponEquipmentManager.Instance.isWeaponDrawn`
- **SaveManger.ApplyEquipmentCoroutine()** 读档时若为拔出状态，触发 `drawWeapon` 动画让动画事件调 `DrawWeapon()` + `SetWeaponDrawState()`，不直接设 `Armed=true`（否则状态机不切换）

### 快捷道具栏存档
- **GameSaveData** 新增 `List<QuickSlotSaveData> quickSlots`（每槽 `itemName` + `count`）
- **SaveManger.SaveQuickSlots()** 遍历 7 槽存 `item.nameOfItem`
- **SaveManger.ApplyQuickSlots()** 读档恢复

**引用不一致 Bug 修复：**
- 原实现从 `ItemDB` 查物品（原始 SO），背包里是 `Instantiate` 副本
- `QuickItemBar.HasItem()` / `ClearSlotByItem()` 用 `==` 引用比较 → 永远不匹配
- **症状**：读档后快捷槽 icon 有但无 count、背包不识别已设快捷、道具用完后槽位残留
- **修复**：`ApplyQuickSlots()` 改为从 `InventoryManager.itemList` 查（与背包同一实例）

### 快捷道具高亮指示器 QuickLight
- **ItemUI** 新增 `quickLightObject` 字段 + `UpdateQuickLight()`，`InitItem()` 时自动检查 `QuickItemBar.HasItem()`
- **InventoryUI** 维护 `Dictionary<ItemSO, ItemUI>` 实现 O(1) 查找 `RefreshQuickLightForItem()`
- **SetQuickUseUI** 设槽时先读旧道具，新旧都刷新 QuickLight（避免被替换的旧道具 QuickLight 残留）
- **SaveManger** 读档完补调 `RefreshAllQuickLights()`（因为 `UpdateInventoryUI` 在 `ApplyQuickSlots` 之前执行）

---

## 待办
- [ ] Scripts 目录整理（按之前列的方案）
- [ ] EnemyHeathBar 拼写修正
- [ ] EnemyTest.cs 等测试脚本清理
- [ ] 清理场景中 Missing Script 引用（Elevator.cs 残留）
- [ ] 所有 LockedMachine GameObject 需要在 Inspector 拖入对应的 SwitchMechanism 引用
- [ ] 场景中 ArchiveManager.mainMenuListController 需要拖入 MenuRoot 的 MenuListController

---

## 2026-06-25 翻滚系统修复

### 问题
偶尔翻滚时角色"抽一下"然后不播放翻滚动画，但体力已被扣除。偶发性，推测为时序竞争问题。

### 排查过程
- 加 `[ROLL]` 前缀 Debug 日志覆盖：输入、StartRoll、翻滚中 velocity/state/normalizedTime、OnRollEnd、WaitForRollEnd
- 日志暴露两条 bug：

### Bug 1：rollRequested 粘性 flag
- `rollRequested` 只在 `!isRolling` 时清除（第 176 行），翻滚中按键不会消费
- 连续按翻滚时输入跨帧残留，翻滚结束后自动再滚
- **修复**：删掉 `rollRequested` 变量，触发条件直接调 `WasPressedThisFrame()`

### Bug 2：rollEndTriggered 在 yield break 路径未重置
- `WaitForRollEnd` 协程：`OnRollEnd` 已做完整清理时走 `if (!isRolling) yield break`，但 `rollEndTriggered` 没清
- 下一次 `StartRoll` 启动协程后 `rollEndTriggered` 仍是 true → 协程立刻触发 → 当场清除翻滚 → 体力扣了滚没滚
- **修复**：`rollEndTriggered = false` 移到 `yield break` 之前

### Bug 3：FadeRollLayerWeight 协程冲突（连续翻滚卡死）
- `OnRollEnd()` 启动 `FadeRollLayerWeight()` 协程（0.2s 淡出 layer weight）
- 连续翻滚时旧 fade 协程还在跑，新 `StartRoll` 设 `layerWeight=1` 后每帧被旧协程 Lerp 下拉
- layer weight 不稳定时 CrossFade 的动画事件不触发 → `OnRollEnd` 永远不调 → 角色卡死在 `isRolling=true`，以 8m/s 速度永久滑动
- **修复**：存 `fadeRollCoroutine` 引用，`StartRoll` 时先 `StopCoroutine` 再设 weight

### WaitForRollEnd 改为动画事件驱动
- 原实现：轮询 `state.IsName(animName) && normalizedTime >= 0.75`，帧卡顿时窗口错过就永远等不到
- 新实现：`WaitForRollEnd` 等 `rollEndTriggered` flag，`OnRollEnd()` 由动画 Animation Event 调用（四个翻滚 clip 75% 处）
- 不依赖状态名、不依赖 normalizedTime、不依赖协程轮询

### 受击/死亡中断兜底
- `PlayerFighter.PlayHitReaction()` / `PlayDeathAnimation()` 入口调 `PlayerController.i.OnRollEnd()` 强制结束翻滚
- `OnRollEnd()` 带 `if (!isRolling) return` 防重复清理

### Roll 层淡出
- 原 `SetLayerWeight(0f)` 瞬切导致 Base Layer 生硬接管
- 改为 `FadeRollLayerWeight()` 协程 0.2s Lerp 淡出
