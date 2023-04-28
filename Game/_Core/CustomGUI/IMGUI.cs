using Godot;
using System;
using System.Collections.Generic;
using Internal.IMGUI;

public class ImmediateWindow : IMGUI_Interface
{
    public Window Window { get; private set; }
    public IMGUI_Interface Header => header;
    ImmediateGUI_Container header;
    public ImmediateWindow()
    {
        Window = new Window().AddToScene(true);
        Window.CloseRequested += () => Window.DestroyNode();

        var vbox = new VBoxContainer
        {
            AnchorRight = 1,
            AnchorBottom = 1,
            OffsetLeft = 8,
            OffsetRight = -8,
            OffsetTop = 8,
            OffsetBottom = -8
        };
        Window.AddChild(vbox);

        vbox.AddChild(header = new ImmediateGUI_Container());
        header.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        header.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;

        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        }.SetParent(vbox);
        Window.Position = new Vector2I(10, 36);
        Window.Size = new Vector2I(256, 320);
        Window.Transient = true;
        container = new ImmediateGUI_Container().SetParent(scroll);
    }

    IMGUI_Interface container;
    T IMGUI_Interface.Get<T>() => Window.IsValid() ? container.Get<T>() : throw new Exception("window is invalid");
}

public partial class ImmediateGUI_Container : BoxContainer, IMGUI_Interface
{
    public ImmediateGUI_Container()
    {
        data = new IMGUI_Element_Data();
        this.AddChild(data.container);
        Size = new Vector2(256, 320);
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsVertical = SizeFlags.ExpandFill;
    }
    IMGUI_Element_Data data;
    T IMGUI_Interface.Get<T>() => data.Get<T>();
}

public static class ImmediateGUI_Extensions
{
    public struct LabelParams
    {
        public LabelParams() { }
        public HorizontalAlignment? horizontal_alignment = default;
        public VerticalAlignment? vertical_alignment = default;
        public int? font_size = null;
        public int? height = null;
    }

    public static T Label<T>(this T self, string label)
        where T : IMGUI_Interface
    {
        var item = self.Get<Godot.Label>();
        item.Text = label;
        return self;
    }

    public static T Label<T>(this T self, string label, string text)
        where T : IMGUI_Interface
    {
        var item = self.Get<IMGUI_Labeled<Godot.Label>>();
        item.label.Text = label;
        item.item.Text = text;
        return self;
    }

    public static bool Button<T>(this T self, string text)
        where T : IMGUI_Interface
    {
        var button = self.Get<IMGUI_Button>();
        button.Text = text;
        return button.IsPushed();
    }

    public static bool Button<T>(this T self, string label, string text)
        where T : IMGUI_Interface
    {
        var button = self.Get<IMGUI_Labeled<IMGUI_Button>>();
        button.label.Text = label;
        button.item.Text = text;
        return button.item.IsPushed();
    }

    public static T Button<T>(this T self, string text, Action on_press)
        where T : IMGUI_Interface
    {
        var button = self.Get<IMGUI_Button>();
        button.Text = text;
        if (button.IsPushed()) on_press?.Invoke();
        return self;
    }

    public static T Button<T>(this T self, string label, string text, Action on_press)
        where T : IMGUI_Interface
    {
        var button = self.Get<IMGUI_Labeled<IMGUI_Button>>();
        button.label.Text = label;
        button.item.Text = text;
        if (button.item.IsPushed()) on_press?.Invoke();
        return self;
    }

    public static bool TextEdit<T>(this T self, ref string value)
        where T : IMGUI_Interface
    {
        var text_edit = self.Get<IMGUI_TextEdit>();
        bool updated;
        if (updated = text_edit.TryGet(out var new_value))
            value = new_value;
        if (!text_edit.HasFocus()) text_edit.Text = value;
        return updated;
    }

    public static bool TextEdit<T>(this T self, string label, ref string value)
    where T : IMGUI_Interface
    {
        var text_edit = self.Get<IMGUI_Labeled<IMGUI_TextEdit>>();
        text_edit.label.Text = label;
        bool updated;
        if (updated = text_edit.item.TryGet(out var new_value))
            value = new_value;
        if (!text_edit.item.HasFocus()) text_edit.item.Text = value;
        return updated;
    }

    public static T Separator<T>(this T self)
        where T : IMGUI_Interface
    {
        self.Get<Godot.HSeparator>();
        return self;
    }

    public static bool Toggle<T>(this T self, ref bool value, string on_text = "ON", string off_text = "OFF")
    where T : IMGUI_Interface
    {
        var toggle = self.Get<IMGUI_Toggle>();
        var updated = toggle.TryGetValue(value, out value);
        toggle.Text = value ? on_text : off_text;
        return updated;
    }

    public static bool Toggle<T>(this T self, string label, ref bool value, string on_text = "ON", string off_text = "OFF")
        where T : IMGUI_Interface
    {
        var toggle = self.Get<IMGUI_Labeled<IMGUI_Toggle>>();
        toggle.label.Text = label;

        var updated = toggle.item.TryGetValue(value, out value);
        toggle.item.Text = value ? on_text : off_text;
        return updated;
    }

    public static T Space<T>(this T self, float height)
        where T : IMGUI_Interface
    {
        self.Get<Godot.Control>().CustomMinimumSize = new Vector2(0, height);
        return self;
    }

    public static bool Property<T, PropertyType>(this T self, string label, ref PropertyType value)
        where T : IMGUI_Interface
    {
        var item = self.Get<IMGUI_PropertyDrawer>();
        item.drawer.Label = label;
        bool updated;
        if (updated = item.drawer.TryUpdate(typeof(PropertyType), value, out var output))
            value = (PropertyType)output;
        return updated;
    }

    public static bool Property<T>(this T self, object value)
        where T : IMGUI_Interface
    {
        var item = self.Get<IMGUI_PropertyDrawer>();
        item.drawer.Expand_Struct_Drawer = true;
        bool updated;
        updated = item.drawer.TryUpdate(value?.GetType(), value, out var output);
        return updated;
    }

    public static T Property<T, PropertyType>(this T self, string label, PropertyType current_value, Action<PropertyType> on_change)
        where T : IMGUI_Interface
    {
        PropertyType property = current_value;
        if (self.Property(label, ref property))
            on_change?.Invoke(current_value);
        return self;
    }

    public static bool Property<T>(this T self, string label, Type property_type, object current_value, out object output)
        where T : IMGUI_Interface
    {
        var item = self.Get<IMGUI_PropertyDrawer>();
        item.drawer.Label = label;
        return item.drawer.TryUpdate(property_type, current_value, out output);
    }

    /// <summary>
    /// returns true if value was updated
    /// </summary>
    public static bool SpinBox<T>(this T self, string label, ref float value, float min = 0, float max = 1000, float step = 0.01f, bool allow_lesser = false, bool allow_greater = true)
        where T : IMGUI_Interface
    {
        var box = self.Get<IMGUI_Labeled<IMGUI_Spinbox>>();
        box.label.Text = label;
        box.item.MinValue = min;
        box.item.MaxValue = max;
        box.item.Step = step;
        box.item.AllowLesser = allow_lesser;
        box.item.AllowGreater = allow_greater;

        bool updated;
        if (updated = box.item.TryGetValue(out var new_value))
            value = (float)new_value;

        foreach (var child in box.item.GetChildren(true))
            if (child is Control control && control.HasFocus())
                return updated;
        box.item.Value = value;
        return updated;
    }

    /// <summary>
    /// returns true if value was updated
    /// </summary>
    public static bool Options<T, OptionType>(this T self, ref OptionType value, IEnumerable<OptionType> choices)
    where T : IMGUI_Interface
    {
        var options = self.Get<IMGUI_Options<OptionType>>();
        options.items.Clear();
        options.items.AddRange(choices);
        bool updated;
        if (updated = options.TryGetValue(out var new_value))
            value = new_value;
        options.Text = value.AsString();
        return updated;
    }

    public static bool Options<T, OptionType>(this T self, string label, IEnumerable<OptionType> choices, out OptionType value)
    where T : IMGUI_Interface
    {
        var options = self.Get<IMGUI_Options<OptionType>>();
        options.items.Clear();
        options.items.AddRange(choices);
        bool updated;
        updated = options.TryGetValue(out value);
        options.Text = label;
        return updated;
    }

    /// <summary>
    /// returns true if value was updated
    /// </summary>
    public static bool Enum<T, E>(this T self, string label, ref E value)
        where T : IMGUI_Interface
        where E : struct, Enum
            => Options<T, E>(self, label, ref value, System.Enum.GetValues<E>());

    /// <summary>
    /// returns true if value was updated
    /// </summary>
    public static bool Enum<T, E>(this T self, ref E value)
        where T : IMGUI_Interface
        where E : struct, Enum
        => Enum(self, typeof(E).Name, ref value);


    /// <summary>
    /// returns true if value was updated
    /// </summary>
    public static bool Options<T, OptionType>(this T self, string label, ref OptionType value, IEnumerable<OptionType> choices)
        where T : IMGUI_Interface
    {
        var options = self.Get<IMGUI_Labeled<IMGUI_Options<OptionType>>>();
        options.label.Text = label;
        options.item.items.Clear();
        options.item.items.AddRange(choices);
        bool updated;
        if (updated = options.item.TryGetValue(out var new_value))
            value = new_value;
        options.item.Text = value.AsString();
        return updated;
    }

    public static T Options<T, OptionType>(this T self, OptionType value, IEnumerable<OptionType> choices, Action<OptionType> on_updated)
        where T : IMGUI_Interface
    {
        OptionType current = value;
        if (self.Options(ref current, choices))
            on_updated?.Invoke(current);
        return self;
    }

    public static T Options<T, OptionType>(this T self, string label, OptionType value, IEnumerable<OptionType> choices, Action<OptionType> on_updated)
        where T : IMGUI_Interface
    {
        OptionType current = value;
        if (self.Options(label, ref current, choices))
            on_updated?.Invoke(current);
        return self;
    }
}


namespace Internal.IMGUI
{
    public class IMGUI_Element_Data
    {
        public IMGUI_Element_Data()
        {
            container = new VBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill
            };

            container.OnUpdate(() =>
            {
                if (element_count == 0)
                {
                    container.Visible = false;
                    return;
                }
                container.Visible = true;
                int i = 0;
                for (; i < element_count; ++i)
                    elements[i].node.Visible = true;
                for (; i < elements.Count; ++i)
                    elements[i].node.Visible = false;
                element_count = 0;
            });
        }

        public Godot.VBoxContainer container;
        List<Element> elements = new List<Element>();
        int element_count;
        public bool Visible => container.Visible;
        public T Get<T>() where T : Godot.Control, new()
        {
            if (element_count == elements.Count)
                elements.Add(new Element(container));
            element_count++;
            return elements[element_count - 1].Get<T>();
        }
    }
    class Element
    {
        public Element(Godot.Control parent)
        {
            node = new BoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                ClipContents = true
            }.SetParent(parent);
        }

        public bool updated;
        public Godot.Control node;
        Godot.Control element;

        public T Get<T>() where T : Godot.Control, new()
        {
            updated = true;
            if (element is T value && element.GetType() == typeof(T))
                return value;
            element.DestroyNode();
            element = new T();
            element.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            element.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            node.AddChild(element);
            return (T)element;
        }
    }

    public interface IMGUI_Interface
    {
        T Get<T>() where T : Godot.Control, new();
    }

    public partial class IMGUI_Toggle : Godot.Button
    {
        public IMGUI_Toggle()
        {
            ToggleMode = true;
            Toggled += value => ReleaseFocus();
        }

        bool toggle_value;

        public bool TryGetValue(bool current, out bool value)
        {
            bool updated = toggle_value != ButtonPressed;
            if (updated)
            {
                current = ButtonPressed;
                toggle_value = ButtonPressed;
            }
            value = current;
            ButtonPressed = value;
            toggle_value = value;
            return updated;
        }
    }

    public partial class IMGUI_Button : Godot.Button
    {
        public IMGUI_Button()
        {
            this.ButtonDown += () => pushed = true;
            this.ButtonUp += () => this.ReleaseFocus();
            ClipText = true;
        }

        bool pushed;

        public bool IsPushed()
        {
            var push = pushed;
            pushed = false;
            return push;
        }
    }

    public partial class IMGUI_Spinbox : Godot.SpinBox
    {
        public IMGUI_Spinbox()
        {
            this.ValueChanged += f =>
            {
                new_value = f;
                foreach (var child in GetChildren(true))
                    if (child is Control control && control.HasFocus())
                        control.ReleaseFocus();

            };
        }

        double? new_value;

        public bool TryGetValue(out double value)
        {
            value = new_value.GetValueOrDefault();
            var has = new_value.HasValue;
            new_value = default;
            return has;
        }
    }

    public partial class IMGUI_TextEdit : LineEdit
    {
        public IMGUI_TextEdit()
        {
            TextChanged += value => new_value = value;
            TextSubmitted += value => ReleaseFocus();
        }

        string new_value;

        public bool TryGet(out string value)
        {
            value = new_value;
            new_value = null;
            return value != null;
        }
    }

    public partial class IMGUI_Options<T> : Button
    {
        public IMGUI_Options()
        {
            bool exit = true;
            this.ButtonUp += () => ReleaseFocus();
            this.ButtonDown += () =>
            {
                if (!exit)
                {
                    exit = true;
                    return;
                }
                exit = false;

                if (items.Count == 0) return;
                var draw_options = new ImmediateWindow();
                draw_options.Window.UnParent();
                draw_options.Window.SetParent(this);
                draw_options.Window.Popup();

                if (this.TryFindParent(out Window window) && window != Scene.Current.GetViewport().GetWindow())
                    draw_options.Window.Position = window.Position + new Vector2I(window.Size.X + 16, 0);
                else
                {
                    var position = GlobalPosition + new Vector2(Size.X, 0);
                    draw_options.Window.Position = new Vector2I(position.X.Round(), position.Y.Round());
                }


                draw_options.Window.Title = typeof(T).Name + " Selection";
                draw_options.Window.OnDestroy(() => exit = true);
                draw_options.Window.CloseRequested += () => exit = true;

                string search = "";

                draw_options.Window.OnUpdate(() =>
                {
                    if (!this.IsValid() || !this.IsVisibleInTree()) exit = true;

                    if (exit)
                    {
                        draw_options.Window.DestroyNode();
                        return;
                    }

                    draw_options.TextEdit("Search", ref search);
                    foreach (var item in items)
                        if (item.AsString().Contains(search, StringComparison.OrdinalIgnoreCase))
                            draw_options.Button(item.AsString(), () =>
                            {
                                value = item;
                                hasValue = true;
                                draw_options.Window.DestroyNode();
                            });
                });
            };
        }

        public bool TryGetValue(out T value)
        {
            value = this.value;
            bool has = hasValue;
            hasValue = false;
            return has;
        }

        bool hasValue;
        T value;
        public List<T> items = new List<T>();

    }

    partial class IMGUI_PropertyDrawer : BoxContainer
    {
        public IMGUI_PropertyDrawer()
        {
            AddChild(drawer);
            //drawer.ShowPrivateFields = true;
        }

        public ObjectDrawer drawer = new ObjectDrawer();
    }

    partial class IMGUI_Labeled<T> : HBoxContainer where T : Godot.Control, new()
    {
        public IMGUI_Labeled()
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            label = new Label
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                VerticalAlignment = VerticalAlignment.Center,
                SizeFlagsStretchRatio = 1,
                ClipContents = true
            }.SetParent(this);

            item = new T
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                SizeFlagsStretchRatio = 2
            }.SetParent(this);
        }

        public Label label;
        public T item;
    }
}