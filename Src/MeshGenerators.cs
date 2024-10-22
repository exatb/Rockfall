//#define USE_TEXTURES

using System.Numerics;

public class Mesh
{
    public List<Vector3> Positions { get; set; } = new List<Vector3>();
    public List<Vector3> Normals { get; set; } = new List<Vector3>();
    public List<Vector2> TextureCoordinates { get; set; } = new List<Vector2>();
    public List<uint> Indices { get; set; } = new List<uint>();

    // Свойство для получения массива вершинных данных
    public float[] VertexArray
    {
        get
        {
            List<float> vertexData = new List<float>();
            for (int i = 0; i < Positions.Count; i++)
            {
                // Позиция
                vertexData.Add(Positions[i].X);
                vertexData.Add(Positions[i].Y);
                vertexData.Add(Positions[i].Z);

                // Нормаль
                if (Normals.Count > i)
                {
                    vertexData.Add(Normals[i].X);
                    vertexData.Add(Normals[i].Y);
                    vertexData.Add(Normals[i].Z);
                }
                else
                {
                    vertexData.Add(0f);
                    vertexData.Add(0f);
                    vertexData.Add(0f);
                }

                #if USE_TEXTURES
                // Текстурные координаты
                if (TextureCoordinates.Count > i)
                {
                    vertexData.Add(TextureCoordinates[i].X);
                    vertexData.Add(TextureCoordinates[i].Y);
                }
                else
                {
                    vertexData.Add(0f);
                    vertexData.Add(0f);
                }
                #endif
            }
            return vertexData.ToArray();
        }
    }

    // Свойство для получения массива индексов
    public uint[] IndexArray
    {
        get
        {
            return Indices.ToArray();
        }
    }
}

public class SphereMeshGenerator
{
    // Поля
    private uint slices = 32;
    private uint stacks = 16;
    private Vector3 center = Vector3.Zero;
    private float radius = 1f;

    // Свойства
    public uint Slices
    {
        get => slices;
        set => slices = value;
    }

    public uint Stacks
    {
        get => stacks;
        set => stacks = value;
    }

    public Vector3 Center
    {
        get => center;
        set => center = value;
    }

    public float Radius
    {
        get => radius;
        set => radius = value;
    }

    // Метод для генерации геометрии сферы
    public Mesh GenerateMesh()
    {
        Mesh mesh = new Mesh();

        // Генерация вершин, нормалей и текстурных координат
        for (int stack = 0; stack <= stacks; stack++)
        {
            float phi = MathF.PI / 2 - stack * MathF.PI / stacks;
            float y = radius * MathF.Sin(phi);
            float scale = radius * MathF.Cos(phi);

            for (int slice = 0; slice <= slices; slice++)
            {
                float theta = slice * 2 * MathF.PI / slices;
                float x = scale * MathF.Cos(theta);
                float z = scale * MathF.Sin(theta);

                Vector3 position = new Vector3(x, y, z) + center;
                Vector3 normal = Vector3.Normalize(new Vector3(x, y, z));

                mesh.Positions.Add(position);
                mesh.Normals.Add(normal);

                mesh.TextureCoordinates.Add(new Vector2(
                    (float)slice / slices,
                    (float)stack / stacks));
            }
        }

        // Генерация индексов треугольников
        for (uint stack = 0; stack < stacks; stack++)
        {
            uint top = stack * (slices + 1);
            uint bottom = (stack + 1) * (slices + 1);

            for (uint slice = 0; slice < slices; slice++)
            {
                if (stack != 0)
                {
                    mesh.Indices.Add(top + slice);
                    mesh.Indices.Add(bottom + slice);
                    mesh.Indices.Add(top + slice + 1);
                }
                if (stack != stacks - 1)
                {
                    mesh.Indices.Add(top + slice + 1);
                    mesh.Indices.Add(bottom + slice);
                    mesh.Indices.Add(bottom + slice + 1);
                }
            }
        }

        return mesh;
    }
}

public class CubeMeshGenerator
{
    private float xScale = 1f;
    private float yScale = 1f;
    private float zScale = 1f;
    private Vector3 position = Vector3.Zero;
    private Vector3 direction = new Vector3(0, 1, 0);

    public float XScale
    {
        get => xScale;
        set => xScale = value;
    }

    public float YScale
    {
        get => yScale;
        set => yScale = value;
    }

    public float ZScale
    {
        get => zScale;
        set => zScale = value;
    }

    public Vector3 Position
    {
        get => position;
        set => position = value;
    }

    public Vector3 Direction
    {
        get => direction;
        set => direction = Vector3.Normalize(value);
    }

    // Метод для генерации геометрии куба
    public Mesh GenerateMesh()
    {
        Mesh mesh = new Mesh();

        // Определение 8 уникальных вершин куба
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(0, 0, 0), // 0
            new Vector3(1, 0, 0), // 1
            new Vector3(1, 1, 0), // 2
            new Vector3(0, 1, 0), // 3
            new Vector3(0, 0, 1), // 4
            new Vector3(1, 0, 1), // 5
            new Vector3(1, 1, 1), // 6
            new Vector3(0, 1, 1)  // 7
        };

        // Индексы треугольников
        uint[] indices = new uint[]
        {
            // Задняя грань
            0, 1, 2,
            0, 2, 3,
            // Передняя грань
            4, 6, 5,
            4, 7, 6,
            // Левая грань
            0, 3, 7,
            0, 7, 4,
            // Правая грань
            1, 5, 6,
            1, 6, 2,
            // Нижняя грань
            0, 4, 5,
            0, 5, 1,
            // Верхняя грань
            3, 2, 6,
            3, 6, 7
        };

        // Определение нормалей граней
        Vector3[] faceNormals = new Vector3[]
        {
            new Vector3(0, 0, -1), // Задняя грань
            new Vector3(0, 0, 1),  // Передняя грань
            new Vector3(-1, 0, 0), // Левая грань
            new Vector3(1, 0, 0),  // Правая грань
            new Vector3(0, -1, 0), // Нижняя грань
            new Vector3(0, 1, 0)   // Верхняя грань
        };

        // Список граней, к которым принадлежит каждая вершина
        List<int>[] vertexFaces = new List<int>[8];

        // Инициализация списков
        for (int i = 0; i < 8; i++)
        {
            vertexFaces[i] = new List<int>();
        }

        // Заполнение списков граней для каждой вершины
        for (int i = 0; i < indices.Length; i += 3)
        {
            int faceIndex = i / 6; // Каждая грань состоит из 2 треугольников (6 индексов)
            for (int j = 0; j < 3; j++)
            {
                uint vertexIndex = indices[i + j];
                if (!vertexFaces[vertexIndex].Contains(faceIndex))
                {
                    vertexFaces[vertexIndex].Add(faceIndex);
                }
            }
        }

        // Применение трансформаций
        Matrix4x4 transform = Matrix4x4.Identity;

        // Центрирование куба
        transform = Matrix4x4.Multiply(transform, Matrix4x4.CreateTranslation(-0.5f, -0.5f, -0.5f));

        // Масштабирование
        transform = Matrix4x4.Multiply(transform, Matrix4x4.CreateScale(xScale, yScale, zScale));

        // Поворот
        Matrix4x4 rotate_transform = Matrix4x4.Identity;
        if (direction != new Vector3(0, 1, 0))
        {
            Vector3 axis = Vector3.Cross(new Vector3(0, 1, 0), direction);
            float angle = MathF.Acos(Vector3.Dot(Vector3.Normalize(direction), new Vector3(0, 1, 0)));
            if (axis.LengthSquared() > 0)
            {
                rotate_transform = Matrix4x4.CreateFromAxisAngle(Vector3.Normalize(axis), angle);
                transform = Matrix4x4.Multiply(transform, rotate_transform);
            }
        }

        // Перенос в позицию
        transform = Matrix4x4.Multiply(transform, Matrix4x4.CreateTranslation(position));

        // Применение трансформаций к вершинам и добавление в меш
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 transformedPosition = Vector3.Transform(vertices[i], transform);
            mesh.Positions.Add(transformedPosition);
        }

        // Применение трансформаций к нормалям граней (только вращение)
        for (int i = 0; i < faceNormals.Length; i++)
        {
            faceNormals[i] = Vector3.Transform(faceNormals[i], rotate_transform);
        }

        // Вычисление нормалей для каждой вершины
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 normalSum = Vector3.Zero;
            foreach (int faceIndex in vertexFaces[i])
            {
                normalSum += faceNormals[faceIndex];
            }
            Vector3 averagedNormal = Vector3.Normalize(normalSum);
            mesh.Normals.Add(averagedNormal);
        }

        // Добавление индексов
        mesh.Indices.AddRange(indices);

        return mesh;
    }
}
