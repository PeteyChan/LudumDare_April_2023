using Godot;
using System;

public partial class CollectionPoint : Area2D
{
    public struct Target
    {
        public Limb.Type limb;
        public Limb.Color color;
        public player.PlayerType player;
        public bool Match(Limb target)
        {
            return target != null
                && limb == target.type
                && color == target.color
                && player == target.player_type;
        }
    }

    public Target target;

    int remaining = 5;

    public override void _EnterTree()
    {
        target.limb = System.Enum.GetValues<Limb.Type>().RandomElement();

        this.BodyEntered += body =>
        {
            if (body.TryFindParent(out player player) && player.is_player)
            {
                if (target.Match(player.Inventory))
                {
                    remaining -= 1;
                    target.limb = System.Enum.GetValues<Limb.Type>().RandomElement();
                    target.player = System.Enum.GetValues<player.PlayerType>().RandomElement();
					player.Inventory = default;
					player.target = target;
                    OneOffLabel.Spawn(Position + new Vector2(0, -400), $"{remaining} To Go!!");
                }

                label.Text = $"Limb: {target.limb}\ncolor: {target.color}\ntype: {target.player}\n";
                player.target = target;

                if (remaining <= 0)
                    Scene.Load("res://Scenes/Win/win.tscn");
            }
        };
        label = FindChild("Label") as Label;
    }

    Label label;
}
