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
    private readonly Label _notice;
    private readonly Control _doorBar;
    private readonly Label _prompt;
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
        hud.OffsetBottom = 56;
        AddChild(hud);

        var status = new HBoxContainer();
        status.SetAnchorsPreset(LayoutPreset.FullRect);
        status.OffsetLeft = 20;
        status.OffsetTop = 6;
        status.OffsetRight = -20;
        status.OffsetBottom = -6;
        status.AddThemeConstantOverride("separation", 16);
        hud.AddChild(status);

        _stats = UiKit.Label("");
        _stats.VerticalAlignment = VerticalAlignment.Center;
        _stats.SizeFlagsVertical = SizeFlags.ExpandFill;
        status.AddChild(_stats);

        _notice = UiKit.Label("");
        _notice.HorizontalAlignment = HorizontalAlignment.Right;
        _notice.VerticalAlignment = VerticalAlignment.Center;
        _notice.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
        _notice.SizeFlagsVertical = SizeFlags.ExpandFill;
        status.AddChild(_notice);

        _doorBar = UiKit.Chrome();
        _doorBar.SetAnchorsPreset(LayoutPreset.BottomWide);
        _doorBar.OffsetLeft = 16;
        _doorBar.OffsetTop = -84;
        _doorBar.OffsetRight = -200;
        _doorBar.OffsetBottom = -12;
        _doorBar.Visible = false;
        AddChild(_doorBar);

        var doorRow = new HBoxContainer();
        doorRow.SetAnchorsPreset(LayoutPreset.FullRect);
        doorRow.OffsetLeft = 18;
        doorRow.OffsetTop = 10;
        doorRow.OffsetRight = -18;
        doorRow.OffsetBottom = -10;
        doorRow.AddThemeConstantOverride("separation", 12);
        _doorBar.AddChild(doorRow);

        _prompt = UiKit.Label("可以窥视或进入。方向键或 WASD 走动。小地图在右下角，不能点。");
        _prompt.VerticalAlignment = VerticalAlignment.Center;
        _prompt.SizeFlagsVertical = SizeFlags.ExpandFill;
        doorRow.AddChild(_prompt);

        _peekButton = UiKit.Button("窥视", OnPeek);
        _peekButton.CustomMinimumSize = new Vector2(148, 40);
        _peekButton.FocusMode = FocusModeEnum.None;
        doorRow.AddChild(_peekButton);

        _enterButton = UiKit.Button("进入", OnEnter);
        _enterButton.CustomMinimumSize = new Vector2(148, 40);
        _enterButton.FocusMode = FocusModeEnum.None;
        doorRow.AddChild(_enterButton);

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
        _doorBar.Visible = _atDoor != null;
        if (_atDoor != null)
        {
            var doorName = NameOf(_atDoor.Value);
            _peekButton.Text = $"窥视{doorName}";
            _enterButton.Text = $"进入{doorName}";
        }
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

    public void SetLog(string text) => _notice.Text = text;

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
        // 顶上只留一行状态。墙还要高出两格，所以地板从这里开始，下面仍给小地图留空。
        const float roomTop = 160f;
        const float roomBottom = 132f;
        _room = new Rect2(72, roomTop, Mathf.Max(480, size.X - 160), Mathf.Max(220, size.Y - roomTop - roomBottom));
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
        _stats.Text = $"生命 {_run.Hp}/{_run.MaxHp}    魔力 {_run.Mana}/{_run.MaxMana}    金币 {_run.Gold}";
    }

    private static string NameOf(Dir dir) => dir switch
    {
        Dir.North => "北",
        Dir.East => "东",
        Dir.South => "南",
        _ => "西",
    };
}
