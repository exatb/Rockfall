using OpenTK.Mathematics;
using System.Collections.Generic;

namespace Rockfall
{
    public class Scene
    {
        public List<RigidBody> RigidBodies { get; private set; } = new List<RigidBody>();
        public Camera Camera { get; private set; } = new Camera();
        public Light Light { get; private set; } = new Light();

        public void AddRigidBody(RigidBody rigidBody)
        {
            RigidBodies.Add(rigidBody);
        }

        Quaternion camera_rotation = new Quaternion(0,0,0,1);

        public void Update(float deltaTime)
        {
            foreach (var rigidBody in RigidBodies)
            {
                rigidBody.Update(deltaTime);
                // Получение мировых координат вершин
                var worldVertices = rigidBody.WorldVertices;
                // Здесь можно использовать worldVertices для проверки столкновений
            }

            // Проверка столкновений между телами 
            CheckCollisions();

            // Вращаем камеру вокруг сцены
            float angle = MathHelper.DegreesToRadians(45f) * deltaTime;
            Quaternion rotation = Quaternion.FromAxisAngle(Vector3.UnitY, angle);
            camera_rotation = rotation * camera_rotation;
        }

        public void Render(Shader shader, float aspectRatio)
        {
            shader.Use();

            Matrix4 view = Matrix4.CreateFromQuaternion(camera_rotation) * Camera.GetViewMatrix();
            Matrix4 projection = Camera.GetProjectionMatrix(aspectRatio);

            shader.SetMatrix4("view", view);
            shader.SetMatrix4("projection", projection);

            shader.SetVector3("lightColor", Light.Color);
            shader.SetVector3("lightPos", Light.Position);

            // Передача позиции камеры
            shader.SetVector3("viewPos", Camera.Position);

            foreach (var rigidBody in RigidBodies)
            {
                rigidBody.Render(shader);
            }
        }

        private void HandleCollision(RigidBody bodyA, RigidBody bodyB, CrossInfo3D crossInfo)
        {
            //Расчет для двух ограничивающих поверхностей - точка пересечения всегда одна
            //используем информацию о глубине проникновения, нормали и точке контакта

            float e = 0.0f; //Коэфициент восстановления
            Vector3 n = -crossInfo.Out; //Для решателя нормаль должна быть направлена от b к a
            Vector3 p = crossInfo.MidPoint;
            float d = crossInfo.Deep;

            bodyA.ContactSolver(bodyA, bodyB, p, n, e, d); //Решаем контакт
        }

        private void CheckCollisions()
        {
            // Проверка столкновений всех тел в сцене 
            for (int i = 0; i < RigidBodies.Count; i++)
            {
                for (int j = i + 1; j < RigidBodies.Count; j++)
                {
                    var bodyA = RigidBodies[i];
                    var bodyB = RigidBodies[j];
                    CrossInfo3D crossInfo = bodyA.IsCollidingWith(bodyB);
                    if (crossInfo.Cross)
                    {
                        HandleCollision(bodyA, bodyB, crossInfo);
                    }
                }
            }
        }

        public void Dispose()
        {
            foreach (var rigidBody in RigidBodies)
            {
                rigidBody.Dispose();
            }
        }
    }
}
