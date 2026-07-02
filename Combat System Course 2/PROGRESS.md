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

## 2026-07-02 ItemSO 体系重构

### 目标
将 `ItemSO.cs` 从单体 God-object 重构为继承体系：抽象基类 `ItemSO` → `EquipmentSO`（装备中间层）→ `WeaponSO` / `ArmorSO`，以及 `ConsumableSO` / `QuestItemSO`。

### 继承层次

```
ItemSO (abstract)
├── EquipmentSO (abstract) — equipmentPrefab + propertyList + equipConditions + CanEquip()
│   ├── WeaponSO — 武器动画/挂点/重武器/基础伤害
│   └── ArmorSO — ArmorType 枚举
├── ConsumableSO — effects 列表 + Use() 回血回蓝
└── QuestItemSO — questID
```

### 新建文件

```
+ Assets/Scripts/RPG/SO/StatType.cs       — 属性枚举（Bonus: MaxHP/MaxEnergy/Defense/CritRate/CritDamage/Strength/Luck | Curr: CurrHP/CurrEnergy）
+ Assets/Scripts/RPG/SO/EquipCondition.cs  — 装备条件结构体（StatType + requiredValue，纯 AND + >=）
+ Assets/Scripts/RPG/SO/EquipmentSO.cs     — 装备抽象中间层
+ Assets/Scripts/RPG/SO/WeaponSO.cs        — 武器 SO（含 SheathLocation/HandSocket 枚举）
+ Assets/Scripts/RPG/SO/ArmorSO.cs         — 护甲 SO
+ Assets/Scripts/RPG/SO/ConsumableSO.cs    — 消耗品 SO
+ Assets/Scripts/RPG/SO/QuestItemSO.cs     — 任务道具 SO
```

### 修改文件

```
M Assets/Scripts/RPG/SO/ItemSO.cs          — 改为 abstract，移除 PropertyType 枚举，itemType 改为 abstract 属性
M Assets/Scripts/Player/PlayerProperty.cs  — base+bonus 双层属性体系，AddProperty/RemoveProperty 重写，新增 GetStatValue()
M Assets/Scripts/Player/ArmorEquipmentManager.cs — weaponPrefab→equipmentPrefab，属性循环调用 AddProperty
M Assets/Scripts/Player/WeaponEquipmentManager.cs — EquipWeapon 参数 ItemSO→WeaponSO
M Assets/Scripts/RPG/Weapon/Weapon.cs      — 删除重复字段，改为 readonly 属性代理到 WeaponSO
M Assets/Scripts/RPG/ItemUsageHandler.cs   — switch(itemType)→is 类型检查 + CanEquip() 条件判断
M Assets/Scripts/RPG/ItemDetailUI.cs       — 属性显示读 EquipmentSO.propertyList / ConsumableSO.effects
M Assets/Scripts/RPG/InventoryUI.cs        — item.armorType→(item is ArmorSO armor).armorType
M Assets/Scripts/RPG/SO/ItemDBSO.cs        — 新增 ContextMenu 一键填充所有 ItemSO
```

### 关键设计决策

1. **StatType 拆分 Bonus/Curr 两类**
   - Bonus（装备加）：MaxHP, MaxEnergy, Defense, CritRate, CritDamage, Strength, Luck
   - Curr（消耗品加）：CurrHP, CurrEnergy
   - 装备不加 Curr 类属性，消耗品不加 Bonus 类属性

2. **装备条件 EquipCondition**
   - `StatType` + `requiredValue`，多个条件纯 AND 关系，全部 >= 判定
   - `EquipmentSO.CanEquip(PlayerProperty)` 逐条检查，不满足则 ItemUsageHandler 显示提示

3. **Weapon.cs 去重**
   - 删除 `drawWeaponTriggerName`、`sheathLocation`、`isHeavy` 等 6 个重复字段
   - 改为 `=> itemSO?.xxx` 只读属性，单一数据源为 WeaponSO
   - `SheathLocation` / `HandSocket` 枚举从 Weapon.cs 移到 WeaponSO.cs

4. **ItemSO.itemType 从字段改为 abstract 属性**
   - 每个子类强制声明自己的 ItemType，编译期保证不遗漏

5. **资产迁移**
   - 15 个现有 .asset 文件通过 YAML 直接编辑从 ItemSO 改为对应子类
   - 遇到 YAML 中文字符 `\uXXXX` 编码问题，从备份恢复并解码解决

### 清理

```
- Assets/Editor/ItemSOMigration.cs + .meta
- Assets/Editor/ItemTextRestore.cs + .meta
- Assets/Editor/ItemTextFixer.cs + .meta
- ItemSO.cs 中 PropertyType 枚举、propertyType 旧字段、MigrateStatType()、ClearLegacyField()
```

### SO 目录最终结构（11 个文件）

```
SO/
├── ItemSO.cs          ← 抽象基类 + ItemType/ArmorType/Rarity/Property
├── StatType.cs        ← 属性枚举
├── EquipCondition.cs  ← 装备条件
├── EquipmentSO.cs     ← 装备中间层 + CanEquip()
├── WeaponSO.cs        ← 武器 + SheathLocation/HandSocket 枚举
├── ArmorSO.cs         ← 护甲
├── ConsumableSO.cs    ← 消耗品
├── QuestItemSO.cs     ← 任务道具
├── ItemDBSO.cs        ← 物品数据库（右键自动填充）
├── ItemDBManager.cs
└── StaticSceneItem.cs
```

---

## 待办
- [ ] Scripts 目录整理（按之前列的方案）
- [ ] EnemyHeathBar 拼写修正
- [ ] EnemyTest.cs 等测试脚本清理
- [ ] 清理场景中 Missing Script 引用（Elevator.cs 残留）
- [ ] 所有 LockedMachine GameObject 需要在 Inspector 拖入对应的 SwitchMechanism 引用
- [ ] 场景中 ArchiveManager.mainMenuListController 需要拖入 MenuRoot 的 MenuListController
- [ ] **攻击穿过敌人**：定位 collider/rigidbody 配置问题
- [ ] **敌人挨打后状态**：测试验证状态机 5 项修复是否生效

---

## 2026-07-01 锁定摄像机系统重构

### 问题

1. **锁定后绕敌转圈，摄像机抖动** — 画面小幅度高频震颤
2. **lockOnMinDistance 无效** — 靠近敌人时摄像机飞到两人头顶

### 根因

**问题1 根因：cinemachineCameraTarget 同时被两个来源驱动旋转，相位差一帧**

- `PlayerController.Update` 旋转 Player → CCT（Player 子节点）继承父旋转 → 世界位置偏移
- `PlayerCameraController.LateUpdate.HandleLockOnCamera` 用 SmoothDampAngle 覆写 CCT rotation
- TargetLockingCam 的 FramingTransposer（XDamping=0, YDamping=0）零延迟跟踪 Follow target 的每一帧微动
- Player 的 RotateTowards 和 CCT 的 SmoothDampAngle 不同步 → 微小差异 → 直接传给摄像机

**问题2 根因：HandleLockOnCamera 的 early return 冻结了 CCT rotation，但 FramingTransposer 不读 rotation**

- FramingTransposer 只看 Follow target 位置
- 近距离时 player + enemy 挤在一起，FramingTransposer 取景两点 → 正上方
- 旧的 Z 轴推远 CCT 起反作用（Follow target 更靠后 → 头顶角度更陡）
- CinemachineCollider Damping=0 让遮挡瞬移无过渡

### 代码改动

**PlayerCameraController.cs：**
- `using TMPro.Examples;` → `using Cinemachine;`
- 移除 `lockCameraTargetZ` 字段
- 新增字段：`lockOnMinDistance`(5)、`lockOnMinCameraDistance`(1.5)、`lockOnDefaultCameraDistance`(4)、`lockCamBody`(private, runtime cache via `lockCam.GetCinemachineComponent<Cinemachine3rdPersonFollow>()`)
- `HandleFreeCamera`：删掉死代码 `deltaTimeMultiplier = 1f`
- `HandleLockOnCamera`：删除 early return，始终平滑追踪敌人方向
- `UpdateLockCameraDistance`：不再推 CCT 的 localPosition.z，改为 lerp `lockCamBody.CameraDistance`（指数衰减），近距离时缩小相机距离防止钻地
- `UnlockCamera`：重置 CameraDistance 替代 Z 偏移重置
- `SetCameraHeight`：改用世界坐标 + 指数衰减 Lerp
- 新增 `SyncCameraTargetPosition(Vector3 playerPosition, float headOffsetY, bool smoothHeight)`：每帧手动同步 CCT 世界位置跟随 Player（替代父子继承），XZ 瞬时、Y 指数衰减

**PlayerController.cs：**
- `LateUpdate` 开头新增 `SyncCameraTargetPosition`（在 SetLookInput 之前）
- 移除 crouch 块中的旧 `SetCameraHeight` 调用

### 场景改动（已完成）

1. CCT 脱离 Player 子节点 → 根层级
2. TargetLockingCam Body → FramingTransposer 改为 3rd Person Follow，参数：CameraDistance=4, ShoulderOffset=(1, 0.39, 0), Damping=(0,0,0)
3. 两台 VCam CinemachineCollider → Damping: 0→0.2, DampingWhenOccluded: 0→0.1

### 涉及文件
```
M Assets/Scripts/Player/PlayerCameraController.cs
M Assets/Scripts/Player/PlayerController.cs
M Assets/Scenes/WhiteBoxes/WhiteBox_Village.unity
```

---

## 2026-06-27 特殊击退系统 + 敌人状态机修复

### 特殊受击系统（连招终结技击飞）
- **接口**：`ICombatSystem` 新增 `CurrentSpecialHitReaction` 属性（set/get）+ `PlayHitReaction` 加 `specialHitReaction` 可选参数
- **玩家侧**：`PlayerFighterNew.AE_SetHitReaction(string)` — Animation Event 入口，传 "KnockUp" 等动画名
- **敌人侧**：`OnTriggerEnter` 读取并消费 attacker 的 `CurrentSpecialHitReaction`，命中后清空
- `DisableHitboxes()` 清除 `CurrentSpecialHitReaction = null`，防止挥空残留到下一刀
- 空参/空字符串 = 默认受击动画（敌人"SwordImpact"，玩家"hit_light_B_body"）

### 手动击退（替代 root motion）
- **问题**：`Dam_FlyDie_Left_Root` 开了 `applyRootMotion=true` 但敌人只位移 0.268 单位 → FBX `Bake Into Pose` 勾着导致
- **修复**：`EnemyFighter` 新增 `[SerializeField] knockbackDistance = 3f`，特殊击退时停 NavAgent → 播动画 → 逐帧 ease-out Lerp 位移 → Warp 同步 → 恢复 NavAgent
- 移除所有 root motion 开关和 debug log

### 敌人状态机 5 项修复

**P0 — Attack()协程劫持状态（概率性）**
- 根因：`GettingHitState.Enter().StopAllCoroutines()` 只停在 GettingHitState 自身，不停 AttackState 上的旧 `Attack()` 协程。被打断后 `ExecuteEnemyAttack` break 设 `Attackstate=Idle` → 旧协程 `WaitUntil(Idle)` 满足 → 调用 `ChangerState(RetreatAfterAttack)` 把状态从 GettingHit 抢走
- 修复：`AttackState.Exit()` 加 `StopAllCoroutines()` + 清理 isAttacking/applyRootMotion/DisableHitboxes/NavAgent.isStopped

**P0 — InAction 被 ExecuteEnemyAttack 覆盖**
- 根因：while break 后无条件设 `InAction=false`，与 `PlayHitReaction` 设的 `InAction=true` 冲突
- 修复：`InAction = false` 仅在 `!IsTakingHit` 时执行

**P1 — EnemyManager 不感知攻击被中断**
- 修复：新增 `previousAttacker` 字段追踪，检测到攻击者瞬间变 GettingHit → 重置 `notAttackingTimer` 为完整间隔

**P2 — RetreatAfterAttack NPE**
- 修复：`Execute()` 开头检查 `Target == null || Target.HealthSystem.IsDead` → 直接切 CombatMovement

**P3 — GettingHit 事件泄漏**
- 修复：匿名 lambda `+= () => StartCoroutine(...)` → 命名方法 `OnHitCompleteHandler` + `Exit()` 中 `-=`

**额外 — 敌人挨打后卡住**
- 根因：`PrepareEnemyAttack` 设 `navAgent.isStopped=true`，攻击被打断后 `FinishEnemyAttack` 不调用 → NavAgent 一直 stopped
- 修复：`CombatMovementState.Enter()` + `AttackState.Exit()` 双重保险设 `navAgent.isStopped = false`

### RegisterHit 防同一刀重复命中
- `ICombatSystem` 新增 `bool RegisterHit(GameObject target)`
- 三个 Fighter 全部实现：`PlayerFighterNew`(已有)、`PlayerFighter`、`EnemyFighter`
- `EnableHitbox()` 时 `hitTargets.Clear()`，`OnTriggerEnter` 调用 `attacker.RegisterHit(this.gameObject)` 做去重

### 涉及文件
```
M Assets/Scripts/CombatSystem/Gemini/ICombatSystem.cs
M Assets/Scripts/CombatSystem/Gemini/PlayerFighterNew.cs
M Assets/Scripts/CombatSystem/Gemini/PlayerFighter.cs
M Assets/Scripts/CombatSystem/Gemini/EnemyFighter.cs
M Assets/Scripts/Enemy/States/AttackState.cs
M Assets/Scripts/Enemy/States/GettingHitState.cs
M Assets/Scripts/Enemy/States/CombatMovementState.cs
M Assets/Scripts/Enemy/States/RetreatAfterAttackState.cs
M Assets/Scripts/Enemy/EnemyManager.cs
```

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

---

## 2026-06-27 武器反弹系统 (Weapon Rebound)

### 功能
玩家攻击时武器碰到 `Tag = "Obstacle"` 的物体触发反弹：定格 → 倒放攻击动画 → 切回执剑待机。

### 碰撞检测
- **Weapon.cs** 基类新增 `OnTriggerEnter`：检测 Obstacle → `ClosestPoint` 取碰撞点 → 回调 `PlayerFighterNew.OnWeaponRebound(hitPoint)`
- 武器已有 Rigidbody (Kinematic) + BoxCollider (IsTrigger)，OnTriggerEnter 原生可用

### 反弹时序
```
碰撞 → ① 关武器碰撞体 → ② 定格(reboundFreezeDuration) → ③ 倒放(AttackSpeed=-1) → ④ Play("Combat Blend Tree")
```
- **Controller 参数方案**：Animator 攻击状态绑定 `AttackSpeed` Float 参数（Min=-5），`SetFloat` 控制正放/定格/倒放，比手动 `Play(normTime)` 丝滑
- **倒放时长**：`normalizedTime × clipLength / |speed|`，ClipInfo 拿真实长度防 BlendTree Infinity
- **切回待机**：`animator.Play("Combat Blend Tree", 0, 0)` 替代 CrossFade，避免过渡期攻击动画事件二次触发

### VFX/音效
- 碰撞点 Instantiate `reboundVfxPrefab` + `AudioSource.PlayClipAtPoint(reboundSfx)`

### 状态管理
- `IsRebounding` 标志：反弹期间 InAction=true，可被敌人攻击打断
- **Abort 路径**：每帧检测 `IsTakingHit || HealthSystem.IsDead`，打断后恢复 AttackSpeed=1、applyRootMotion=false、canCombo=true、LockRotation=false
- **清理**：`ForceResetAttackState()` 重置 canCombo + applyRootMotion；延迟一帧 `LockRotation=false` 确保倒放触发的 StartRotationLock 之后生效
- `TryAttack` IsRebounding 阻断防止反弹期间发起新攻击
- `AE_EnableHitbox` IsRebounding 守卫防止倒放时动画事件重新开启武器碰撞体

### Inspector 可调参数 (PlayerFighterNew → Rebound)
| 参数 | 范围 | 默认 | 效果 |
|------|------|------|------|
| Rebound Freeze Duration | 0~0.5s | 0.02s | 碰撞定格时长 |
| Rebound Speed | -5~-0.1 | -1 | 倒放速度 |
| Rebound Vfx Prefab | GameObject | null | 火花粒子 |
| Rebound Sfx | AudioClip | null | 金属碰撞音 |

### 涉及文件
```
M Assets/Scripts/RPG/Weapon/Weapon.cs
M Assets/Scripts/CombatSystem/Gemini/PlayerFighterNew.cs
M Assets/Scripts/Player/PlayerAttack.cs
M Assets/GameData/Animator/P&E/PlayerController.controller
```

### 待办
- [ ] 敌人攻击反弹（目前仅玩家）
- [ ] 反弹后硬直帧可配置（目前直接切 Idle，无停顿）

---

## 2026-07-01 伤害飘字系统 (Floating Text)

### 功能
角色受击/暴击/恢复时，头顶弹出伤害数字飘字：弹入→停留→淡出→回收。支持三种类型：普通攻击（白）、暴击（橙红+图标）、恢复（绿）。

### 数据流
```
攻击方 Fighter.OnTriggerEnter
  → Random.value < attacker.CritRate/100 → isCrit
  → target.TakeDamage(damage, isCrit)
  → HealthSystem.TakeDamage(damage, armor, isCrit)
  → 构造 HealthChangeInfo { delta, isCrit }
  → OnHealthChanged(HealthSystem, HealthChangeInfo)
  → FloatingTextManager 选 Config → 出池 → 曲线动画 → 回收
```

### 新建文件
```
+ Assets/Scripts/CombatSystem/HealthChangeInfo.cs
+ Assets/Scripts/CombatSystem/FloatingText/FloatingTextConfig.cs
+ Assets/Scripts/CombatSystem/FloatingText/FloatingTextDatabase.cs
+ Assets/Scripts/CombatSystem/FloatingText/FloatingTextManager.cs
+ Assets/Scripts/CombatSystem/FloatingText/FloatingTextInstance.cs
+ Assets/GameData/FloatingTexts/CommonFloatingText.asset
+ Assets/GameData/FloatingTexts/CriticalDamageFloatingText.asset
+ Assets/GameData/FloatingTexts/HealingFloatingText.asset
+ Assets/GameData/FloatingTexts/Floating Text Database.asset
+ Assets/GameData/FloatingTexts/FloatingTMP.prefab
```

### 修改文件
```
M Assets/Scripts/CombatSystem/HealthSystem.cs
M Assets/Scripts/CombatSystem/Gemini/ICombatSystem.cs
M Assets/Scripts/CombatSystem/Gemini/PlayerFighterNew.cs
M Assets/Scripts/CombatSystem/Gemini/PlayerFighter.cs
M Assets/Scripts/CombatSystem/Gemini/EnemyFighter.cs
M Assets/Scripts/CombatSystem/Gemini/KnightDFighter.cs
M Assets/Scripts/Wolf/WolfFighter.cs
M Assets/Scripts/Player/PlayerProperty.cs
M Assets/Scripts/Player/PlayerHUDUI.cs
M Assets/Scripts/Enemy/EnemyHeathBar.cs
M Assets/Scripts/Enemy/BossHealthBar.cs
M Assets/Scenes/WhiteBoxes/WhiteBox_Village.unity
```

### 架构设计

**HealthChangeInfo** — 血量变化信息结构体，随 `OnHealthChanged` 事件传递：
- `delta`（正=恢复，负=伤害）
- `isCrit`（暴击标记）

**FloatingTextConfig (SO)** — 单个飘字类型的配置：
- 外观：`textColor`、`baseFontSize`、`icon`（Sprite 可选）
- 动画曲线：`sizeCurve`（弹入 0.3→1.4→0.95→0.8）、`horizontalOffsetCurve`（右飘 0→0.6）、`alphaCurve`（保持0.4s→淡出至0）
- 行为：`duration`(1.2s)、`heightOffset`(2.5)、`randomHorizontalRange`(0.5)

**三种 Config：**

| | 攻击 (Common) | 暴击 (Critical) | 恢复 (Healing) |
|---|---|---|---|
| 颜色 | 白 | 橙红 | 绿 |
| 字号 | 4 | 5 | 4 |
| 图标 | 无 | 有 | 无 |

**FloatingTextDatabase (SO)** — 聚合三个 Config，参考 ItemDBSO 模式。`SelectConfig(info)` 按 delta 正负 + isCrit 选择。

**FloatingTextManager** — 单例 + 对象池 + 生命周期：
- `Awake` 预创建 20 个池实例，`Start` 扫描已有 HealthSystem 注册
- `Update` 所有活跃飘字面向相机（billboard）
- `RegisterHealthSystem`/`UnregisterHealthSystem` 供动态刷出的敌人自注册
- `OnHealthChanged` → 选 Config → 出池 → 设文字/颜色/字号/图标 → 定世界坐标 → 播放动画协程
- 动画结束回调 `ReturnToPool`

**FloatingTextInstance** — 挂池对象上：
- `TextMeshPro`（世界空间） + `SpriteRenderer`（Icon 子对象，`GetComponentInChildren` 查找）
- `PlayAnimation(config, onComplete)` → 协程逐帧 `Evaluate` 曲线驱动 Scale/Alpha/Position
- 无 Canvas 依赖，纯世界空间 Mesh 渲染

### 暴击系统（前置依赖）

- **PlayerProperty** 新增 `baseCritRate`(5%) + `bonusCritRate`(0%) + `TotalCritRate`
- **ICombatSystem** 新增 `float CritRate { get; }`
- **PlayerFighter / PlayerFighterNew** — CritRate 读 PlayerProperty.TotalCritRate
- **EnemyFighter** — CritRate 返回 0（静态可背板，不引入不确定性）
- **WolfFighter / KnightDFighter** — 继承 EnemyFighter 的 CritRate=0
- OnTriggerEnter 中 `Random.value < attacker.CritRate/100f` 判定，结果传入 TakeDamage → HealthSystem

### HealthSystem 改造

- `OnHealthChanged` 事件签名：`Action<HealthSystem>` → `Action<HealthSystem, HealthChangeInfo>`
- `TakeDamage(float, int, bool isCrit=false)` / `RestoreHealth` / `SetMaxHealth` / `ResetHealth` 全部构造 HealthChangeInfo 后触发
- `Start()` 中向 FloatingTextManager 自注册（覆盖动态生成的敌人）
- `OnDestroy()` 中注销

**向后兼容**：3 个订阅者（PlayerHUDUI、EnemyHeathBar、BossHealthBar）只加参、方法体不动。

### ICombatSystem 接口同步

- `TakeDamage(float)` → `TakeDamage(float, bool isCrit=false)` — 5 个实现类全部更新
- KnightDFighter override 格挡逻辑不变，`base.TakeDamage(damage, isCrit)` 透传

### 对象池

- 20 个 TMP 实例预创建，Queue 管理
- `GetFromPool` 池空时动态扩容
- `ReturnToPool` 停动画 → 关显 → 入队
- 后备：无 prefab 时自动创建默认 TMP（fontSize=36, sortingOrder=100）

### 预制体结构
```
FloatingTMP (RectTransform, 无 Canvas)
├── FloatingTextInstance (脚本)
├── TextMeshPro (MeshRenderer + 世界空间 TMP)
└── Icon
    └── SpriteRenderer
```

### 注意

- 世界空间 TextMeshPro 的 `fontSize` 语义不同于 UI 版（TextMeshProUGUI），取决于字体资产采样点数。当前字号 4~5 对应字体资产的实际渲染尺寸
- 曲线用 `tangentMode: 0`（Auto）简化序列化，免手动调切线
- CritRate 不加 ICombatSystem 接口方法体，直接在 Fighter 中用 `Random.value < attacker.CritRate/100f`

---

## 2026-06-30 大剑系统 + 挂点枚举重构

### 大剑武器
- **GreatSword.cs** — 新增重型武器类（`RPG/Weapon/GreatSword.cs`）
- **Weapon_GreatSword.asset** — 大剑 ItemSO 配置
- **GreatSword.prefab** — 大剑预制体（手部 socket2）
- **动画**：MassiveGreatSword_AnimSet 包导入，`WP_Equip`（拔武器）和 `WP_Unequip`（收武器）

### Weapon.cs 枚举化挂点选择
- 新增 `SheathLocation` 枚举：`Waist`（默认，腰间）/ `Back`（背部）
- 新增 `HandSocket` 枚举：`Primary`（默认，primary socket）/ `Secondary`（第二 socket）
- 新增字段：`drawWeaponTriggerName`、`sheathWeaponTriggerName`、`sheathLocation`、`handSocket`、`isHeavy`
- 以后加新挂点只需：枚举加一项 → lookup 加一个 case → Inspector 选上

### WeaponEquipmentManager.cs 挂点路由
- 新增 `GetSheathHolder(Weapon)` — 根据 `sheathLocation` 返回 `weaponHolder` 或 `weaponHolder_Back`
- 新增 `GetHandSocket(Weapon)` — 根据 `handSocket` 返回 `weaponSocket` 或 `weaponSocket2`
- `DrawWeapon()` 用 `GetHandSocket()` 替代写死的 `weaponSocket`
- `SheathWeapon()` 用 `GetSheathHolder()` 替代写死的 `weaponHolder`
- `EquipWeapon()` 先实例化到腰间，再根据武器配置移到正确挂点

### 重型武器减速系统
- **Weapon.cs**：`isHeavy` bool 字段（大剑勾上）
- **WeaponEquipmentManager.cs**：`heavyWeaponSpeedMultiplier`（Inspector 可调，默认 0.7）+ `CurrentSpeedMultiplier` 公开属性（重型+已拔出=倍率，其余=1）
- **PlayerController.cs**：`targetMoveSpeed *= WeaponEquipmentManager.Instance.CurrentSpeedMultiplier`

### 大剑 Animator Controller 配置
- ArmsLayer 新增 `playerDraw_GreatSword` 和 `playerSheath_GreatSword` 两个 state
- Draw 流程：`DefaultState` → `drawGreatSword` trigger → `playerDraw_GreatSword` → HasExitTime=1 → `Combat`
- Sheath 流程：`Combat` → `sheathGreatSword` trigger → `playerSheath_GreatSword` → HasExitTime=1 → `DefaultState`
- **Bug 诊断**：ExitTime=1 但动画"还播一点点" → 根因是 `WP_Equip.fbx.meta` 和 `WP_Unequip.fbx.meta` 的 `loopTime: 1`，循环动画在末帧过渡期间重启导致。改 `loopTime: 0` 即可（用户自己改）

### 涉及文件
```
M Assets/Scripts/RPG/Weapon/Weapon.cs
M Assets/Scripts/Player/WeaponEquipmentManager.cs
M Assets/Scripts/Player/PlayerController.cs
M Assets/GameData/Animator/P&E/PlayerController.controller
+ Assets/Scripts/RPG/Weapon/GreatSword.cs
+ Assets/GameData/DataSO/Weapon_GreatSword.asset
+ Assets/Res/Prefabs/GreatSword.prefab
```
