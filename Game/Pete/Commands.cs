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

}
