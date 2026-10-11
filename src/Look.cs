using Godot;

namespace CaveCurse;

public static class Look
{
    public static void Room(CanvasItem canvas, Rect2 room)
    {
        canvas.DrawRect(room.Grow(18), new Color("2a241c"));
        var tile = 36f;
        var col = 0;
        for (var y = room.Position.Y; y < room.End.Y; y += tile)
        {
            var row = col;
            for (var x = room.Position.X; x < room.End.X; x += tile)
            {
                var w = Mathf.Min(tile - 1, room.End.X - x);
                var h = Mathf.Min(tile - 1, room.End.Y - y);
                var shade = (row + col) % 2 == 0 ? new Color("6d7c86") : new Color("62727c");
                canvas.DrawRect(new Rect2(x, y, w, h), shade);
                row++;
            }
            col++;
        }
    }

    public static void DoorGap(CanvasItem canvas, Rect2 door, bool near)
    {
        canvas.DrawRect(door.Grow(6), near ? new Color("f0d78c") : new Color("c4a574"));
        canvas.DrawRect(door, new Color("1c1814"));
    }

    public static void Person(CanvasItem canvas, Vector2 pos, Vector2 face)
    {
        if (face.LengthSquared() < 0.01f)
            face = Vector2.Down;
        face = face.Normalized();
        canvas.DrawCircle(pos + new Vector2(0, 6), 14, new Color(0, 0, 0, 0.35f));
        canvas.DrawCircle(pos, 13, new Color("243042"));
        canvas.DrawCircle(pos + face * 7f, 6, new Color("f3d2b3"));
        canvas.DrawLine(pos, pos + face * 16f, new Color("d7c4a3"), 2f);
    }

    public static void Shooter(CanvasItem canvas, Vector2 pos, Vector2 face, bool winding)
    {
        if (face.LengthSquared() < 0.01f)
            face = Vector2.Down;
        face = face.Normalized();
        var body = winding ? new Color("f0d078") : new Color("c0392b");
        canvas.DrawCircle(pos, 18, body);
        canvas.DrawRect(new Rect2(pos + face * 8f - new Vector2(4, 4), new Vector2(16, 8)), new Color("2c2c2c"));
        canvas.DrawCircle(pos + new Vector2(-5, -4), 3, new Color("f7f3ea"));
        canvas.DrawCircle(pos + new Vector2(5, -4), 3, new Color("f7f3ea"));
    }

    public static void Dasher(CanvasItem canvas, Vector2 pos, Vector2 face, bool winding)
    {
        if (face.LengthSquared() < 0.01f)
            face = Vector2.Down;
        face = face.Normalized();
        var side = new Vector2(-face.Y, face.X);
        var nose = pos + face * 22f;
        var left = pos - face * 14f + side * 14f;
        var right = pos - face * 14f - side * 14f;
        var color = winding ? new Color("f0d078") : new Color("d35400");
        canvas.DrawColoredPolygon(new[] { nose, left, right }, color);
    }

    public static void Boss(CanvasItem canvas, Vector2 pos, Vector2 face, bool winding)
    {
        Shooter(canvas, pos, face, winding);
        canvas.DrawArc(pos, 28, 0, Mathf.Tau, 24, new Color("8e44ad"), 3f);
    }
}
