using CaveCurse.Core;
using Godot;

namespace CaveCurse;

public sealed partial class ExploreView : Control
{
    private readonly RunState _run;
    private readonly FloorMap _map;
    private readonly Action<Dir> _enter;
    private readonly Action<Dir> _peek;
    private readonly Label _stats;
    private readonly Label _prompt;
    private readonly Label _log;
    private readonly Button _peekButton;
    private readonly Button _enterButton;
    private int _x;
    private int _y;
    private Vector2 _player;
    private Vector2 _face = Vector2.Down;
    private Dir? _atDoor;
    private Rect2 _room;

    public ExploreView(RunState run, FloorMap map, int x, int y, Action<Dir> enter, Action<Dir> peek)
    {
        _run = run;
        _map = map;
        _x = x;
        _y = y;
        _enter = enter;
        _peek = peek;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
        TextureFilter = TextureFilterEnum.Nearest;

        var hud = UiKit.Chrome();
        hud.SetAnchorsPreset(LayoutPreset.TopWide);
        hud.OffsetLeft = 16;
        hud.OffsetTop = 8;
        hud.OffsetRight = -16;
        hud.OffsetBottom = 228;
        AddChild(hud);

        var top = new VBoxContainer();
        top.SetAnchorsPreset(LayoutPreset.TopWide);
        top.OffsetLeft = 36;
        top.OffsetTop = 12;
        top.OffsetRight = -36;
        top.OffsetBottom = 220;
        top.ClipContents = false;
        top.AddThemeConstantOverride("separation", 6);
        AddChild(top);

        _stats = UiKit.Label("");
        top.AddChild(_stats);

        _prompt = UiKit.Label("走到发亮的门口，再点窥视或进入。");
        top.AddChild(_prompt);

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);
        top.AddChild(row);

        _peekButton = UiKit.Button("窥视", OnPeek);
        _peekButton.CustomMinimumSize = new Vector2(160, 44);
        _peekButton.FocusMode = FocusModeEnum.None;
        row.AddChild(_peekButton);

        _enterButton = UiKit.Button("进入", OnEnter);
        _enterButton.CustomMinimumSize = new Vector2(160, 44);
        _enterButton.FocusMode = FocusModeEnum.None;
        row.AddChild(_enterButton);

        _log = UiKit.Label("你在出生的房间里。方向键或 WASD 走动。小地图在右下角，不能点。");
        top.AddChild(_log);

        _player = new Vector2(640, 360);
        RefreshHud();
    }

    public override void _Process(double delta)
    {
        FitRoom();
        var input = Vector2.Zero;
        if (Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left)) input.X -= 1;
        if (Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right)) input.X += 1;
        if (Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.Up)) input.Y -= 1;
        if (Input.IsKeyPressed(Key.S) || Input.IsKeyPressed(Key.Down)) input.Y += 1;
        if (input.LengthSquared() > 0f)
        {
            _face = input.Normalized();
            _player += _face * _run.MoveSpeed * (float)delta;
        }
        ClampPlayer();
        _atDoor = DoorUnderPlayer();
        var doorName = _atDoor == null ? "先走到门口" : NameOf(_atDoor.Value);
        _peekButton.Text = _atDoor == null ? "窥视" : $"窥视{doorName}";
        _enterButton.Text = _atDoor == null ? "进入" : $"进入{doorName}";
        _peekButton.Disabled = _atDoor == null;
        _enterButton.Disabled = _atDoor == null;
        QueueRedraw();
    }

    public void EnterFrom(int x, int y, Dir from)
    {
        _x = x;
        _y = y;
        FitRoom();
        _player = from switch
        {
            Dir.North => new Vector2(_room.GetCenter().X, _room.End.Y - 70),
            Dir.South => new Vector2(_room.GetCenter().X, _room.Position.Y + 70),
            Dir.West => new Vector2(_room.End.X - 70, _room.GetCenter().Y),
            _ => new Vector2(_room.Position.X + 70, _room.GetCenter().Y),
        };
        RefreshHud();
        QueueRedraw();
    }

    public void SetPeek(string text) => _prompt.Text = text;

    public void SetLog(string text) => _log.Text = text;

    public void Refresh() => RefreshHud();

    private void OnPeek()
    {
        if (_atDoor != null)
            _peek(_atDoor.Value);
    }

    private void OnEnter()
    {
        if (_atDoor != null)
            _enter(_atDoor.Value);
    }

    private void FitRoom()
    {
        var size = Size.X < 200 ? GetViewportRect().Size : Size;
        _room = new Rect2(72, 336, Mathf.Max(480, size.X - 160), Mathf.Max(220, size.Y - 468));
    }

    private void ClampPlayer()
    {
        _player = new Vector2(
            Mathf.Clamp(_player.X, _room.Position.X + 18, _room.End.X - 18),
            Mathf.Clamp(_player.Y, _room.Position.Y + 18, _room.End.Y - 18));
    }

    private Dir? DoorUnderPlayer()
    {
        foreach (var dir in new[] { Dir.North, Dir.East, Dir.South, Dir.West })
        {
            if (_map.Step(_x, _y, dir) == null)
                continue;
            if (DoorRect(dir).Grow(18).HasPoint(_player))
                return dir;
        }
        return null;
    }

    private Rect2 DoorRect(Dir dir)
    {
        var center = _room.GetCenter();
        return dir switch
        {
            Dir.North => new Rect2(center.X - 46, _room.Position.Y - 8, 92, 28),
            Dir.South => new Rect2(center.X - 46, _room.End.Y - 20, 92, 28),
            Dir.West => new Rect2(_room.Position.X - 8, center.Y - 46, 28, 92),
            _ => new Rect2(_room.End.X - 20, center.Y - 46, 28, 92),
        };
    }

    public override void _Draw()
    {
        FitRoom();
        DrawRect(new Rect2(Vector2.Zero, Size), new Color("16141c"));
        Look.Room(this, _room);
        foreach (var dir in new[] { Dir.North, Dir.East, Dir.South, Dir.West })
        {
            if (_map.Step(_x, _y, dir) == null)
                continue;
            Look.DoorGap(this, DoorRect(dir), _atDoor == dir, _room);
        }

        Look.Person(this, _player, _face);
        DrawMinimap();
    }

    private void DrawMinimap()
    {
        const float cell = 14f;
        var origin = new Vector2(Size.X - 24 - FloorMap.Size * (cell + 2), Size.Y - 24 - FloorMap.Size * (cell + 2));
        if (origin.X < 0)
            origin.X = 24;
        for (var y = 0; y < FloorMap.Size; y++)
        {
            for (var x = 0; x < FloorMap.Size; x++)
            {
                var rect = new Rect2(origin.X + x * (cell + 2), origin.Y + y * (cell + 2), cell, cell);
                var here = x == _x && y == _y;
                var room = _map.At(x, y);
                Color color;
                if (here)
                    color = new Color("f2d48a");
                else if (!room.Visited)
                    color = new Color("2a2a34");
                else if (room.CombatCleared && room.Kind is CellKind.Combat or CellKind.Gate)
                    color = new Color("5c5c66");
                else
                    color = room.Kind switch
                    {
                        CellKind.Start => new Color("7dcea0"),
                        CellKind.Combat => new Color("e07a7a"),
                        CellKind.Shop => new Color("7eb6d6"),
                        CellKind.Event => new Color("e0c36a"),
                        CellKind.Gate => new Color("c39bd3"),
                        _ => Colors.White,
                    };
                DrawRect(rect, color);
            }
        }
    }

    private void RefreshHud()
    {
        var weapon = _run.ActiveWeapon;
        var room = _map.At(_x, _y);
        _stats.Text =
            $"洞穴之咒    {FloorMap.KindLabel(room.Kind)}\n" +
            $"生命 {_run.Hp}/{_run.MaxHp}    魔力 {_run.Mana}/{_run.MaxMana}    护盾 {_run.Shield}\n" +
            $"力量 {_run.Attack}    防御 {_run.Defense}    速度 {(int)(_run.MoveSpeed / RunState.MoveSpeedPerPoint)}    金币 {_run.Gold}\n" +
            $"手持 {(weapon == null ? "空" : weapon.Name)}";
    }

    private static string NameOf(Dir dir) => dir switch
    {
        Dir.North => "北",
        Dir.East => "东",
        Dir.South => "南",
        _ => "西",
    };
}
