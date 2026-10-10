using CaveCurse.Core;
using Godot;

namespace CaveCurse;

public sealed partial class ExploreView : Control
{
    private readonly RunState _run;
    private readonly FloorMap _map;
    private readonly Action<Dir> _move;
    private readonly Action<Dir> _peek;
    private readonly Label _stats;
    private readonly Label _peekText;
    private readonly Label _log;
    private readonly Button[,] _cells = new Button[FloorMap.Size, FloorMap.Size];
    private int _x;
    private int _y;

    public ExploreView(RunState run, FloorMap map, int x, int y, Action<Dir> move, Action<Dir> peek)
    {
        _run = run;
        _map = map;
        _x = x;
        _y = y;
        _move = move;
        _peek = peek;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var backdrop = UiKit.Panel(new Color("14141c"));
        backdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(backdrop);

        var grid = new GridContainer
        {
            Columns = FloorMap.Size,
            Position = new Vector2(28, 28),
        };
        grid.AddThemeConstantOverride("h_separation", 4);
        grid.AddThemeConstantOverride("v_separation", 4);
        AddChild(grid);

        for (var cy = 0; cy < FloorMap.Size; cy++)
        {
            for (var cx = 0; cx < FloorMap.Size; cx++)
            {
                var button = new Button
                {
                    CustomMinimumSize = new Vector2(48, 48),
                    Theme = UiKit.Theme,
                    Disabled = true,
                };
                _cells[cx, cy] = button;
                grid.AddChild(button);
            }
        }

        var side = new VBoxContainer
        {
            Position = new Vector2(620, 28),
            Size = new Vector2(620, 660),
        };
        AddChild(side);
        side.AddChild(UiKit.Label("洞穴之咒  ·  第一层", 22));
        _stats = UiKit.Label("");
        side.AddChild(_stats);
        side.AddChild(UiKit.Label("站在格子里，只能从有路的门窥视隔壁。窥视不会写进地图。"));
        _peekText = UiKit.Label("还没有窥视。");
        side.AddChild(_peekText);

        foreach (var dir in new[] { Dir.North, Dir.East, Dir.South, Dir.West })
        {
            var row = new HBoxContainer();
            var captured = dir;
            row.AddChild(UiKit.Button($"窥视{NameOf(dir)}", () => _peek(captured)));
            row.AddChild(UiKit.Button($"走向{NameOf(dir)}", () => _move(captured)));
            side.AddChild(row);
        }

        _log = UiKit.Label("你在最外圈醒来。金币是 0。身上只有一把非常普通的剑。");
        _log.CustomMinimumSize = new Vector2(600, 180);
        side.AddChild(_log);
        Refresh();
    }

    public void SetPlayer(int x, int y)
    {
        _x = x;
        _y = y;
        Refresh();
    }

    public void SetPeek(string text)
    {
        _peekText.Text = text;
    }

    public void SetLog(string text)
    {
        _log.Text = text;
    }

    public void Refresh()
    {
        var weapon = _run.ActiveWeapon;
        _stats.Text =
            $"生命 {_run.Hp}/{_run.MaxHp}    蓝 {_run.Mana}/{_run.MaxMana}    护盾 {_run.Shield}\n" +
            $"攻击 {_run.Attack}    防御 {_run.Defense}    移速 {(int)_run.MoveSpeed}    金币 {_run.Gold}\n" +
            $"手持 {(weapon == null ? "空" : weapon.Name)}    倍率 {weapon?.BasicMultiplier:0.00}";

        for (var y = 0; y < FloorMap.Size; y++)
        {
            for (var x = 0; x < FloorMap.Size; x++)
            {
                var cell = _map.At(x, y);
                var button = _cells[x, y];
                if (!cell.Visited)
                {
                    button.Text = "";
                    button.Modulate = new Color("2a2a34");
                    continue;
                }

                button.Text = cell.CombatCleared && cell.Kind is CellKind.Combat or CellKind.Gate
                    ? "过"
                    : Short(cell.Kind);
                var here = x == _x && y == _y;
                button.Modulate = here ? new Color("f2d48a") : ColorOf(cell);
            }
        }
    }

    private static string NameOf(Dir dir) => dir switch
    {
        Dir.North => "北",
        Dir.East => "东",
        Dir.South => "南",
        _ => "西",
    };

    private static string Short(CellKind kind) => kind switch
    {
        CellKind.Start => "起",
        CellKind.Combat => "战",
        CellKind.Shop => "店",
        CellKind.Event => "事",
        CellKind.Gate => "门",
        _ => "?",
    };

    private static Color ColorOf(FloorCell cell)
    {
        if (cell.CombatCleared && cell.Kind is CellKind.Combat or CellKind.Gate)
            return new Color("5c5c66");
        return cell.Kind switch
        {
            CellKind.Start => new Color("7dcea0"),
            CellKind.Combat => new Color("e07a7a"),
            CellKind.Shop => new Color("7eb6d6"),
            CellKind.Event => new Color("e0c36a"),
            CellKind.Gate => new Color("c39bd3"),
            _ => Colors.White,
        };
    }
}
