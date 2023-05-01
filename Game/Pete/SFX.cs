using Godot;
using System;

public partial class SFX : AudioStreamPlayer2D
{
    public static void PlayLimbRip(Vector2 position)
    {
        var sound = GD.Load<PackedScene>("res://Assets/SFX/LimbRipSound.tscn").Instantiate() as SFX;
        sound.Position = position;
        Scene.Current.AddChild(sound);
    }

    public static void PlayHitImpact(Vector2 position)
    {
        var sound = GD.Load<PackedScene>("res://Assets/SFX/HitImpact.tscn").Instantiate() as SFX;
        sound.Position = position;
        sound.pitch_variance = .4f;
        Scene.Current.AddChild(sound);
    }

    public static void PlayAttackWhoosh(Vector2 position)
    {
        var sound = GD.Load<PackedScene>("res://Assets/SFX/Attack.tscn").Instantiate() as SFX;
        sound.Position = position;
        sound.pitch_variance = .4f;
        Scene.Current.AddChild(sound);
    }

    public static void PlayBinBreak(Vector2 position)
    {
        var sound = GD.Load<PackedScene>("res://Assets/SFX/BinBreak.tscn").Instantiate() as SFX;
        sound.Position = position;
        sound.pitch_variance = .4f;
        Scene.Current.AddChild(sound);
    }

    public static void PlayJumpSound(Vector2 position)
    {
        var sound = GD.Load<PackedScene>("res://Assets/SFX/JumpSound.tscn").Instantiate() as SFX;
        sound.Position = position;
        sound.pitch_variance = .2f;
        Scene.Current.AddChild(sound);
    }

    static float last_landing;
    public static void PlayLanding(Vector2 position)
    {
        if (Bootstrap.seconds_since_bootstrap < last_landing + .2f)
            return;
        last_landing = Bootstrap.seconds_since_bootstrap;

        var sound = GD.Load<PackedScene>("res://Assets/SFX/Landing.tscn").Instantiate() as SFX;
        sound.Position = position;
        sound.pitch_variance = .4f;
        Scene.Current.AddChild(sound);
    }

    float pitch_variance;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        var random = System.Random.Shared.NextSingle();
        PitchScale = PitchScale - pitch_variance * random;
        Play();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (!Playing) this.DestroyNode();
    }
}
