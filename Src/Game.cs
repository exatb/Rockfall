using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Graphics.OpenGL4;

namespace Rockfall
{
    public class Game : GameWindow
    {
        private Scene _scene;
        private Shader _shader;

        public Game(int width, int height, string title)
            : base(GameWindowSettings.Default, new NativeWindowSettings()
            {
                ClientSize = new Vector2i(width, height),
                Title = title
            })
        {
            // Загрузка шейдера
            _shader = new Shader("shader.vert", "shader.frag");

            // Инициализация сцены
            _scene = new Scene();
        }


        protected override void OnLoad()
        {
            base.OnLoad();

            // Настройки OpenGL
            GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
            GL.Enable(EnableCap.DepthTest);

            // Настройка камеры
            _scene.Camera.Position = new Vector3(0f, 0f, 5f);

            // Создание объектов и добавление в сцену
            CreateRigidBodies();
        }

        private void CreateRigidBodies()
        {

            CubeMeshGenerator planeMeshGenerator = new CubeMeshGenerator();
            planeMeshGenerator.YScale = 0.2f;
            planeMeshGenerator.XScale = 10f;
            planeMeshGenerator.ZScale = 10f;
            Mesh m = planeMeshGenerator.GenerateMesh();
            RigidBody plane = new RigidBody(m.VertexArray, m.IndexArray, new Vector3(0.5f, 0.5f, 0.5f), true); // Серый
            plane.M = float.MaxValue;
            plane.Position = new Vector3(0, -2, 0);   
            _scene.AddRigidBody(plane);


            // Создание первого объекта
            SphereMeshGenerator sphereMeshGenerator = new SphereMeshGenerator();
            sphereMeshGenerator.Radius = 0.5f;
            Mesh mesh1 = sphereMeshGenerator.GenerateMesh();
            RigidBody body1 = new RigidBody(mesh1.VertexArray, mesh1.IndexArray, new Vector3(1.0f, 0.0f, 0.0f), false); // Красный
            body1.Position = new Vector3(-1.0f, 0.0f, 0.0f);
            _scene.AddRigidBody(body1);

            // Создание второго объекта
            CubeMeshGenerator cubeMeshGenerator = new CubeMeshGenerator();
            cubeMeshGenerator.YScale = 0.2f;
            Mesh mesh2 = cubeMeshGenerator.GenerateMesh();
            RigidBody body2 = new RigidBody(mesh2.VertexArray, mesh2.IndexArray, new Vector3(0.0f, 0.0f, 1.0f), false); // Голубой
            body2.Position = new Vector3(0.5f, 0.0f, 0.0f);
            _scene.AddRigidBody(body2);
        }

        public double Time { get; set; } = 0;
        double renderTime = 0;
        double fpsTime = 0;
        public float deltaTime { get; set; } = 0.0001f;
        public double Frames { get; set;  } = 0;

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            double dt = args.Time;
            Time += dt;

            // Обновление сцены
            if ((Time - renderTime) >= deltaTime)
            {
                _scene.Update(deltaTime);
                renderTime = Time;
                Frames += 1;
            }

            if ((Time - fpsTime)>1)
            {
                int triangles = 0;
                for (int i = 0; i < _scene.RigidBodies.Count; i++) 
                {
                    triangles += _scene.RigidBodies[i].WorldVertices.Count();
                }
                
                Console.WriteLine("FPS = " + (Frames / Time).ToString("F0") + ", Time=" + Time.ToString("F2") + "c, Bodies=" + _scene.RigidBodies.Count +", Triangles=" + triangles);
                fpsTime = Time;
            }

            // Обработка ввода
            if (KeyboardState.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Escape))
            {
                Close();
            }

            if (KeyboardState.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.A))
            {
                CubeMeshGenerator cubeMeshGenerator = new CubeMeshGenerator();
                Random random = new Random();
                cubeMeshGenerator.XScale = 0.2f + 0.7f * (float)random.NextDouble();
                cubeMeshGenerator.YScale = 0.2f + 0.7f * (float)random.NextDouble();
                cubeMeshGenerator.ZScale = 0.2f + 0.7f * (float)random.NextDouble();

                cubeMeshGenerator.Direction = new System.Numerics.Vector3((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble());
                
                cubeMeshGenerator.Position = new System.Numerics.Vector3(0.0f, 1+(float)random.NextDouble(), 0.0f);
                Mesh mesh = cubeMeshGenerator.GenerateMesh();
                RigidBody body = new RigidBody(mesh.VertexArray, mesh.IndexArray, new Vector3(0.0f, 0.0f, 1.0f), false); // Голубой
                _scene.AddRigidBody(body);
            }

            if (KeyboardState.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.S))
            {
                SphereMeshGenerator sphereMeshGenerator = new SphereMeshGenerator();
                Random random = new Random();
                sphereMeshGenerator.Radius = 0.2f + 0.2f * (float)random.NextDouble();
                sphereMeshGenerator.Slices = 16;
                sphereMeshGenerator.Stacks = 8;

                sphereMeshGenerator.Center= new System.Numerics.Vector3(0.0f, 3+2*(float)random.NextDouble(), 0.0f);
                Mesh mesh = sphereMeshGenerator.GenerateMesh();
                RigidBody body = new RigidBody(mesh.VertexArray, mesh.IndexArray, new Vector3(1.0f, 0.0f, 0.0f), false); // Красный
                _scene.AddRigidBody(body);
            }

        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            // Очистка буферов
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // Отрисовка сцены
            _scene.Render(_shader, Size.X / (float)Size.Y);

            SwapBuffers();
            GL.Finish();
        }

        protected override void OnFramebufferResize(FramebufferResizeEventArgs e)
        {
            base.OnFramebufferResize(e);

            GL.Viewport(0, 0, e.Width, e.Height);
        }

        protected override void OnUnload()
        {
            base.OnUnload();

            // Освобождение ресурсов
            _shader.Dispose();
            _scene.Dispose();
        }
    }
}
