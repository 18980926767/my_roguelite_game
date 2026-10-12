# 素材

房间、门、玩家和敌人是 [0x72](https://0x72.itch.io/dungeontileset-ii) 的 16x16 DungeonTileset II。高角度能看见墙面和正面的人，不是正上方，也不是菱形等距。许可是 CC0，可以商用，署名不强制。

界面底框仍用 [Kenney](https://kenney.nl) 的 Pixel UI。没有中文字形，文字仍用系统里的中文字体。

| 包 | 用在哪里 | 许可 |
| --- | --- | --- |
| 0x72 DungeonTileset II | 地板、立起来的砖墙、墙上的门；玩家；吐弹的守卫、向前冲的小怪、守门 | `dungeon/License.txt`，来源 https://0x72.itch.io/dungeontileset-ii |
| Kenney Pixel UI Pack | 顶栏、弹窗和按钮的九宫格 | `kenney/pixel-ui/License.txt` |

子弹和小地图仍是纯色。Kenney Tiny Dungeon 和 Monster Builder 还留在仓库里，房间和怪物已经不用它们。

音效和两层背景音乐是仓库里的 `tools/gen_audio.py` 自己合成的，方波加噪声，风格接近 sfxr。没有拿 Kenney 或其他人的采样。Kenney 的 CC0 音效包是单次音效和短旋律，拆不成「一直响的氛围」和「进战斗才进来的鼓」这两层，所以音乐用这段原创循环当占位。可以随便用，也可以再跑一遍脚本重新生成：

```bash
python3 tools/gen_audio.py
```

特效颜色从 [Resurrect 64](https://lospec.com/palette-list/resurrect-64)（Kerrie Lake）里取了大约 16 个偏暗的颜色。调色板本身的许可随 Lospec 页面；这里只用了色值，没有用别人的图。
