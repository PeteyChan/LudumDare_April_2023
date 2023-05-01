using Godot;
using System;

public partial class collection_target : Node
{
    [Export] public Limb.Type limb;
    [Export] public Limb.Color color;
    [Export] public player.PlayerType type;
}
