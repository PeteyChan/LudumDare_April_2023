public static class Game
{
    public static bool Show_Debug_Gizmos = false;
    public static Inputs move_left = Inputs.key_a, move_right = Inputs.key_d, jump = Inputs.key_space, attack = Inputs.mouse_left_click;


    static void Update(Bootstrap.Process args)
    {
        if (Inputs.key_f1.OnPressed())
            Show_Debug_Gizmos = !Show_Debug_Gizmos;
    }

    static void GameSettings(Debug.Console args)
    {
        ImmediateWindow window = new ImmediateWindow();

        window.Window.Title = "Game Settings";

        window.Window.OnUpdate(() =>
        {
            foreach (var field in typeof(Game).GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
            {
                var obj = field.GetValue(null);
                if (window.Property(field.Name, obj, out var output))
                    field.SetValue(null, output);
            }
        });
    }
}


public interface Interactable
{
    void OnEvent(object event_type) { }
}

namespace Events
{
    public class OnAttack
    {
        public object Attacker;
        public Godot.Vector2 force;
    }
}