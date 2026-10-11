using Godot;

namespace CaveCurse;

public static class Art
{
    private static readonly Dictionary<string, Texture2D> Cache = new();

    public static Texture2D Floor => Get("res://assets/kenney/tiny-dungeon/floor.png");
    public static Texture2D FloorSpeck => Get("res://assets/kenney/tiny-dungeon/floor-speck.png");
    public static Texture2D FloorMottle => Get("res://assets/kenney/tiny-dungeon/floor-mottle.png");
    public static Texture2D Wall => Get("res://assets/kenney/tiny-dungeon/wall.png");
    public static Texture2D DoorArch => Get("res://assets/kenney/tiny-dungeon/door-arch.png");
    public static Texture2D DoorSide => Get("res://assets/kenney/tiny-dungeon/door-side.png");
    public static Texture2D Player => Get("res://assets/kenney/tiny-dungeon/player.png");
    public static Texture2D Shooter => Get("res://assets/kenney/monster-builder/shooter.png");
    public static Texture2D Dasher => Get("res://assets/kenney/monster-builder/dasher.png");
    public static Texture2D Boss => Get("res://assets/kenney/monster-builder/boss.png");
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
