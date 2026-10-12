using Godot;

namespace CaveCurse;

public static class Keys
{
    public static bool Confirm(InputEventKey key) =>
        Pressed(key) && key.Keycode is Key.Enter or Key.KpEnter or Key.Space or Key.J;

    public static bool Cancel(InputEventKey key) =>
        Pressed(key) && key.Keycode is Key.Escape or Key.K;

    public static bool Up(InputEventKey key) =>
        Pressed(key) && key.Keycode is Key.Up or Key.W;

    public static bool Down(InputEventKey key) =>
        Pressed(key) && key.Keycode is Key.Down or Key.S;

    public static bool Previous(InputEventKey key) =>
        Pressed(key) && key.Keycode is Key.Up or Key.W or Key.Left or Key.A;

    public static bool Next(InputEventKey key) =>
        Pressed(key) && key.Keycode is Key.Down or Key.S or Key.Right or Key.D;

    public static int Tab(InputEventKey key)
    {
        if (!Pressed(key))
            return 0;
        if (key.Keycode == Key.Backtab || (key.Keycode == Key.Tab && key.ShiftPressed))
            return -1;
        if (key.Keycode == Key.Tab)
            return 1;
        return 0;
    }

    public static int? Digit(InputEventKey key)
    {
        if (!Pressed(key))
            return null;
        if (key.Keycode >= Key.Key1 && key.Keycode <= Key.Key9)
            return (int)key.Keycode - (int)Key.Key1 + 1;
        if (key.Keycode >= Key.Kp1 && key.Keycode <= Key.Kp9)
            return (int)key.Keycode - (int)Key.Kp1 + 1;
        return null;
    }

    private static bool Pressed(InputEventKey key) => key.Pressed && !key.Echo;
}

public sealed class ChoiceList
{
    private readonly List<Button> _buttons = new();
    private readonly List<Action> _actions = new();
    private readonly List<string> _labels = new();
    private int _index;

    public int Count => _buttons.Count;

    public void Clear()
    {
        _buttons.Clear();
        _actions.Clear();
        _labels.Clear();
        _index = 0;
    }

    public int Index
    {
        get => _index;
        set
        {
            if (_buttons.Count == 0)
            {
                _index = 0;
                return;
            }
            _index = Mathf.Clamp(value, 0, _buttons.Count - 1);
            PaintAll();
        }
    }

    public bool SelectedDisabled =>
        _buttons.Count > 0 && _buttons[_index].Disabled;

    public void Add(Button button, string label, Action action)
    {
        var shown = _buttons.Count < 9 ? $"[{_buttons.Count + 1}] {label}" : label;
        _buttons.Add(button);
        _actions.Add(action);
        _labels.Add(shown);
        PaintAll();
    }

    public void SetLabel(int index, string label)
    {
        if (index < 0 || index >= _labels.Count)
            return;
        _labels[index] = index < 9 ? $"[{index + 1}] {label}" : label;
        PaintAll();
    }

    public void Move(int delta)
    {
        if (_buttons.Count == 0)
            return;
        var count = _buttons.Count;
        _index = ((_index + delta) % count + count) % count;
        PaintAll();
    }

    public void Confirm()
    {
        if (_buttons.Count == 0 || _buttons[_index].Disabled)
            return;
        _actions[_index]();
    }

    public bool PressNumber(int number)
    {
        var index = number - 1;
        if (index < 0 || index >= _buttons.Count)
            return false;
        _index = index;
        PaintAll();
        if (_buttons[index].Disabled)
            return false;
        _actions[index]();
        return true;
    }

    public void PaintAll()
    {
        for (var i = 0; i < _buttons.Count; i++)
            Paint(_buttons[i], _labels[i], i == _index);
    }

    public static void Paint(Button button, string label, bool picked)
    {
        button.Text = picked ? $"▶ {label}" : label;
        button.Modulate = picked ? new Color(1.28f, 1.05f, 0.48f) : Colors.White;
    }
}
