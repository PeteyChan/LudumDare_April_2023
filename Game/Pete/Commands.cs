using Godot;
using System;

public class Commands
{

    static void Teleport(Debug.Console args)
    {
        foreach (var player in Scene.Current.FindAll<player>())
        {
            if (player.is_player)
            {
                player.Position = new Vector2(args.ToInt(0), args.ToInt(1));
            }
        }
    }

    static void LoadRicksLevel(Debug.Console args)
    {
        Scene.Load("res://Rick Assets/Ricks Test Scene.tscn");
    }

    static void LoadTitle(Debug.Console args)
    {
        Scene.Load("res://Scenes/Title/Title.tscn");
    }

    static void SpawnPlayer(Debug.Console args)
    {
        var player = GD.Load<PackedScene>("res://Pete/PlayerBase/player.tscn").Instantiate();
        Scene.Current.AddChild(player);
    }

    static void SpawnDude(Debug.Console args)
    {
        var player = GD.Load<PackedScene>("res://Assets/Actors/cyberpunk_dude.tscn").Instantiate();
        Scene.Current.AddChild(player);
    }

    static void SpawnGirl(Debug.Console args)
    {
        var player = GD.Load<PackedScene>("res://Assets/Actors/cyberpunk_girl.tscn").Instantiate();
        Scene.Current.AddChild(player);
    }
}
