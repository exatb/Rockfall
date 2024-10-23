# Rockfall
This demo based on very simple and small 3D engine Rockfall and OpenTK library for demonstrating work the GJKEPA library. Written in pure C#, with a minimum of external references.

GJKEPA library provides a fast and stable realisaton is Gilbert-Johnson-Keerthi (GJK) algorithm and the Expanding Polytope Algorithm (EPA).
![Link to repository](https://github.com/exatb/GJKEPA)

The Rackfall engine includes a simple physics solver for realistic motion and collisions of rigid bodies.

It's easy to use! Just launch it, press A or S to add a cube or sphere. And watch it funny fall.

![Example of using](https://github.com/exatb/Rockfall/blob/main/Example.jpg)

This demo have 2 directories:

Bin - have all binaries for quick start and testing.

Src - Rockfall demo source files. 


I used VS2022 to create the project for Windows, but the source code should build and run on other systems.

In VS2022, you need to create a new console project, then launch the NuGet console and type "Install-Package OpenTK".

After installation, add all the sources to your project, build and run!  

## The Rockfall demo consists of the following source files:
```
Camera.cs - Camera class. You can control position, FOV and camera movement.
Game.cs - Main game window module. Contains all main events and game logic.
Ligth.cs - Contains a description of the light source. 
MeshGenerators - 3D mesh generators for creating objects.
Program.cs - A short dialog to launch the game window.
RigidBody.cs - The main class defines the rigid body motion logic and collision solver. 
Scene.cs - Collects all objects in the scene and provides interaction with them.
Shader.cs - Class for loading and compiling shaders.
gjkepa.cs - Implementation of GJK and EPA.
shader.frag - Fragment shader source.
shader.vert - Vertex shader source.
```
