using Godot;
using System;

namespace SceneAssets.Instructions
{
    public partial class Instruction : Label
    {
        public override void _Ready()
        {
            Text =
            @$"
			-- Objective --
			Deliver the specified parts to the collector as fast a possible.
			Parts can only be retrieved while the owners are knocked out.

			-- Controls --
			Move Left :	{Game.move_left}
			Move Right: {Game.move_right}
			Attack: {Game.attack}
			Collect: {Game.collect}
            Back to Title: {Game.back_to_title}

            -- Press Attack to Continue -- 			
			";
        }

        double time = 0;
        public override void _Process(double delta)
        {
            time += delta;
            if (time > 20 || Game.attack.OnPressed())
            {
                Scene.Load("res://Scenes/Game/Game.tscn");
            }
        }
    }
}

