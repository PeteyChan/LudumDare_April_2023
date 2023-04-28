using System.Collections.Generic;

public partial class TreeNode<BaseNode, Data> where BaseNode : TreeNode<BaseNode, Data>
{
    public float state_time { get; private set; }
    protected virtual void OnInit(in List<BaseNode> child_nodes) { }
    protected virtual bool EntryCondition() => true;
    protected virtual bool ExitCondition() => false;
    protected virtual void OnEnter() { }
    protected virtual void OnUpdate(float delta) { }
    protected virtual void OnExit() { }
    protected BehaviourTree tree { get; private set; }
    public BaseNode parent { get; private set; }
    public BaseNode active_child { get; private set; }
    public IReadOnlyList<BaseNode> child_nodes => _child_nodes;
    List<BaseNode> _child_nodes = new List<BaseNode>();

    public override string ToString()
    {
        var type = GetType();
        var sub_type = active_child?.GetType();

        if (sub_type == null) return type.Name;
        return type.Name.AsString(":", sub_type.ToString());
    }
}

public partial class TreeNode<BaseNode, Data>
{
    public class BehaviourTree
    {
        public Data data { get; private set; }

        public BehaviourTree SetRoot<Node>(Data data) where Node : BaseNode, new()
        {
            if (root != null) ExitNode(root);
            this.data = data;
            root = new Node();
            InitNode(root);
            return this;
        }

        public BaseNode root { get; private set; }
        BaseNode active;

        public override string ToString() => $"Tree {typeof(Data).Name} :".AsString(active);

        public void Update(float delta)
        {
            if (active == null)
            {
                if (root?.EntryCondition() == true)
                {
                    active = root;
                    active.OnEnter();
                }
            }
            else
            {
                if (active.ExitCondition() == true)
                {
                    ExitNode(active);
                    active = null;
                }
                else
                {
                    UpdateNode(active, delta);
                    TransitionNode(active);
                }
            }
        }

        public void Exit()
        {
            if (active != null)
                ExitNode(active);
            active = null;
        }

        void InitNode(BaseNode node)
        {
            node.tree = this;
            node.OnInit(node._child_nodes);
            foreach (var child in node.child_nodes)
            {
                child.parent = node;
                InitNode(child);
            }
        }

        void TransitionNode(BaseNode node)
        {
            foreach (var child in node._child_nodes)
            {
                if (child == node.active_child)
                {
                    if (child.ExitCondition())
                    {
                        ExitNode(child);
                        node.active_child = null;
                        return;
                    }

                    TransitionNode(child);
                    return;
                }

                if (child.EntryCondition())
                {
                    ExitNode(node.active_child);
                    node.active_child = child;
                    EnterNode(child);
                    return;
                }
            }
        }

        void EnterNode(BaseNode node)
        {
            if (node == null) return;
            node.state_time = 0;
            node.OnEnter();
            foreach (var child in node._child_nodes)
                if (child.EntryCondition())
                {
                    node.active_child = child;
                    EnterNode(child);
                    return;
                }
        }

        void UpdateNode(BaseNode node, float delta)
        {
            if (node == null) return;
            node.state_time += delta;
            node.OnUpdate(delta);
            UpdateNode(node.active_child, delta);
        }

        void ExitNode(BaseNode node)
        {
            if (node == null) return;
            ExitNode(node.active_child);
            node.OnExit();
            node.active_child = null;
        }

        /// <summary>
        /// finds first occurance of node of type T by node depth
        /// </summary>
        public bool TryFind<T>(out T target)
        {
            target = default;
            if (root == null) return false;
            return TryFind(root, out target);

            bool TryFind(BaseNode node, out T target)
            {
                foreach (var child in node._child_nodes)
                    if (child is T value)
                    {
                        target = value;
                        return true;
                    }

                foreach (var child in node._child_nodes)
                    if (TryFind(child, out target)) return true;

                target = default;
                return false;
            }
        }

        /// <summary>
        /// finds all occurances of T
        /// </summary>
        public List<T> FindAll<T>(List<T> buffer = default)
        {
            buffer = buffer == null ? new List<T>() : buffer;
            buffer.Clear();

            if (root == null) return buffer;
            FindAll(root);
            return buffer;

            void FindAll(BaseNode node)
            {
                if (node is T value)
                    buffer.Add(value);

                foreach (var child in node._child_nodes)
                    FindAll(child);
            }
        }

        public bool IsActive<T>() => IsActive<T>(out T target);
        public bool IsActive<T>(out T target)
        {
            return IsActive(active, out target);

            bool IsActive(BaseNode node, out T target)
            {
                switch (node)
                {
                    case null:
                        target = default;
                        return false;

                    case T value:
                        target = value;
                        return true;

                    default:
                        return IsActive(node.active_child, out target);
                }
            }
        }

        static Dictionary<BehaviourTree, Godot.Window> inspectors = new();
        public static void Inspect(BehaviourTree tree)
        {
            if (inspectors.TryGetValue(tree, out var window))
            {
                window.DestroyNode();
                return;
            }

            window = new Godot.Window { Title = typeof(BaseNode).Name.AsString("Beahvour Tree") }
                .AddToScene();
            window.Name = window.Title;
            window.OnDestroy(() => inspectors.Remove(tree));
            window.CloseRequested += (() => window.DestroyNode());

            window.Position = new Godot.Vector2I(100, 100);
            window.Size = new Godot.Vector2I(400, 320);

            inspectors.Add(tree, window);


            Dictionary<BaseNode, Godot.Button> nodes = new();
            BaseNode current = null;
            ObjectDrawer data_viewer = default;

            window.OnUpdate(() =>
            {
                if (current != tree.root)
                {
                    current = tree.root;

                    if (window.GetChildCount() > 1)
                        window.GetChild(1).DestroyNode();

                    var container = new Godot.HBoxContainer
                    {
                        AnchorRight = 1,
                        AnchorBottom = 1
                    }
                    .SetParent(window);

                    {
                        var data_scroll = new Godot.ScrollContainer
                        {
                            SizeFlagsHorizontal = Godot.Control.SizeFlags.ExpandFill,
                            SizeFlagsVertical = Godot.Control.SizeFlags.ExpandFill,
                            Name = "data scroll"
                        }
                        .SetParent(container);

                        data_viewer = new ObjectDrawer();
                        data_viewer.node.Name = "data viewer";
                        data_scroll.AddChild(data_viewer);
                    }

                    {
                        var graph_scroll = new Godot.ScrollContainer
                        {
                            SizeFlagsHorizontal = Godot.Control.SizeFlags.ExpandFill,
                            SizeFlagsVertical = Godot.Control.SizeFlags.ExpandFill,
                            SizeFlagsStretchRatio = 2,
                            Name = "graph scroll",
                        }
                        .SetParent(container);

                        CreateNodes(tree.root, graph_scroll);
                    }
                }

                data_viewer.TryUpdate(tree.data?.GetType(), tree.data, out var output);

                foreach (var (base_node, label) in nodes)
                {
                    if (isActive(base_node))
                        label.Modulate = Godot.Colors.LightGreen;
                    else label.Modulate = new Godot.Color(.3f, .3f, .3f);
                }

                bool isActive(BaseNode node)
                    => node == tree.active || node.parent?.active_child == node;

                void CreateNodes(BaseNode node, Godot.Node parent)
                {
                    var container = new Godot.HBoxContainer().SetParent(parent);
                    container.SizeFlagsHorizontal = Godot.Control.SizeFlags.ExpandFill;
                    container.SizeFlagsVertical = Godot.Control.SizeFlags.ExpandFill;

                    var label = new Godot.Button { Text = node.GetType().Name }
                        .SetParent(container);
                    label.Name = label.Text;
                    var child_container = new Godot.VBoxContainer()
                        .SetParent(container);

                    nodes.Add(node, label);

                    foreach (var child in node._child_nodes)
                        CreateNodes(child, child_container);
                }
            });
        }
    }
}
