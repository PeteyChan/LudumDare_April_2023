public static class Game
{



    static void ShowGameData(Debug.Console args)
    {
        ImmediateWindow window = new ImmediateWindow();

        window.Window.OnUpdate(() =>
        {
            foreach (var field in typeof(Game).GetFields(System.Reflection.BindingFlags.Static))
            {
                
            }
        });
    }
}