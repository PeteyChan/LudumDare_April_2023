using Godot;
using System;
using System.Collections.Generic;

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

    Queue<collection_target> collection_targets = new Queue<collection_target>();
    public override void _EnterTree()
    {
        label = FindChild("Label") as Label;

        foreach (var collection_target in this.FindAll<collection_target>())
            collection_targets.Enqueue(collection_target);

        if (collection_targets.Count > 0)
            TryGetNextTarget();

        UpdateLabel();

        this.BodyEntered += body =>
        {
            if (body.TryFindParent(out player player) && player.is_player)
            {
                if (target.Match(player.Inventory))
                {
                    player.Inventory = default;

                    if (!TryGetNextTarget()) return;

                    string text = collection_targets.Count == 0 ? "Last one!" : $"{collection_targets.Count + 1} To Go!!";

                    OneOffLabel.Spawn(Position + new Vector2(0, -400), text);
                }

                player.target = target;
            }
        };
    }

    Label label;

    bool TryGetNextTarget()
    {
        if (collection_targets.TryDequeue(out var next))
        {
            target.limb = next.limb;
            target.color = next.color;
            target.player = next.type;

            UpdateLabel();
            return true;
        }
        else
        {
            Scene.Load("res://Scenes/Win/win.tscn");
            return false;
        }
    }

    void UpdateLabel()
    {
        label.Text = $"--Target--\nLimb: {target.limb}\nFrom: {target.player}\n";
        //label.Text = $"Limb: {target.limb}\ncolor: {target.color}\ntype: {target.player}\n";
    }
}
