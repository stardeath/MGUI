using System;

public class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        using var game = new MGUI.Samples2.Game1();
        game.Run();
    }
}
