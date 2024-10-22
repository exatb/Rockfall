using OpenTK.Mathematics;

namespace Rockfall
{
    public class Light
    {
        public Vector3 Position { get; set; } = new Vector3(1.2f, 1.0f, 2.0f);
        public Vector3 Color { get; set; } = Vector3.One; // Белый свет
    }
}
