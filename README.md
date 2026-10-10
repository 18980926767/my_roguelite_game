# 洞穴之咒

第一版是一层 10×10 的迷宫。扮演「只是一个普通人」，打赢守门的小 Boss 即通关。

## 环境

- [Godot 4.7 .NET](https://godotengine.org/download)（和工程里的 `Godot.NET.Sdk/4.7.2` 对齐）
- .NET 8 SDK

## 运行

用 Godot 4.7 .NET 打开本目录的 `project.godot`，再按运行。

规则库不依赖引擎，可以单独测：

```bash
dotnet test tests/CaveCurse.Core.Tests.csproj
```

## 操作

- 地图上用「窥视」看有路的隔壁类型。窥视不会写进地图。
- 「走向」只会走进有路的格子。走过的格子才留在地图上。
- 战斗前可以换武器。WASD 或方向键移动。
- 蓄力条满了按空格进入时停，可以一直不按。
- 时停里排普攻、随机属性、闪避。切武器不花行动点。跳过会结算，然后给你 2 秒只能自己走。
