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
    private bool _exitsLit;
    private int _doorPick = 1;
    public bool AcceptKeys { get; set; } = true;

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

        _prompt = UiKit.Label("走到门口。[1] 窥视，[J] 进入。Tab 切换当前项。");
        top.AddChild(_prompt);

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);
        top.AddChild(row);

        _peekButton = UiKit.Button("窥视", OnPeek);
        _peekButton.CustomMinimumSize = new Vector2(220, 44);
        _peekButton.FocusMode = FocusModeEnum.None;
        row.AddChild(_peekButton);

        _enterButton = UiKit.Button("进入", OnEnter);
        _enterButton.CustomMinimumSize = new Vector2(220, 44);
        _enterButton.FocusMode = FocusModeEnum.None;
        row.AddChild(_enterButton);

        _log = UiKit.Label("你在出生的房间里。WASD 或方向键走动。小地图在右下角，不能点。按 V 调音量。");
        top.AddChild(_log);

        _player = new Vector2(640, 360);
        RefreshHud();
    }

    public override void _Process(double delta)
    {
        FitRoom();
        if (AcceptKeys && Visible && AudioManager.Current?.MenuOpen != true)
        {
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
        }
        ClampPlayer();
        _atDoor = DoorUnderPlayer();
        if (_atDoor == null)
            _doorPick = 1;
        var doorName = _atDoor == null ? "" : NameOf(_atDoor.Value);
        var peek = _atDoor == null ? "[1] 窥视" : $"[1] 窥视{doorName}";
        var enter = _atDoor == null ? "[J] 进入" : $"[J] 进入{doorName}";
        var atDoor = _atDoor != null;
        ChoiceList.Paint(_peekButton, peek, atDoor && _doorPick == 0);
        ChoiceList.Paint(_enterButton, enter, atDoor && _doorPick == 1);
        _peekButton.Disabled = !atDoor;
        _enterButton.Disabled = !atDoor;
        QueueRedraw();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!AcceptKeys || !Visible || _atDoor == null || AudioManager.Current?.MenuOpen == true)
            return;
        if (@event is not InputEventKey key)
            return;
        var digit = Keys.Digit(key);
        var tab = Keys.Tab(key);
        if (digit == 1)
            OnPeek();
        else if (digit == 2)
            OnEnter();
        else if (tab != 0)
            _doorPick = _doorPick == 0 ? 1 : 0;
        else if (Keys.Confirm(key))
        {
            if (_doorPick == 0)
                OnPeek();
            else
                OnEnter();
        }
        else
            return;
        GetViewport().SetInputAsHandled();
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
        _exitsLit = false;
        _doorPick = 1;
        _prompt.Text = "走到门口。[1] 窥视，[J] 进入。Tab 切换当前项。";
        RefreshHud();
        QueueRedraw();
    }

    public void CelebrateClear()
    {
        _exitsLit = true;
        _prompt.Text = "房间清空。走到门口，按 [J] 进入。";
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
            if (Look.DoorRect(_room, dir).Grow(18).HasPoint(_player))
                return dir;
        }
        return null;
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
            Look.DoorGap(this, Look.DoorRect(_room, dir), _exitsLit || _atDoor == dir, _room);
        }

        Look.Person(this, _player, _face);
        if (_run.ActiveWeapon is { IsOrdinarySword: true } sword)
            Look.Sword(this, _player, _face, SwordImbue.Tier(sword.Stain));
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
            $"手持 {(weapon == null ? "空" : weapon.Name)}{SwordHud()}";
    }

    private string SwordHud()
    {
        var sword = _run.Backpack.FirstOrDefault(item => item.IsOrdinarySword);
        return sword == null ? "" : $"    普通剑 {SwordImbue.HudLine(sword.Stain)}";
    }

    private static string NameOf(Dir dir) => dir switch
    {
        Dir.North => "北",
        Dir.East => "东",
        Dir.South => "南",
        _ => "西",
    };
}
