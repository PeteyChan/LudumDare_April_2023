public static class Game
{
    public static bool Show_Debug_Gizmos = false;
    
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