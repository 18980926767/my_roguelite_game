using Godot;

namespace CaveCurse;

public static class Art
{
    private static readonly Dictionary<string, Texture2D> Cache = new();

    public static Texture2D FloorA => Get("res://assets/dungeon/floor-a.png");
    public static Texture2D FloorB => Get("res://assets/dungeon/floor-b.png");
    public static Texture2D FloorC => Get("res://assets/dungeon/floor-c.png");
    public static Texture2D FloorD => Get("res://assets/dungeon/floor-d.png");
    public static Texture2D WallFaceLeft => Get("res://assets/dungeon/wall-face-left.png");
    public static Texture2D WallFace => Get("res://assets/dungeon/wall-face.png");
    public static Texture2D WallFaceRight => Get("res://assets/dungeon/wall-face-right.png");
    public static Texture2D WallCapLeft => Get("res://assets/dungeon/wall-cap-left.png");
    public static Texture2D WallCap => Get("res://assets/dungeon/wall-cap.png");
    public static Texture2D WallCapRight => Get("res://assets/dungeon/wall-cap-right.png");
    public static Texture2D DoorJambLeft => Get("res://assets/dungeon/door-jamb-left.png");
    public static Texture2D DoorJambRight => Get("res://assets/dungeon/door-jamb-right.png");
    public static Texture2D DoorLintel => Get("res://assets/dungeon/door-lintel.png");
    public static Texture2D DoorOpen => Get("res://assets/dungeon/door-open.png");
    public static Texture2D Player => Get("res://assets/dungeon/player.png");
    public static Texture2D Shooter => Get("res://assets/dungeon/shooter.png");
    public static Texture2D Dasher => Get("res://assets/dungeon/dasher.png");
    public static Texture2D Boss => Get("res://assets/dungeon/boss.png");
    public static Texture2D Panel => Get("res://assets/kenney/pixel-ui/panel.png");
    public static Texture2D Button => Get("res://assets/kenney/pixel-ui/button.png");
    public static Texture2D ButtonHover => Get("res://assets/kenney/pixel-ui/button-hover.png");
    public static Texture2D ButtonPressed => Get("res://assets/kenney/pixel-ui/button-pressed.png");
    public static Texture2D ButtonDisabled => Get("res://assets/kenney/pixel-ui/button-disabled.png");

    private static Texture2D Get(string path)
    {
        if (Cache.TryGetValue(path, out var cached))
            return cached;
        var loaded = Load(path);
        Cache[path] = loaded;
        return loaded;
    }

    private static Texture2D Load(string path)
    {
        if (ResourceLoader.Exists(path))
        {
            var imported = GD.Load<Texture2D>(path);
            if (imported != null)
                return imported;
        }

        var image = Image.LoadFromFile(ProjectSettings.GlobalizePath(path));
        if (image == null)
        {
            GD.PushError($"洞穴之咒缺少图片 {path}。PNG 放在仓库里即可，打开工程时 Godot 会再导入。");
            image = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8);
            image.Fill(new Color("c4a574"));
        }

        return ImageTexture.CreateFromImage(image);
    }
}
