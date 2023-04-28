using Godot;
using System.Collections.Generic;

public enum Inputs : byte
{
    //keyboard
    key_a,
    key_b,
    key_c,
    key_d,
    key_e,
    key_f,
    key_g,
    key_h,
    key_i,
    key_j,
    key_k,
    key_l,
    key_m,
    key_n,
    key_o,
    key_p,
    key_q,
    key_r,
    key_s,
    key_t,
    key_u,
    key_v,
    key_w,
    key_x,
    key_y,
    key_z,

    key_1,
    key_2,
    key_3,
    key_4,
    key_5,
    key_6,
    key_7,
    key_8,
    key_9,
    key_0,

    key_f1,
    key_f2,
    key_f3,
    key_f4,
    key_f5,
    key_f6,
    key_f7,
    key_f8,
    key_f9,
    key_f10,
    key_f11,
    key_f12,

    key_print_scrren,
    key_num_lock,
    key_scroll_lock,
    key_pause_break,
    key_escape,
    key_back_quote,
    key_caps_lock,
    key_context_menu,

    key_tab,
    key_space,
    key_shift,
    key_control,
    key_alt,

    key_left_bracket,
    key_right_bracket,
    key_semi_colon,
    key_quote,
    key_less_than,
    key_greater_than,
    key_back_slash,
    key_forward_slash,

    key_minus,
    key_equals,
    key_backspace,
    key_enter,

    key_insert,
    key_home,
    key_page_up,
    key_page_down,
    key_delete,
    key_end,

    key_up_arrow,
    key_down_arrow,
    key_left_arrow,
    key_right_arrow,

    key_pad_divide,
    key_pad_multiply,
    key_pad_minus,
    key_pad_plus,

    key_pad_0,
    key_pad_1,
    key_pad_2,
    key_pad_3,
    key_pad_4,
    key_pad_5,
    key_pad_6,
    key_pad_7,
    key_pad_8,
    key_pad_9,
    key_pad_dot,
    key_pad_enter,

    //mouse

    mouse_left_click,
    mouse_right_click,
    mouse_middle_click,
    mouse_extra_button1,
    mouse_extra_button2,
    mouse_wheel_up,
    mouse_wheel_down,

    // mouse axis inputs    
    mouse_move_up,
    mouse_move_down,
    mouse_move_left,
    mouse_move_right,

    // controllers

    joy1_start,
    joy1_select,
    joy1_dpad_up,
    joy1_dpad_down,
    joy1_dpad_left,
    joy1_dpad_right,
    joy1_lstick_up,
    joy1_lstick_down,
    joy1_lstick_left,
    joy1_lstick_right,
    joy1_rstick_up,
    joy1_rstick_down,
    joy1_rstick_left,
    joy1_rstick_right,
    joy1_left_shoulder,
    joy1_left_trigger,
    joy1_left_hat,
    joy1_right_shoulder,
    joy1_right_trigger,
    joy1_right_hat,
    joy1_button_cross,
    joy1_button_circle,
    joy1_button_triangle,
    joy1_button_square,
    joy1_home,

    joy2_start,
    joy2_select,
    joy2_dpad_up,
    joy2_dpad_down,
    joy2_dpad_left,
    joy2_dpad_right,
    joy2_lstick_up,
    joy2_lstick_down,
    joy2_lstick_left,
    joy2_lstick_right,
    joy2_rstick_up,
    joy2_rstick_down,
    joy2_rstick_left,
    joy2_rstick_right,
    joy2_left_shoulder,
    joy2_left_trigger,
    joy2_left_hat,
    joy2_right_shoulder,
    joy2_right_trigger,
    joy2_right_hat,
    joy2_button_cross,
    joy2_button_circle,
    joy2_button_triangle,
    joy2_button_square,
    joy2_home,

    joy3_start,
    joy3_select,
    joy3_dpad_up,
    joy3_dpad_down,
    joy3_dpad_left,
    joy3_dpad_right,
    joy3_lstick_up,
    joy3_lstick_down,
    joy3_lstick_left,
    joy3_lstick_right,
    joy3_rstick_up,
    joy3_rstick_down,
    joy3_rstick_left,
    joy3_rstick_right,
    joy3_left_shoulder,
    joy3_left_trigger,
    joy3_left_hat,
    joy3_right_shoulder,
    joy3_right_trigger,
    joy3_right_hat,
    joy3_button_cross,
    joy3_button_circle,
    joy3_button_triangle,
    joy3_button_square,
    joy3_home,

    joy4_start,
    joy4_select,
    joy4_dpad_up,
    joy4_dpad_down,
    joy4_dpad_left,
    joy4_dpad_right,
    joy4_lstick_up,
    joy4_lstick_down,
    joy4_lstick_left,
    joy4_lstick_right,
    joy4_rstick_up,
    joy4_rstick_down,
    joy4_rstick_left,
    joy4_rstick_right,
    joy4_left_shoulder,
    joy4_left_trigger,
    joy4_left_hat,
    joy4_right_shoulder,
    joy4_right_trigger,
    joy4_right_hat,
    joy4_button_cross,
    joy4_button_circle,
    joy4_button_triangle,
    joy4_button_square,
    joy4_home,
}

public static partial class InputSystem
{
    public static IEnumerable<Inputs> MouseInputs
    {
        get
        {
            for (int i = (int)Inputs.mouse_left_click; i <= (int)Inputs.mouse_move_right; ++i)
                yield return (Inputs)i;

        }
    }

    public static IEnumerable<Inputs> KeyboardInputs
    {
        get
        {
            for (int i = 0; i <= (int)Inputs.key_pad_enter; ++i)
                yield return (Inputs)i;

        }
    }

    public static IEnumerable<Inputs> GamepadInputs
    {
        get
        {
            for (int i = (int)Inputs.joy1_start; i <= (int)Inputs.joy4_home; ++i)
                yield return (Inputs)i;
        }
    }

    public static IEnumerable<Inputs> All_Excluding_Mouse_Move
    {
        get
        {
            foreach (var input in KeyboardInputs)
                yield return input;

            for (int i = (int)Inputs.mouse_left_click; i <= (int)Inputs.mouse_wheel_down; ++i)
                yield return (Inputs)i;

            foreach (var input in GamepadInputs)
                yield return input;
        }
    }


    public static bool IsKeyboardInput(this Inputs input) => (int)input <= (int)Inputs.key_pad_enter;
    public static bool IsMouseAxis(this Inputs input) => (int)input >= (int)Inputs.mouse_move_up && (int)input <= (int)Inputs.mouse_move_right;
    public static bool IsMouseInput(this Inputs input) => (int)input >= (int)Inputs.mouse_left_click && (int)input <= (int)Inputs.mouse_extra_button2;
    public static bool IsJoypadInput(this Inputs input) => (int)input >= (int)Inputs.joy1_start;
    public static float deadzone = .25f;
    public static bool Pressed(this Inputs input, Godot.Node node = null) => CurrentValue(input) >= deadzone;
    public static bool Released(this Inputs input, Godot.Node node = null) => CurrentValue(input) < deadzone;
    public static bool OnPressed(this Inputs input, Godot.Node node = null) => Pressed(input) && PreviousValue(input) < deadzone;
    public static bool OnReleased(this Inputs input, Godot.Node node = null) => Released(input) && PreviousValue(input) >= deadzone;
    public static float CurrentValue(this Inputs input, Godot.Node node = null) => impl.current[(int)input];
    public static float PreviousValue(this Inputs input, Godot.Node node = null) => impl.previous[(int)input];
    static Impl impl = new Impl().SetParent(Internal.Bootstrap.Node.GetTree().Root);
    static void Update(Bootstrap.Process args) => impl.OnUpdate();
    partial class Impl : Godot.Node
    {
        public Impl() => Name = "Input System";
        static int input_count => System.Enum.GetValues<Inputs>().Length;
        public float[] previous = new float[input_count];
        public float[] current = new float[input_count];

        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventMouseButton button && button.Pressed)
                switch (button.ButtonIndex)
                {
                    case MouseButton.WheelUp: wheel_up = 1f; break;
                    case MouseButton.WheelDown: wheel_down = 1f; break;
                    default: break;
                }
        }

        float wheel_up, wheel_down;
        public void OnUpdate()
        {
            System.Array.Copy(current, previous, previous.Length);
            System.Array.Clear(current);
            var mouse_velocity = Godot.Input.GetLastMouseVelocity();

            foreach (var input in System.Enum.GetValues<Inputs>())
                switch (input)
                {
                    case Inputs.key_a: Set(input, Key.A); break;
                    case Inputs.key_b: Set(input, Key.B); break;
                    case Inputs.key_c: Set(input, Key.C); break;
                    case Inputs.key_d: Set(input, Key.D); break;
                    case Inputs.key_e: Set(input, Key.E); break;
                    case Inputs.key_f: Set(input, Key.F); break;
                    case Inputs.key_g: Set(input, Key.G); break;
                    case Inputs.key_h: Set(input, Key.H); break;
                    case Inputs.key_i: Set(input, Key.I); break;
                    case Inputs.key_j: Set(input, Key.J); break;
                    case Inputs.key_k: Set(input, Key.K); break;
                    case Inputs.key_l: Set(input, Key.L); break;
                    case Inputs.key_m: Set(input, Key.M); break;
                    case Inputs.key_n: Set(input, Key.N); break;
                    case Inputs.key_o: Set(input, Key.O); break;
                    case Inputs.key_p: Set(input, Key.P); break;
                    case Inputs.key_q: Set(input, Key.Q); break;
                    case Inputs.key_r: Set(input, Key.R); break;
                    case Inputs.key_s: Set(input, Key.S); break;
                    case Inputs.key_t: Set(input, Key.T); break;
                    case Inputs.key_u: Set(input, Key.U); break;
                    case Inputs.key_v: Set(input, Key.V); break;
                    case Inputs.key_w: Set(input, Key.W); break;
                    case Inputs.key_x: Set(input, Key.X); break;
                    case Inputs.key_y: Set(input, Key.Y); break;
                    case Inputs.key_z: Set(input, Key.Z); break;

                    case Inputs.key_1: Set(input, Key.Key1); break;
                    case Inputs.key_2: Set(input, Key.Key2); break;
                    case Inputs.key_3: Set(input, Key.Key3); break;
                    case Inputs.key_4: Set(input, Key.Key4); break;
                    case Inputs.key_5: Set(input, Key.Key5); break;
                    case Inputs.key_6: Set(input, Key.Key6); break;
                    case Inputs.key_7: Set(input, Key.Key7); break;
                    case Inputs.key_8: Set(input, Key.Key8); break;
                    case Inputs.key_9: Set(input, Key.Key9); break;
                    case Inputs.key_0: Set(input, Key.Key0); break;

                    case Inputs.key_f1: Set(input, Key.F1); break;
                    case Inputs.key_f2: Set(input, Key.F2); break;
                    case Inputs.key_f3: Set(input, Key.F3); break;
                    case Inputs.key_f4: Set(input, Key.F4); break;
                    case Inputs.key_f5: Set(input, Key.F5); break;
                    case Inputs.key_f6: Set(input, Key.F6); break;
                    case Inputs.key_f7: Set(input, Key.F7); break;
                    case Inputs.key_f8: Set(input, Key.F8); break;
                    case Inputs.key_f9: Set(input, Key.F9); break;
                    case Inputs.key_f10: Set(input, Key.F10); break;
                    case Inputs.key_f11: Set(input, Key.F11); break;
                    case Inputs.key_f12: Set(input, Key.F12); break;

                    case Inputs.key_print_scrren: Set(input, Key.Print); break;
                    case Inputs.key_num_lock: Set(input, Key.Numlock); break;
                    case Inputs.key_scroll_lock: Set(input, Key.Scrolllock); break;
                    case Inputs.key_pause_break: Set(input, Key.Pause); break;
                    case Inputs.key_escape: Set(input, Key.Escape); break;
                    case Inputs.key_back_quote: Set(input, Key.Quoteleft); break;
                    case Inputs.key_caps_lock: Set(input, Key.Capslock); break;
                    case Inputs.key_context_menu: Set(input, Key.Menu); break;
                    case Inputs.key_tab: Set(input, Key.Tab); break;
                    case Inputs.key_space: Set(input, Key.Space); break;
                    case Inputs.key_shift: Set(input, Key.Shift); break;
                    case Inputs.key_control: Set(input, Key.Ctrl); break;
                    case Inputs.key_alt: Set(input, Key.Alt); break;
                    case Inputs.key_left_bracket: Set(input, Key.Bracketleft); break;
                    case Inputs.key_right_bracket: Set(input, Key.Bracketright); break;
                    case Inputs.key_semi_colon: Set(input, Key.Semicolon); break;
                    case Inputs.key_quote: Set(input, Key.Apostrophe); break;
                    case Inputs.key_less_than: Set(input, Key.Comma); break;
                    case Inputs.key_greater_than: Set(input, Key.Period); break;
                    case Inputs.key_back_slash: Set(input, Key.Backslash); break;
                    case Inputs.key_forward_slash: Set(input, Key.Slash); break;
                    case Inputs.key_minus: Set(input, Key.Minus); break;
                    case Inputs.key_equals: Set(input, Key.Equal); break;
                    case Inputs.key_backspace: Set(input, Key.Backspace); break;
                    case Inputs.key_enter: Set(input, Key.Enter); break;

                    case Inputs.key_insert: Set(input, Key.Insert); break;
                    case Inputs.key_home: Set(input, Key.Home); break;
                    case Inputs.key_page_up: Set(input, Key.Pageup); break;
                    case Inputs.key_page_down: Set(input, Key.Pagedown); break;
                    case Inputs.key_delete: Set(input, Key.Delete); break;
                    case Inputs.key_end: Set(input, Key.End); break;
                    case Inputs.key_up_arrow: Set(input, Key.Up); break;
                    case Inputs.key_down_arrow: Set(input, Key.Down); break;
                    case Inputs.key_left_arrow: Set(input, Key.Left); break;
                    case Inputs.key_right_arrow: Set(input, Key.Right); break;

                    case Inputs.key_pad_divide: Set(input, Key.KpDivide); break;
                    case Inputs.key_pad_multiply: Set(input, Key.KpMultiply); break;
                    case Inputs.key_pad_minus: Set(input, Key.KpSubtract); break;
                    case Inputs.key_pad_plus: Set(input, Key.KpAdd); break;

                    case Inputs.key_pad_0: Set(input, Key.Kp0); break;
                    case Inputs.key_pad_1: Set(input, Key.Kp1); break;
                    case Inputs.key_pad_2: Set(input, Key.Kp2); break;
                    case Inputs.key_pad_3: Set(input, Key.Kp3); break;
                    case Inputs.key_pad_4: Set(input, Key.Kp4); break;
                    case Inputs.key_pad_5: Set(input, Key.Kp5); break;
                    case Inputs.key_pad_6: Set(input, Key.Kp6); break;
                    case Inputs.key_pad_7: Set(input, Key.Kp7); break;
                    case Inputs.key_pad_8: Set(input, Key.Kp8); break;
                    case Inputs.key_pad_9: Set(input, Key.Kp9); break;
                    case Inputs.key_pad_dot: Set(input, Key.KpPeriod); break;
                    case Inputs.key_pad_enter: Set(input, Key.KpEnter); break;

                    case Inputs.mouse_left_click: Set(input, MouseButton.Left); break;
                    case Inputs.mouse_right_click: Set(input, MouseButton.Right); break;
                    case Inputs.mouse_middle_click: Set(input, MouseButton.Middle); break;
                    case Inputs.mouse_wheel_up: Set(input, wheel_up); break;
                    case Inputs.mouse_wheel_down: Set(input, wheel_down); break;
                    case Inputs.mouse_extra_button1: Set(input, MouseButton.Xbutton1); break;
                    case Inputs.mouse_extra_button2: Set(input, MouseButton.Xbutton2); break;

                    case Inputs.mouse_move_up: Set(input, -mouse_velocity.Y); break;
                    case Inputs.mouse_move_down: Set(input, mouse_velocity.Y); break;
                    case Inputs.mouse_move_left: Set(input, -mouse_velocity.X); break;
                    case Inputs.mouse_move_right: Set(input, mouse_velocity.X); break;

                    case Inputs.joy1_start: Set(input, 0, JoyButton.Start); break;
                    case Inputs.joy1_select: Set(input, 0, JoyButton.Back); break;
                    case Inputs.joy1_dpad_up: Set(input, 0, JoyButton.DpadUp); break;
                    case Inputs.joy1_dpad_down: Set(input, 0, JoyButton.DpadDown); break;
                    case Inputs.joy1_dpad_left: Set(input, 0, JoyButton.DpadLeft); break;
                    case Inputs.joy1_dpad_right: Set(input, 0, JoyButton.DpadRight); break;
                    case Inputs.joy1_left_shoulder: Set(input, 0, JoyButton.LeftShoulder); break;
                    case Inputs.joy1_left_hat: Set(input, 0, JoyButton.LeftStick); break;
                    case Inputs.joy1_right_shoulder: Set(input, 0, JoyButton.RightShoulder); break;
                    case Inputs.joy1_right_hat: Set(input, 0, JoyButton.RightStick); break;
                    case Inputs.joy1_button_cross: Set(input, 0, JoyButton.A); break;
                    case Inputs.joy1_button_circle: Set(input, 0, JoyButton.B); break;
                    case Inputs.joy1_button_triangle: Set(input, 0, JoyButton.Y); break;
                    case Inputs.joy1_button_square: Set(input, 0, JoyButton.X); break;
                    case Inputs.joy1_home: Set(input, 0, JoyButton.Guide); break;

                    case Inputs.joy1_lstick_up: Set(input, -Godot.Input.GetJoyAxis(0, JoyAxis.LeftY)); break;
                    case Inputs.joy1_lstick_down: Set(input, Godot.Input.GetJoyAxis(0, JoyAxis.LeftY)); break;
                    case Inputs.joy1_lstick_left: Set(input, -Godot.Input.GetJoyAxis(0, JoyAxis.LeftX)); break;
                    case Inputs.joy1_lstick_right: Set(input, Godot.Input.GetJoyAxis(0, JoyAxis.LeftX)); break; ;
                    case Inputs.joy1_rstick_up: Set(input, -Godot.Input.GetJoyAxis(0, JoyAxis.RightY)); break;
                    case Inputs.joy1_rstick_down: Set(input, Godot.Input.GetJoyAxis(0, JoyAxis.RightY)); break; ;
                    case Inputs.joy1_rstick_left: Set(input, -Godot.Input.GetJoyAxis(0, JoyAxis.RightX)); break; ;
                    case Inputs.joy1_rstick_right: Set(input, Godot.Input.GetJoyAxis(0, JoyAxis.RightX)); break;
                    case Inputs.joy1_right_trigger: Set(input, Godot.Input.GetJoyAxis(0, JoyAxis.TriggerRight)); break;
                    case Inputs.joy1_left_trigger: Set(input, Godot.Input.GetJoyAxis(0, JoyAxis.TriggerLeft)); break;

                    case Inputs.joy2_start: Set(input, 1, JoyButton.Start); break;
                    case Inputs.joy2_select: Set(input, 1, JoyButton.Back); break;
                    case Inputs.joy2_dpad_up: Set(input, 1, JoyButton.DpadUp); break;
                    case Inputs.joy2_dpad_down: Set(input, 1, JoyButton.DpadDown); break;
                    case Inputs.joy2_dpad_left: Set(input, 1, JoyButton.DpadLeft); break;
                    case Inputs.joy2_dpad_right: Set(input, 1, JoyButton.DpadRight); break;
                    case Inputs.joy2_left_shoulder: Set(input, 1, JoyButton.LeftShoulder); break;
                    case Inputs.joy2_left_hat: Set(input, 1, JoyButton.LeftStick); break;
                    case Inputs.joy2_right_shoulder: Set(input, 1, JoyButton.RightShoulder); break;
                    case Inputs.joy2_right_hat: Set(input, 1, JoyButton.RightStick); break;
                    case Inputs.joy2_button_cross: Set(input, 1, JoyButton.A); break;
                    case Inputs.joy2_button_circle: Set(input, 1, JoyButton.B); break;
                    case Inputs.joy2_button_triangle: Set(input, 1, JoyButton.Y); break;
                    case Inputs.joy2_button_square: Set(input, 1, JoyButton.X); break;
                    case Inputs.joy2_home: Set(input, 1, JoyButton.Guide); break;

                    case Inputs.joy2_lstick_up: Set(input, -Godot.Input.GetJoyAxis(1, JoyAxis.LeftY)); break;
                    case Inputs.joy2_lstick_down: Set(input, Godot.Input.GetJoyAxis(1, JoyAxis.LeftY)); break;
                    case Inputs.joy2_lstick_left: Set(input, -Godot.Input.GetJoyAxis(1, JoyAxis.LeftX)); break;
                    case Inputs.joy2_lstick_right: Set(input, Godot.Input.GetJoyAxis(1, JoyAxis.LeftX)); break; ;
                    case Inputs.joy2_rstick_up: Set(input, -Godot.Input.GetJoyAxis(1, JoyAxis.RightY)); break;
                    case Inputs.joy2_rstick_down: Set(input, Godot.Input.GetJoyAxis(1, JoyAxis.RightY)); break; ;
                    case Inputs.joy2_rstick_left: Set(input, -Godot.Input.GetJoyAxis(1, JoyAxis.RightX)); break; ;
                    case Inputs.joy2_rstick_right: Set(input, Godot.Input.GetJoyAxis(1, JoyAxis.RightX)); break;
                    case Inputs.joy2_right_trigger: Set(input, Godot.Input.GetJoyAxis(1, JoyAxis.TriggerRight)); break;
                    case Inputs.joy2_left_trigger: Set(input, Godot.Input.GetJoyAxis(1, JoyAxis.TriggerLeft)); break;

                    case Inputs.joy3_start: Set(input, 2, JoyButton.Start); break;
                    case Inputs.joy3_select: Set(input, 2, JoyButton.Back); break;
                    case Inputs.joy3_dpad_up: Set(input, 2, JoyButton.DpadUp); break;
                    case Inputs.joy3_dpad_down: Set(input, 2, JoyButton.DpadDown); break;
                    case Inputs.joy3_dpad_left: Set(input, 2, JoyButton.DpadLeft); break;
                    case Inputs.joy3_dpad_right: Set(input, 2, JoyButton.DpadRight); break;
                    case Inputs.joy3_left_shoulder: Set(input, 2, JoyButton.LeftShoulder); break;
                    case Inputs.joy3_left_hat: Set(input, 2, JoyButton.LeftStick); break;
                    case Inputs.joy3_right_shoulder: Set(input, 2, JoyButton.RightShoulder); break;
                    case Inputs.joy3_right_hat: Set(input, 2, JoyButton.RightStick); break;
                    case Inputs.joy3_button_cross: Set(input, 2, JoyButton.A); break;
                    case Inputs.joy3_button_circle: Set(input, 2, JoyButton.B); break;
                    case Inputs.joy3_button_triangle: Set(input, 2, JoyButton.Y); break;
                    case Inputs.joy3_button_square: Set(input, 2, JoyButton.X); break;
                    case Inputs.joy3_home: Set(input, 2, JoyButton.Guide); break;

                    case Inputs.joy3_lstick_up: Set(input, -Godot.Input.GetJoyAxis(2, JoyAxis.LeftY)); break;
                    case Inputs.joy3_lstick_down: Set(input, Godot.Input.GetJoyAxis(2, JoyAxis.LeftY)); break;
                    case Inputs.joy3_lstick_left: Set(input, -Godot.Input.GetJoyAxis(2, JoyAxis.LeftX)); break;
                    case Inputs.joy3_lstick_right: Set(input, Godot.Input.GetJoyAxis(2, JoyAxis.LeftX)); break; ;
                    case Inputs.joy3_rstick_up: Set(input, -Godot.Input.GetJoyAxis(2, JoyAxis.RightY)); break;
                    case Inputs.joy3_rstick_down: Set(input, Godot.Input.GetJoyAxis(2, JoyAxis.RightY)); break; ;
                    case Inputs.joy3_rstick_left: Set(input, -Godot.Input.GetJoyAxis(2, JoyAxis.RightX)); break; ;
                    case Inputs.joy3_rstick_right: Set(input, Godot.Input.GetJoyAxis(2, JoyAxis.RightX)); break;
                    case Inputs.joy3_right_trigger: Set(input, Godot.Input.GetJoyAxis(2, JoyAxis.TriggerRight)); break;
                    case Inputs.joy3_left_trigger: Set(input, Godot.Input.GetJoyAxis(2, JoyAxis.TriggerLeft)); break;

                    case Inputs.joy4_start: Set(input, 3, JoyButton.Start); break;
                    case Inputs.joy4_select: Set(input, 3, JoyButton.Back); break;
                    case Inputs.joy4_dpad_up: Set(input, 3, JoyButton.DpadUp); break;
                    case Inputs.joy4_dpad_down: Set(input, 3, JoyButton.DpadDown); break;
                    case Inputs.joy4_dpad_left: Set(input, 3, JoyButton.DpadLeft); break;
                    case Inputs.joy4_dpad_right: Set(input, 3, JoyButton.DpadRight); break;
                    case Inputs.joy4_left_shoulder: Set(input, 3, JoyButton.LeftShoulder); break;
                    case Inputs.joy4_left_hat: Set(input, 3, JoyButton.LeftStick); break;
                    case Inputs.joy4_right_shoulder: Set(input, 3, JoyButton.RightShoulder); break;
                    case Inputs.joy4_right_hat: Set(input, 3, JoyButton.RightStick); break;
                    case Inputs.joy4_button_cross: Set(input, 3, JoyButton.A); break;
                    case Inputs.joy4_button_circle: Set(input, 3, JoyButton.B); break;
                    case Inputs.joy4_button_triangle: Set(input, 3, JoyButton.Y); break;
                    case Inputs.joy4_button_square: Set(input, 3, JoyButton.X); break;
                    case Inputs.joy4_home: Set(input, 3, JoyButton.Guide); break;

                    case Inputs.joy4_lstick_up: Set(input, -Godot.Input.GetJoyAxis(3, JoyAxis.LeftY)); break;
                    case Inputs.joy4_lstick_down: Set(input, Godot.Input.GetJoyAxis(3, JoyAxis.LeftY)); break;
                    case Inputs.joy4_lstick_left: Set(input, -Godot.Input.GetJoyAxis(3, JoyAxis.LeftX)); break;
                    case Inputs.joy4_lstick_right: Set(input, Godot.Input.GetJoyAxis(3, JoyAxis.LeftX)); break; ;
                    case Inputs.joy4_rstick_up: Set(input, -Godot.Input.GetJoyAxis(3, JoyAxis.RightY)); break;
                    case Inputs.joy4_rstick_down: Set(input, Godot.Input.GetJoyAxis(3, JoyAxis.RightY)); break; ;
                    case Inputs.joy4_rstick_left: Set(input, -Godot.Input.GetJoyAxis(3, JoyAxis.RightX)); break; ;
                    case Inputs.joy4_rstick_right: Set(input, Godot.Input.GetJoyAxis(3, JoyAxis.RightX)); break;
                    case Inputs.joy4_right_trigger: Set(input, Godot.Input.GetJoyAxis(3, JoyAxis.TriggerRight)); break;
                    case Inputs.joy4_left_trigger: Set(input, Godot.Input.GetJoyAxis(3, JoyAxis.TriggerLeft)); break;
                }
            wheel_up = 0;
            wheel_down = 0;
        }

        void Set(Inputs input, float value) =>
            current[(int)input] = value < 0 ? 0 : value;

        void Set(Inputs input, Key value) =>
            current[(int)input] = Godot.Input.IsKeyPressed(value) ? 1 : 0;

        void Set(Inputs input, MouseButton value) =>
            current[(int)input] = Godot.Input.IsMouseButtonPressed(value) ? 1 : 0;

        void Set(Inputs input, int index, JoyButton value) =>
            current[(int)input] = Godot.Input.IsJoyButtonPressed(index, value) ? 1 : 0;
    }

}
