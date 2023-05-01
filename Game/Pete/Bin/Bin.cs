using Godot;
using System;

namespace SceneAssets
{
    public partial class Bin : Node2D, Interactable
    {
        void Interactable.OnEvent(object event_type)
        {
            switch (event_type)
            {
                case Events.OnAttack attack:
                    this.TryFind(out AnimationPlayer player);
                    player.Play("Break", customSpeed: 2f);

                    this.TryFind(out Area2D area);
                    area.CollisionLayer = 0;
                    return;
            }
        }
    }

}