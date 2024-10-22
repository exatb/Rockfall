using Rockfall;

Console.WriteLine("Rockfall demo.");
Console.WriteLine("Press A to add a random box and press S to add a random sphere.");
Console.WriteLine("Press Esc to exit.");
Console.WriteLine("Press any key to start.");
Console.ReadKey();
Console.WriteLine("Start OpenGL window");
Game game = new(600, 600, "OpenGL window");
game.Run();
Console.WriteLine("Stop game. FPS = " + game.Frames / game.Time + " Elapsed " + game.Time + "c");


