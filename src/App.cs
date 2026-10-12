using CaveCurse.Core;
using Godot;

namespace CaveCurse;

public partial class App : Control
{
    private readonly Random _rng = new();
    private RunState _run = null!;
    private FloorMap _map = null!;
    private int _x;
    private int _y;
    private ExploreView _explore = null!;
    private Control? _overlay;

    public override void _Ready()
    {
        GetWindow().Title = "洞穴之咒";
        StartRun();
    }

    private void StartRun()
    {
        Engine.TimeScale = 1;
        _run = RunState.CreateNew();
        _map = FloorGenerator.Generate(_rng);
        _x = _map.SpawnX;
        _y = _map.SpawnY;
        ClearScreens();
        _explore = new ExploreView(_run, _map, _x, _y, Move, Peek);
        AddChild(_explore);
    }

    private void ClearScreens()
    {
        _overlay?.QueueFree();
        _overlay = null;
        if (IsInstanceValid(_explore))
            _explore.QueueFree();
        foreach (var child in GetChildren())
            child.QueueFree();
    }

    private void Peek(Dir dir)
    {
        var step = _map.Step(_x, _y, dir);
        if (step == null)
        {
            _explore.SetPeek($"{NameOf(dir)}边没有路，地图上仍是迷雾。");
            return;
        }
        var cell = _map.At(step.Value.X, step.Value.Y);
        _explore.SetPeek($"{NameOf(dir)}：{FloorMap.KindLabel(cell.Kind)}。这不会写进地图。");
    }

    private void Move(Dir dir)
    {
        var step = _map.Step(_x, _y, dir);
        if (step == null)
        {
            _explore.SetLog("这边没有路。");
            return;
        }
        _x = step.Value.X;
        _y = step.Value.Y;
        var cell = _map.At(_x, _y);
        cell.Visited = true;
        _explore.EnterFrom(_x, _y, dir);
        Enter(cell);
    }

    private void Enter(FloorCell cell)
    {
        switch (cell.Kind)
        {
            case CellKind.Start:
                _explore.SetLog("这是你醒来的格子。");
                break;
            case CellKind.Combat when cell.CombatCleared:
                _explore.SetLog("这里已经通过了。");
                break;
            case CellKind.Combat:
                OpenBackpack(cell, false);
                break;
            case CellKind.Gate when cell.CombatCleared:
                _explore.SetLog("小 Boss 已经倒下。这一版到此通关。");
                break;
            case CellKind.Gate:
                OpenBackpack(cell, true);
                break;
            case CellKind.Shop:
                OpenShop(cell);
                break;
            case CellKind.Event when cell.EventUsed:
                _explore.SetLog("这个事件已经结束了。");
                break;
            case CellKind.Event:
                OpenEvent(cell);
                break;
        }
    }

    private void OpenBackpack(FloorCell cell, bool boss)
    {
        CloseOverlay();
        var box = Modal("开战之前，换好武器。");
        foreach (var weapon in _run.Backpack)
        {
            var captured = weapon;
            var row = new HBoxContainer();
            row.AddChild(UiKit.Label(weapon.Name));
            row.AddChild(UiKit.Button("放入槽 1", () =>
            {
                _run.Equip(captured, 0);
                _run.ActiveSlot = 0;
                _explore.Refresh();
            }));
            row.AddChild(UiKit.Button("放入槽 2", () =>
            {
                _run.Equip(captured, 1);
                _run.ActiveSlot = 1;
                _explore.Refresh();
            }));
            box.AddChild(row);
        }
        box.AddChild(UiKit.Button(boss ? "迎战小 Boss" : "开始战斗", () =>
        {
            CloseOverlay();
            StartCombat(cell, boss);
        }));
        box.AddChild(UiKit.Button("先离开", CloseOverlay));
    }

    private void StartCombat(FloorCell cell, bool boss)
    {
        _explore.Visible = false;
        CombatView? combat = null;
        var specs = new List<EnemySpec>();
        if (boss)
            specs.Add(EnemyCatalog.Boss);
        else
        {
            var threat = _map.Threat(_x, _y);
            specs.Add((_rng.Next(2) == 0 ? EnemyCatalog.Bullet : EnemyCatalog.Dasher).AtThreat(threat));
            specs.Add((_rng.Next(2) == 0 ? EnemyCatalog.Bullet : EnemyCatalog.Dasher).AtThreat(threat));
        }
        var exits = new List<Dir>();
        foreach (var dir in new[] { Dir.North, Dir.East, Dir.South, Dir.West })
        {
            if (_map.Step(_x, _y, dir) != null)
                exits.Add(dir);
        }
        combat = new CombatView(_run, specs, _rng, (won, message) =>
        {
            Engine.TimeScale = 1;
            combat!.QueueFree();
            _explore.Visible = true;
            if (!won)
            {
                _run.Dead = true;
                ShowEnd("你死了", message);
                return;
            }
            cell.CombatCleared = true;
            _explore.Refresh();
            _explore.SetLog(message);
            _explore.CelebrateClear();
            if (boss)
            {
                _run.Won = true;
                ShowEnd("通关", "守门的小 Boss 倒下了。这一版到此结束。");
            }
        }, exits);
        AddChild(combat);
    }

    private void OpenShop(FloorCell cell)
    {
        CloseOverlay();
        var box = Modal("商店。买过的货不会回来。没买的还在，可以花钱刷新。");
        void Fill()
        {
            foreach (var child in box.GetChildren())
            {
                if (child is not Label { Text: not null } label || !label.Text.StartsWith("商店"))
                    child.QueueFree();
            }
            foreach (var offer in cell.Shop)
            {
                var captured = offer;
                var row = new HBoxContainer();
                var state = offer.Sold ? "已售" : $"{offer.Price} 金";
                row.AddChild(UiKit.Label($"{offer.Name}  {offer.Detail}  {state}"));
                if (!offer.Sold)
                {
                    row.AddChild(UiKit.Button("购买", () =>
                    {
                        if (ShopCatalog.TryBuy(_run, cell, captured.Id, out var note))
                        {
                            _explore.Refresh();
                            _explore.SetLog(note);
                            CloseOverlay();
                            OpenShop(cell);
                        }
                        else
                            _explore.SetLog(note);
                    }));
                }
                box.AddChild(row);
            }
            box.AddChild(UiKit.Button($"刷新（{FloorMap.RefreshCost} 金）", () =>
            {
                if (_run.Gold < FloorMap.RefreshCost)
                {
                    _explore.SetLog("金币不够刷新。");
                    return;
                }
                if (cell.Shop.All(item => item.Sold))
                {
                    _explore.SetLog("没有还能刷新的货。");
                    return;
                }
                _run.Gold -= FloorMap.RefreshCost;
                FloorGenerator.RefreshShop(cell, _rng);
                _explore.Refresh();
                CloseOverlay();
                OpenShop(cell);
            }));
            box.AddChild(UiKit.Button("离开", CloseOverlay));
        }
        Fill();
    }

    private void OpenEvent(FloorCell cell)
    {
        CloseOverlay();
        var mazeEvent = EventTable.For(cell);
        var box = Modal($"{mazeEvent.Title}\n\n{mazeEvent.Body}");
        foreach (var choice in mazeEvent.Choices)
        {
            var captured = choice;
            box.AddChild(UiKit.Button(captured.Label, () =>
            {
                var result = captured.Apply(_run, _rng);
                if (result.Contains("箱子没有开"))
                {
                    _explore.SetLog(result);
                    return;
                }
                cell.EventUsed = true;
                _explore.Refresh();
                CloseOverlay();
                var done = Modal($"{mazeEvent.Title}\n\n{result}");
                done.AddChild(UiKit.Button("离开", () =>
                {
                    _explore.SetLog(result);
                    CloseOverlay();
                }));
            }));
        }
    }

    private void ShowEnd(string title, string body)
    {
        CloseOverlay();
        var box = Modal($"{title}\n{body}");
        box.AddChild(UiKit.Button("再来一局", StartRun));
    }

    private VBoxContainer Modal(string text)
    {
        CloseOverlay();
        var dim = UiKit.Panel(new Color(0, 0, 0, 0.55f));
        dim.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(dim);
        _overlay = dim;
        var box = new VBoxContainer
        {
            Position = new Vector2(280, 140),
            Size = new Vector2(720, 460),
            CustomMinimumSize = new Vector2(720, 200),
        };
        var chrome = UiKit.Chrome();
        chrome.Position = new Vector2(256, 116);
        chrome.Size = new Vector2(768, 220);
        dim.AddChild(chrome);
        dim.AddChild(box);
        box.AddChild(UiKit.Label(text));
        Callable.From(() =>
        {
            if (!IsInstanceValid(chrome) || !IsInstanceValid(box))
                return;
            var content = box.GetCombinedMinimumSize();
            chrome.Size = new Vector2(768, Mathf.Max(120, content.Y + 48));
        }).CallDeferred();
        return box;
    }

    private void CloseOverlay()
    {
        _overlay?.QueueFree();
        _overlay = null;
    }

    private static string NameOf(Dir dir) => dir switch
    {
        Dir.North => "北",
        Dir.East => "东",
        Dir.South => "南",
        _ => "西",
    };
}
