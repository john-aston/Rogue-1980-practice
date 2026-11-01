# Rogue 1980 Practice 代码导读

> 对应版本：2026-10-31 的 `main`。行号以当前文件为准；以后改代码行号会漂移，按方法名搜索即可。

## 1. 规模

| 范围 | 行数（含空行/注释） | 有效代码行 |
| --- | ---: | ---: |
| `src/`（游戏本体） | 3307 | 2719 |
| `tests/`（测试） | 841 | 677 |
| 合计 | 4148 | 3396 |

- 源文件 22 个：`Rogue.Core` 20 个，`Rogue.Cli` 2 个。
- 方法/构造函数约 **174 个**（不含属性）。
- 最大的文件：

| 文件 | 行数 | 说明 |
| --- | ---: | --- |
| `Game.cs` | 1134 | 回合、战斗、物品、陷阱、饥饿、存档，全都在这 |
| `LevelGenerator.cs` | 443 | 3×3 扇区地牢生成 |
| `Program.cs` | 429 | 输入、绘制、背包界面、结算 |
| `Level.cs` | 197 | 地图容器与查询 |
| `SaveData.cs` | 137 | 存档 DTO |

`Game.cs` 已经偏大。以后可以按“回合/战斗/物品/存档”拆成 partial class 或独立系统类；现在先不拆，避免打断阅读。

## 2. 建议阅读顺序

1. `Tile.cs` → `Room.cs` → `Level.cs`：先搞清楚地图是什么。
2. `Visibility.cs`：玩家能看见什么。
3. `LevelGenerator.cs`：地图怎么来的。
4. `Player.cs` → `Game.cs` 的构造函数、`TryMove`、`EndTurn`：一回合怎么转。
5. `Monster*.cs` → `Game.cs` 的战斗方法：怪怎么动、怎么打。
6. `Item*.cs` → `Game.cs` 的物品方法：捡、用、鉴定。
7. `Trap*.cs` → `Game.cs` 的 `TriggerTrap`/`HungerTick`。
8. `SaveData.cs` → `SaveStore.cs` → `Game.Snapshot/Restore`。
9. `Screen.cs` → `Program.cs`：最后一层“显示与输入”。

## 3. 一次运行的完整流程

```text
Program.Main
  └─ 检查终端 / 关闭光标 / 注册 Ctrl+C
      └─ Program.Run
          ├─ SaveStore.TryLoad
          │    ├─ 成功：Game.Restore(save) → 删除存档
          │    └─ 失败：new Game() → 生成第 1 层
          └─ while (true)
               ├─ Draw(screen, game)          // 画地图/物品/陷阱/怪物/玩家/状态栏
               ├─ Console.ReadKey
               ├─ 按键 → game.TryMove / PickUp / Quaff / Read / Search / TryDescend ...
               │    └─ 动作内部：改状态 → AddMessage → EndTurn
               │         └─ EndTurn: Turn++ → HungerTick → MonstersAct → 死亡检查
               ├─ 通关/死亡 → Finish（删档、记分、排行榜、结算界面）
               └─ Esc/Ctrl+Q → SaveStore.Save(game.Snapshot()) → 退出
```

**回合顺序**是最重要的约定：

```text
玩家动作（移动/攻击/用物品/等待）
  → Turn++
  → 饥饿结算（可能饿死）
  → 怪物行动（可能被打死）
  → 死亡消息
  → 回到主循环重绘
```

换层（`<`/`>`）和读档是例外：换层**不**推进回合，也不让怪物行动。

## 4. 按文件

### 4.1 `Tile.cs` — 地图字符表

| 行 | 成员 | 作用 |
| ---: | --- | --- |
| 5-12 | `Tiles.Rock/Floor/Corridor/...` | 所有地图字符的常量，避免到处写 `'.'`、`'#'` |
| 15 | `Tiles.IsWalkable(char)` | 哪些字符可以站人：地面、走廊、门、上下楼梯 |

### 4.2 `Dice.cs` — 骰子

|   行 | 方法                        | 作用                                    |
| --: | ------------------------- | ------------------------------------- |
|   7 | `Roll(count, sides, rng)` | 掷 `count` 个 `sides` 面骰并求和；战斗、HP、陷阱都用它 |

### 4.3 `Room.cs` — 房间矩形

|     行 | 成员                                  | 作用                          |
| ----: | ----------------------------------- | --------------------------- |
|     6 | `Room(x, y, width, height, isDark)` | 房间坐标包含四周墙；`isDark` 决定是否整间点亮 |
| 15-20 | `Right/Bottom/CenterX/CenterY`      | 右/下边界与中心点，生成走廊和放楼梯时用        |
|    24 | `Contains(x, y)`                    | 包含墙的矩形判定                    |
|    29 | `ContainsInterior(x, y)`            | 只算内部地面（玩家站的地方），`RoomAt` 用它  |

### 4.4 `Player.cs` — 玩家数据

|     行 | 成员                                              | 作用               |
| ----: | ----------------------------------------------- | ---------------- |
|   7-8 | `X` / `Y`                                       | 当前坐标             |
| 10-16 | `Hp/MaxHp/Level/Strength/Armor/Gold/Experience` | 状态栏与战斗数值         |
| 18-20 | `Inventory/Weapon/WornArmor`                    | 背包、当前武器、当前护甲     |
|    21 | `Food`                                          | 饥饿值，初始 1300      |
| 23-37 | `HasAmulet`                                     | 背包里是否有 `,`；通关判定用 |

> 注意：`Weapon` / `WornArmor` 是**从背包里拿出来**的，不在 `Inventory` 里。所以存档时可以分开存，不会出现同一个对象两个引用。

### 4.5 `MonsterDefinition.cs` / `MonsterTable.cs` / `Monster.cs` / `MonsterSpawner.cs`

| 文件:行                     | 成员                            | 作用                           |
| ------------------------ | ----------------------------- | ---------------------------- |
| `MonsterDefinition.cs:4` | `record MonsterDefinition`    | 怪物模板：符号、名字、等级、HP 骰、伤害骰、护甲、经验 |
| `MonsterTable.cs:6`      | `All`                         | 26 个字母怪物的静态表                 |
| `MonsterTable.cs:37`     | `Pick(depth, rng)`            | 只挑等级 ≤ 当前深度的怪                |
| `Monster.cs:5`           | `Monster(...)`                | 一只有坐标、HP、是否醒着的实例             |
| `Monster.cs:16`          | `IsDead`                      | `Hp <= 0`                    |
| `MonsterSpawner.cs:6`    | `Populate(level, depth, rng)` | 每个非起始房间 60% 放一只，避开楼梯/物品/怪物   |

### 4.6 `Item.cs` — 物品

|        行 | 成员                                   | 作用                            |
| -------: | ------------------------------------ | ----------------------------- |
|     3-11 | `ItemKind`                           | 药水/卷轴/食物/武器/护甲/金币/Amulet      |
|    13-19 | `PotionType`                         | 治疗/强效治疗/毒/经验                  |
|    21-26 | `ScrollType`                         | 鉴定/传送/魔法地图                    |
|       31 | `Item(...)`                          | 符号、真名、坐标                      |
|       43 | `Appearance`                         | 未鉴定时的外观（“a red potion”）       |
|       55 | `DisplayName(knowledge)`             | 已鉴定显示真名，否则显示外观                |
|       62 | `CreatePotion`                       | 按类型创建药水，外观从 `ItemKnowledge` 取 |
|       69 | `CreateScroll`                       | 按类型创建卷轴，标题从 `ItemKnowledge` 取 |
| 76/79/82 | `CreateFood/CreateGold/CreateAmulet` | 食物 500 营养、金币数量、Amulet         |
|    85/94 | `PotionName/ScrollName`              | 枚举到真名的映射                      |
|          |                                      |                               |

### 4.7 `ItemKnowledge.cs` — 本局的“外观表”和鉴定状态

|     行 | 成员                             | 作用                         |
| ----: | ------------------------------ | -------------------------- |
|    22 | `ItemKnowledge(rng)`           | 开局把颜色/卷轴标题洗牌，决定本局哪种药水是什么颜色 |
|    28 | 私有构造                           | 只给 `Restore` 用             |
|    32 | `Snapshot()`                   | 导出四组数组给存档                  |
|    40 | `Restore(data)`                | 从存档恢复                      |
|    52 | `Copy<T>`                      | 长度取小，防止旧存档越界               |
| 57/58 | `PotionAppearance/ScrollTitle` | 查本局外观                      |
| 60/61 | `IsKnown`                      | 这个类型是否已鉴定                  |
| 63/64 | `Identify`                     | 标记已鉴定                      |
|    66 | `Shuffle`                      | Fisher–Yates 洗牌            |

### 4.8 `ItemSpawner.cs` — 地上物品

| 行 | 方法 | 作用 |
| ---: | --- | --- |
| 19 | `Populate(level, depth, knowledge, rng)` | 每个非起始房间 50% 金币、35% 一件物品 |
| 41 | `CreateItem` | 按权重随机：药水 30%、卷轴 25%、食物 20%、武器 15%、护甲 10% |
| 74 | `Place` | 在房间里找一个空格，避开楼梯/物品/怪物 |

### 4.9 `Trap.cs` / `TrapSpawner.cs`

| 文件:行 | 成员 | 作用 |
| --- | --- | --- |
| `Trap.cs:4` | `TrapType` | 落穴/毒箭/毒针/传送 |
| `Trap.cs:12` | `Trap(...)` | 类型、坐标、是否已被搜索发现 |
| `TrapSpawner.cs:5` | `Populate(level, depth, rng)` | 每个非起始房间 30% 放一个陷阱，避开楼梯/物品/怪物 |

### 4.10 `Level.cs` — 一层地图

| 行 | 成员 | 作用 |
| ---: | --- | --- |
| 18 | `Level(width, height)` | 建空白地图，全部填 `Tiles.Rock` |
| 27/28 | `Width/Height` | 只读尺寸 |
| 30 | `Rooms` | 所有房间（点到暗房间、点亮整间都靠它） |
| 31 | `SecretDoors` | 还没被发现的秘密门坐标 |
| 34/35 | `UpStairs/DownStairs` | 上下楼梯坐标；最底层 `DownStairs` 是 `(-1,-1)` |
| 37 | `this[x, y]` | 读格子；内部是 `_tiles[y, x]`，**x 是列、y 是行** |
| 40 | `InBounds` | 坐标是否在地图内 |
| 44 | `IsWalkable` | 边界检查 + `Tiles.IsWalkable` |
| 47 | `Fill` | 全图填一个字符 |
| 58 | `Set` | 带边界检查的写格子 |
| 67 | `CarveCorridor` | 只把“岩石”改成走廊，不覆盖门/地面/楼梯 |
| 76 | `DrawRoom` | 画四周 `-`/`|` 墙，内部填 `.` |
| 96 | `AddRoom` | 登记房间 |
| 99 | `RoomAt` | 玩家站的位置属于哪个房间内部；走廊/门口返回 `null` |
| 112 | `IsSecretDoor` | 这格是不是尚未发现的秘密门 |
| 115 | `AddSecretDoor` | 把门伪装成墙并记录 |
| 122 | `RevealSecretDoor` | 揭开：改成 `+`，从集合里移除 |
| 132/134/136 | `AddMonster/RemoveMonster/MonsterAt` | 怪物列表 |
| 147/151/153 | `AddItem/RemoveItem/ItemAt` | 地上物品 |
| 166/168/170 | `AddTrap/RemoveTrap/TrapAt` | 陷阱 |
| 184 | `FromAscii` | 用字符串数组画测试地图，测试专用 |

### 4.11 `LevelGenerator.cs` — 地牢生成

| 行 | 方法 | 作用 |
| ---: | --- | --- |
| 14 | `Generate(width, height, seed)` | 总入口：房间 → 走廊 → 楼梯 |
| 26 | `PlaceRooms` | 3×3 扇区里随机放房间，10% 暗房，登记到 `Level` |
| 67 | `ChooseSectors` | 决定九宫格哪些格子有房间；至少 4 间，且必须连通 |
| 119 | `IsConnected` | 九宫格 BFS 连通性检查 |
| 175 | `ConnectRooms` | 先建一棵生成树，再随机加 20% 额外通道 |
| 259 | `ConnectPair` | 根据同行/同列分派横/竖连接 |
| 280 | `ConnectHorizontally` | 左右房间：两墙各开一门，中间空档拐弯 |
| 313 | `ConnectVertically` | 上下房间：同样的逻辑 |
| 346 | `PlaceDoor` | 20% 概率变成秘密门，否则 `+` |
| 358 | `PlaceStairs` | 随机房间放 `<`，离它最远的房间放 `>` |
| 414 | `RandomFloor` | 房间内部随机一格 |
| 417 | `Neighbors` | 九宫格的上下左右邻居 |
| 440 | `PairKey` | 把无向边规范化成有序四元组，供 `HashSet` 去重 |

### 4.12 `Visibility.cs` — 视野与记忆

| 行 | 成员 | 作用 |
| ---: | --- | --- |
| 10 | `Visibility(level)` | 为这一层分配 `visible/seen` 两个布尔数组 |
| 17/20 | `IsVisible/IsSeen` | 当前可见 / 曾经见过 |
| 24 | `Update(playerX, playerY)` | 点亮整间亮房，或只亮身边 3×3 |
| 54 | `Show` | 标记可见 + 已见 |
| 66 | `SnapshotSeen` | 摊平成一维数组给存档 |
| 82 | `RestoreSeen` | 从存档恢复 |
| 99 | `RevealAll` | 魔法地图：所有非岩石格子标为已见 |

### 4.13 `DungeonLevel.cs` — 一层 + 它的视野

| 行 | 成员 | 作用 |
| ---: | --- | --- |
| 6 | `DungeonLevel(Level)` | 把地图和它自己的 `Visibility` 绑在一起，换层后记忆不会串 |

### 4.14 `SaveData.cs` — 存档 DTO

| 行 | 成员 | 作用 |
| ---: | --- | --- |
| 6-14 | `SaveData` | 根对象：版本、种子、回合、层数、玩家、知识、所有楼层 |
| 17-31 | `PlayerData` | 玩家数值、背包、武器、护甲 |
| 34-39 | `KnowledgeData` | 四组外观/鉴定数组 |
| 42-57 | `LevelData` | 行字符串、房间、秘密门、楼梯、怪物、物品、陷阱、已见 |
| 60-66 | `RoomData` | 房间矩形 + 是否暗 |
| 69-73 | `SecretDoorData` | 坐标 + 伪装字符 |
| 76-82 | `MonsterData` | 符号 + 坐标 + HP + 是否醒 |
| 85-90 | `TrapData` | 类型 + 坐标 + 是否发现 |
| 93-107 | `ItemData` | 物品全部字段 |
| 109 | `ItemData.From(Item)` | 游戏对象 → 存档数据 |
| 126 | `ItemData.ToItem()` | 存档数据 → 游戏对象 |

### 4.15 `SaveStore.cs` — 存档与排行榜文件

| 行 | 成员 | 作用 |
| ---: | --- | --- |
| 6-13 | `ScoreEntry` | 排行榜一行：名字、分数、层数、金币、经验、是否通关、日期 |
| 27 | `SaveStore(directory?)` | 默认 `~/.rogue1980`，测试可以传临时目录 |
| 37 | `TryLoad(out SaveData?)` | 读 `save.json`；损坏/版本不对就删掉并返回 false |
| 68 | `Save(SaveData)` | 写 `save.json` |
| 80 | `DeleteSave()` | 删除存档（读档后立即调用，永久死亡） |
| 95 | `AddScore(entry)` | 读排行榜 → 加入 → 按分数降序取前 10 → 写回 |
| 116 | `LoadScores()` | 读 `scores.json`；损坏就返回空表 |

### 4.16 `Game.cs` — 核心（按职责分组）

#### 常量与字段

| 行 | 成员 | 作用 |
| ---: | --- | --- |
| 5-9 | `MapWidth/MapHeight/MaxDepth/WakeDistance/InventorySize` | 80×22、26 层、8 格唤醒、26 格背包 |
| 11 | `_rng` | 战斗/搜索/药水用的随机数 |
| 12 | `_seed` | 本局种子，换层和存档用 |
| 13 | `_levels` | 已访问楼层：层号 → `DungeonLevel` |
| 14 | `_messages` | 当前动作产生的消息，只显示最后一条 |
| 15-17 | `_hungryShown/_weakShown/_deathCause` | 饥饿提示去重、死因 |
| 18 | `_current` | 当前层 |

#### 属性

| 行 | 属性 | 作用 |
| ---: | --- | --- |
| 20/21 | `Player` / `Knowledge` | 玩家与鉴定知识 |
| 22/23 | `Turn` / `LevelNumber` | 回合数、当前层 |
| 24/25 | `IsDead` / `IsWon` | 输赢 |
| 27 | `Score` | 金币 + 经验 + 层数×10 + Amulet 1000 |
| 30/31 | `Map` / `Sight` | 当前层的图和视野（委托给 `_current`） |
| 32 | `Message` | 最后一条消息 |

#### 构造与读档

| 行 | 成员 | 作用 |
| ---: | --- | --- |
| 34 | `Game()` | 随机种子开局 |
| 38 | `Game(int seed)` | 指定种子；初始化知识、玩家、第 1 层 |
| 51 | `Game(Level, x, y)` | 测试专用：直接用给定地图和起点 |
| 932 | `Game(SaveData)` | 私有读档构造；重建玩家、知识、所有楼层、当前层 |

#### 玩家命令（都会清空消息、改状态、推进回合）

| 行 | 方法 | 作用 |
| ---: | --- | --- |
| 64 | `TryMove(dx, dy)` | 有怪就攻击；否则移动；踩金币自动捡；有陷阱就触发；最后 `EndTurn` |
| 119 | `Wait()` | 原地等一回合 |
| 126 | `PickUp()` | 捡脚下物品；金币直接加钱；背包满提示 |
| 160 | `Drop(index)` | 把背包第 index 件丢到脚下 |
| 185 | `Quaff(index)` | 喝药水 → 鉴定 → 应用效果 |
| 210 | `Read(index)` | 读卷轴 → 鉴定 → 鉴定/传送/魔法地图 |
| 252 | `Eat(index)` | 吃食物 → 加 `Food` → 重置饥饿提示 |
| 287 | `Wield(index)` | 装备武器；旧武器放回背包 |
| 317 | `Wear(index)` | 穿甲；AC 增加 |
| 348 | `TakeOff()` | 脱甲；AC 减少 |
| 372 | `Notify(message)` | 给 UI 用：设置一条消息（如“背包是空的”） |
| 378 | `Search()` | 搜八格：秘密门 1/5、陷阱 1/2；然后 `EndTurn` |
| 429 | `TryDescend()` | 站在 `>` 上下楼；不推进回合 |
| 446 | `TryAscend()` | 站在 `<` 上楼；第 1 层带 Amulet 则 `IsWon` |

#### 结算与系统

| 行 | 方法 | 作用 |
| ---: | --- | --- |
| 478 | `ApplyPotion` | 治疗/强效治疗/毒/经验四种效果 |
| 504 | `IdentifySomething` | 鉴定卷轴：找背包里第一个未鉴定的药水/卷轴 |
| 526 | `TeleportPlayer` | 随机找个可走的空格传送 |
| 545 | `TriggerTrap` | 落穴/毒箭/毒针/传送；落穴换层返回 true |
| 608 | `HungerTick` | Food--，300 饿、0 虚弱、-200 饿死 |
| 632 | `AddDeathMessage` | 根据 `_deathCause` 生成死亡消息 |
| 639 | `EndTurn` | Turn++ → 饥饿 → 怪物 → 死亡检查 |
| 655 | `MonstersAct` | 8 格内醒来、相邻攻击、否则朝玩家走一步 |
| 689 | `AttackMonster` | 玩家命中/伤害；击杀 → 移除 + 经验 |
| 720 | `MonsterAttack` | 怪物命中/伤害 |
| 738 | `GainExperience` | 加经验；`Experience >= Level*5` 升级，MaxHp +3 |
| 751 | `StepTowardPlayer` | 贪心追：先斜向，再横，再竖 |
| 769 | `TryStep` | 怪物单步：可走、无怪、不撞玩家 |
| 799/804 | `IsAdjacent/Distance` | 八方向相邻 / Chebyshev 距离 |
| 807 | `AddMessage` | 追加消息 |
| 809 | `MovePlayerTo` | 改坐标并刷新视野 |
| 816 | `LoadLevel` | 首次访问某层：生成地图 → 怪物 → 物品 → 陷阱 → 第 26 层放 Amulet |
| 850 | `PlaceAmulet` | 在非起始房间找个空格放 `,` |
| 888 | `Snapshot` | 保存：玩家 + 知识 + 所有楼层 |
| 922 | `Restore` | 读档入口（校验版本） |
| 987 | `SnapshotLevel` | 一层 → `LevelData`（行、房间、门、怪、物品、陷阱、已见） |
| 1070 | `BuildLevel` | `LevelData` → `DungeonLevel` |
| 1134 | `LevelSeed` | 层号 → 种子：`seed + number * 7919` |

### 4.17 `Screen.cs` — 80×24 帧缓冲

| 行 | 成员 | 作用 |
| ---: | --- | --- |
| 8-9 | `Width/Height` | 80×24 |
| 11-14 | `_chars/_colors/_shownChars/_shownColors` | 后缓冲与“已经画到终端”的前缓冲 |
| 16 | `Screen()` | 把前缓冲标成 `'\0'`，保证第一帧全画 |
| 27 | `Clear()` | 后缓冲填空格 + 灰色 |
| 39 | `Put(x, y, c, color)` | 越界忽略；写后缓冲 |
| 51 | `Write(x, y, text, color)` | 写字符串，超出右边界截断 |
| 60 | `Present()` | 逐格比较前后缓冲，只把变化的格子用 ANSI 定位+颜色写出去 |
| 109 | `ToAnsi(ConsoleColor)` | `ConsoleColor` → ANSI 前景色码（别直接拿枚举值当 ANSI） |

### 4.18 `Program.cs` — 输入与界面

| 行 | 成员 | 作用 |
| ---: | --- | --- |
| 7 | `Hint` | 消息行没内容时显示的提示 |
| 10 | `Main` | 终端检查、光标隐藏、Ctrl+C 收尾、调用 `Run` |
| 38 | `Run` | 读档/新局 → 主循环 → 按键分派 → 通关/死亡结算 |
| 190 | `Finish` | 删档、写排行榜、清屏、打印结算 + Top 10、等按键 |
| 246 | `Draw` | 画已见地形、物品、已发现陷阱、怪物、玩家、状态栏、消息 |
| 311 | `ShowInventory` | 画出背包两列（a–z），返回读到的按键 |
| 344 | `ChooseItem` | 空背包提示；否则调 `ShowInventory` 并解析字母 |
| 362 | `TileColor` | 地形颜色 |
| 372 | `DimColor` | 记忆里的地形用暗色 |
| 378 | `ItemColor` | 物品颜色（金币黄、药水洋红、卷轴白、食物暗黄、Amulet 黄） |
| 388 | `ReadDirection` | 方向键 + hjkl/yubn → (dx, dy) |
| 412 | `TerminalTooSmall` | 窗口小于 80×24 时提示 |
| 424 | `RestoreTerminal` | 恢复颜色/光标/清屏 |

## 5. 关键流程细看

### 5.1 一回合

```text
TryMove / PickUp / Quaff / ... 
  → _messages.Clear()
  → 改状态
  → EndTurn()
       Turn++
       HungerTick()          // 可能饿死
       if (!IsDead) MonstersAct()   // 醒来、追击、攻击
       if (IsDead) AddDeathMessage()
```

换层例外：`TryDescend/TryAscend` 改层、刷新视野、写消息，但不 `EndTurn`，所以怪物不动、饥饿不减。

### 5.2 战斗

```text
玩家：1d20 + 玩家等级 ≥ 10 + 怪物护甲
伤害：武器骰（没武器 1d4）+ max(0, (力量-16)/2)

怪物：1d20 + 怪物等级 ≥ 10 + 玩家护甲
伤害：怪物伤害骰
```

击杀 → `Map.RemoveMonster` → `GainExperience` → 可能升级（`MaxHp += 3`，回满）。

### 5.3 怪物 AI

- 距离（Chebyshev）> 8：继续睡。
- ≤ 8：醒来。
- 相邻（八方向）：攻击。
- 否则：先尝试斜向一步，再横向，再纵向；被墙/怪/玩家挡住就放弃这一步。

### 5.4 存档

```text
退出 → Game.Snapshot()
        ├─ PlayerData（数值、背包、武器、护甲）
        ├─ KnowledgeData（外观表 + 已鉴定）
        └─ 每个已访问楼层 LevelData（行字符串、房间、门、怪、物品、陷阱、Seen）
     → JSON → ~/.rogue1980/save.json

启动 → SaveStore.TryLoad → Game.Restore
     → 重建所有 DungeonLevel → 当前层 Sight.Update → 删除存档
```

### 5.5 通关

```text
第 26 层：PlaceAmulet 放 ','
  → g 拾取（进入背包，HasAmulet = true）
  → 一路 < 回到第 1 层
  → 站在 '<' 上按 '<'
  → TryAscend: LevelNumber <= 1 && HasAmulet → IsWon = true
  → Program.Finish(won: true)
```

## 6. 容易读错的点

1. **`Level[x, y]` 是列、行**，内部 `_tiles[y, x]`。看到 `_tiles` 时先想“先行后列”。
2. **`Game.Map` / `Game.Sight` 不是字段**，是委托到 `_current`；换层后自动指向新层。
3. **消息只显示最后一条**：`_messages` 是列表，但 `Message` 取 `[^1]`。所以“你打中蝙蝠”可能被“蝙蝠打中你”覆盖。
4. **`TryMove` 返回 false 不消耗回合**（撞墙）；返回 true 才推进。
5. **换层不消耗回合**，这是原版行为。
6. **存档里没有随机数状态**：读档后 `_rng` 用 `seed ^ turn*7919` 重新播种，后续随机结果会和未中断的存档不同。
7. **`Weapon`/`WornArmor` 不在背包里**，所以 `i` 看不到当前武器/护甲；这是有意的，换装时才交换。
8. **秘密门在 `Level.SecretDoors` 里**，地图上显示为墙；`RevealSecretDoor` 才变成 `+`。
9. **陷阱默认隐藏**，`IsFound` 后才画 `^`；踩上去仍会触发。
10. **`Game.cs` 1134 行**，读的时候按“命令 / 结算 / 战斗 / 存档”四块看，不要从头顺读。

## 7. 魔法数字速查

| 值 | 位置 | 含义 |
| ---: | --- | --- |
| 26 | `Game.MaxDepth` | 地牢层数，Amulet 在 26 层 |
| 8 | `Game.WakeDistance` | 怪物 8 格内醒来 |
| 26 | `Game.InventorySize` | 背包上限 |
| 1300 | `Player.Food` | 初始食物 |
| 300 / 0 / -200 | `HungerTick` | 饿 / 虚弱 / 饿死 |
| 500 | `Item.CreateFood` | 一份食物恢复量 |
| 60% | `MonsterSpawner` | 每个非起始房间放怪概率 |
| 50% / 35% | `ItemSpawner` | 金币 / 物品概率 |
| 30% | `TrapSpawner` | 陷阱概率 |
| 20% | `LevelGenerator.PlaceDoor` | 秘密门概率 |
| 1/5 | `Game.Search` | 每次搜索发现秘密门概率 |
| 1/2 | `Game.Search` | 每次搜索发现陷阱概率 |
| 1d4 + 力量 | `AttackMonster` | 空手伤害 |
| `Level*5` | `GainExperience` | 升级经验阈值 |
| +3 | `GainExperience` | 升级最大 HP |
| 1000 | `Game.Score` | Amulet 加分 |

## 8. 测试都在测什么

| 文件 | 覆盖 |
| --- | --- |
| `LevelTest.cs` | `FromAscii`、可走判定、秘密门揭开 |
| `LevelGenerateTest.cs` | 同种子同地图、连通性、经典字符、暗房/秘密门存在 |
| `GameTests.cs` | 起点在 `<`、撞墙、搜索秘密门 |
| `StairsTests.cs` | 下楼落点、上楼落点、楼层持久化、第 1 层不能上、最底层无 `>`、换层不耗回合 |
| `VisibilityTests.cs` | 亮房全亮、暗房 3×3、离开后记忆 |
| `MonsterTest.cs` | 生成、攻击/经验、追击、被打死、升级 |
| `ItemTests.cs` | 外观稳定、喝药鉴定、鉴定卷轴、武器/护甲、金币、拾取/丢弃、魔法地图 |
| `TrapTests.cs` | 传送陷阱、落穴换层、搜索发现、饿死、饥饿提示、生成位置 |
| `SaveTests.cs` | 快照/恢复、JSON 往返、秘密门/怪/物品/陷阱持久化、Amulet 通关、存档文件、排行榜 |
| `UnitTest1.cs` | 模板残留的空测试，可删 |
