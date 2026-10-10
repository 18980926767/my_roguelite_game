using CaveCurse.Core;
using Godot;

namespace CaveCurse;

public sealed partial class CombatView : Control
{
    private enum Phase { Realtime, Focus, Grace }

    private sealed class Foe
    {
        public required EnemySpec Spec { get; init; }
        public Vector2 Pos;
        public int Hp;
        public int Shield;
        public float Ai = 1f;
        public float Freeze;
        public float FreezePending;
        public float PoisonLeft;
        public float PoisonFrac;
        public float PoisonTick;
        public Vector2 DashVel;
        public float DashLeft;
        public bool Alive => Hp > 0;
    }

    private sealed class Shot
    {
        public Vector2 Pos;
        public Vector2 Vel;
        public int Damage;
        public bool Alive = true;
    }

    private enum Act { Attack, Skill, Dodge, Swap }

    private sealed class Plan
    {
        public Act Kind;
        public int Target;
        public Vector2 Dodge;
        public string Text = "";
    }

    private readonly RunState _run;
    private readonly List<Foe> _foes = new();
    private readonly List<Shot> _shots = new();
    private readonly List<Plan> _queue = new();
    private readonly Random _rng;
    private readonly Action<bool, string> _finished;
    private readonly Rect2 _arena = new(24, 72, 760, 500);
    private readonly List<string> _log = new();

    private Phase _phase = Phase.Realtime;
    private Vector2 _player;
    private float _charge;
    private float _grace;
    private float _iframe;
    private int _ap;
    private bool _ended;
    private bool _playerFlash;
    private Label _hud = null!;
    private Label _logLabel = null!;
    private VBoxContainer _actions = null!;
    private readonly List<Button> _actionButtons = new();

    public CombatView(RunState run, IReadOnlyList<EnemySpec> specs, Random rng, Action<bool, string> finished)
    {
        _run = run;
        _rng = rng;
        _finished = finished;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        ProcessMode = ProcessModeEnum.Always;
        var backdrop = UiKit.Panel(new Color("101018"));
        backdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        backdrop.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(backdrop);

        _player = _arena.GetCenter();
        var i = 0;
        foreach (var spec in specs)
        {
            _foes.Add(new Foe
            {
                Spec = spec,
                Hp = spec.MaxHp,
                Shield = spec.Shield,
                Pos = new Vector2(_arena.Position.X + 180 + i * 220, _arena.Position.Y + 120),
            });
            i++;
        }

        var side = new VBoxContainer
        {
            Position = new Vector2(820, 24),
            Size = new Vector2(430, 680),
        };
        AddChild(side);
        _hud = UiKit.Label("");
        side.AddChild(_hud);
        _actions = new VBoxContainer();
        side.AddChild(_actions);
        _logLabel = UiKit.Label("蓄力条满了之后，按空格进入时停。可以一直不按。");
        _logLabel.CustomMinimumSize = new Vector2(420, 280);
        side.AddChild(_logLabel);
        BuildFocusButtons();
        SetFocusUi(false);
    }

    public override void _Process(double delta)
    {
        if (_ended)
            return;
        var dt = (float)delta;
        if (_iframe > 0)
            _iframe -= dt;

        if (_phase == Phase.Realtime)
        {
            _charge = MathF.Min(1f, _charge + dt / 3.5f);
            MovePlayer(dt);
            TickFoes(dt);
            TickShots(dt);
            TickPoison(dt);
            Touch();
        }
        else if (_phase == Phase.Grace)
        {
            _grace -= dt;
            MovePlayer(dt);
            Touch();
            if (_grace <= 0f)
            {
                _phase = Phase.Realtime;
                foreach (var foe in _foes)
                {
                    if (foe.FreezePending > 0f)
                    {
                        foe.Freeze = MathF.Max(foe.Freeze, foe.FreezePending);
                        foe.FreezePending = 0f;
                    }
                }
                Note("时间恢复了。");
            }
        }

        if (_run.Hp <= 0)
        {
            End(false, "你倒下了。这一局结束。");
            return;
        }
        if (_foes.All(foe => !foe.Alive))
        {
            var gold = Loot.GoldFor(_foes.Count);
            _run.Gold += gold;
            var extra = "";
            if (Loot.RollsMana(_rng))
            {
                _run.Mana = Math.Min(_run.MaxMana, _run.Mana + Loot.ManaFromLoot);
                extra = $" 蓝 +{Loot.ManaFromLoot}。";
            }
            End(true, $"战斗结束。获得 {gold} 金币。{extra}");
            return;
        }

        RefreshHud();
        QueueRedraw();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_ended || _phase != Phase.Realtime || _charge < 1f)
            return;
        if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Space })
            BeginFocus();
    }

    public override void _Draw()
    {
        DrawRect(_arena, new Color("1b1b26"));
        DrawRect(_arena, new Color("3a3a4a"), false, 2);
        foreach (var shot in _shots)
        {
            if (shot.Alive)
                DrawCircle(shot.Pos, 6, new Color("f6e58d"));
        }
        foreach (var foe in _foes)
        {
            if (!foe.Alive)
                continue;
            var color = foe.Freeze > 0f || _phase != Phase.Realtime
                ? new Color("7fdbda")
                : foe.Spec.Dashes && !foe.Spec.Shoots ? new Color("e67e22") : new Color("e74c3c");
            if (foe.Spec.MaxHp > 80)
                color = new Color("8e44ad");
            DrawCircle(foe.Pos, 18, color);
            DrawString(UiKit.Font, foe.Pos + new Vector2(-18, 28), $"{foe.Hp}", fontSize: 14);
        }
        DrawCircle(_player, 14, _playerFlash ? new Color("f7dc6f") : new Color("ecf0f1"));
        var bar = new Rect2(_arena.Position.X, _arena.Position.Y - 28, _arena.Size.X, 16);
        DrawRect(bar, new Color("2c2c38"));
        DrawRect(new Rect2(bar.Position, new Vector2(bar.Size.X * _charge, bar.Size.Y)), new Color("f1c40f"));
    }

    private void BeginFocus()
    {
        _phase = Phase.Focus;
        Engine.TimeScale = 0;
        _ap = _run.ActionPoints;
        _queue.Clear();
        if (_run.SkillCooldown > 0)
            _run.SkillCooldown--;
        var nearest = Nearest();
        _actionButtons[0].Text = nearest >= 0 ? $"普攻 {_foes[nearest].Spec.Name}" : "没有目标";
        SetFocusUi(true);
        Note("时停。排出这一次行动，然后跳过。");
        RefreshHud();
    }

    private void BuildFocusButtons()
    {
        AddAction("普攻最近的敌人", () => QueueAttack(Nearest()));
        AddAction("随机属性", QueueSkill);
        AddAction("向北闪避", () => QueueDodge(Vector2.Up));
        AddAction("向南闪避", () => QueueDodge(Vector2.Down));
        AddAction("向西闪避", () => QueueDodge(Vector2.Left));
        AddAction("向东闪避", () => QueueDodge(Vector2.Right));
        AddAction("切换手持", QueueSwap);
        AddAction("撤销上一步", Undo);
        AddAction("跳过并结算", Commit);
    }

    private void AddAction(string text, Action action)
    {
        var button = UiKit.Button(text, action);
        _actionButtons.Add(button);
        _actions.AddChild(button);
    }

    private void SetFocusUi(bool on)
    {
        _actions.Visible = on;
    }

    private void QueueAttack(int index)
    {
        if (_ap <= 0 || index < 0)
            return;
        _ap--;
        _queue.Add(new Plan { Kind = Act.Attack, Target = index, Text = $"普攻 {_foes[index].Spec.Name}" });
        Note(string.Join(" → ", _queue.Select(item => item.Text)));
    }

    private void QueueSkill()
    {
        if (_ap <= 0 || _run.SkillCooldown > 0 || _run.Mana < RunState.SkillManaCost)
        {
            Note(_run.Mana < RunState.SkillManaCost ? "蓝不够。" : "技能还在冷却，或者行动点用完了。");
            return;
        }
        _ap--;
        _run.Mana -= RunState.SkillManaCost;
        _run.SkillCooldown = RunState.SkillCooldownTurns;
        _queue.Add(new Plan { Kind = Act.Skill, Text = "随机属性" });
        Note(string.Join(" → ", _queue.Select(item => item.Text)));
    }

    private void QueueDodge(Vector2 dir)
    {
        if (_ap <= 0)
            return;
        _ap--;
        _queue.Add(new Plan { Kind = Act.Dodge, Dodge = dir, Text = "闪避" });
        Note(string.Join(" → ", _queue.Select(item => item.Text)));
    }

    private void QueueSwap()
    {
        var other = 1 - _run.ActiveSlot;
        if (_run.Slots[other] == null && _run.Slots[_run.ActiveSlot] != null)
        {
            Note("另一个槽是空的。");
            return;
        }
        _run.ActiveSlot = other;
        var name = _run.ActiveWeapon?.Name ?? "空槽";
        _queue.Add(new Plan { Kind = Act.Swap, Text = $"切到{name}" });
        Note($"切到 {name}。这不花行动点。");
    }

    private void Undo()
    {
        if (_queue.Count == 0)
            return;
        var last = _queue[^1];
        _queue.RemoveAt(_queue.Count - 1);
        if (last.Kind is Act.Attack or Act.Skill or Act.Dodge)
            _ap++;
        if (last.Kind == Act.Swap)
            _run.ActiveSlot = 1 - _run.ActiveSlot;
        if (last.Kind == Act.Skill)
        {
            _run.Mana = Math.Min(_run.MaxMana, _run.Mana + RunState.SkillManaCost);
            _run.SkillCooldown = 0;
        }
        Note(_queue.Count == 0 ? "队列空了。" : string.Join(" → ", _queue.Select(item => item.Text)));
    }

    private void Commit()
    {
        foreach (var plan in _queue)
        {
            if (plan.Kind == Act.Attack)
                ResolveAttack(plan.Target);
            else if (plan.Kind == Act.Skill)
                Note("随机属性：" + _run.ApplyRandomBonus(_rng));
            else if (plan.Kind == Act.Dodge)
            {
                _player += plan.Dodge * 110f;
                ClampPlayer();
            }
        }
        _queue.Clear();
        Engine.TimeScale = 1;
        _charge = 0;
        _phase = Phase.Grace;
        _grace = 2f;
        SetFocusUi(false);
        Note("结算完了。接下来 2 秒只有你能走，碰到子弹和敌人仍会受伤。");
    }

    private void ResolveAttack(int index)
    {
        if (index < 0 || index >= _foes.Count || !_foes[index].Alive)
        {
            Note("这一下没打到人。");
            return;
        }
        var foe = _foes[index];
        var weapon = _run.ActiveWeapon;
        var mult = weapon?.BasicMultiplier ?? 0.5f;
        var damage = CombatMath.BasicDamage(_run.Attack, mult, foe.Spec.Defense);
        var shield = foe.Shield;
        var hp = foe.Hp;
        CombatMath.ApplyStrike(ref shield, ref hp, damage);
        foe.Shield = shield;
        foe.Hp = hp;
        _playerFlash = true;
        if (weapon != null && weapon.PoisonSeconds > 0f)
            foe.PoisonLeft = weapon.PoisonSeconds;
        if (weapon != null && weapon.FreezeSeconds > 0f)
            foe.FreezePending = weapon.FreezeSeconds;
        var knock = "";
        if (weapon != null && weapon.KnockbackStacks > 0)
            knock = ApplyKnockback(foe, weapon.KnockbackStacks);
        Note($"砍中 {foe.Spec.Name}，伤害 {damage}。{knock}");
    }

    private string ApplyKnockback(Foe foe, int stacks)
    {
        var min = new Vec2(_arena.Position.X + 18, _arena.Position.Y + 18);
        var max = new Vec2(_arena.End.X - 18, _arena.End.Y - 18);
        var from = new Vec2(_player.X, _player.Y);
        var target = new Vec2(foe.Pos.X, foe.Pos.Y);
        var next = CombatMath.Push(from, target, CombatMath.KnockbackDistance(stacks, foe.Spec.Weight), min, max, out var hitWall);
        foe.Pos = new Vector2(next.X, next.Y);
        var text = hitWall ? "撞墙受伤。" : "";
        if (hitWall)
            foe.Hp -= CombatMath.CollisionDamage;
        foreach (var other in _foes)
        {
            if (other == foe || !other.Alive)
                continue;
            if (foe.Pos.DistanceTo(other.Pos) < 36f)
            {
                foe.Hp -= CombatMath.CollisionDamage;
                other.Hp -= CombatMath.CollisionDamage;
                text += "撞上另一个敌人，双方受伤。";
            }
        }
        return text;
    }

    private int Nearest()
    {
        var best = -1;
        var dist = float.MaxValue;
        for (var i = 0; i < _foes.Count; i++)
        {
            if (!_foes[i].Alive)
                continue;
            var d = _player.DistanceTo(_foes[i].Pos);
            if (d < dist)
            {
                dist = d;
                best = i;
            }
        }
        return best;
    }

    private void MovePlayer(float dt)
    {
        var input = Vector2.Zero;
        if (Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left)) input.X -= 1;
        if (Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right)) input.X += 1;
        if (Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.Up)) input.Y -= 1;
        if (Input.IsKeyPressed(Key.S) || Input.IsKeyPressed(Key.Down)) input.Y += 1;
        if (input.LengthSquared() > 0f)
            _player += input.Normalized() * _run.MoveSpeed * dt;
        ClampPlayer();
    }

    private void ClampPlayer()
    {
        _player = new Vector2(
            Mathf.Clamp(_player.X, _arena.Position.X + 16, _arena.End.X - 16),
            Mathf.Clamp(_player.Y, _arena.Position.Y + 16, _arena.End.Y - 16));
    }

    private void TickFoes(float dt)
    {
        foreach (var foe in _foes)
        {
            if (!foe.Alive)
                continue;
            if (foe.Freeze > 0f)
            {
                foe.Freeze -= dt;
                continue;
            }
            if (foe.DashLeft > 0f)
            {
                foe.Pos += foe.DashVel * dt;
                foe.DashLeft -= dt;
                foe.Pos = ClampFoe(foe.Pos);
                continue;
            }
            foe.Ai -= dt;
            if (foe.Ai > 0f)
                continue;
            if (foe.Spec.Shoots)
            {
                var dir = (_player - foe.Pos).Normalized();
                _shots.Add(new Shot { Pos = foe.Pos, Vel = dir * 170f, Damage = 7 });
                foe.Ai = 1.5f;
            }
            if (foe.Spec.Dashes)
            {
                var dir = (_player - foe.Pos).Normalized();
                foe.DashVel = dir * 280f;
                foe.DashLeft = 0.32f;
                foe.Ai = 2.1f;
            }
        }
    }

    private void TickShots(float dt)
    {
        foreach (var shot in _shots)
        {
            if (!shot.Alive)
                continue;
            shot.Pos += shot.Vel * dt;
            if (!_arena.Grow(-4).HasPoint(shot.Pos))
                shot.Alive = false;
        }
    }

    private void TickPoison(float dt)
    {
        foreach (var foe in _foes)
        {
            if (!foe.Alive || foe.PoisonLeft <= 0f)
                continue;
            foe.PoisonLeft -= dt;
            foe.PoisonTick += dt;
            while (foe.PoisonTick >= 1f)
            {
                foe.PoisonTick -= 1f;
                var hp = foe.Hp;
                CombatMath.ApplyPoisonTick(foe.Spec.MaxHp, ref foe.PoisonFrac, ref hp);
                foe.Hp = hp;
            }
        }
    }

    private void Touch()
    {
        foreach (var foe in _foes)
        {
            if (foe.Alive && _player.DistanceTo(foe.Pos) < 30f)
                Hurt(foe.Spec.Contact);
        }
        foreach (var shot in _shots)
        {
            if (shot.Alive && _player.DistanceTo(shot.Pos) < 18f)
            {
                shot.Alive = false;
                Hurt(shot.Damage);
            }
        }
    }

    private void Hurt(int amount)
    {
        if (_iframe > 0 || amount <= 0)
            return;
        var shield = _run.Shield;
        var hp = _run.Hp;
        CombatMath.ApplyStrike(ref shield, ref hp, amount);
        _run.Shield = shield;
        _run.Hp = hp;
        _iframe = 0.4f;
    }

    private Vector2 ClampFoe(Vector2 pos)
    {
        return new Vector2(
            Mathf.Clamp(pos.X, _arena.Position.X + 18, _arena.End.X - 18),
            Mathf.Clamp(pos.Y, _arena.Position.Y + 18, _arena.End.Y - 18));
    }

    private void RefreshHud()
    {
        var phase = _phase switch
        {
            Phase.Focus => "时停",
            Phase.Grace => $"提前移动 {_grace:0.0}s",
            _ => _charge >= 1f ? "按空格时停" : "蓄力中",
        };
        _hud.Text =
            $"{phase}\n生命 {_run.Hp}/{_run.MaxHp}  蓝 {_run.Mana}/{_run.MaxMana}  护盾 {_run.Shield}\n" +
            $"行动点 {_ap}    技能冷却 {_run.SkillCooldown}    手持 {_run.ActiveWeapon?.Name}";
    }

    private void Note(string text)
    {
        _log.Insert(0, text);
        if (_log.Count > 8)
            _log.RemoveAt(_log.Count - 1);
        _logLabel.Text = string.Join("\n", _log);
    }

    private void End(bool won, string message)
    {
        if (_ended)
            return;
        _ended = true;
        Engine.TimeScale = 1;
        _finished(won, message);
    }
}
