# Easy2D
A Pure 2D OpenGL Framework and various projects that use it

# how to maek game

Create a .net standard project, that references Easy2D.Game

1. Choose your main class and make it inherit the abstract class Game in Easy2D.Game
2. Everything else is self explainatory i think
3. Now make another project for your target platform
4. Make that project reference your game project
5. Create a Silk.NET window with the right flags
6. Forward events from window to the game
7. Run the window
