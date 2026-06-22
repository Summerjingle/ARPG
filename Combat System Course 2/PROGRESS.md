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

## 待办
- [ ] Scripts 目录整理（按之前列的方案）
- [ ] EnemyHeathBar 拼写修正
- [ ] EnemyTest.cs 等测试脚本清理
- [ ] 清理场景中 Missing Script 引用（Elevator.cs 残留）
- [ ] 所有 LockedMachine GameObject 需要在 Inspector 拖入对应的 SwitchMechanism 引用
- [ ] 场景中 ArchiveManager.mainMenuListController 需要拖入 MenuRoot 的 MenuListController
