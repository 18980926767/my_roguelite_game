using Godot;

namespace CaveCurse;

public static class Look
{
    private const float Tile = 32f;
    private const float WallBand = 18f;
    private const float PersonSize = 32f;
    private const float ShooterHeight = 46f;
    private const float DasherHeight = 44f;
    private const float BossHeight = 76f;

    public static float FoeSpan(bool boss, bool dasher)
    {
        if (boss)
            return BossHeight;
        return dasher ? DasherHeight : ShooterHeight;
    }

    public static void Chrome(CanvasItem canvas, Rect2 rect)
    {
        DrawNineSlice(canvas, Art.Panel, rect, 6f);
    }

    public static void Room(CanvasItem canvas, Rect2 room)
    {
        TileFill(canvas, room.Grow(WallBand), Art.Wall, vary: false);
        TileFill(canvas, room, Art.Floor, vary: true);
    }

    public static void DoorGap(CanvasItem canvas, Rect2 door, bool near, Rect2 room)
    {
        var opening = Opening(door, room);
        TileFill(canvas, opening, Art.Floor, vary: true);
        var tint = near ? new Color(1f, 0.93f, 0.72f) : Colors.White;
        if (door.Size.X >= door.Size.Y)
            DrawMark(canvas, Art.DoorArch, door.GetCenter(), 48f, tint);
        else
            canvas.DrawTextureRect(Art.DoorSide, opening, false, tint);
        if (near)
            canvas.DrawRect(opening, new Color(1f, 0.84f, 0.4f, 0.34f));
    }

    public static void Person(CanvasItem canvas, Vector2 pos, Vector2 face)
    {
        if (face.LengthSquared() < 0.01f)
            face = Vector2.Down;
        face = face.Normalized();
        DrawActor(canvas, Art.Player, pos, face, PersonSize, rotate: true, winding: false);
    }

    public static void Shooter(CanvasItem canvas, Vector2 pos, Vector2 face, bool winding)
    {
        DrawActor(canvas, Art.Shooter, pos, face, ShooterHeight, rotate: false, winding: winding);
    }

    public static void Dasher(CanvasItem canvas, Vector2 pos, Vector2 face, bool winding)
    {
        DrawActor(canvas, Art.Dasher, pos, face, DasherHeight, rotate: false, winding: winding);
    }

    public static void Boss(CanvasItem canvas, Vector2 pos, Vector2 face, bool winding)
    {
        DrawActor(canvas, Art.Boss, pos, face, BossHeight, rotate: false, winding: winding);
    }

    private static Rect2 Opening(Rect2 door, Rect2 room)
    {
        if (door.Size.X >= door.Size.Y)
        {
            var top = door.GetCenter().Y < room.GetCenter().Y;
            var y = top ? room.Position.Y - WallBand : door.Position.Y;
            var bottom = top ? door.End.Y : room.End.Y + WallBand;
            return new Rect2(door.Position.X, y, door.Size.X, bottom - y);
        }

        var left = door.GetCenter().X < room.GetCenter().X;
        var x = left ? room.Position.X - WallBand : door.Position.X;
        var right = left ? door.End.X : room.End.X + WallBand;
        return new Rect2(x, door.Position.Y, right - x, door.Size.Y);
    }

    private static void TileFill(CanvasItem canvas, Rect2 area, Texture2D primary, bool vary)
    {
        if (area.Size.X < 1f || area.Size.Y < 1f)
            return;
        var x0 = Mathf.Floor(area.Position.X / Tile) * Tile;
        var y0 = Mathf.Floor(area.Position.Y / Tile) * Tile;
        for (var y = y0; y < area.End.Y; y += Tile)
        {
            for (var x = x0; x < area.End.X; x += Tile)
            {
                var cell = new Rect2(x, y, Tile, Tile);
                var visible = cell.Intersection(area);
                if (visible.Size.X < 0.5f || visible.Size.Y < 0.5f)
                    continue;
                var tex = primary;
                if (vary && Speck(Mathf.FloorToInt(x / Tile), Mathf.FloorToInt(y / Tile)))
                    tex = Art.FloorSpeck;
                DrawCell(canvas, tex, cell, visible);
            }
        }
    }

    private static bool Speck(int ix, int iy)
    {
        var n = (ix * 73856093) ^ (iy * 19349663);
        return (n & 15) == 0;
    }

    private static void DrawCell(CanvasItem canvas, Texture2D tex, Rect2 cell, Rect2 visible)
    {
        if (Mathf.IsEqualApprox(visible.Position.X, cell.Position.X)
            && Mathf.IsEqualApprox(visible.Position.Y, cell.Position.Y)
            && Mathf.IsEqualApprox(visible.Size.X, cell.Size.X)
            && Mathf.IsEqualApprox(visible.Size.Y, cell.Size.Y))
        {
            canvas.DrawTextureRect(tex, cell, false);
            return;
        }

        var sx = tex.GetWidth() / cell.Size.X;
        var sy = tex.GetHeight() / cell.Size.Y;
        var src = new Rect2(
            (visible.Position.X - cell.Position.X) * sx,
            (visible.Position.Y - cell.Position.Y) * sy,
            visible.Size.X * sx,
            visible.Size.Y * sy);
        canvas.DrawTextureRectRegion(tex, visible, src);
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

    private static void DrawMark(CanvasItem canvas, Texture2D tex, Vector2 center, float size, Color tint)
    {
        canvas.DrawTextureRect(tex, new Rect2(center.X - size / 2f, center.Y - size / 2f, size, size), false, tint);
    }

    private static void DrawActor(CanvasItem canvas, Texture2D tex, Vector2 pos, Vector2 face, float height, bool rotate, bool winding)
    {
        if (face.LengthSquared() < 0.01f)
            face = Vector2.Down;
        face = face.Normalized();
        var aspect = tex.GetWidth() / (float)Mathf.Max(1, tex.GetHeight());
        var width = height * aspect;
        var tint = winding ? new Color(1f, 0.88f, 0.55f) : Colors.White;
        var rotation = rotate ? face.Angle() - Mathf.Pi / 2f : 0f;
        var flip = !rotate && face.X < 0f ? -1f : 1f;
        canvas.DrawSetTransform(pos, rotation, new Vector2(flip, 1f));
        canvas.DrawTextureRect(tex, new Rect2(-width / 2f, -height / 2f, width, height), false, tint);
        canvas.DrawSetTransform(Vector2.Zero, 0f, Vector2.One);
        if (!winding)
            return;
        canvas.DrawRect(
            new Rect2(pos.X - width / 2f, pos.Y - height / 2f, width, height),
            new Color(1f, 0.75f, 0.22f, 0.4f));
    }
}
