using Godot;
using System;
using System.Collections.Generic;

public partial class player : RigidBody2D
{
    public enum PlayerStates
    {
        Idle,
        Run,
        Falling,
        Jump,
        Wall_Jump,
        Landing,
    }

    [Export] public Inputs move_left = Inputs.key_a, move_right = Inputs.key_d, jump = Inputs.key_space;
    [Export] public float move_speed = 200f, jump_strength = 500f, grounded_offset = -20f;

    Statemachine<PlayerStates> state_machine = new Statemachine<PlayerStates>();

    AnimationPlayer animator;
    List<Godot.Node> node_buffer = new List<Node>();
    Node2D armature;

    public override void _Ready()
    {
        if (!this.TryFind(out animator))
            throw new Debug.Exception(Name, "failed to find animator");

        armature = this.FindChild("Armature") as Node2D;
        grounded_query_params = new PhysicsShapeQueryParameters2D
        {
            Shape = new CircleShape2D { Radius = 25 },
            CollideWithBodies = true,
            Exclude = new Godot.Collections.Array<Rid> { this.GetRid() },
        };
    }



    public override void _Process(double delta)
    {
        LimitVelocity();
        UpdateGrounded();
        UpdateStatemachine((float)delta);

        move_direction = move_right.CurrentValue() - move_left.CurrentValue();

        if (Game.Show_Debug_Gizmos)
        {
            Debug.Label(state_machine.current);
            Debug.Label("grounded", grounded);
            Debug.Label("velocity", LinearVelocity.ToString("0"));
            Debug.Label("position", Position.ToString("0"));
            Debug.Label("facing left", facing_left);
        }

        void UpdateGrounded()
        {
            var grounded_transform = Godot.Transform2D.Identity;
            grounded_transform.Origin = new Vector2(GlobalTransform.Origin.X, GlobalTransform.Origin.Y + grounded_offset - 20);
            grounded_query_params.Transform = grounded_transform;
            grounded_query_params.Motion = Vector2.Up * grounded_offset * 2f;

            grounded = Physics.TryShapeCast2D(grounded_query_params, out var result, debug: Game.Show_Debug_Gizmos);
            ground_normal = result.normal;
        }

        void LimitVelocity()
        {
            var velcoity = LinearVelocity;
            float max = 2000;
            velcoity.X = velcoity.X.Clamp(-max, max);
            velcoity.Y = velcoity.Y.Clamp(-max, max);
            if (velcoity != LinearVelocity)
                LinearVelocity = velcoity;
        }
    }

    PhysicsShapeQueryParameters2D grounded_query_params;

    float move_direction;
    bool grounded, wall_jumped;
    bool facing_left => armature.Scale.X > 0;
    Vector2 ground_normal;

    void UpdateStatemachine(float delta)
    {
        switch (state_machine.Update(delta))
        {
            case PlayerStates.Idle:
                if (state_machine.entered_state)
                {
                    animator.Play("Idle", .2f);
                }

                LinearVelocity = LinearVelocity.Lerp(Vector2.Zero, delta * 7f);

                if (move_direction.Abs() > .3f) state_machine.next = PlayerStates.Run;
                if (!grounded) state_machine.next = PlayerStates.Falling;
                if (jump.OnPressed()) state_machine.next = PlayerStates.Jump;
                break;

            case PlayerStates.Run:
                if (state_machine.entered_state)
                {
                    if (move_direction != 0)
                    {
                        animator.Play("Run", .2f, customSpeed: 2f);
                        UpdateFacing(move_direction < 0);
                    }
                }

                var target_speed = new Vector2(move_speed * (facing_left ? -1 : 1), -20f);

                LinearVelocity = LinearVelocity.Lerp(target_speed, delta * 7f);

                if (move_direction.Abs() < .3f) state_machine.next = PlayerStates.Idle;
                if (!grounded) state_machine.next = PlayerStates.Falling;
                if (jump.OnPressed()) state_machine.next = PlayerStates.Jump;

                break;

            case PlayerStates.Falling:
                if (state_machine.entered_state)
                {
                    animator.Play("Fall", .2f);
                }

                if (state_machine.previous == PlayerStates.Run // for a short period of time running off a platform, you can still jump 
                    && state_machine.state_time < .1f
                    && jump.Pressed())
                {
                    state_machine.next = PlayerStates.Jump;
                    Debug.Log("platform jump");
                }

                if (CanWallJump())
                    state_machine.next = PlayerStates.Wall_Jump;

                if (grounded) state_machine.next = PlayerStates.Landing;
                break;

            case PlayerStates.Jump:
                if (state_machine.entered_state)
                {
                    animator.Play("Fall");
                    LinearVelocity = new Vector2(LinearVelocity.X, -jump_strength);
                }

                if (state_machine.state_time > .1f)
                    state_machine.next = PlayerStates.Falling;
                break;

            case PlayerStates.Wall_Jump:
                if (state_machine.entered_state)
                {
                    wall_jumped = false;
                    animator.Play("WallJump", customSpeed: 1.5f);
                    UpdateFacing(!facing_left);
                    LinearVelocity = Vector2.Zero;
                }

                if (state_machine.state_time > .2f)
                    state_machine.next = PlayerStates.Falling;

                if (state_machine.state_time > .1f && !wall_jumped)
                {
                    LinearVelocity = new Vector2(move_speed * (facing_left ? -1 : 1), -jump_strength);
                    wall_jumped = true;
                }

                if (grounded)
                {
                    state_machine.next = PlayerStates.Landing;
                    UpdateFacing(!facing_left);
                }
                break;

            case PlayerStates.Landing:
                if (state_machine.entered_state)
                    animator.Play("Crouching");

                LinearVelocity = LinearVelocity.Lerp(Vector2.Zero, delta * 10f);

                if (state_machine.state_time > .1f)
                    if (move_direction == 0)
                        state_machine.next = PlayerStates.Idle;
                    else state_machine.next = PlayerStates.Run;
                break;

            default:
                state_machine.next = PlayerStates.Idle;
                break;
        }

        bool CanWallJump()
        {
            if (!jump.Pressed()) return false;
            if (LinearVelocity.X.Abs() < 10f)
                return false;

            Vector2 wall_jump_offset = new Vector2(30, -40);
            wall_jump_offset.X *= facing_left ? -1 : 1;
            var transform = Transform2D.Identity;
            transform.Origin = Position + wall_jump_offset;
            grounded_query_params.Transform = transform;
            return (Physics.TryOverlapShape2D(grounded_query_params, node_buffer, debug: Game.Show_Debug_Gizmos));
        }

        void UpdateFacing(bool face_left)
        {
            var scale = armature.Scale;
            if (face_left && armature.Scale.X < 0)
                scale.X = -scale.X;
            if (!face_left && armature.Scale.X > 0)
                scale.X = -scale.X;
            armature.Scale = scale;
        }
    }
}

class Statemachine<T> where T : struct, Enum
{
    public T? previous { get; private set; }
    public T? current { get; private set; }
    public T? next;
    public float state_time { get; private set; }
    public bool entered_state => state_time == 0;
    public bool exiting_state => next != null;
    public T? Update(float delta)
    {
        if (next != null)
        {
            previous = current;
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