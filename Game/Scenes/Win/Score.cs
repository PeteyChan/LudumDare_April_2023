using Godot;
using System;

namespace SceneAssets
{

    public partial class Score : Label
    {
        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
			Text = $"Completed in {(int)Game.score} seconds!";
        }
    }
}