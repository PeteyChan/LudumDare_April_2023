using Godot;
using System;

namespace SceneAssets.Title
{
    public partial class Exit : Button
    {
        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            this.OnButtonDown(() => Scene.Tree.Quit());
        }
    }
}