using Godot;

namespace CaveCurse;

public static class UiKit
{
    public static Font Font { get; } = CreateFont();

    public static Theme Theme { get; } = CreateTheme();

    private static Font CreateFont()
    {
        var font = new SystemFont();
        font.FontNames = new[]
        {
            "Microsoft YaHei",
            "Microsoft YaHei UI",
            "WenQuanYi Micro Hei",
            "Droid Sans Fallback",
            "Noto Sans CJK SC",
        };
        return font;
    }

    private static Theme CreateTheme()
    {
        var theme = new Theme();
        theme.DefaultFont = Font;
        theme.DefaultFontSize = 16;
        return theme;
    }

    public static Label Label(string text, int size = 16)
    {
        return new Label
        {
            Text = text,
            Theme = Theme,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
    }

    public static Button Button(string text, Action onPressed)
    {
        var button = new Button
        {
            Text = text,
            Theme = Theme,
            CustomMinimumSize = new Vector2(0, 36),
        };
        button.Pressed += onPressed;
        return button;
    }

    public static Panel Panel(Color color)
    {
        var panel = new Panel();
        var box = new StyleBoxFlat
        {
            BgColor = color,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
        };
        panel.AddThemeStyleboxOverride("panel", box);
        return panel;
    }
}
