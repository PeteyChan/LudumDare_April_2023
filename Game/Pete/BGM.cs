using Godot;
using System;

public partial class BGM : AudioStreamPlayer
{
    public override void _Process(double delta)
    {
		if (!Playing)
			Play();
    }
}
