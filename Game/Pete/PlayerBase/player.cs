using Godot;
using System;
using System.Collections.Generic;

public partial class player : RigidBody2D
{

    public enum PlayerStates
    {
        Idle,
        Walk,
        Falling,
        Jump
    }

    [Export] public Inputs move_left = Inputs.key_a, move_right = Inputs.key_d, jump = Inputs.key_space;
    [Export] public float move_speed = 200f, jump_strength = 500f, grounded_offset = -20f;

    Statemachine<PlayerStates> state_machine = new Statemachine<PlayerStates>();

    AnimationPlayer aniamtor_torso, animator_legs;
    List<Godot.Node> node_buffer = new List<Node>();

    public override void _Ready()
    {
        foreach (var animator in this.FindAll<AnimationPlayer>())
        {
            if (animator.Name.ToString().Contains("Torso"))
                aniamtor_torso = animator;
            if (animator.Name.ToString().Contains("Hips"))
                animator_legs = animator;
        }

        Debug.Assert(() => animator_legs.IsValid() && aniamtor_torso.IsValid(), Name, ": Animators were not set up properly");

        grounded_query_params = new PhysicsShapeQueryParameters2D
        {
            Shape = new CircleShape2D { Radius = 25 },
            CollideWithBodies = true,
            Exclude = new Godot.Collections.Array<Rid> { this.GetRid() },
        };
    }



    public override void _Process(double delta)
    {
        UpdateGrounded();
        UpdateStatemachine((float)delta);

        move_direction = move_right.CurrentValue() - move_left.CurrentValue();


        Debug.Label(state_machine.current);
        Debug.Label("grounded", grounded);
        Debug.Label("velocity", LinearVelocity);
        Debug.Label("position", Position);

        void UpdateGrounded()
        {
            var grounded_transform = Godot.Transform2D.Identity;
            grounded_transform.Origin = new Vector2(GlobalTransform.Origin.X, GlobalTransform.Origin.Y + grounded_offset - 20);
            grounded_query_params.Transform = grounded_transform;
            grounded_query_params.Motion = Vector2.Up * grounded_offset * 2f;

            grounded = Physics.TryShapeCast2D(grounded_query_params, out var result, debug: true);
        }
    }
    
    PhysicsShapeQueryParameters2D grounded_query_params;

    float move_direction;

    bool grounded;


    void UpdateStatemachine(float delta)
    {
        switch (state_machine.Update(delta))
        {
            case PlayerStates.Idle:
                if (state_machine.entered_state)
                {
                    aniamtor_torso.Play("Idle");
                    animator_legs.Play("Idle");
                }

                LinearVelocity = Vector2.Zero;

                if (move_direction.Abs() > .3f) state_machine.next = PlayerStates.Walk;
                if (!grounded) state_machine.next = PlayerStates.Falling;
                if (jump.OnPressed()) state_machine.next = PlayerStates.Jump;
                break;

            case PlayerStates.Walk:
                if (state_machine.entered_state)
                {
                    aniamtor_torso.Play("Walk");
                    animator_legs.Play("Walk");
                }

                LinearVelocity = new Vector2(move_direction * move_speed, -20f);

                if (move_direction.Abs() < .3f) state_machine.next = PlayerStates.Idle;
                if (!grounded) state_machine.next = PlayerStates.Falling;
                if (jump.OnPressed()) state_machine.next = PlayerStates.Jump;

                break;

            case PlayerStates.Falling:
                if (state_machine.entered_state)
                {
                    aniamtor_torso.Play("Fall");
                    aniamtor_torso.Play("Fall");
                }

                if (CanWallJump())
                {
                    var sign = LinearVelocity.X < 0 ? 1 : -1;
                    LinearVelocity = new Vector2(move_speed * sign, -jump_strength);
                    state_machine.next = PlayerStates.Jump;
                }

                if (grounded) state_machine.next = PlayerStates.Idle;
                break;

            case PlayerStates.Jump:
                if (state_machine.entered_state)
                {
                    aniamtor_torso.Play("Fall");
                    aniamtor_torso.Play("Fall");
                    LinearVelocity = new Vector2(LinearVelocity.X, -jump_strength);
                }

                if (state_machine.state_time > .1f)
                    state_machine.next = PlayerStates.Falling;
                break;

            default:
                state_machine.next = PlayerStates.Idle;
                break;
        }

        bool CanWallJump()
        {
            if (!jump.Pressed()) return false;
            if (LinearVelocity.X == 0)
                return false;

            Vector2 wall_jump_offset = new Vector2(30, -40);
            wall_jump_offset.X *= LinearVelocity.X < 0 ? -1f : 1;

            var transform = Transform2D.Identity;
            transform.Origin = Position + wall_jump_offset;
            grounded_query_params.Transform = transform;
            return (Physics.TryOverlapShape2D(grounded_query_params, node_buffer, debug: true));
        }
    }
}

class Statemachine<T> where T : struct, Enum
{
    public T? current { get; private set; }
    public T? next;
    public float state_time { get; private set; }
    public bool entered_state => state_time == 0;
    public bool exiting_state => next != null;
    public T? Update(float delta)
    {
        if (next != null)
        {
            current = next;
            next = null;
            state_time = 0;
        }
        else
        {
            state_time += delta;
        }
        return current;
    }
}