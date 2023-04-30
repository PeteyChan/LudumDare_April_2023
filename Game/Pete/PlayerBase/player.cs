using Godot;
using System;
using System.Collections.Generic;

public partial class player : RigidBody2D, Interactable
{
    public enum PlayerStates
    {
        Idle,
        Run,
        Falling,
        Jump,
        Wall_Jump,
        Landing,
        Attack,
        Damaged,
        KnockedOut,
        Dead
    }

    public enum AIStates
    {
        Idle,
        Wander,
    }

    public struct State_Data
    {
        public State_Data() { }
        public int hits = default;
        public HashSet<object> attackers = new HashSet<object>();
    }

    public struct Ai_Data
    {
        public float target_time;

    }


    [Export] public float move_speed = 900f, jump_strength = 2200f;
    [Export] public int health = 5;
    [Export] public bool is_player;

    Statemachine<PlayerStates> state = new Statemachine<PlayerStates>();
    State_Data state_data = new State_Data();
    Statemachine<AIStates> ai = new Statemachine<AIStates>();
    Ai_Data ai_data = new Ai_Data();

    (float move, bool jump, bool attack) input = default;
    List<Godot.Node> results_buffer = new List<Node>();
    Node2D armature;
    AnimationPlayer animator;
    public void OnEvent(object event_type)
    {
        switch (event_type)
        {
            case Events.OnAttack on_attacked:
                if (state_data.attackers.Contains(on_attacked.Attacker)) return;
                state_data.attackers.Add(on_attacked.Attacker);
                LinearVelocity = on_attacked.force;
                break;
        }
    }

    public override void _EnterTree()
    {
        grounded_query_params = new PhysicsShapeQueryParameters2D
        {
            Shape = new CircleShape2D { Radius = 25 },
            CollideWithBodies = true,
            Exclude = new Godot.Collections.Array<Rid> { this.GetRid() },
        };

        if (!this.TryFind(out animator)) throw new Debug.Exception(Name, "failed to setup animator");
        armature = FindChild("Armature") as Node2D;

        if (is_player)
        {
            var camera = GD.Load<PackedScene>("res://Pete/PlayerBase/player_camera.tscn").Instantiate() as Camera2D;
            AddChild(camera);
        }
    }

    public override void _Process(double delta)
    {
        LimitVelocity();
        UpdateGrounded();
        ProcessInput((float)delta);
        Cleanup();
        UpdateStatemachine((float)delta);

        if (Game.Show_Debug_Gizmos)
        {
            Debug.Label(state.current);
            Debug.Label("grounded", is_grounded);
            Debug.Label("velocity", LinearVelocity.ToString("0"));
            Debug.Label("position", Position.ToString("0"));
            Debug.Label("facing left", is_facing_left);
            Debug.Label();
        }

        void UpdateGrounded()
        {
            var grounded_transform = Godot.Transform2D.Identity;
            grounded_transform.Origin = new Vector2(GlobalTransform.Origin.X, GlobalTransform.Origin.Y - 40);
            grounded_query_params.Transform = grounded_transform;
            grounded_query_params.Motion = Vector2.Up * -40f;

            is_grounded = Physics.TryShapeCast2D(grounded_query_params, out var result, debug: Game.Show_Debug_Gizmos);
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

        void ProcessInput(float delta)
        {
            if (is_player)
            {
                input.move = Game.move_right.CurrentValue() - Game.move_left.CurrentValue();
                input.jump = Game.jump.Pressed();
                input.attack = Game.attack.Pressed();
            }
            else
            {
                switch (ai.Update(delta))
                {
                    case AIStates.Idle:                        
                        input = default;
                        if (ai.entered_state)
                            ai_data.target_time = Random.Shared.Range(2, 3);
                        
                        if (ai.state_time > ai_data.target_time)
                            ai.next = AIStates.Wander;
                        break;

                    case AIStates.Wander:
                        if (ai.entered_state)
                        {
                            ai_data.target_time = Random.Shared.Range(1f, 2f);
                            input.move = Random.Shared.NextSingle() > .5f ? -1 : 1;
                        }
                        if (ai.state_time > ai_data.target_time)
                            ai.next = AIStates.Idle;
                        break;

                    default:
                        ai.next = AIStates.Idle;
                        break;
                }
            }

            input.move = input.move.Clamp(-1, 1);
        }

        void Cleanup()
        {
            if (!state.exiting_state) return;

            switch (state.current)
            {
                case PlayerStates.Damaged:

                    armature.Modulate = Colors.White;
                    state_data.attackers.Clear();
                    break;

                case PlayerStates.KnockedOut:
                    state_data.hits = 0;
                    goto case PlayerStates.Damaged;
            }
        }
    }

    PhysicsShapeQueryParameters2D grounded_query_params;
    bool is_grounded, has_wall_jumped;
    bool is_facing_left => armature.Scale.X > 0;
    Vector2 ground_normal;

    void UpdateStatemachine(float delta)
    {
        switch (state.Update(delta))
        {
            case PlayerStates.Idle:
                if (state.entered_state)
                {
                    animator.Play("Idle", .2f);
                }

                LinearVelocity = LinearVelocity.Lerp(Vector2.Zero, delta * 7f);

                if (input.move.Abs() > .3f) state.next = PlayerStates.Run;
                if (input.attack) state.next = PlayerStates.Attack;
                if (!is_grounded) state.next = PlayerStates.Falling;
                if (input.jump) state.next = PlayerStates.Jump;

                if (state_data.attackers.Count > 0) state.next = PlayerStates.Damaged;
                break;

            case PlayerStates.Run:
                if (state.entered_state)
                {
                    if (input.move != 0)
                    {
                        animator.Play("Run", .2f, customSpeed: 1.5f);
                        UpdateFacing(input.move < 0);
                    }
                }

                var target_speed = new Vector2(move_speed * (is_facing_left ? -1 : 1), -20f);

                LinearVelocity = LinearVelocity.Lerp(target_speed, delta * 7f);

                if (input.move.Abs() < .3f) state.next = PlayerStates.Idle;
                if (input.attack) state.next = PlayerStates.Attack;
                if (!is_grounded) state.next = PlayerStates.Falling;
                if (input.jump) state.next = PlayerStates.Jump;

                if (state_data.attackers.Count > 0) state.next = PlayerStates.Damaged;
                break;

            case PlayerStates.Falling:
                if (state.entered_state)
                {
                    animator.Play("Fall", .2f);
                }

                if (state.previous == PlayerStates.Run // for a short period of time running off a platform, you can still jump 
                    && state.state_time < .1f
                    && input.jump)
                {
                    state.next = PlayerStates.Jump;
                }

                if (CanWallJump())
                    state.next = PlayerStates.Wall_Jump;

                if (is_grounded) state.next = PlayerStates.Landing;
                break;

            case PlayerStates.Jump:
                if (state.entered_state)
                {
                    animator.Play("Fall");
                    LinearVelocity = new Vector2(LinearVelocity.X, -jump_strength);
                }

                if (state.state_time > .1f)
                    state.next = PlayerStates.Falling;
                break;

            case PlayerStates.Wall_Jump:
                if (state.entered_state)
                {
                    has_wall_jumped = false;
                    animator.Play("WallJump", customSpeed: 1.5f);
                    UpdateFacing(!is_facing_left);
                    LinearVelocity = Vector2.Zero;
                }

                if (state.state_time > .2f)
                    state.next = PlayerStates.Falling;

                if (state.state_time > .1f && !has_wall_jumped)
                {
                    LinearVelocity = new Vector2(move_speed * (is_facing_left ? -1 : 1), -jump_strength);
                    has_wall_jumped = true;
                }

                if (is_grounded)
                {
                    state.next = PlayerStates.Landing;
                    UpdateFacing(!is_facing_left);
                }
                break;

            case PlayerStates.Landing:
                if (state.entered_state)
                    animator.Play("Crouching");

                LinearVelocity = LinearVelocity.Lerp(Vector2.Zero, delta * 10f);

                if (state.state_time > .1f)
                    if (input.move == 0)
                        state.next = PlayerStates.Idle;
                    else state.next = PlayerStates.Run;

                if (!is_grounded)
                    state.next = PlayerStates.Falling;
                break;


            case PlayerStates.Damaged:
                if (state.entered_state)
                {
                    state_data.hits++;
                    animator.Play("Damaged");
                    animator.Seek(0, true);
                }

                LinearVelocity = LinearVelocity.Lerp(Vector2.Zero, delta * 10f);

                if (state.state_time > .25f)
                    state.next = PlayerStates.Idle;

                if (!is_grounded)
                    state.next = PlayerStates.Falling;

                if (state_data.hits >= health)
                    state.next = PlayerStates.KnockedOut;

                armature.Modulate = Colors.Red.Lerp(Colors.White, state.state_time * 4f);
                break;

            case PlayerStates.Attack:
                if (state.entered_state)
                {
                    animator.Play("Attack", customSpeed: 2f);
                }
                LinearVelocity = LinearVelocity.Lerp(Vector2.Zero, delta * 10f);

                if (!is_grounded)
                    state.next = PlayerStates.Falling;
                if (state.state_time > .25f)
                    if (input.move == 0) state.next = PlayerStates.Idle;
                    else state.next = PlayerStates.Run;

                var foot = armature.FindChild("Foot_Left") as Node2D;
                grounded_query_params.Transform = foot.GlobalTransform;

                if (Physics.TryOverlapShape2D(grounded_query_params, results_buffer, debug: Game.Show_Debug_Gizmos))
                {
                    foreach (var node in results_buffer)
                        if (node.TryFindParent(out Interactable interactable))
                        {
                            interactable.OnEvent(new Events.OnAttack
                            {
                                Attacker = this,
                                force = new Vector2(move_speed * (is_facing_left ? -1 : 1), 0),
                            });
                        }
                }
                break;

            case PlayerStates.KnockedOut:
                if (state.entered_state)
                {
                    animator.Play("KnockDown");
                }

                float knockout_time = 5f;

                armature.Modulate = Colors.Red.Lerp(Colors.White, state.state_time / knockout_time);

                LinearVelocity = LinearVelocity.Lerp(Vector2.Zero, delta * 10f);

                if (!is_grounded || state.state_time > knockout_time)
                    state.next = PlayerStates.Idle;
                break;

            default:
                state.next = PlayerStates.Idle;
                break;
        }

        bool CanWallJump()
        {
            if (!input.jump) return false;
            if (LinearVelocity.X.Abs() < 10f)
                return false;

            Vector2 wall_jump_offset = new Vector2(30, -40);
            wall_jump_offset.X *= is_facing_left ? -1 : 1;
            var transform = Transform2D.Identity;
            transform.Origin = Position + wall_jump_offset;
            grounded_query_params.Transform = transform;
            return (Physics.TryOverlapShape2D(grounded_query_params, results_buffer, debug: Game.Show_Debug_Gizmos));
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
    public float total_time { get; private set; }
    public bool entered_state => state_time == 0;
    public bool exiting_state => next != null;
    public T? Update(float delta)
    {
        total_time += delta;
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