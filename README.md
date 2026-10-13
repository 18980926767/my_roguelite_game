# 洞穴之咒

第一版是一层 10×10 的迷宫。扮演「只是一个普通人」，打赢守门的小 Boss 即通关。

## 环境

- [Godot 4.7 .NET](https://godotengine.org/download)（和工程里的 `Godot.NET.Sdk/4.7.2` 对齐）
- .NET 8 SDK

## 运行

用 Godot 4.7 .NET 打开本目录的 `project.godot`，再按运行。房间和门是 0x72 的 CC0 地牢图，高角度能看见墙面；玩家是棕发、拿银剑、默认朝右的像素人。界面底框仍用 Kenney Pixel UI。说明在 [assets/CREDITS.md](assets/CREDITS.md)。像素图按最近邻显示。仓库里的 PNG 可以直接读；用编辑器打开后 Godot 会再生成 `.import`。

规则库不依赖引擎，可以单独测：

```bash
dotnet test tests/CaveCurse.Core.Tests.csproj
```

## 操作

- 主画面是高角度的房间，地板在地面上，墙立着。方向键或 WASD 在地板上走，走到门口再点「窥视」或「进入」。
- 右下角是小地图，不能点。走过的房间才显示类型。
- 战斗前可以换武器。WASD 或方向键移动。
- 蓄力条满了按空格进入时停，可以一直不按。
- 时停里点普攻，或点左侧技能槽。没有位移按钮。切武器点左侧武器槽，不花行动点。跳过会结算。只有这时行动点还不少于 1，敌人才再停 2 秒。
