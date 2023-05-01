using Godot;
using System;

public partial class OneOffLabel : Label
{
    public static void Spawn(Vector2 position, string Text, float life_time = 2f)
    {
        var label = GD.Load<PackedScene>("res://Pete/OneOffLabel.tscn").Instantiate() as OneOffLabel;
        label.Text = Text;
        label.Position = position;
        Scene.Current.AddChild(label);
        label.lifetime = life_time;
    }

    float lifetime;

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        Modulate = Colors.White.Lerp(new Color(1, 1, 1, 0), (1 - lifetime).Clamp(0, 1));
        lifetime -= (float)delta;
        if (lifetime < 0)
            this.DestroyNode();
    }
}
