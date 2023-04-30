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
        Debug.Label(lifetime);

        lifetime -= (float)delta;
        if (lifetime < 0)
            this.DestroyNode();
    }
}
