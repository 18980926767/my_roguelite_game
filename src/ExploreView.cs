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
    private readonly Button[] _go = new Button[4];
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
        MouseFilter = MouseFilterEnum.Ignore;

        var backdrop = UiKit.Panel(new Color("12121a"));
        backdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        backdrop.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(backdrop);

        var root = new HBoxContainer();
        root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        root.OffsetLeft = 28;
        root.OffsetTop = 24;
        root.OffsetRight = -28;
        root.OffsetBottom = -24;
        root.AddThemeConstantOverride("separation", 28);
        AddChild(root);

        var mapSide = new CenterContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        root.AddChild(mapSide);

        var grid = new GridContainer { Columns = FloorMap.Size };
        grid.AddThemeConstantOverride("h_separation", 6);
        grid.AddThemeConstantOverride("v_separation", 6);
        mapSide.AddChild(grid);

        for (var cy = 0; cy < FloorMap.Size; cy++)
        {
            for (var cx = 0; cx < FloorMap.Size; cx++)
            {
                var bx = cx;
                var by = cy;
                var button = new Button
                {
                    CustomMinimumSize = new Vector2(58, 58),
                    Theme = UiKit.Theme,
                    FocusMode = FocusModeEnum.None,
                };
                button.Pressed += () => TryStep(bx, by);
                _cells[cx, cy] = button;
                grid.AddChild(button);
            }
        }

        var side = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(420, 0),
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        side.AddThemeConstantOverride("separation", 10);
        root.AddChild(side);
        side.AddChild(UiKit.Label("洞穴之咒  ·  第一层", 28));
        _stats = UiKit.Label("");
        side.AddChild(_stats);
        side.AddChild(UiKit.Label("方向键或 WASD 走动。发亮的邻格也能点。没路的方向走不了。"));
        _peekText = UiKit.Label("还没有窥视。");
        side.AddChild(_peekText);

        var dirs = new[] { Dir.North, Dir.East, Dir.South, Dir.West };
        for (var i = 0; i < dirs.Length; i++)
        {
            var captured = dirs[i];
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 8);
            var peekButton = UiKit.Button($"窥视{NameOf(captured)}", () => _peek(captured));
            peekButton.CustomMinimumSize = new Vector2(160, 44);
            peekButton.FocusMode = FocusModeEnum.None;
            var go = UiKit.Button($"走向{NameOf(captured)}", () => _move(captured));
            go.CustomMinimumSize = new Vector2(160, 44);
            go.FocusMode = FocusModeEnum.None;
            _go[i] = go;
            row.AddChild(peekButton);
            row.AddChild(go);
            side.AddChild(row);
        }

        _log = UiKit.Label("你在最外圈醒来。用方向键走进有路的格子。");
        _log.CustomMinimumSize = new Vector2(400, 160);
        _log.SizeFlagsVertical = SizeFlags.ExpandFill;
        side.AddChild(_log);
        Refresh();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } key)
            return;
        var dir = key.Keycode switch
        {
            Key.W or Key.Up => Dir.North,
            Key.D or Key.Right => Dir.East,
            Key.S or Key.Down => Dir.South,
            Key.A or Key.Left => Dir.West,
            _ => (Dir?)null,
        };
        if (dir == null)
            return;
        _move(dir.Value);
        GetViewport().SetInputAsHandled();
    }

    public void SetPlayer(int x, int y)
    {
        _x = x;
        _y = y;
        Refresh();
    }

    public void SetPeek(string text) => _peekText.Text = text;

    public void SetLog(string text) => _log.Text = text;

    public void Refresh()
    {
        var weapon = _run.ActiveWeapon;
        _stats.Text =
            $"生命 {_run.Hp}/{_run.MaxHp}    蓝 {_run.Mana}/{_run.MaxMana}    护盾 {_run.Shield}\n" +
            $"攻击 {_run.Attack}    防御 {_run.Defense}    移速 {(int)_run.MoveSpeed}    金币 {_run.Gold}\n" +
            $"手持 {(weapon == null ? "空" : weapon.Name)}";

        for (var y = 0; y < FloorMap.Size; y++)
        {
            for (var x = 0; x < FloorMap.Size; x++)
            {
                var cell = _map.At(x, y);
                var button = _cells[x, y];
                var here = x == _x && y == _y;
                var linked = CanStep(x, y);
                button.Disabled = !linked;

                if (here)
                {
                    button.Text = "你";
                    button.Modulate = new Color("f2d48a");
                    continue;
                }

                if (!cell.Visited)
                {
                    button.Text = linked ? "?" : "";
                    button.Modulate = linked ? new Color("4a4a5c") : new Color("22222c");
                    continue;
                }

                button.Text = cell.CombatCleared && cell.Kind is CellKind.Combat or CellKind.Gate
                    ? "过"
                    : Short(cell.Kind);
                button.Modulate = ColorOf(cell);
            }
        }

        var dirs = new[] { Dir.North, Dir.East, Dir.South, Dir.West };
        for (var i = 0; i < dirs.Length; i++)
        {
            var open = _map.Step(_x, _y, dirs[i]) != null;
            _go[i].Disabled = !open;
            _go[i].Text = open ? $"走向{NameOf(dirs[i])}" : $"{NameOf(dirs[i])}没路";
        }
    }

    private void TryStep(int x, int y)
    {
        if (!CanStep(x, y))
            return;
        if (y == _y - 1)
            _move(Dir.North);
        else if (x == _x + 1)
            _move(Dir.East);
        else if (y == _y + 1)
            _move(Dir.South);
        else if (x == _x - 1)
            _move(Dir.West);
    }

    private bool CanStep(int x, int y) =>
        Math.Abs(x - _x) + Math.Abs(y - _y) == 1 && _map.HasEdge(_x, _y, x, y);

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
