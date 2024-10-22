using OpenTK.Mathematics;

namespace Rockfall
{
    public class Camera
    {
        public Vector3 Position { get; set; } = Vector3.Zero;
        public Vector3 Front { get; set; } = -Vector3.UnitZ;
        public Vector3 Up { get; set; } = Vector3.UnitY;

        public float Fov { get; set; } = 60f;

        public Matrix4 GetViewMatrix()
        {
            return Matrix4.LookAt(Position, Position + Front, Up);
        }

        public Matrix4 GetProjectionMatrix(float aspectRatio)
        {
            return Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(Fov), aspectRatio, 0.1f, 100f);
        }
    }
}
