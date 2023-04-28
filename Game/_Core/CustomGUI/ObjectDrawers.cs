using System;
using Godot;
using System.Collections.Generic;
using System.Reflection;
using Internal.IMGUI;

class ObjectDrawer
{
    public static implicit operator Godot.Node(ObjectDrawer drawer) => drawer.container;
    public Godot.Node node => container;
    protected Type current_type;
    protected Godot.VBoxContainer container = new VBoxContainer { Name = "Object Drawer", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
    public bool Visible { get => container.Visible; set => container.Visible = value; }
    PropertyDrawer drawer;
    public string Label;
    public int Spaced { get; init; }
    public bool TryUpdate(object input) => TryUpdate(input?.GetType(), input);
    public bool TryUpdate(Type input_type, object input) => TryUpdate(input_type, input, out var obj);
    //public bool ShowPrivateFields;
    public bool Expand_Struct_Drawer;
    /// <summary>
    /// returns true if object changes
    /// </summary>
    public bool TryUpdate(Type input_type, object input, out object output)
    {
        output = default;

        if (input_type == null) input_type = typeof(void);
        if (input == null && input_type.IsValueType) input_type = typeof(void);
        if (input_type == current_type && drawer != null)
        {
            Label = Label == null ? string.Empty : Label;
            return drawer.Updated(current_type, Label, input, out output);
        }

        current_type = input_type;
        drawer?.node.DestroyNode();

        foreach (var drawer in all_drawers)
            if (drawer.CanDraw(input_type))
                this.drawer = System.Activator.CreateInstance(drawer.GetType()) as PropertyDrawer;

        container.AddChild(drawer.node);
        if (Spaced > 0)
        {
            var spacer = new Godot.Control { CustomMinimumSize = new Vector2(0, Spaced) };
            container.AddChild(spacer);
            drawer.node.OnDestroy(() => spacer.DestroyNode());
        }
        if (drawer is Struct_Drawer sd)
        {
            sd.expand = Expand_Struct_Drawer;
        }
        return false;
    }

    static List<PropertyDrawer> all_drawers = GetDrawers();
    static List<PropertyDrawer> GetDrawers()
    {
        var drawers = new List<PropertyDrawer>();

        foreach (var type in typeof(PropertyDrawer).Assembly.GetTypes())
        {
            if (type.IsGenericType) continue;
            if (!typeof(PropertyDrawer).IsAssignableFrom(type)) continue;
            if (type.GetConstructor(System.Type.EmptyTypes) == null) continue;
            drawers.Add(System.Activator.CreateInstance(type) as PropertyDrawer);
        }

        drawers.Sort((x, y) => x.draw_priority.CompareTo(y.draw_priority));
        return drawers;
    }

    public abstract class PropertyDrawer
    {
        /// <summary>
        /// higher values are chosen first
        /// </summary>
        public virtual int draw_priority => 0;
        public abstract bool CanDraw(Type type);
        public abstract Godot.Node node { get; }
        bool init;

        protected Type property_type { get; private set; }
        public bool Updated(Type property_type, string label, object input, out object output)
        {
            if (!init)
            {
                this.property_type = property_type;
                init = true;
                Init();
            }
            return Updated(label, input, out output);
        }
        protected virtual void Init() { }
        protected virtual bool Updated(string label, object input, out object output)
        { output = default; return false; }
    }

    class Int16_Drawer : Int64_Drawer
    {
        public override bool CanDraw(Type type) => type == typeof(short);
        protected override bool Updated(string label, object input, out object output)
        {
            input = (long)(short)input;
            if (base.Updated(label, input, out output))
            {
                output = (short)(long)output;
                return true;
            }
            output = default;
            return false;
        }
    }

    class Int32_Drawer : Int64_Drawer
    {
        public override bool CanDraw(Type type) => type == typeof(int);
        protected override bool Updated(string label, object input, out object output)
        {
            input = (long)(int)input;
            if (base.Updated(label, input, out output))
            {
                output = (int)(long)output;
                return true;
            }
            output = default;
            return false;
        }
    }

    class Int64_Drawer : Double_Drawer
    {
        public override bool CanDraw(Type type) => type == typeof(long);

        protected override void Init()
        {
            base.Init();
            spin_box.item.Step = 1;
        }

        protected override bool Updated(string label, object input, out object output)
        {
            input = (double)(long)input;
            if (base.Updated(label, input, out output))
            {
                output = (long)Mathf.Round(((double)output));
                return true;
            }
            output = default;
            return false;
        }
    }

    class FloatDrawer : Double_Drawer
    {
        public override bool CanDraw(Type type) => type == typeof(float);
        protected override bool Updated(string label, object input, out object output)
        {
            input = (double)(float)input;
            if (base.Updated(label, input, out var new_value))
            {
                output = (float)(double)new_value;
                return true;
            }
            output = default;
            return false;
        }
    }

    class Double_Drawer : PropertyDrawer
    {
        public override int draw_priority => -100;
        public override bool CanDraw(Type type) => type == typeof(double);
        public override Node node => spin_box;
        //CustomGUI.FloatInput input = new CustomGUI.FloatInput(0, 1000, .1f) { AllowLesser = true, AllowGreater = true };
        protected IMGUI_Labeled<IMGUI_Spinbox> spin_box = new IMGUI_Labeled<IMGUI_Spinbox> { };

        protected override void Init()
        {
            spin_box.item.MinValue = -10000;
            spin_box.item.MaxValue = 10000;
            spin_box.item.Value = 0;
            spin_box.item.Step = 0.01f;
            spin_box.item.AllowLesser = true;
            spin_box.item.AllowGreater = true;
        }

        protected override bool Updated(string label, object input, out object output)
        {
            spin_box.label.Text = label;
            bool updated;
            if (updated = spin_box.item.TryGetValue(out var new_value))
            {
                output = new_value;
                spin_box.item.Value = new_value;
                return true;
            }
            output = default;
            if (!spin_box.item.HasFocusInHeirarchy())
            {
                spin_box.item.Value = (double)input;
                spin_box.item.TryGetValue(out new_value);
            }
            return false;
        }
    }

    public class Struct_Drawer : PropertyDrawer
    {
        public override bool CanDraw(Type type) => type != typeof(void);
        public override int draw_priority => int.MinValue + 100;
        public override Node node { get; } = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        ImmediateGUI_Container container = new ImmediateGUI_Container();
        IMGUI_Labeled<IMGUI_Button> control = new();
        List<ObjectDrawer> drawers = new List<ObjectDrawer>();
        bool show;
        public bool expand;

        protected override void Init()
        {
            node.AddChild(control);

            var hbox = new HBoxContainer().SetParent(node);
            hbox.AddChild(new Control { CustomMinimumSize = new Vector2(24, 0) });
            hbox.AddChild(container);
            control.item.OnButtonDown(() => show = !show);
        }

        protected override bool Updated(string label, object input, out object output)
        {
            if (expand) control.Visible = false;
            else
            {
                control.Visible = true;
                control.label.Text = label;
                control.item.Text = input.AsString();
            }

            bool updated = false;
            if (show || expand)
            {
                if (input == null)
                {
                    output = default;
                    container.Label("NULL");
                    return false;
                }

                foreach (var field in property_type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (container.Property(field.Name, field.FieldType, field.GetValue(input), out var new_field_value))
                    {
                        field.SetValue(input, new_field_value);
                        output = input;
                        updated = true;
                        continue;
                    }
                }
            }
            output = input;
            return updated;
        }
    }

    class String_Drawer : PropertyDrawer
    {
        public override bool CanDraw(Type type) => typeof(string).IsAssignableFrom(type);
        public override Node node => control;
        IMGUI_Labeled<IMGUI_TextEdit> control = new IMGUI_Labeled<IMGUI_TextEdit>();

        protected override bool Updated(string label, object input, out object output)
        {
            control.label.Text = label;
            if (input == null) input = "";

            bool updated;
            if (updated = control.item.TryGet(out string new_value))
            {
                input = output = new_value;
                return true;
            }

            if (!control.item.HasFocus())
                control.item.Text = (string)input;
            output = default;
            return false;
        }
    }

    class Bool_Drawer : PropertyDrawer
    {
        public override bool CanDraw(Type type) => typeof(bool).IsAssignableFrom(type);
        public override Node node => control;
        IMGUI_Labeled<IMGUI_Toggle> control = new();

        protected override bool Updated(string label, object input, out object output)
        {
            control.label.Text = label;
            if (input == null) input = "";

            bool updated;
            if (updated = control.item.TryGetValue((bool)input, out bool new_value))
            {
                input = output = new_value;
                return true;
            }

            control.item.ButtonPressed = (bool)input;
            control.item.Text = control.item.ButtonPressed ? "ON" : "OFF";
            output = default;
            return false;
        }
    }

    class Enum_Drawer : PropertyDrawer
    {
        public override Node node => container;
        ImmediateGUI_Container container = new ImmediateGUI_Container();
        public override bool CanDraw(Type type) => type.IsEnum;

        static List<object> values = new List<object>();
        protected override bool Updated(string label, object input, out object output)
        {
            values.Clear();
            foreach (var item in System.Enum.GetValues(property_type))
                values.Add(item);

            var enum_val = input;
            bool updated;
            updated = container.Options(label, ref enum_val, values);
            output = enum_val;
            return updated;

        }
    }

    class ListDrawer : PropertyDrawer
    {
        public override int draw_priority => int.MinValue + 300;
        public override bool CanDraw(Type type) => typeof(System.Collections.IList).IsAssignableFrom(type);
        public override Node node => container;
        ImmediateGUI_Container container = new();
        ImmediateGUI_Container popup;
        protected override bool Updated(string label, object input, out object output)
        {
            if (container.Button(label, property_type.Name))
            {
                if (popup.IsValid()) popup.DestroyNode();
                else
                {
                    var window = new Window { Size = new Vector2I(256, 320) }.AddToScene();
                    window.Title = property_type.Name;
                    window.CloseRequested += () => popup.DestroyNode();
                    var scroll = new ScrollContainer { AnchorRight = 1, AnchorBottom = 1 }.SetParent(window);
                    popup = new ImmediateGUI_Container();
                    popup.OnDestroy(() => window.DestroyNode());
                    popup.SetParent(scroll);

                    var parent = node.GetViewport().GetWindow();
                    if (parent != Scene.Current.GetViewport().GetWindow())
                        window.Position = parent.Position + new Vector2I(parent.Size.X + 16, 0);
                    else
                    {
                        var pos = ((Control)node).GlobalPosition + new Vector2I((int)((Control)node).Size.X, 0);
                        window.Position = new Vector2I((int)pos.X, (int)pos.Y);
                    }
                }
            }

            bool updated = false;
            if (popup.IsValid() && input is System.Collections.IList list)
            {
                for (int i = 0; i < list.Count; ++i)
                {
                    string text = $"Item {i}:";
                    var item = list[i];
                    if (popup.Property(text, item?.GetType(), item, out var new_item))
                    {
                        list[i] = new_item;
                        updated = true;
                    }
                }
            }

            output = input;
            return updated;
        }
    }

    class Enumerable_Drawer : PropertyDrawer
    {
        public override int draw_priority => int.MinValue + 200;
        public override bool CanDraw(Type type) => typeof(System.Collections.IEnumerable).IsAssignableFrom(type);
        public override Node node => container;
        ImmediateGUI_Container container = new();
        ImmediateGUI_Container popup;
        protected override bool Updated(string label, object input, out object output)
        {
            if (container.Button(label, property_type.Name))
            {
                if (popup.IsValid()) popup.DestroyNode();
                else
                {
                    var window = new Window { Size = new Vector2I(256, 320) }.AddToScene();
                    window.Title = property_type.Name;
                    window.CloseRequested += () => popup.DestroyNode();
                    var scroll = new ScrollContainer { AnchorRight = 1, AnchorBottom = 1 }.SetParent(window);
                    popup = new ImmediateGUI_Container();
                    popup.OnDestroy(() => window.DestroyNode());
                    popup.SetParent(scroll);

                    var parent = node.GetViewport().GetWindow();
                    if (parent != Scene.Current.GetViewport().GetWindow())
                        window.Position = parent.Position + new Vector2I(parent.Size.X + 16, 0);
                    else
                    {
                        var pos = ((Control)node).GlobalPosition + new Vector2I((int)((Control)node).Size.X, 0);
                        window.Position = new Vector2I((int)pos.X, (int)pos.Y);
                    }


                }
            }

            bool updated = false;
            if (popup.IsValid() && input is System.Collections.IEnumerable enumerable)
            {
                int count = 0;
                foreach (var item in enumerable)
                {
                    string text = $"Item {count++}:";
                    if (item != null && item.GetType().IsClass)
                    {
                        var val = item;
                        popup.Property(text, val.GetType(), val, out output);
                        updated = true;
                    }
                    else
                        popup.Label(text, item.AsString());
                }

            }

            output = input;
            return updated;
        }
    }

    public class Node_Drawer : PropertyDrawer
    {
        public override bool CanDraw(Type type) => typeof(Godot.GodotObject).IsAssignableFrom(type);
        public override int draw_priority => int.MinValue + 100;
        public override Node node { get; } = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        ImmediateGUI_Container container = new ImmediateGUI_Container();
        IMGUI_Labeled<IMGUI_Button> control = new();
        List<ObjectDrawer> drawers = new List<ObjectDrawer>();
        bool show;
        protected override void Init()
        {
            node.AddChild(control);

            var hbox = new HBoxContainer().SetParent(node);
            hbox.AddChild(new Control { CustomMinimumSize = new Vector2(24, 0) });
            hbox.AddChild(container);
            control.item.OnButtonDown(() => show = !show);
        }

        protected override bool Updated(string label, object input, out object output)
        {
            control.label.Text = label;
            control.item.Text = input.AsString();

            bool updated = false;
            if (show)
            {
                if (input == null || (input is Godot.GodotObject go_obj && !go_obj.IsValid()))
                {
                    output = default;
                    container.Label("NULL");
                    return false;
                }

                foreach (var field in property_type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (container.Property(field.Name, field.FieldType, field.GetValue(input), out var new_field_value))
                    {
                        if (field.Name == "NativePtr") continue;
                        field.SetValue(input, new_field_value);
                        output = input;
                        updated = true;
                        continue;
                    }
                }

                foreach (var prop in property_type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (!(prop.CanRead && prop.CanWrite)) continue;
                    switch (prop.Name)
                    {
                        case "Owner":
                        case "UniqueNameInOwner":
                        case "_ImportPath":
                        case "VisibilityParent":
                        case "SceneFilePath":
                        case "EditorDescription":
                        case "Rotation":
                        case "Quaternion":
                        case "Basis":
                        case "RotationEditMode":
                        case "Transform":
                        case "GlobalTransform":
                        case "RotationOrder":
                        case "GlobalRotation":
                        case "_weakReferenceToSelf":
                        case "NativeValue":
                            continue;
                    }

                    if (container.Property(prop.Name, prop.PropertyType, prop.GetValue(input), out var new_field_value))
                    {
                        prop.SetValue(input, new_field_value);
                        output = input;
                        updated = true;
                        continue;
                    }
                }
            }
            output = input;
            return updated;
        }
    }

    class Invalid_Drawer : PropertyDrawer
    {
        public override int draw_priority => int.MinValue;
        public override bool CanDraw(Type type) => true;
        public override Node node => control;
        IMGUI_Labeled<Godot.Label> control = new();
        protected override bool Updated(string label, object input, out object output)
        {
            control.label.Text = label;
            control.item.Text = "Type and input mismatch";
            output = default;
            return false;
        }
    }
}

