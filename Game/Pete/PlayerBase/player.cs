using Godot;
using System;
using System.Collections.Generic;

static class Layers
{
    public const int Default = 1;
    public const int Players = 1 << 1;
    public const int EnemyOnlyCollision = 1 << 2;
}

public partial class player : RigidBody2D, Interactable
{
    public enum PlayerType
    {
        cyberpunk_dude,
        cyberpunk_girl,
        cygore
    }

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
        Dead,
        Collect
    }

    public enum AIStates
    {
        Idle,
        Wander,
    }

    public class State_Data
    {
        public int hits = default;
        public HashSet<object> attackers = new HashSet<object>();
    }

    public class Ai_Data
    {
        public float target_time;

    }

    public class PlayerLimbs
    {
        Limb[] limbs = new Limb[System.Enum.GetValues<Limb.Type>().Length];
        public Limb this[Limb.Type type]
        {
            get => limbs[(int)type];
            set => limbs[(int)type] = value;
        }

        public int leg_count
        {
            get
            {
                int count = 0;
                if (this[Limb.Type.Right_Leg] != null) count++;
                if (this[Limb.Type.Left_Leg] != null) count++;
                return count;
            }
        }

        public int arm_count
        {
            get
            {
                int count = 0;
                if (this[Limb.Type.Right_Arm] != null) count++;
                if (this[Limb.Type.Left_Arm] != null) count++;
                return count;
            }
        }
        public bool has_head => this[Limb.Type.Head] != null;
    }


    [Export] public float move_speed = 900f, jump_strength = 2200f;
    [Export] public int health = 5;
    [Export] public bool is_player;
    [Export] public PlayerType player_type = PlayerType.cyberpunk_dude;

    [Export]
    public Limb.Color
        limb_color_head,
        limb_color_left_arm,
        limb_color_right_arm,
        limb_color_left_leg,
        limb_color_right_leg;

    public Limb Inventory;
    public CollectionPoint.Target target;
    public PlayerLimbs limbs = new PlayerLimbs();
    Statemachine<PlayerStates> state = new Statemachine<PlayerStates>();
    State_Data state_data = new State_Data();
    Statemachine<AIStates> ai = new Statemachine<AIStates>();
    Ai_Data ai_data = new Ai_Data();
    (float move, bool jump, bool attack, bool collect) input = default;
    List<Godot.Node> results_buffer = new List<Node>();
    List<Godot.Node> exclude_buffer;
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
            CollisionMask = Layers.Default,
            Exclude = new Godot.Collections.Array<Rid> { this.GetRid() },
        };

        if (!this.TryFind(out animator)) throw new Debug.Exception(Name, "failed to setup animator");
        armature = FindChild("Armature") as Node2D;

        exclude_buffer = new List<Node> { this };

        CollisionLayer = Layers.Players;
        CollisionMask = Layers.Default;
        if (is_player)
        {
            var camera = GD.Load<PackedScene>("res://Pete/PlayerBase/player_camera.tscn").Instantiate() as Camera2D;
            AddChild(camera);
        }
        else
        {
            CollisionMask |= Layers.EnemyOnlyCollision;
        }

        foreach (var limb in System.Enum.GetValues<Limb.Type>())
        {
            limbs[limb] = new Limb(this, limb);
        }
    }

    public override void _Process(double delta)
    {
        Game.score = state.total_time;
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
            float max = 2500;
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
                input.attack = Game.attack.OnPressed();
                input.collect = Game.collect.OnPressed();

                if (Game.back_to_title.OnPressed())
                    Debug.Console.Send("Load Title");
            }
            else
            {
                switch (ai.Update(delta))
                {
                    case AIStates.Idle:
                        input = default;
                        if (ai.entered_state)
                            ai_data.target_time = Random.Shared.Range(1, 2);

                        if (ai.state_time > ai_data.target_time)
                            ai.next = AIStates.Wander;
                        break;

                    case AIStates.Wander:
                        {
                            input.attack = default;
                            if (ai.entered_state)
                            {
                                ai_data.target_time = Random.Shared.Range(1f, 2f);
                                input.move = Random.Shared.NextSingle() > .5f ? -1 : 1;
                            }
                            if (ai.state_time > ai_data.target_time)
                                ai.next = AIStates.Idle;

                            if (ai.update_count % 3 == 0 && Physics.TryOverlapCircle2D(
                                GlobalPosition + new Vector2(face_left_value * -60, -120),
                                50, results_buffer, collide_area: false,
                                exclude: exclude_buffer, // this doesn't seem to be doing anything
                                debug: Game.Show_Debug_Gizmos)
                            )
                            {
                                int collisions = 0;
                                foreach (var item in results_buffer)
                                {
                                    if (item.TryFind(out player player))
                                    {
                                        if (player.is_player)
                                        {
                                            input.attack = true;
                                            return;
                                        }
                                        continue;
                                    }
                                    collisions++;
                                }
                                if (collisions > 0)
                                {
                                    input.move = -input.move;
                                }
                            }
                            break;
                        }

                    default:
                        ai.next = AIStates.Idle;
                        break;
                }

                if (Inputs.key_pad_4.Pressed())
                    input.move = -1;
                if (Inputs.key_pad_6.Pressed())
                    input.move = 1;
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
    float face_left_value => is_facing_left ? 1 : -1;
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

                if (input.collect) state.next = PlayerStates.Collect;
                TryAttacking();
                TryJumping();

                if (!is_grounded) state.next = PlayerStates.Falling;
                if (state_data.attackers.Count > 0) state.next = PlayerStates.Damaged;
                break;

            case PlayerStates.Run:
                if (state.entered_state)
                {
                    if (input.move != 0)
                    {
                        if (limbs.leg_count < 2)
                            animator.Play("Hop", .2f, customSpeed: 2f);
                        else
                            animator.Play("Run", .2f, customSpeed: 1.5f);
                    }
                }

                var target_speed = new Vector2(move_speed * (is_facing_left ? -1 : 1), -20f);

                if (limbs.leg_count < 2)
                    target_speed.X /= 5f;

                LinearVelocity = LinearVelocity.Lerp(target_speed, delta * 7f);

                if (input.move.Abs() < .3f) state.next = PlayerStates.Idle;
                if (input.collect) state.next = PlayerStates.Collect;

                TryAttacking();
                TryJumping();

                if (!is_grounded) state.next = PlayerStates.Falling;

                if (state_data.attackers.Count > 0) state.next = PlayerStates.Damaged;

                if (!state.exiting_state)
                    UpdateFacing(input.move < 0);
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
                    SFX.PlayJumpSound(GlobalPosition);
                    animator.Play("Fall");
                    LinearVelocity = new Vector2(LinearVelocity.X, -jump_strength);
                }

                if (state.state_time > .1f)
                    state.next = PlayerStates.Falling;
                break;

            case PlayerStates.Wall_Jump:
                if (state.entered_state)
                {
                    SFX.PlayLanding(GlobalPosition);

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
                    SFX.PlayJumpSound(GlobalPosition);
                }

                if (is_grounded)
                {
                    state.next = PlayerStates.Landing;
                    UpdateFacing(!is_facing_left);
                }
                break;

            case PlayerStates.Landing:
                if (state.entered_state)
                {
                    animator.Play("Crouching");
                    SFX.PlayLanding(GlobalPosition);
                }

                LinearVelocity = LinearVelocity.Lerp(Vector2.Zero, delta * 10f);

                if (state.state_time > .1f)
                    if (input.move == 0)
                        state.next = PlayerStates.Idle;
                    else state.next = PlayerStates.Run;

                if (state_data.attackers.Count > 0) state.next = PlayerStates.Damaged;

                if (!is_grounded)
                    state.next = PlayerStates.Falling;
                break;


            case PlayerStates.Damaged:
                if (state.entered_state)
                {
                    state_data.hits++;
                    animator.Play("Damaged");
                    animator.Seek(0, true);

                    SFX.PlayHitImpact(GlobalPosition);
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
                    SFX.PlayAttackWhoosh(GlobalPosition);

                    if (input.move != 0)
                        UpdateFacing(input.move < 0);

                    LinearVelocity = new Vector2(-face_left_value * move_speed, 0);
                }
                LinearVelocity = LinearVelocity.Lerp(Vector2.Zero, delta * 10f);

                if (!is_grounded)
                    state.next = PlayerStates.Falling;

                if (state.state_time > .4f)
                    if (input.move == 0) state.next = PlayerStates.Idle;
                    else state.next = PlayerStates.Run;

                if (state_data.attackers.Count > 0) state.next = PlayerStates.Damaged;

                if (animator.CurrentAnimationPosition > .2f &&
                    animator.CurrentAnimationPosition < .5f)
                {
                    var foot = armature.FindChild("Foot_Left") as Node2D;

                    if (Physics.TryOverlapCircle2D(foot.GlobalPosition, 20, results_buffer, mask: Layers.Players | Layers.Default, exclude: exclude_buffer, debug: Game.Show_Debug_Gizmos))
                    {
                        foreach (var node in results_buffer)
                        {
                            if (node == this) continue;
                            if (node.TryFindParent(out Interactable interactable))
                            {
                                interactable.OnEvent(new Events.OnAttack
                                {
                                    Attacker = this,
                                    force = new Vector2(move_speed * (is_facing_left ? -1 : 1), 0),
                                });
                            }
                        }
                    }
                }
                break;

            case PlayerStates.KnockedOut:
                if (state.entered_state)
                {
                    animator.Play("KnockDown");
                }

                float knockout_time = 5f;

                armature.Modulate = Colors.Red.Lerp(Colors.White, (state.state_time / knockout_time).MaxValue(1));

                var vel = LinearVelocity;
                vel.X = vel.X.Lerp(0, delta * 10f);
                LinearVelocity = vel;


                if (!limbs.has_head)
                    break;

                if (limbs.leg_count == 0)
                    break;

                if (state.state_time > knockout_time)
                    state.next = PlayerStates.Idle;
                break;

            case PlayerStates.Collect:
                if (state.entered_state)
                {
                    animator.Play("Crouching", .1f);

                    Vector2 label_position = Position + new Vector2(0, -240);

                    if (Inventory != null)
                    {
                        OneOffLabel.Spawn(label_position, "Inventory is full");
                        return;
                    }

                    else if (Physics.TryOverlapCircle2D(Position, 100, results_buffer, exclude: exclude_buffer, debug: Game.Show_Debug_Gizmos))
                    {
                        foreach (var node in results_buffer)
                            if (node.TryFindParent(out player player) && player.state.current == PlayerStates.KnockedOut)
                            {
                                var limb = player.limbs[target.limb];
                                if (target.Match(limb))
                                {
                                    Inventory = limb;
                                    player.limbs[target.limb] = null;
                                    foreach (var target_nodes in player.GetLimbNodes(target.limb))
                                    {
                                        target_nodes.Visible = false;
                                    }
                                    OneOffLabel.Spawn(label_position, "Got Target Limb!!");
                                    SFX.PlayLimbRip(label_position);
                                    return;
                                }
                            }
                    }

                    OneOffLabel.Spawn(label_position, "Nothing Here");
                }

                if (state.state_time > .5f) state.next = PlayerStates.Idle;
                LinearVelocity = LinearVelocity.Lerp(Vector2.Zero, delta * 10f);
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

        void TryJumping()
        {
            if (input.jump)
            {
                if (limbs.leg_count == 2)
                    state.next = PlayerStates.Jump;
            }
        }

        void TryAttacking()
        {
            if (input.attack)
            {
                if (limbs.leg_count == 2)
                    state.next = PlayerStates.Attack;
            }
        }
    }

    public Node2D[] GetLimbNodes(Limb.Type type)
    {
        var nodes = new List<Node2D>();
        switch (type)
        {
            case Limb.Type.Head:
                Get("Head");
                break;

            case Limb.Type.Left_Arm:
                Get("Shoulder_Left");
                Get("Forearm_Left");
                Get("Hand_Left");
                break;

            case Limb.Type.Right_Arm:
                Get("Shoulder_Right");
                Get("Forearm_Right");
                Get("Hand_Right");
                break;

            case Limb.Type.Left_Leg:
                Get("Thigh_Left");
                Get("Calf_Left");
                Get("Foot_Left");
                break;

            case Limb.Type.Right_Leg:
                Get("Thigh_Right");
                Get("Calf_Right");
                Get("Foot_Right");
                break;
        }

        void Get(string name)
        {
            var node = armature.FindChild(name) as Node2D;
            if (!node.IsValid()) throw new Debug.Exception(this.Name, "failed to find ", name);
            nodes.Add(node);
        }

        return nodes.ToArray();
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
    public int update_count { get; private set; }
    public T? Update(float delta)
    {
        total_time += delta;
        update_count++;
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

public class Limb
{
    public Limb(player player, Type type)
    {
        switch (type)
        {
            case Type.Head:
                color = player.limb_color_head;
                break;
            case Type.Left_Arm:
                color = player.limb_color_left_arm;
                break;

            case Type.Right_Arm:
                color = player.limb_color_right_arm;
                break;

            case Type.Left_Leg:
                color = player.limb_color_left_leg;
                break;

            case Type.Right_Leg:
                color = player.limb_color_right_leg;
                break;
        }

        this.type = type;
        player_type = player.player_type;



        Godot.Color modulate_color = Colors.White;
        switch (color)
        {
            case Color.Blue: modulate_color = Colors.NavyBlue; break;
            case Color.Green: modulate_color = Colors.Green; break;
            case Color.Pink: modulate_color = Colors.Pink; break;
            case Color.Purple: modulate_color = Colors.Purple; break;
        }

        foreach (var node in player.GetLimbNodes(type))
        {
            node.Modulate = modulate_color;
        }
    }

    public Color color = default;
    public Type type;
    public player.PlayerType player_type;
    public enum Color
    {
        None,
        Blue,
        Green,
        Pink,
        Purple,
    }

    public enum Type
    {
        Head,
        Left_Arm,
        Right_Arm,
        Left_Leg,
        Right_Leg
    }
}