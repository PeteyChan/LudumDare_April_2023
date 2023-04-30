using Godot;
using System;

namespace SceneAssets.Win
{
    public partial class Button : Godot.Button
    {
        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            this.OnButtonDown(() =>
            {
                Scene.Load("res://Scenes/Title/Title.tscn");
            });
        }
    }
}