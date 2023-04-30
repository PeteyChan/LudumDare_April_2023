using Godot;
using System;

public class Commands
{

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

}
