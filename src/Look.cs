using CaveCurse.Core;
using Godot;

namespace CaveCurse;

public static class Look
{
    private const float Tile = 48f;
    public const float ActorScale = 3f;
    private const float Scale = ActorScale;
    private static readonly Color Void = new("100c14");

    public static float FoeSpan(bool boss, bool dasher)
    {
        var tex = boss ? Art.Boss : dasher ? Art.Dasher : Art.Shooter;
        return tex.GetHeight() * Scale;
    }

    public static void Chrome(CanvasItem canvas, Rect2 rect)
    {
        DrawNineSlice(canvas, Art.Panel, rect, 6f);
    }

    public static void Room(CanvasItem canvas, Rect2 room)
    {
        DrawFloor(canvas, room);
        DrawStandingWalls(canvas, room);
    }

    public static Rect2 DoorRect(Rect2 room, Dir dir)
    {
        var center = room.GetCenter();
        return dir switch
        {
            Dir.North => new Rect2(center.X - 46, room.Position.Y - 8, 92, 28),
            Dir.South => new Rect2(center.X - 46, room.End.Y - 20, 92, 28),
            Dir.West => new Rect2(room.Position.X - 8, center.Y - 46, 28, 92),
            _ => new Rect2(room.End.X - 20, center.Y - 46, 28, 92),
        };
    }

    public static void DoorGap(CanvasItem canvas, Rect2 door, bool near, Rect2 room, bool locked = false)
    {
        var glow = near && !locked;
        var tint = locked
            ? new Color(0.42f, 0.4f, 0.46f)
            : glow ? new Color(1f, 0.93f, 0.72f) : Colors.White;
        if (door.Size.X >= door.Size.Y)
        {
            var north = door.GetCenter().Y < room.GetCenter().Y;
            DrawFaceDoor(canvas, room, north, door.GetCenter().X, tint, glow, locked);
            return;
        }

        var west = door.GetCenter().X < room.GetCenter().X;
        DrawSideDoor(canvas, room, west, door.GetCenter().Y, tint, glow, locked);
    }

    public static void Person(CanvasItem canvas, Vector2 pos, Vector2 face, float scale = 1f, float flash = 0f, float alpha = 1f)
    {
        if (face.LengthSquared() < 0.01f)
            face = Vector2.Down;
        DrawActor(canvas, Art.Player, pos, face.X < -0.01f, winding: false, scale, flash, alpha);
    }

    public static void Sword(CanvasItem canvas, Vector2 pos, Vector2 face, int tier, float scale = 1f)
    {
        var pixel = ActorScale * Mathf.Max(0.05f, scale);
        var flip = face.X < -0.01f;
        // Sprite pixels from the texture center. The grip sits on the body's side, and the blade points up.
        // Facing left mirrors that offset the same way DrawActor mirrors the sprite.
        var hand = pos + new Vector2(flip ? -5f : 5f, 2f) * pixel;
        var tip = hand + new Vector2(flip ? -1.5f : 1.5f, -14f) * pixel;
        var width = MathF.Max(4f, 3.5f * scale);
        if (SwordImbue.Glows(tier))
        {
            var glow = RgbColor(SwordImbue.Glow(tier));
            glow.A = 0.55f;
            canvas.DrawLine(hand, tip, glow, width * 3.2f);
        }
        canvas.DrawLine(hand, tip, RgbColor(SwordImbue.Blade(tier)), width);
        var guard = new Vector2(3.2f * pixel, 0f);
        canvas.DrawLine(hand - guard, hand + guard, new Color("6e675c"), MathF.Max(2f, width * 0.7f));
    }

    private static Color RgbColor(Rgb color) =>
        new(color.R / 255f, color.G / 255f, color.B / 255f);

    public static void Shooter(CanvasItem canvas, Vector2 pos, Vector2 face, bool winding, float scale = 1f, float flash = 0f, float alpha = 1f)
    {
        DrawActor(canvas, Art.Shooter, pos, face.X < -0.01f, winding, scale, flash, alpha);
    }

    public static void Dasher(CanvasItem canvas, Vector2 pos, Vector2 face, bool winding, float scale = 1f, float flash = 0f, float alpha = 1f)
    {
        DrawActor(canvas, Art.Dasher, pos, face.X < -0.01f, winding, scale, flash, alpha);
    }

    public static void Boss(CanvasItem canvas, Vector2 pos, Vector2 face, bool winding, float scale = 1f, float flash = 0f, float alpha = 1f)
    {
        DrawActor(canvas, Art.Boss, pos, face.X < -0.01f, winding, scale, flash, alpha);
    }

    private static void DrawFloor(CanvasItem canvas, Rect2 room)
    {
        for (var y = room.Position.Y; y < room.End.Y; y += Tile)
        {
            for (var x = room.Position.X; x < room.End.X; x += Tile)
            {
                var dest = new Rect2(x, y, Mathf.Min(Tile, room.End.X - x), Mathf.Min(Tile, room.End.Y - y));
                var ix = Mathf.FloorToInt((x - room.Position.X) / Tile);
                var iy = Mathf.FloorToInt((y - room.Position.Y) / Tile);
                BlitFit(canvas, FloorAt(ix, iy), dest, Colors.White);
            }
        }
    }

    private static Texture2D FloorAt(int ix, int iy)
    {
        var n = (ix * 3 + iy * 5) & 3;
        return n switch
        {
            0 => Art.FloorA,
            1 => Art.FloorB,
            2 => Art.FloorC,
            _ => Art.FloorD,
        };
    }

    private static void DrawStandingWalls(CanvasItem canvas, Rect2 room)
    {
        var x0 = room.Position.X;
        var y0 = room.Position.Y;
        var x1 = room.End.X;
        var y1 = room.End.Y;
        for (var x = x0; x < x1; x += Tile)
        {
            var w = Mathf.Min(Tile, x1 - x);
            var left = x <= x0 + 0.5f;
            var right = x + Tile >= x1 - 0.5f;
            BlitFit(canvas, Face(left, right), new Rect2(x, y0 - Tile, w, Tile), Colors.White);
            BlitFit(canvas, Cap(left, right), new Rect2(x, y0 - Tile * 2f, w, Tile), Colors.White);
            BlitFit(canvas, Cap(left, right), new Rect2(x, y1 - Tile, w, Tile), Colors.White);
            BlitFit(canvas, Face(left, right), new Rect2(x, y1, w, Tile), Colors.White);
        }

        DrawColumn(canvas, x0 - Tile, y0, y1, west: true);
        DrawColumn(canvas, x1, y0, y1, west: false);
    }

    private static void DrawColumn(CanvasItem canvas, float x, float y0, float y1, bool west)
    {
        var face = west ? Art.WallFaceLeft : Art.WallFaceRight;
        var cap = west ? Art.WallCapLeft : Art.WallCapRight;
        Blit(canvas, cap, x, y0 - Tile * 2f, Colors.White);
        for (var y = y0 - Tile; y < y1 + Tile; y += Tile)
            Blit(canvas, face, x, y, Colors.White);
    }

    private static void DrawFaceDoor(CanvasItem canvas, Rect2 room, bool north, float centerX, Color tint, bool glow, bool locked)
    {
        var x = SnapSpan(centerX, room.Position.X, room.Position.X, room.End.X, 4);
        var y = north ? room.Position.Y - Tile * 2f : room.End.Y - Tile;
        canvas.DrawRect(new Rect2(x, y, Tile * 4f, Tile * 2f), Void);
        Blit(canvas, Art.DoorJambLeft, x, y, tint);
        Blit(canvas, Art.DoorOpen, x + Tile, y, tint);
        Blit(canvas, Art.DoorJambRight, x + Tile * 3f, y, tint);
        Blit(canvas, Art.DoorLintel, x + Tile, y - Tile, tint);
        if (locked)
            canvas.DrawRect(new Rect2(x + Tile + 8f, y + 16f, Tile * 2f - 16f, Tile * 2f - 28f), new Color(0.05f, 0.04f, 0.07f, 0.62f));
        if (!glow)
            return;
        canvas.DrawRect(new Rect2(x + Tile + 16f, y + 20f, Tile * 2f - 32f, Tile * 2f - 36f), new Color(1f, 0.84f, 0.4f, 0.34f));
    }

    private static void DrawSideDoor(CanvasItem canvas, Rect2 room, bool west, float centerY, Color tint, bool glow, bool locked)
    {
        var x = west ? room.Position.X - Tile : room.End.X;
        var y = SnapSpan(centerY, room.Position.Y, room.Position.Y, room.End.Y, 2);
        canvas.DrawRect(new Rect2(x, y, Tile, Tile * 2f), Void);
        // The arch is the same front-facing opening, seated in this wall and a
        // little wider than the column so the stone jambs stay visible.
        var dest = new Rect2(x - 12f, y - 10f, Tile + 24f, Tile * 2f + 12f);
        canvas.DrawTextureRect(Art.DoorOpen, dest, false, tint);
        if (locked)
            canvas.DrawRect(new Rect2(x + 6f, y + 16f, Tile - 12f, Tile * 2f - 28f), new Color(0.05f, 0.04f, 0.07f, 0.62f));
        if (!glow)
            return;
        canvas.DrawRect(new Rect2(x + 10f, y + 18f, Tile - 20f, Tile * 2f - 30f), new Color(1f, 0.84f, 0.4f, 0.34f));
    }

    private static Texture2D Face(bool left, bool right)
    {
        if (left)
            return Art.WallFaceLeft;
        if (right)
            return Art.WallFaceRight;
        return Art.WallFace;
    }

    private static Texture2D Cap(bool left, bool right)
    {
        if (left)
            return Art.WallCapLeft;
        if (right)
            return Art.WallCapRight;
        return Art.WallCap;
    }

    private static float SnapSpan(float center, float origin, float min, float max, int tiles)
    {
        var length = tiles * Tile;
        var maxIndex = Mathf.Max(0, Mathf.FloorToInt((max - min - length) / Tile));
        var index = Mathf.Clamp(Mathf.RoundToInt((center - length * 0.5f - origin) / Tile), 0, maxIndex);
        return origin + index * Tile;
    }

    private static void Blit(CanvasItem canvas, Texture2D tex, float x, float y, Color tint)
    {
        var dest = new Rect2(x, y, tex.GetWidth() * Scale, tex.GetHeight() * Scale);
        canvas.DrawTextureRect(tex, dest, false, tint);
    }

    private static void BlitFit(CanvasItem canvas, Texture2D tex, Rect2 dest, Color tint)
    {
        var fullW = tex.GetWidth() * Scale;
        var fullH = tex.GetHeight() * Scale;
        if (dest.Size.X >= fullW - 0.5f && dest.Size.Y >= fullH - 0.5f)
        {
            canvas.DrawTextureRect(tex, dest, false, tint);
            return;
        }

        var src = new Rect2(0f, 0f, tex.GetWidth() * dest.Size.X / fullW, tex.GetHeight() * dest.Size.Y / fullH);
        canvas.DrawTextureRectRegion(tex, dest, src, tint);
    }

    private static void DrawNineSlice(CanvasItem canvas, Texture2D tex, Rect2 rect, float srcMargin)
    {
        if (rect.Size.X < 4f || rect.Size.Y < 4f)
            return;
        var tw = tex.GetWidth();
        var th = tex.GetHeight();
        var margin = Mathf.Min(srcMargin, Mathf.Floor(Mathf.Min(rect.Size.X, rect.Size.Y) / 2f));
        float[] srcX = { 0f, srcMargin, tw - srcMargin, tw };
        float[] srcY = { 0f, srcMargin, th - srcMargin, th };
        float[] dstX = { rect.Position.X, rect.Position.X + margin, rect.End.X - margin, rect.End.X };
        float[] dstY = { rect.Position.Y, rect.Position.Y + margin, rect.End.Y - margin, rect.End.Y };
        for (var row = 0; row < 3; row++)
        {
            for (var col = 0; col < 3; col++)
            {
                var dest = new Rect2(dstX[col], dstY[row], dstX[col + 1] - dstX[col], dstY[row + 1] - dstY[row]);
                var src = new Rect2(srcX[col], srcY[row], srcX[col + 1] - srcX[col], srcY[row + 1] - srcY[row]);
                if (dest.Size.X < 0.5f || dest.Size.Y < 0.5f)
                    continue;
                canvas.DrawTextureRectRegion(tex, dest, src);
            }
        }
    }

    private static void DrawActor(CanvasItem canvas, Texture2D tex, Vector2 pos, bool flipX, bool winding, float scale, float flash, float alpha)
    {
        scale = Mathf.Max(0.05f, scale);
        alpha = Mathf.Clamp(alpha, 0f, 1f);
        var width = tex.GetWidth() * Scale * scale;
        var height = tex.GetHeight() * Scale * scale;
        var tint = winding ? new Color(1f, 0.88f, 0.55f, alpha) : new Color(1f, 1f, 1f, alpha);
        // A negative width flips the texture, and Godot keeps the rect's left edge where it is.
        // Start at the left edge either way so facing left mirrors in place instead of sliding a full body width.
        var rect = new Rect2(pos.X - width / 2f, pos.Y - height / 2f, flipX ? -width : width, height);
        canvas.DrawTextureRect(tex, rect, false, tint);
        if (winding)
        {
            canvas.DrawRect(
                new Rect2(pos.X - width / 2f, pos.Y - height / 2f, width, height),
                new Color(1f, 0.75f, 0.22f, 0.4f * alpha));
        }
        if (flash <= 0.02f)
            return;
        canvas.DrawRect(
            new Rect2(pos.X - width / 2f, pos.Y - height / 2f, width, height),
            new Color(1f, 0.97f, 0.9f, 0.55f * Mathf.Clamp(flash, 0f, 1f) * alpha));
    }
}
