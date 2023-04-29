using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

public static partial class Debug
{
    public class Exception : System.Exception
    {
        public Exception(params object[] args)
        {
            _message = "".AsString(args);
        }

        string _message;
        public override string Message => _message;
    }

    class DebugNode
    {
        static Godot.Node node;
        public static Godot.Node instance
        {
            get
            {
                if (node == null)
                {
                    node = new Node() { Name = "Debug" };
                    Internal.Bootstrap.Node.AddChild(node);
                }
                return node;
            }
        }
    }

    public static void Log(params object[] args)
    {
        var text = "".AsString(args);
        Console.WriteLine(Colors.White, text);
        GD.Print(text);
    }

    public static void ColorLog(Color color, params object[] args)
    {
        var text = "".AsString(args);
        Console.WriteLine(color, text);
        GD.Print(text);
    }

    public static void LogError(params object[] args)
    {
        var text = "".AsString(args);
        Console.WriteLine(Colors.Red, text);
        GD.PushError(text);
    }

    public static void LogWarning(params object[] args)
    {
        var text = "".AsString(args);
        Console.WriteLine(Colors.Orange, text);
        GD.PushWarning(text);
    }

    public static void Label(params object[] args)
        => ColorLabel(Colors.White, args);
    public static void ColorLabel(Godot.Color color, params object[] args)
        => DebugLabel_Inpl.Add("".AsString(args), color);

    public static void Label2D(Vector2 position, params object[] args)
        => ColorLabel2D(Colors.White, position, args);

    public static void ColorLabel2D(Color color, Vector2 position, params object[] args)
        => DebugLabel2D_Impl.Add(position, "".AsString(args), color);

    public static void Label3D(Vector3 position, params object[] args)
        => DebugLabel3D_Impl.Add(position, "".AsString(args), Colors.White);

    public static void ColorLabel3D(Color color, Vector3 position, params object[] args)
        => DebugLabel3D_Impl.Add(position, "".AsString(args), color);

    static System.Diagnostics.Stopwatch timer => Internal.Bootstrap.timer;

    public static TimeSpan Time(string name, System.Action action, bool output_to_console = true)
        => Time(name, 1, action, output_to_console);

    public static TimeSpan Time(string name, int interations, System.Action action, bool output_to_console = true)
    {
        interations = interations.MinValue(1);
        var time = timer.Elapsed;
        for (int i = 0; i < interations; ++i)
            action();
        time = timer.Elapsed - time;
        if (output_to_console)
            if (interations == 1)
                Debug.Log(name, "took", time.TotalMilliseconds.ToString("0.000"), "ms");
            else
                Debug.Log(name, $"x{interations}", "took", time.TotalMilliseconds.ToString("0.000"), "ms");
        return time;
    }

    partial class DebugLabel2D_Impl : Godot.Node
    {
        static DebugLabel2D_Impl instance;

        public static void Add(Vector2 position, string text, Color color)
        {
            if (!Node.IsInstanceValid(instance))
            {
                var layer = new CanvasLayer();
                layer.FollowViewportEnabled = true;
                DebugNode.instance.AddChild(layer);

                instance = new() { Name = "Debug Label2D" };
                instance.SetParent(layer);
            }
            instance.values.Add((position, text, color));
        }

        List<(Vector2 position, string text, Color color)> values = new();
        List<Godot.Label> labels = new List<Label>();

        public override void _Process(double delta)
        {
            int i = 0;
            for (; i < values.Count; ++i)
            {
                Label label;
                if (labels.Count == i)
                {
                    label = new();
                    this.AddChild(label);
                    labels.Add(label);
                }
                else label = labels[i];
                label.Text = values[i].text;
                label.Modulate = values[i].color;
                label.Position = values[i].position;
                label.Visible = true;
            }
            for (; i < labels.Count; ++i)
                labels[i].Visible = false;

            values.Clear();
        }
    }

    partial class DebugLabel3D_Impl : Godot.Node
    {
        static DebugLabel3D_Impl instance;

        public static void Add(Vector3 position, string text, Color color)
        {
            if (!Node.IsInstanceValid(instance))
            {
                instance = new() { Name = "Debug Label3D" };
                DebugNode.instance.AddChild(instance);
            }
            instance.values.Add((position, text, color));
        }

        List<(Vector3 position, string text, Color color)> values = new();
        List<Godot.Label> labels = new List<Label>();

        public override void _Process(double delta)
        {
            var camera = Scene.Current.GetViewport().GetCamera3D();
            if (camera.IsValid())
            {
                int i = 0;
                for (; i < values.Count; ++i)
                {
                    Label label;
                    if (labels.Count == i)
                    {
                        label = new();
                        //label.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
                        this.AddChild(label);
                        labels.Add(label);
                    }
                    else label = labels[i];
                    label.Text = values[i].text;
                    label.Modulate = values[i].color;
                    label.Position = camera.UnprojectPosition(values[i].position);
                    label.Visible = camera.IsPositionInFrustum(values[i].position);
                }
                for (; i < labels.Count; ++i)
                    labels[i].Visible = false;
            }
            values.Clear();
        }
    }

    partial class DebugLabel_Inpl : Godot.Node
    {
        public static void Add(string message, Godot.Color color)
        {
            if (Godot.Engine.IsInPhysicsFrame())
                Get()._physics_messages.Add((color, message));
            else Get()._frame_messages.Add((color, message));
        }
        static DebugLabel_Inpl _instance;
        static DebugLabel_Inpl Get()
        {
            if (!Godot.Node.IsInstanceValid(_instance))
                DebugNode.instance.AddChildDeffered(_instance = new DebugLabel_Inpl { Name = "Debug Labels" });
            return _instance;
        }

        List<(Godot.Color color, string message)> _frame_messages = new List<(Color, string message)>();
        List<(Godot.Color color, string message)> _physics_messages = new List<(Color, string message)>();

        public DebugLabel_Inpl()
        {
            Name = "Debug Labels";
            var node = GD.Load<PackedScene>("res://_Core/Debug/DebugLabels.tscn").Instantiate();
            if (node.TryFind<Control>(out var control))
                control.MouseFilter = Control.MouseFilterEnum.Ignore;

            _frame_container = node.FindChild("Frame", true) as VBoxContainer;
            _physics_container = node.FindChild("Physics", true) as VBoxContainer;
            this.AddChild(node);
        }

        VBoxContainer _frame_container, _physics_container;
        List<Label> _frame_labels = new List<Label>();
        List<Label> _physics_labels = new List<Label>();

        public override void _Process(double delta)
        {
            DrawMessage(_frame_container, _frame_labels, _frame_messages);
        }

        void DrawMessage(VBoxContainer container, List<Label> labels, List<(Godot.Color color, string message)> messages)
        {
            int i = 0;
            for (; i < messages.Count; ++i)
            {
                if (labels.Count == i)
                {
                    var label = container.GetChild(0) as Label;
                    label = label.Duplicate() as Label;
                    label.MouseFilter = Control.MouseFilterEnum.Ignore;
                    labels.Add(label);
                    container.AddChild(label);
                }
                var frame = labels[i];
                frame.Visible = true;
                frame.Modulate = messages[i].color;
                frame.Text = messages[i].message;
            }
            for (; i < labels.Count; ++i)
                labels[i].Visible = false;
            messages.Clear();
        }

        public override void _PhysicsProcess(double delta)
        {
            DrawMessage(_physics_container, _physics_labels, _physics_messages);
        }
    }
    public static void Assert(bool action, params object[] message) => Assert(action, "".AsString(message));
    public static void Assert(Func<bool> action, params object[] message) => Assert(action(), message);
    public static void Assert(bool value, string message = "Failed Assertion")
    {
        if (!value) throw new System.Exception(message);
    }

    /// <summary>
    /// 
    /// </summary>
    public struct Test
    {
        /// <summary>
        /// Runs all static private methods marked with Debug.Test
        /// </summary>
        static void RunTests(Debug.Console args)
        {
            Debug.Log();
            Debug.Log("Starting Tests...");
            int pass = 0; int fail = 0;

            foreach (var type in typeof(Debug).Assembly.GetTypes())
            {
                if (type.IsGenericType) continue;
                foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic))
                {
                    var parameters = method.GetParameters();
                    if (parameters.Length != 1 || parameters[0].ParameterType != typeof(Test))
                        continue;

                    var name = type.FullName + $".{method.Name}";

                    try
                    {
                        method.Invoke(null, new object[] { default });
                        Debug.ColorLog(Colors.LightGreen, "Pass:", name);
                        pass++;
                    }
                    catch (System.Exception e)
                    {
                        var message = e.InnerException?.Message;
                        Debug.ColorLog(Colors.OrangeRed, "FAIL:", name, "->", String.IsNullOrEmpty(message) ? e.Message : message);
                        fail++;
                    }
                }
            }

            Debug.Log($"{pass}/{pass + fail} passed");
            if (fail == 0) Debug.ColorLog(Colors.Green, $"RESULT : PASS");
            else Debug.ColorLog(Colors.Red, $"RESULT: FAIL");
        }
    }
}

public static partial class Debug // console
{

    /// <summary>
    /// Allows calling static methods in non generic classes using the Debug.Console
    /// </summary>
    public class Console : IEnumerable<string>
    {
        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            foreach (var item in _parameters)
                yield return item;
        }

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<string>)this).GetEnumerator();


        /// <summary>
        /// Adds help message to Console.Debug methods in the debug console
        /// </summary>
        [System.AttributeUsage(AttributeTargets.Method)]
        public class Help : System.Attribute
        {
            public string _message;
            public Help(string message)
            {
                this._message = message;
            }
        }

        static Dictionary<string, string> _help_messages = new Dictionary<string, string>();
        static Dictionary<string, Action<Console>> _callbacks = new Dictionary<string, Action<Console>>();

        static Console()
        {
            var target_assembly = typeof(Debug).Assembly;
            var types = target_assembly.GetTypes();
            int count = types.Length;
            for (int i = 0; i < count; ++i)
            {
                var type = types[i];
                if (type.IsGenericType) continue;

                foreach (var method in type.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
                {
                    var para = method.GetParameters();
                    if (para.Length != 1) continue;
                    if (para[0].ParameterType != typeof(Debug.Console)) continue;
                    if (method.ReturnType != typeof(void)) continue;

                    string name = method.Name.ToLower();
                    _callbacks.TryGetValue(name, out var action);
                    action += Delegate.CreateDelegate(typeof(Action<Debug.Console>), null, method) as Action<Debug.Console>;
                    _callbacks[name] = action;

                    var help_message = method.GetCustomAttribute<Help>()?._message;

                    if (help_message != null)
                    {
                        if (!_help_messages.TryGetValue(name, out var stored))
                            stored = "";
                        stored += help_message;

                        _help_messages[name] = stored;
                    }
                }
            }
        }


        public static void Toggle() => Console_Inpl.Get().Toggle();

        /// <summary>
        /// Sends a message to the console as if you entered it into the console
        /// </summary>
        public static void Send(string input) => new Console(input);
        public static void WriteLine(Color color, string message) => Console_Inpl.Get().LogToConsole(color, message);
        public static void Clear() => Console_Inpl.Get().Clear();

        public Console(string input)
        {
            RawInput = input == null ? "" : input;
            var args = input.Split(" ");

            for (int arg = args.Length - 1; arg >= 0; --arg)
            {
                var search = "";
                for (int i = 0; i <= arg; ++i)
                    search += args[i].ToLower();

                if (_callbacks.TryGetValue(search, out var action))
                {
                    List<string> values = new List<string>();
                    for (int i = arg + 1; i < args.Length; ++i)
                        if (!String.IsNullOrEmpty(args[i]))
                            values.Add(args[i]);

                    _parameters = values.ToArray();
                    action?.Invoke(this);
                    return;
                }
            }
        }

        public readonly string RawInput;
        public string[] Parameters => new List<string>(_parameters).ToArray();
        string[] _parameters;

        public int Count => _parameters.Length;

        public string this[int index]
            => index < 0 ? "" : index >= _parameters.Length ? "" : _parameters[index];

        public int ToInt(int arg) => int.TryParse(this[arg], out int value) ? value : 0;
        public float ToFloat(int arg) => float.TryParse(this[arg], out float value) ? value : 0;
        public bool ToBool(int arg) => bool.TryParse(this[arg], out bool value) ? value : false;

        public string ToString(int arg) => this[arg];

        public override string ToString() => this[0];

        [Help("Lists all Console Debug Commands")]
        static void Commands(Debug.Console console)
        {
            Debug.Log();
            Console.WriteLine(Colors.Yellow, "Commands:");
            foreach (var key in _callbacks.Keys)
                if (_help_messages.TryGetValue(key, out var value))
                    Debug.Log(key, ":", value);
                else Debug.Log(key, ":");
            Debug.Log();
        }

        [Help("Lists all Console Debug Commands")]
        static void help(Debug.Console console) => Commands(console);


        static void OnUpdate(Bootstrap.Process args)
        {
            if (Inputs.key_back_quote.OnPressed()) Debug.Console.Toggle();
        }
    }

    partial class Console_Inpl : Godot.Node
    {
        static Console_Inpl instance;

        List<string> _console_inputs = new List<string>();
        int _console_input_position = 0;

        VBoxContainer _label_container;
        ScrollContainer _scroll_container;
        public List<Label> _labels = new List<Label>();
        int _visible_labels = 0;

        public static Console_Inpl Get()
        {
            if (!instance.IsValid())
            {
                instance = new Console_Inpl { Name = "Console" };
                DebugNode.instance.AddChild(instance);
            }
            return instance;
        }

        public void Toggle()
        {
            var control = Get().GetChild(0) as Godot.Control;
            control.Visible = !control.Visible;
            _console_input_position = 0;
        }

        public void Clear()
        {
            _visible_labels = 0;
            foreach (var label in _labels)
                label.Visible = false;
        }

        public void LogToConsole(Godot.Color color, string message)
        {
            Label label;
            if (_visible_labels == _labels.Count)
            {
                label = _label_container.GetChild(0).Duplicate() as Label;
                _labels.Add(label);
                _label_container.AddChild(label);
            }

            label = _labels[_visible_labels];
            label.Visible = true;
            label.Modulate = color;
            label.Text = string.IsNullOrEmpty(message) ? "" : message;
            _visible_labels++;
            _scroll_to_end = 4;
        }

        int _scroll_to_end;
        string path = OS.GetUserDataDir() + "/DebugConsoleLogs.txt";

        Console_Inpl()
        {
            Name = "Debug Console";

            var node = GD.Load<PackedScene>("res://_Core/Debug/DebugConsole.tscn").Instantiate() as Control;
            node.Visible = false;

            if (System.IO.File.Exists(path))
                foreach (var line in System.IO.File.ReadAllLines(path))
                    _console_inputs.Add(line);

            _console_input_position = _console_inputs.Count;

            _label_container = node.FindChild("Labels", true) as Godot.VBoxContainer;

            if (!node.TryFind(out _scroll_container))
                throw new Exception("failed ot find scroll container");

            _label_container.GetChild<Label>(0).Visible = false;
            AddChild(node);

            line_edit = node.FindChild("LineEdit") as LineEdit;

            node.VisibilityChanged += () =>
            {
                line_edit.Text = "";
                if (node.Visible)
                {
                    line_edit.GrabFocus();
                }

                _scroll_to_end = 2;
            };
        }
        LineEdit line_edit;

        bool keydown;
        public override void _Process(double time)
        {
            if (!_scroll_container.IsVisibleInTree()) return;

            if (_scroll_to_end > 0)
            {
                _scroll_to_end--;
                if (!_scroll_container.GetGlobalRect().HasPoint(this.GetViewport().GetMousePosition()))
                    _scroll_container.ScrollVertical = int.MaxValue;
            }

            var position = _console_input_position;

            if (Inputs.key_enter.OnPressed() && !string.IsNullOrEmpty(line_edit.Text))
            {
                _console_inputs.Add(line_edit.Text);
                if (_console_inputs.Count > 20)
                    _console_inputs.RemoveAt(0);

                _console_input_position = _console_inputs.Count;

                new Console(line_edit.Text);
                line_edit.Text = "";

                System.IO.File.WriteAllLines(path, _console_inputs);

                return;
            }

            if (Inputs.key_up_arrow.OnPressed())
                position--;

            if (Inputs.key_down_arrow.OnPressed())
                position++;

            if (position == _console_input_position) return;

            if (position >= _console_inputs.Count) position = 0;
            if (position < 0) position = (_console_inputs.Count - 1).MinValue(0);

            if (position != _console_input_position && _console_inputs.Count > 0)
            {
                _console_input_position = position;
                line_edit.Text = _console_inputs[position];
            }
        }

        [Console.Help("Filters the console messages to those that contains supplied arguements")]
        static void Filter(Debug.Console console)
        {
            List<(Color color, string message)> items = new List<(Color Color, string value)>();

            foreach (var item in Console_Inpl.Get()._labels)
            {
                if (item.Visible)
                {
                    if (item.Text.ToLower().Contains(console[0].ToLower()))
                        items.Add((item.Modulate, item.Text));
                }
            }

            Console.Clear();
            foreach (var item in items)
                Console.WriteLine(item.color, item.message);
        }
    }

    class Console_Commands
    {
        [Console.Help("Quits the application")]
        static void Exit(Debug.Console console) => Quit(console);

        [Console.Help("Quits the application")]
        static void Quit(Debug.Console console) => Internal.Bootstrap.Node.Quit();

        [Console.Help("Closes the Debug console")]
        static void Close(Debug.Console console) => Console.Toggle();

        [Console.Help("Clears the debug console")]
        static void Clear(Debug.Console console) => Console.Clear();

        [Console.Help("Logs a message to the console")]
        static void Log(Debug.Console args)
        {
            string value = "";
            foreach (var param in args.Parameters)
                value += $"{param} ";
            Console.WriteLine(Colors.White, value);
        }

        [Debug.Console.Help("Counts all lines currently in Debug Console")]
        static void CountLines(Debug.Console args)
        {
            Debug.Log("Lines:", Console_Inpl.Get()._labels.Count);
        }


        [Debug.Console.Help("Counts all duplicate messages in Debug Console, args ='clear' to clear before displaying counts")]
        static void Aggregate(Debug.Console args)
        {
            var labels = Console_Inpl.Get()._labels;

            System.Collections.Generic.Dictionary<string, int> counts = new Dictionary<string, int>();
            foreach (var item in labels)
            {
                if (!item.Visible) continue;
                var text = item.Text;
                counts.TryGetValue(text, out var value);
                counts[text] = value + 1;
            }

            if (args.ToString() == "clear")
                Debug.Console.Clear();

            Debug.Log("Aggregate Count -->", counts.Count);
            var list = new List<System.Collections.Generic.KeyValuePair<string, int>>(counts);
            list.Sort((x, y) => x.Value.CompareTo(y.Value));
            foreach (var item in list)
                Debug.Log(item.Value, "x", item.Key);
        }

        [Console.Help("Changes mouse mode, Options = {visible, confined, hidden, captured }")]
        static void MouseMode(Debug.Console args)
        {
            switch (args.ToString().ToLower())
            {
                case "show":
                case "visible":
                    Input.MouseMode = Input.MouseModeEnum.Visible;
                    break;
                case "confined":
                    Input.MouseMode = Input.MouseModeEnum.Confined;
                    break;
                case "hidden":
                    Input.MouseMode = Input.MouseModeEnum.Hidden;
                    break;
                case "captured":
                    Input.MouseMode = Input.MouseModeEnum.Captured;
                    break;
                default:
                    Debug.Log(args.ToString().ToLower());
                    break;
            }
        }

        [Debug.Console.Help("Toggles fps counter")]
        static void FPS(Debug.Console args)
        {
            string name = "FPS Counter";

            if (DebugNode.instance.TryFind(out Godot.Node node, node => node.Name.ToString().Contains(name)))
                node.DestroyNode();
            else
                new Godot.Node { Name = name }
                    .OnUpdate(() =>
                    {
                        var fps = Godot.Engine.GetFramesPerSecond();
                        var color = Colors.LightBlue;
                        if (fps < 90f) color = Colors.LightGreen;
                        if (fps < 60f) color = Colors.Yellow;
                        if (fps < 45f) color = Colors.Orange;
                        if (fps < 30f) color = Colors.OrangeRed;
                        if (fps < 15f) color = Colors.Red;
                        Debug.ColorLabel(color, fps.ToString("0.00"), "fps");
                    })
                    .SetParent(DebugNode.instance);
        }

        [Debug.Console.Help("Shows the total runtime")]
        static void Runtime(Debug.Console args)
        {
            string name = "Runtime";
            if (DebugNode.instance.TryFind(out Godot.Node node, node => node.Name.ToString().Contains(name)))
                node.DestroyNode();
            else
                new Godot.Node { Name = name }
                    .OnUpdate(() =>
                    {
                        float seconds = (float)timer.Elapsed.TotalSeconds;
                        int minutes = (int)timer.Elapsed.TotalMinutes;
                        Debug.Label($"Runtime: {minutes: 00}:{(int)seconds % 60: 00}.{(seconds - (int)seconds) * 1000: 000}");
                    })
                    .SetParent(DebugNode.instance);
        }
    }
}

public static partial class Debug // Draw3d
{
    public static void DrawLine3D(Vector3 global_origin, Vector3 global_end, Color color)
    {
        DrawLine3DImpl.Get().Lines.Add((global_origin, global_end, color));
    }

    public static void DrawLine3D(Transform3D transform, Vector3 start, Vector3 end, Color color)
        => DrawLine3D(transform.TranslatedLocal(start).Origin, transform.TranslatedLocal(end).Origin, color);

    public static void DrawSquare3D(Vector3 global_origin, float size, Color color)
        => DrawRectangle3D(global_origin, new Vector2(size / 2, size / 2), color);

    public static void DrawArrow(Vector3 global_origin, Vector3 direction, Color color)
    {
        Debug.DrawLine3D(global_origin, global_origin + direction, color);

        var Viewport = DebugNode.instance.GetViewport();
        var right = direction.Cross(Viewport.GetCamera3D().GlobalTransform.GetForward()).Normalized();
        var head = global_origin + direction * .9f;
        var length = (direction * .1f).Length();
        Debug.DrawLine3D(head + right * length, global_origin + direction, color);
        Debug.DrawLine3D(head - right * length, global_origin + direction, color);
    }

    public static void DrawRectangle3D(Vector3 global_origin, Vector2 extents, Color color)
    {
        var p1 = global_origin + new Vector3(extents.X, 0, extents.Y);
        var p2 = global_origin + new Vector3(-extents.X, 0, extents.Y);
        var p3 = global_origin + new Vector3(extents.X, 0, -extents.Y);
        var p4 = global_origin + new Vector3(-extents.X, 0, -extents.Y);

        DrawLine3D(p1, p2, color);
        DrawLine3D(p1, p3, color);
        DrawLine3D(p2, p4, color);
        DrawLine3D(p3, p4, color);
    }

    public static void DrawCircle3D(Vector3 global_origin, float radius, Color color)
        => DrawCircle3D(global_origin, Vector3.Up, radius, color);

    public static void DrawCircle3D(Vector3 global_origin, Vector3 normal, float radius, Color color, int segments = 16)
    {
        if (segments < 3) segments = 3;
        var arc_angle = Mathf.Pi / ((float)segments / 2f);

        if (normal == Vector3.Zero)
            return;
        if (normal == Vector3.Up)
            normal.X += 0.00001f;

        normal = normal.Normalized();
        Transform3D t = Transform3D.Identity;
        t = t.LookingAt(normal, Vector3.Up);
        t.Origin = global_origin;

        var angle = 0f;
        var start = t.TranslatedLocal(new Vector3(radius, 0, 0)).Origin;
        for (int i = 0; i < segments; ++i)
        {
            angle += arc_angle;
            var end = t.TranslatedLocal(new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0)).Origin;
            DrawLine3D(start, end, color);
            start = end;
        }
    }

    public static void DrawCube(Transform3D transform, Vector3 extents, Color color)
    {
        var x = extents.X;
        var y = extents.Y;
        var z = extents.Z;

        DrawLine3D(transform, new Vector3(-x, -y, -z), new Vector3(-x, -y, z), color);
        DrawLine3D(transform, new Vector3(-x, -y, -z), new Vector3(x, -y, -z), color);
        DrawLine3D(transform, new Vector3(-x, -y, z), new Vector3(x, -y, z), color);
        DrawLine3D(transform, new Vector3(x, -y, -z), new Vector3(x, -y, z), color);

        DrawLine3D(transform, new Vector3(-x, y, -z), new Vector3(-x, y, z), color);
        DrawLine3D(transform, new Vector3(-x, y, -z), new Vector3(x, y, -z), color);
        DrawLine3D(transform, new Vector3(-x, y, z), new Vector3(x, y, z), color);
        DrawLine3D(transform, new Vector3(x, y, -z), new Vector3(x, y, z), color);

        DrawLine3D(transform, new Vector3(-x, -y, -z), new Vector3(-x, y, -z), color);
        DrawLine3D(transform, new Vector3(-x, -y, z), new Vector3(-x, y, z), color);
        DrawLine3D(transform, new Vector3(x, -y, -z), new Vector3(x, y, -z), color);
        DrawLine3D(transform, new Vector3(x, -y, z), new Vector3(x, y, z), color);
    }

    public static void DrawCube(Transform3D transform, float size, Color color)
        => DrawCube(transform, Vector3.One * size / 2f, color);

    public static void DrawCapsule(Transform3D transform, float height, float radius, Color color, bool draw_rings = false)
    {


        Vector3 Xform(Vector3 position) => transform.TranslatedLocal(position).Origin;
        var arc_angle = Mathf.Pi / 8f;

        height /= 2f;
        var angle = 0f;
        var start = Xform(new Vector3(radius, 0, height));
        Debug.DrawLine3D(start, Xform(new Vector3(radius, 0, -height)), color);
        for (int i = 0; i < 8; ++i)
        {
            angle += arc_angle;
            var end = Xform(new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius + height));
            Debug.DrawLine3D(start, end, color);
            start = end;
        }
        start = Xform(new Vector3(-radius, 0, -height));
        Debug.DrawLine3D(start, Xform(new Vector3(-radius, 0, height)), color);
        for (int i = 0; i < 8; ++i)
        {
            angle += arc_angle;
            var end = Xform(new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius - height));
            Debug.DrawLine3D(start, end, color);
            start = end;
        }

        angle = 0f;
        start = Xform(new Vector3(0, radius, height));
        Debug.DrawLine3D(start, Xform(new Vector3(0, radius, -height)), color);
        for (int i = 0; i < 8; ++i)
        {
            angle += arc_angle;
            var end = Xform(new Vector3(0, Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius + height));
            Debug.DrawLine3D(start, end, color);
            start = end;
        }
        start = Xform(new Vector3(0, -radius, -height));
        Debug.DrawLine3D(start, Xform(new Vector3(0, -radius, height)), color);
        for (int i = 0; i < 8; ++i)
        {
            angle += arc_angle;
            var end = Xform(new Vector3(0, Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius - height));
            Debug.DrawLine3D(start, end, color);
            start = end;
        }

        if (draw_rings)
        {
            angle = 0;
            start = Xform(new Vector3(radius, 0, -height));
            for (int i = 0; i < 16; ++i)
            {
                angle += arc_angle;
                var end = Xform(new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, -height));
                Debug.DrawLine3D(start, end, color);
                start = end;
            }
            angle = 0;
            start = Xform(new Vector3(radius, 0, height));
            for (int i = 0; i < 16; ++i)
            {
                angle += arc_angle;
                var end = Xform(new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, height));
                Debug.DrawLine3D(start, end, color);
                start = end;
            }
        }
    }

    partial class DrawLine3DImpl : Godot.MeshInstance3D
    {
        static DrawLine3DImpl instance;

        public static DrawLine3DImpl Get()
        {
            if (!instance.IsValid())
                instance = new DrawLine3DImpl { Name = "Debug Line3D" }.SetParentDeffered(DebugNode.instance);
            return instance;
        }

        public List<(Vector3 start, Vector3 end, Color color)> Lines = new List<(Vector3 start, Vector3 end, Color color)>();
        ImmediateMesh mesh;
        Material material;

        public DrawLine3DImpl()
        {
            Mesh = mesh = new ImmediateMesh();
            this.MaterialOverride = GD.Load<Material>("res://_Core/Debug/DebugDrawLine3D.material");
        }

        public override void _Process(double delta)
        {
            mesh.ClearSurfaces();
            if (Lines.Count == 0) return;
            mesh.SurfaceBegin(Mesh.PrimitiveType.Lines, material);
            foreach (var line in Lines)
            {
                mesh.SurfaceSetColor(line.color);
                mesh.SurfaceAddVertex(line.start);
                mesh.SurfaceSetColor(line.color);
                mesh.SurfaceAddVertex(line.end);
            }
            mesh.SurfaceEnd();
            Lines.Clear();
        }
    }

    public static void DrawLine2D(Vector2 start, Vector2 end, Color color)
    {
        DrawLine2DImpl.Get().Lines.Add((start, end, color));
    }

    public static void DrawSquare2D(Vector2 position, float size, Color color)
        => DrawRectangle2D(position, new Vector2(size, size), color);

    public static void DrawRectangle2D(Vector2 position, Vector2 size, Color color)
    {
        size /= 2f;
        var top_left = new Vector2(position.X - size.X, position.Y - size.Y);
        var top_right = new Vector2(position.X + size.X, position.Y - size.Y);
        var bot_left = new Vector2(position.X - size.X, position.Y + size.Y);
        var bot_right = new Vector2(position.X + size.X, position.Y + size.Y);
        Debug.DrawLine2D(top_left, top_right, color);
        Debug.DrawLine2D(bot_left, bot_right, color);
        Debug.DrawLine2D(top_left, bot_left, color);
        Debug.DrawLine2D(top_right, bot_right, color);
    }

    public static void DrawCircle2D(Vector2 position, float radius, Color color, int segments = 16)
    {
        if (segments < 3) segments = 3;

        var arc_angle = Mathf.Pi / ((float)segments / 2f);

        Transform2D t = Transform2D.Identity;
        t.Origin = position;
        var angle = 0f;
        var start = t.TranslatedLocal(new Vector2(radius, 0)).Origin;
        for (int i = 0; i < segments; ++i)
        {
            angle += arc_angle;
            var end = t.TranslatedLocal(new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius)).Origin;
            DrawLine2D(start, end, color);
            start = end;
        }
    }

    partial class DrawLine2DImpl : Godot.MeshInstance2D
    {
        static DrawLine2DImpl instance;

        public static DrawLine2DImpl Get()
        {
            if (!instance.IsValid())
            {
                var layer = new CanvasLayer().SetParentDeffered(DebugNode.instance);
                layer.FollowViewportEnabled = true;
                instance = new DrawLine2DImpl { Name = "Debug Line 2D" }.SetParent(layer);
            }
            return instance;
        }

        public List<(Vector2 start, Vector2 end, Color color)> Lines = new List<(Vector2 start, Vector2 end, Color color)>();
        ImmediateMesh mesh;
        Material material;

        public DrawLine2DImpl()
        {
            Mesh = mesh = new ImmediateMesh();
            this.Material = GD.Load<Material>("res://_Core/Debug/DebugDrawLine3D.material");
        }

        public override void _Process(double delta)
        {

            mesh.ClearSurfaces();
            if (Lines.Count == 0) return;
            mesh.SurfaceBegin(Mesh.PrimitiveType.Lines, material);
            min = max = Lines[0].start;

            foreach (var line in Lines)
            {
                var start = line.start;
                var end = line.end;

                min.X = min.X.MinValue(start.X).MinValue(end.X);
                max.X = max.X.MaxValue(start.X).MaxValue(end.X);

                mesh.SurfaceSetColor(line.color);
                mesh.SurfaceAddVertex(new Vector3(start.X, start.Y, 0));
                mesh.SurfaceSetColor(line.color);
                mesh.SurfaceAddVertex(new Vector3(end.X, end.Y, 0));
            }
            mesh.SurfaceEnd();
            Lines.Clear();

            //var aabb = new Aabb(new Vector3(min.X, 0, min.Y), new Vector3(max.X, 0, max.Y));


            //QueueRedraw();
        }

        Vector2 min, max;
        public override void _Draw()
        {
            float extents = 1000000;
            var aabb = new Aabb(new Vector3(-extents, -extents, -extents), new Vector3(extents * 2f, extents * 2f, extents * 2f));
            RenderingServer.MeshSetCustomAabb(mesh.GetRid(), aabb);
            
            Debug.Label(mesh.GetAabb());
            //RenderingServer.CanvasItemSetCustomRect(this.mesh.GetRid(), true, new Rect2(min , max - min));
        }
    }
}

public static partial class Debug
{
    partial class TestCamera3D : Godot.Camera3D
    {
        static TestCamera3D camera;


        [Console.Help("Spawns a 3D Camera")]
        static void TestCamera(Console consle)
        {
            if (camera.IsValid()) camera.QueueFree();
            else camera = new TestCamera3D { Name = "Test Camera", Current = true }.AddToScene();
        }

        float pitch, yaw;
        Vector3 current_velocity;

        public override void _Ready()
        {
            Position = new Vector3(0, 5, -10);
            LookAt(Vector3.Zero, Vector3.Up);
            pitch = Rotation.X;
            yaw = Rotation.Y;
        }

        bool locked;

        public override void _Process(double delta)
        {

            Vector3 target_velocity = default;

            if (Inputs.mouse_middle_click.OnPressed())
                locked = !locked;

            if (locked) goto Complete;

            if (Inputs.key_w.Pressed())
                target_velocity += -Transform.GetForward();

            if (Inputs.key_s.Pressed())
                target_velocity += -Transform.GetBack();

            if (Inputs.key_a.Pressed())
                target_velocity += Transform.GetLeft();

            if (Inputs.key_d.Pressed())
                target_velocity += Transform.GetRight();

            if (Inputs.key_e.Pressed())
                target_velocity += Transform.GetUp();

            if (Inputs.key_q.Pressed())
                target_velocity += Transform.GetDown();

            target_velocity *= (float)delta * 10f;

            if (Inputs.key_space.Pressed())
                target_velocity *= 4f;

            if (Inputs.key_shift.Pressed())
                target_velocity *= .25f;

            float offset = .0001f;

            Print(Inputs.mouse_move_left);
            Print(Inputs.mouse_move_right);
            Print(Inputs.mouse_move_up);
            Print(Inputs.mouse_move_down);


            void Print(Inputs input) => Debug.Label(input, input.CurrentValue());

            pitch += Inputs.mouse_move_up.CurrentValue() * offset;
            pitch -= Inputs.mouse_move_down.CurrentValue() * offset;

            yaw += Inputs.mouse_move_left.CurrentValue() * offset;
            yaw -= Inputs.mouse_move_right.CurrentValue() * offset;


        Complete:

            Debug.Label("WASDQE: move around");
            Debug.Label("Mouse: look around");
            Debug.Label("Space Shift: speed up, slow down");
            Debug.Label("Middle Click: toggle lock camera");

            current_velocity = current_velocity.Lerp(target_velocity, (float)delta * 10f);
            Position += current_velocity;
            Rotation = new Vector3(pitch, yaw, 0);
        }
    }
}