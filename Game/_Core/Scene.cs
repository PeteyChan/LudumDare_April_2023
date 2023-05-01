public static partial class Scene
{
    public static Godot.SceneTree Tree => Godot.Engine.GetMainLoop() as Godot.SceneTree;

    static Godot.Node _current;
    public static Godot.Node Current
    {
        get => _current == null ? Tree.CurrentScene : _current;
        set
        {
            _current = value;
            Tree.Root.AddChild(_current);
            var old = Tree.CurrentScene;
            old.OnDestroy(() =>
            {
                Tree.CurrentScene = value;
                _current = null;
            });
            old.QueueFree();
        }
    }
    public static Godot.Node Load(string path) => Current = Godot.GD.Load<Godot.PackedScene>(path).Instantiate();
}