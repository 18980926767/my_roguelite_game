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
        var ink = new Color("24180f");
        theme.SetStylebox("normal", "Button", ButtonBox(Art.Button));
        theme.SetStylebox("hover", "Button", ButtonBox(Art.ButtonHover));
        theme.SetStylebox("pressed", "Button", ButtonBox(Art.ButtonPressed));
        theme.SetStylebox("hover_pressed", "Button", ButtonBox(Art.ButtonPressed));
        theme.SetStylebox("disabled", "Button", ButtonBox(Art.ButtonDisabled));
        theme.SetStylebox("focus", "Button", ButtonBox(Art.ButtonHover));
        theme.SetColor("font_color", "Button", ink);
        theme.SetColor("font_hover_color", "Button", ink);
        theme.SetColor("font_pressed_color", "Button", ink);
        theme.SetColor("font_focus_color", "Button", ink);
        theme.SetColor("font_disabled_color", "Button", new Color("4a453c"));
        return theme;
    }

    private static StyleBoxTexture ButtonBox(Texture2D texture)
    {
        return new StyleBoxTexture
        {
            Texture = texture,
            TextureMarginLeft = 6,
            TextureMarginTop = 6,
            TextureMarginRight = 6,
            TextureMarginBottom = 6,
            ContentMarginLeft = 12,
            ContentMarginTop = 4,
            ContentMarginRight = 12,
            ContentMarginBottom = 4,
        };
    }

    public static StyleBoxTexture PanelBox()
    {
        return new StyleBoxTexture
        {
            Texture = Art.Panel,
            TextureMarginLeft = 6,
            TextureMarginTop = 6,
            TextureMarginRight = 6,
            TextureMarginBottom = 6,
            ContentMarginLeft = 16,
            ContentMarginTop = 12,
            ContentMarginRight = 16,
            ContentMarginBottom = 12,
        };
    }

    public static Label Label(string text, int size = 16)
    {
        return new Label
        {
            Text = text,
            Theme = Theme,
            AutowrapMode = TextServer.AutowrapMode.Off,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(640, 0),
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

    public static Panel Chrome()
    {
        var panel = new Panel
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        panel.AddThemeStyleboxOverride("panel", PanelBox());
        return panel;
    }
}
