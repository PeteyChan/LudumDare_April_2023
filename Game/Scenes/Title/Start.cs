using Godot;
using System;

public partial class Start : Button
{
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        this.OnButtonDown(() =>
        {
            Scene.Load("res://Scenes/Game/Game.tscn");
        });
    }
}
