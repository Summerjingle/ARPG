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

## 待办
- [ ] Scripts 目录整理（按之前列的方案）
- [ ] EnemyHeathBar 拼写修正
- [ ] EnemyTest.cs 等测试脚本清理
