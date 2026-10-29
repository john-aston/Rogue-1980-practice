# Rogue 1980 Practice

纯 ASCII 地牢游戏，用 C# 从零实现，致敬 1980 年的 Rogue。

> An ASCII roguelike built from scratch in C#, inspired by Rogue (1980).

这是一个学习项目：目标不是做商业游戏，而是用一个"小而完整"的项目练 C#——
集合与泛型、模式匹配、可空引用、单元测试、项目分层，都在这里有真实的用武之地。

- 纯字符终端，不依赖任何图形库

- 回合制：一次按键 = 一个动作
- 机制向原版靠拢：随机地牢、永久死亡、物品鉴定、饥饿

## 当前画面
<img width="400" height="211" alt="录屏2026-10-27 23 45 11 2" src="https://github.com/user-attachments/assets/84e4d187-6a9d-4ba7-ad3f-7a23159b2739" />




## 环境要求

- .NET SDK 10.0 或更高（`global.json` 锁定 10.0.401）
- 终端至少 80 列 × 24 行，支持 ANSI 颜色
- macOS / Linux / Windows Terminal

## 快速开始

```bash
git clone https://github.com/john-aston/Rogue-1980-practice.git
cd Rogue-1980-practice
dotnet run --project src/Rogue.Cli
```

## 地图图例

| 字符 | 含义 |
| --- | --- |
| `@` | 玩家 |
| `#` | 岩层 / 墙 |
| `.` | 房间或走廊地面 |
| `>` | 向下的楼梯（尚未实现换层） |
| `+` | 门（可行走判定已支持，生成器还没用上） |

## 目录结构

```text
Rogue/
├── global.json                  # 锁定 .NET SDK 版本（10.0.401）
├── Rogue.slnx                   # 解决方案：只是一份“项目清单”
├── Directory.Build.props        # 所有项目共享的 MSBuild 设置（目前是空的）
├── .editorconfig                # 代码风格规则（缩进、命名、大括号位置…）
├── .gitignore                   # 哪些文件不要进 git
├── LICENSE                      # MIT
│
├── src/
│   ├── Rogue.Core/              # ★ 规则层：不依赖 Console
│   │   ├── Tile.cs              # 地图字符常量 + 可走判定
│   │   ├── Level.cs             # 地图数据、画房间、挖走廊
│   │   ├── LevelGenerator.cs    # 3×3 扇区随机生成
│   │   ├── Player.cs            # 玩家数据（坐标、HP…）
│   │   ├── Game.cs              # 把上面几个组合起来，提供 TryMove
│   │   ├── Rogue.Core.csproj    # 项目定义
│   │   ├── bin/                 # ← 生成
│   │   └── obj/                 # ← 生成
│   │
│   └── Rogue.Cli/               # ★ 界面层：只负责终端
│       ├── Program.cs           # 主循环、读键、调 Draw
│       ├── Screen.cs            # 80×24 帧缓冲 + ANSI 差分输出
│       ├── Rogue.Cli.csproj     # 项目定义（引用 Core）
│       ├── bin/                 # ← 生成（这里面的东西才是“能跑的”）
│       └── obj/                 # ← 生成
│
├── tests/
│   └── Rogue.Core.Tests/
│       ├── LevelTest.cs         # Level 的测试
│       ├── LevelGenerateTest.cs # 生成器的测试（种子、连通性）
│       ├── GameTests.cs         # 移动规则测试
│       ├── UnitTest1.cs         # 模板残留的空测试
│       ├── Rogue.Core.Tests.csproj
│       ├── bin/                 # ← 生成（5.2M，因为把测试框架也复制进来了）
│       └── obj/                 # ← 生成
│
└── .git/                        # 版本库内部数据（不是“项目文件”）
```

## 设计说明

- **规则与界面分离**：`Rogue.Core` 不知道终端的存在，所以规则可以用测试直接验证，将来也能无头跑。
- **帧缓冲 + 差分刷新**：每帧先在内存里画好 80×24 个字符，再只把变化的格子用 ANSI 序列写到终端，避免闪烁。
- **一格一字符**：地图是 `char[,]`，1 格 = 1 个字符。游戏内文本一律 ASCII——全角字符占两列，会把网格撑破。
- **回合制**：目前"一次移动 = 一回合"，怪物行动和各类结算会在后续接进同一个回合入口。

## 路线图

- [x] 80×24 帧缓冲与 ANSI 差分渲染
- [x] 房间 / 走廊地图与碰撞判定
- [x] 八方向移动
- [x] 单元测试（地图规则、移动规则）
- [ ] 随机地牢生成（3×3 扇区 + 走廊 + 门）
- [ ] 楼梯与换层
- [ ] 视野与已探索记忆
- [ ] 怪物、AI、战斗、经验
- [ ] 物品、随机外观、鉴定
- [ ] 饥饿、陷阱
- [ ] 存档、死亡结算、排行榜

## 已知限制

- 地图是硬编码的 `Level.CreateDemo()`，还不是随机生成。
- 没有怪物、物品、视野。
- 撞墙是否应该消耗回合还没和原版核对，目前不消耗。
- 屏幕固定 80×24，调整窗口大小不会自适应。
- WASD 没有启用：`w` / `a` / `s` / `d` 在原版里对应持武器、搜索等命令，加进来会冲突，等引入键位表时再决定。
