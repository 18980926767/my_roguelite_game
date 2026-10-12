using CaveCurse.Core;
using Godot;

namespace CaveCurse;

public partial class AudioManager : Node
{
    public static AudioManager? Current { get; private set; }
    public bool MenuOpen { get; private set; }

    private readonly MusicMix _mix = new();
    private readonly VolumeKnob _volume = new();
    private readonly Random _rng = new();
    private readonly Dictionary<SfxCue, List<AudioStreamWav>> _cues = new();
    private readonly List<AudioStreamPlayer> _pool = new();
    private AudioStreamPlayer? _ambient;
    private AudioStreamPlayer? _drums;
    private CanvasLayer? _layer;
    private Button? _musicButton;
    private Button? _sfxButton;
    private Button? _downButton;
    private Button? _upButton;
    private Button? _closeButton;
    private int _row;
    private int _bus;
    private ulong _clock;

    public override void _Ready()
    {
        Current = this;
        ProcessMode = ProcessModeEnum.Always;
        EnsureBuses();
        ApplyVolume();
        LoadCues();
        BuildPool();
        StartMusic();
        BuildMenu();
    }

    public override void _ExitTree()
    {
        if (Current == this)
            Current = null;
    }

    public override void _Process(double _)
    {
        _mix.Tick(RealSlice());
        if (_drums == null)
            return;
        _drums.VolumeDb = _mix.Drums <= 0.001f ? -80f : Mathf.LinearToDb(_mix.Drums);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventKey key || !key.Pressed || key.Echo)
            return;
        if (!MenuOpen)
        {
            if (key.Keycode != Key.V)
                return;
            SetMenu(true);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (key.Keycode is Key.V or Key.Escape or Key.K)
            SetMenu(false);
        else if (Keys.Up(key))
            Move(-1);
        else if (Keys.Down(key))
            Move(1);
        else if (key.Keycode is Key.Left or Key.A)
            Nudge(-1);
        else if (key.Keycode is Key.Right or Key.D)
            Nudge(1);
        else if (Keys.Digit(key) is int number)
            PressNumber(number);
        else if (Keys.Confirm(key))
            ActivateRow();
        GetViewport().SetInputAsHandled();
    }

    public static void Play(SfxCue cue) => Current?.PlayCue(cue);

    public static void EnterCombat() => Current?._mix.EnterCombat();

    public static void LeaveCombat() => Current?._mix.LeaveCombat();

    public static void ResetExplore() => Current?._mix.ResetExplore();

    public void PlayCue(SfxCue cue)
    {
        if (!_cues.TryGetValue(cue, out var clips) || clips.Count == 0 || _pool.Count == 0)
            return;
        var player = NextPlayer();
        player.Stream = clips[CueBank.Variant(_rng, clips.Count)];
        player.PitchScale = SfxPitch.Next(_rng);
        player.Play();
    }

    private void Select(int row)
    {
        _row = Mathf.Clamp(row, 0, 4);
        if (_row <= 1)
            _bus = _row;
        RefreshMenu();
    }

    private void Move(int delta)
    {
        _row = ((_row + delta) % 5 + 5) % 5;
        if (_row <= 1)
            _bus = _row;
        RefreshMenu();
    }

    private void PressNumber(int number)
    {
        if (number == 5)
        {
            SetMenu(false);
            return;
        }
        if (number is < 1 or > 4)
            return;
        Select(number - 1);
        if (number == 3)
            Nudge(-1);
        else if (number == 4)
            Nudge(1);
    }

    private void ActivateRow()
    {
        if (_row == 2)
            Nudge(-1);
        else if (_row == 3)
            Nudge(1);
        else if (_row == 4)
            SetMenu(false);
    }

    private void Nudge(int direction)
    {
        if (_bus == 0)
            _volume.NudgeMusic(direction);
        else
            _volume.NudgeSfx(direction);
        ApplyVolume();
        RefreshMenu();
    }

    private void SetMenu(bool open)
    {
        MenuOpen = open;
        if (_layer != null)
            _layer.Visible = open;
        RefreshMenu();
    }

    private void RefreshMenu()
    {
        if (_musicButton == null || _sfxButton == null || _downButton == null || _upButton == null || _closeButton == null)
            return;
        ChoiceList.Paint(_musicButton, $"[1] 音乐 {Percent(_volume.Music)}", _row == 0);
        ChoiceList.Paint(_sfxButton, $"[2] 音效 {Percent(_volume.Sfx)}", _row == 1);
        ChoiceList.Paint(_downButton, "[3] 减小", _row == 2);
        ChoiceList.Paint(_upButton, "[4] 加大", _row == 3);
        ChoiceList.Paint(_closeButton, "[5] 关闭  [Esc]", _row == 4);
    }

    private static string Percent(float value) => $"{Mathf.RoundToInt(value * 100f)}%";

    private AudioStreamPlayer NextPlayer()
    {
        foreach (var player in _pool)
        {
            if (!player.Playing)
                return player;
        }
        return _pool[_rng.Next(_pool.Count)];
    }

    private void EnsureBuses()
    {
        var music = Bus("Music");
        var sfx = Bus("SFX");
        AudioServer.SetBusSend(music, "Master");
        AudioServer.SetBusSend(sfx, "Master");
        AudioEffectReverb? reverb = null;
        for (var i = 0; i < AudioServer.GetBusEffectCount(sfx); i++)
        {
            if (AudioServer.GetBusEffect(sfx, i) is AudioEffectReverb existing)
                reverb = existing;
        }
        if (reverb == null)
        {
            reverb = new AudioEffectReverb();
            AudioServer.AddBusEffect(sfx, reverb);
        }
        reverb.RoomSize = 0.3f;
        reverb.Wet = 0.15f;
    }

    private static int Bus(string name)
    {
        var index = AudioServer.GetBusIndex(name);
        if (index >= 0)
            return index;
        index = AudioServer.BusCount;
        AudioServer.AddBus(index);
        AudioServer.SetBusName(index, name);
        return index;
    }

    private void ApplyVolume()
    {
        SetBus("Music", _volume.Music);
        SetBus("SFX", _volume.Sfx);
    }

    private static void SetBus(string name, float linear)
    {
        var index = AudioServer.GetBusIndex(name);
        if (index < 0)
            return;
        if (linear <= 0.001f)
        {
            AudioServer.SetBusMute(index, true);
            return;
        }
        AudioServer.SetBusMute(index, false);
        AudioServer.SetBusVolumeDb(index, Mathf.LinearToDb(linear));
    }

    private void LoadCues()
    {
        foreach (SfxCue cue in Enum.GetValues<SfxCue>())
        {
            var clips = new List<AudioStreamWav>();
            var prefix = CueBank.Prefix(cue);
            for (var variant = 1; variant <= CueBank.MaxVariants; variant++)
            {
                var clip = LoadWav($"res://audio/sfx/{prefix}-{variant}.wav", loop: false);
                if (clip != null)
                    clips.Add(clip);
            }
            _cues[cue] = clips;
            if (clips.Count < CueBank.MinVariants)
                GD.PushWarning($"音效 {prefix} 变体不足，找到 {clips.Count} 个。");
        }
    }

    private void BuildPool()
    {
        for (var i = 0; i < 12; i++)
        {
            var player = new AudioStreamPlayer
            {
                Bus = "SFX",
                VolumeDb = 0f,
            };
            AddChild(player);
            _pool.Add(player);
        }
    }

    private void StartMusic()
    {
        var ambient = LoadWav("res://audio/music/ambient.wav", loop: true);
        var drums = LoadWav("res://audio/music/drums.wav", loop: true);
        if (ambient != null)
        {
            _ambient = new AudioStreamPlayer { Bus = "Music", Stream = ambient, VolumeDb = 0f };
            AddChild(_ambient);
            _ambient.Play();
        }
        if (drums != null)
        {
            _drums = new AudioStreamPlayer { Bus = "Music", Stream = drums, VolumeDb = -80f };
            AddChild(_drums);
            _drums.Play();
        }
    }

    private void BuildMenu()
    {
        _layer = new CanvasLayer { Layer = 40, Visible = false };
        AddChild(_layer);
        var dim = new ColorRect
        {
            Color = new Color(0f, 0f, 0f, 0.55f),
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        dim.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _layer.AddChild(dim);
        var chrome = UiKit.Chrome();
        chrome.Position = new Vector2(330, 120);
        chrome.Size = new Vector2(620, 460);
        _layer.AddChild(chrome);
        var box = new VBoxContainer
        {
            Position = new Vector2(360, 146),
            Size = new Vector2(560, 410),
        };
        box.AddThemeConstantOverride("separation", 10);
        _layer.AddChild(box);
        box.AddChild(VolumeHint("音量。上下移动金色高亮，左右加减当前的音乐或音效。"));
        box.AddChild(VolumeHint("[J] 确认，[Esc] 或 V 关闭。"));
        _musicButton = MenuButton("[1] 音乐", () => Select(0));
        _sfxButton = MenuButton("[2] 音效", () => Select(1));
        _downButton = MenuButton("[3] 减小", () =>
        {
            Select(2);
            Nudge(-1);
        });
        _upButton = MenuButton("[4] 加大", () =>
        {
            Select(3);
            Nudge(1);
        });
        _closeButton = MenuButton("[5] 关闭", () => SetMenu(false));
        box.AddChild(_musicButton);
        box.AddChild(_sfxButton);
        box.AddChild(_downButton);
        box.AddChild(_upButton);
        box.AddChild(_closeButton);
        RefreshMenu();
    }

    private static Button MenuButton(string text, Action action)
    {
        var button = UiKit.Button(text, action);
        button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        return button;
    }

    private static Label VolumeHint(string text)
    {
        var label = UiKit.Label(text);
        label.AutowrapMode = TextServer.AutowrapMode.Arbitrary;
        label.CustomMinimumSize = new Vector2(520, 0);
        return label;
    }

    private float RealSlice()
    {
        var now = Time.GetTicksMsec();
        if (_clock == 0)
        {
            _clock = now;
            return 0f;
        }
        var step = MathF.Min(0.05f, (now - _clock) / 1000f);
        _clock = now;
        return step;
    }

    private static AudioStreamWav? LoadWav(string path, bool loop)
    {
        if (!Godot.FileAccess.FileExists(path))
        {
            GD.PushWarning($"缺少音频 {path}");
            return null;
        }
        using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
        if (file == null)
            return null;
        var bytes = file.GetBuffer((long)file.GetLength());
        if (!TryParseWav(bytes, out var pcm, out var rate, out var channels, out var bits))
        {
            GD.PushWarning($"读不了音频 {path}");
            return null;
        }
        var stream = new AudioStreamWav
        {
            Data = pcm,
            MixRate = rate,
            Stereo = channels > 1,
            Format = bits <= 8 ? AudioStreamWav.FormatEnum.Format8Bits : AudioStreamWav.FormatEnum.Format16Bits,
        };
        if (!loop)
            return stream;
        stream.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
        stream.LoopBegin = 0;
        var frame = Math.Max(1, channels * Math.Max(1, bits / 8));
        stream.LoopEnd = pcm.Length / frame;
        return stream;
    }

    private static bool TryParseWav(byte[] bytes, out byte[] pcm, out int rate, out int channels, out int bits)
    {
        pcm = Array.Empty<byte>();
        rate = 44100;
        channels = 1;
        bits = 16;
        if (bytes.Length < 44)
            return false;
        if (ReadFour(bytes, 0) != "RIFF" || ReadFour(bytes, 8) != "WAVE")
            return false;
        var cursor = 12;
        var found = false;
        while (cursor + 8 <= bytes.Length)
        {
            var id = ReadFour(bytes, cursor);
            var size = BitConverter.ToInt32(bytes, cursor + 4);
            cursor += 8;
            if (size < 0 || cursor + size > bytes.Length)
                return false;
            if (id == "fmt " && size >= 16)
            {
                channels = BitConverter.ToInt16(bytes, cursor + 2);
                rate = BitConverter.ToInt32(bytes, cursor + 4);
                bits = BitConverter.ToInt16(bytes, cursor + 14);
            }
            else if (id == "data")
            {
                pcm = new byte[size];
                Buffer.BlockCopy(bytes, cursor, pcm, 0, size);
                found = true;
                break;
            }
            cursor += size + (size & 1);
        }
        return found && pcm.Length > 0 && channels is 1 or 2 && bits is 8 or 16;
    }

    private static string ReadFour(byte[] bytes, int index) =>
        System.Text.Encoding.ASCII.GetString(bytes, index, 4);
}
