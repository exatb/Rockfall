using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Net;

namespace Rockfall
{
    public class RigidBody : IDisposable
    {
        // Геометрические данные
        private int _vao;
        private int _vbo;
        private int _ebo;
        private float[] _vertices;
        private uint[] _indices;

        // Материал (цвет)
        public Vector3 Color { get; set; }

        // Матрица модели
        public Matrix4 ModelMatrix { get; private set; }

        // Параметры трансформации при инициализации
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; } = Quaternion.Identity;
        public Vector3 Scale { get; set; } = Vector3.One;

        //Динамически вычисляемые геометрические параметры вычисляется при вызове Update
        public Vector3 LocalMidPoint { get; set; } // Середина тела в локальных координатах                 
        public Vector3 MidPoint { get; set; } // Середина тела в мировых координатах                 
        public Vector3[] WorldVertices { get; private set; } // Мировые координаты вершин
        public Vector3[] LocalVertices { get; private set; } // Локальные координаты вершин

        //Динамически вычисляемые параметры движения
        public const double SubZero = 0.000001;    //Величина, принимаемая за ноль
        public float t;            //Время
        public float dt;           //Шаг по времени
        public Vector3 p;          //Текущее положение тела в локальных координатах (радиус-вектор), начинается всегда с 0
        public Vector3 v;          //Скорость тела
        public Vector3 w;          //Угловая скорость тела (в псевдо-векторе углы поворота в радианах/c)
        public Quaternion q;       //Текущий поворот тела, представленный кватернионом, начинается с нулевого поворота
        private Vector3 g;          //Ускорение свободного падения
        private Vector3 g_dir;      //Направление ускорения свободного падения
        public Vector3 G
        {
            get { return g; }
            set
            {
                g = value;
                g_dir = g;
                g_dir.Normalize();
            }
        }

        public bool Static = false; //Определяет, будет ли тело перемещаться, если тело неподвижно, то масса=момент инерции=бесконечность

        private float kf = 0.1f;    //Коэфициент вязкого трения 0..1 (0 - трение отсуствует, 1-трение максимлаьное)
        public float FrictionFactor { get { return kf; } set { kf = value; } }

        private float max_slope = 0.97f; //Максимальный угол наклона относительно вектора g, начиная с которого трение покоя переходит в трение скольжения  
        //Устанавливаем максимальный угол наклона относительно вектора g в градусах, начиная с которого трение покоя переходит в трение скольжения  
        public float MaxSlopeAngle
        {
            get { return max_slope; }
            set
            {
                max_slope = (float)Math.Cos(value * Math.PI / 180);
            }
        }

        private float m;           //Масса тела
        private float im;          //Инвертированная масса тела        
        //Возвращает и устанавливает массу тела
        public float M
        {
            get { return m; }
            set
            {
                m = value;
                if (Static)
                    im = 0;
                else
                    im = 1 / m;
            }
        }

        private float inertia;      //Момент инерции тела, усредненный по всем направлениям (сильное упрощение)
        private float ii;           //Инвертированный момент инерции тела
        //Возвращает и устанавливает момент инерции тела
        public float I
        {
            get { return inertia; }
            set
            {
                inertia = value;
                if (Static)
                    ii = 0;
                else
                    ii = 1 / inertia;
            }
        }


        //Расчет тензора инерции для тела
        public void CalculateMoment()
        {
            int cnt = LocalVertices.Length;
            float mas = m / cnt; //Масса каждой точки
            //Расчитаем моменты инерции относительно осей вращения и средней точки тела
            float Ix = 0;
            float Iy = 0;
            float Iz = 0;
            foreach (Vector3 p in LocalVertices)
            {
                Vector3 px = (Vector3)(p - LocalMidPoint);
                px.X = 0;
                Ix += mas * Vector3.Dot(px, px);
                Vector3 py = (Vector3)(p - LocalMidPoint);
                py.Y = 0;
                Iy += mas * Vector3.Dot(py, py);
                Vector3 pz = (Vector3)(p - LocalMidPoint);
                pz.Z = 0;
                Iz += mas * Vector3.Dot(pz, pz);
            }
            Vector3 kin = new Vector3(Ix, Iy, Iz); //Тензор инерции
            I = kin.Length; //Усредненный по всем направлениям момент инерции - для упрощения вычислений
        }

        public RigidBody(float[] vertices, uint[] indices, Vector3 color, bool staticBody)
        {
            _vertices = vertices;
            _indices = indices;
            Color = color;
            InitializeBuffers();
            Static = staticBody;
            WorldVertices = new Vector3[_vertices.Length / 6]; // 6 компонентов на вершину
            LocalVertices = new Vector3[_vertices.Length / 6]; // 6 компонентов на вершину
            p = new Vector3(0,0,0);
            v = new Vector3(0,0,0);
            w = new Vector3(0,0,0);
            q = new Quaternion(0,0,0,1);
            G = new Vector3(0, -1000, 0);
            M = 10f;

            Vector3 temp = Vector3.Zero;

            int vertexCount = _vertices.Length / 6; // 6 компонентов на вершину

            for (int i = 0; i < vertexCount; i++)
            {
                LocalVertices[i] = new Vector3(
                    _vertices[i * 6],
                    _vertices[i * 6 + 1],
                    _vertices[i * 6 + 2]);

                temp += LocalVertices[i];
            }

            LocalMidPoint = temp / vertexCount;

            CalculateMoment();
            MaxSlopeAngle = 5f;
            FrictionFactor = 0.1f;
        }

        private void InitializeBuffers()
        {
            // Генерация и настройка буферов
            _vao = GL.GenVertexArray();
            _vbo = GL.GenBuffer();
            _ebo = GL.GenBuffer();

            GL.BindVertexArray(_vao);

            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices, BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, _indices.Length * sizeof(uint), _indices, BufferUsageHint.StaticDraw);

            int stride = 6 * sizeof(float); // Позиция (3 float) + нормаль (3 float)

            // Позиция атрибута 0: позиции вершин
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
            GL.EnableVertexAttribArray(0);

            // Позиция атрибута 1: нормали вершин
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            GL.BindVertexArray(0);
        }

        public void Update(float deltaTime)
        {
            dt = deltaTime;
            t += dt;

            if (!Static)
            {
                
                //Расчитаем линейное перемещение
                Vector3 f = g * m; //Формируем суммарную силу тяжести
                Vector3 a = f / m;
                v += a * dt;
                p += v * dt;

                //Расчитаем угловое перемещение
                Vector3 qdt = w * dt / 2;
                q += (new Quaternion(qdt.X, qdt.Y, qdt.Z, 0)) * q;
                q.Normalize();

                //Проверим на валидность полученные величины                 
                if (double.IsNaN(p.X) || double.IsNaN(p.Y) || double.IsNaN(p.Z))
                {
                    Static = true;
                    //throw new InvalidOperationException("p==NaN"); //???????????????????????????????
                }
                if (double.IsInfinity(p.X) || double.IsInfinity(p.Y) || double.IsInfinity(p.Z))
                {
                    Static = true;
                    //throw new InvalidOperationException("p==Inf"); //???????????????????????????????
                }

            }
            if (Static)   
            {
                    //Если тело статичное - все обнуляем
                    v = new Vector3(0, 0, 0);
                    w = new Vector3(0, 0, 0);
                    p = new Vector3(0, 0, 0);
                    q = new Quaternion(0,0,0,1);
            }

            // Обновление матрицы модели
            ModelMatrix = Matrix4.CreateTranslation(-LocalMidPoint) *
                          Matrix4.CreateScale(Scale) *
                          Matrix4.CreateFromQuaternion(q * Rotation) *
                          Matrix4.CreateTranslation(LocalMidPoint) *
                          Matrix4.CreateTranslation(Position + p);

            // Обновление мировых координат вершин
            UpdateWorldVertices();
        }

        
        //Решатель столкновения 2х тел в одной точке, за основу взяты выкладки из книги David H. Eberly "Game Physics Second Edition"
        //примененена стабилизации Баумгарте 
        //параметры:
        //тела a и b, p - точка контакта, n - направление выхода из коллизии для тела a (нормаль контакта, направленная от b к a)
        //e - от 0 до 1, определяет потерю кинетической энергии при столкновении (1-без потерь, абсолютно упругое)
        //d - глубина проникновения тел
        public void ContactSolver(RigidBody a, RigidBody b, Vector3 p, Vector3 n, float e, float d)
        {
            Vector3 ra = p - a.MidPoint;
            Vector3 rb = p - b.MidPoint;
            Vector3 wa = Vector3.Cross(a.w, ra); //скорость точки a, обусловленная вращением
            Vector3 wb = Vector3.Cross(b.w, rb); //скорость точки b, обусловленная вращением
            Vector3 va = a.v + wa; //полная скорость точки a
            Vector3 vb = b.v + wb; //полная скорость точки b
            Vector3 adv = new Vector3(0, 0, 0); //Изменение скорости тела a
            Vector3 adw = new Vector3(0, 0, 0); //Изменение угловой скорости тела a
            Vector3 bdv = new Vector3(0, 0, 0); //Изменение скорости тела b
            Vector3 bdw = new Vector3(0, 0, 0); //Изменение угловой скорости тела b

            //Для исключения численных ошибок зададим явно значения для неподвижных тел
            if (a.Static)
            {
                ra = new Vector3(0, 0, 0);
                va = new Vector3(0, 0, 0);
            }
            if (b.Static)
            {
                rb = new Vector3(0, 0, 0);
                vb = new Vector3(0, 0, 0);
            }

            Vector3 vrel = va - vb; //относительная скорость точек (от b к a)

            //Сначала решим в направлении нормали
            float vreln = Vector3.Dot(vrel, n); //проекция относительной скорости на n

            if ((Math.Abs(vreln) > SubZero) && (vreln < 0))
            {
                //Если скорость не нулевая и тела движутся в направлении взаимного проникновения 

                Vector3 tmpa = a.ii * Vector3.Cross(Vector3.Cross(ra, n), ra);
                Vector3 tmpb = b.ii * Vector3.Cross(Vector3.Cross(rb, n), rb);

                if (a.Static)
                {
                    tmpa = new Vector3(0, 0, 0);
                }

                if (b.Static)
                {
                    tmpb = new Vector3(0, 0, 0);
                }

                float Jn = ((0.01f * d / dt) - (e + 1) * vreln) / (a.im + b.im + Vector3.Dot(tmpa, n) + Vector3.Dot(tmpb, n));

                adv += a.im * Jn * n;
                bdv -= b.im * Jn * n;

                adw += a.ii * Vector3.Cross(ra, Jn * n);
                bdw += b.ii * Vector3.Cross(rb, -Jn * n);
            }

            //Теперь решим в перпендикулярном направлении (в плоскости контакта)
            Vector3 vectorFromPointToVector = vrel - p;
            float distance = Vector3.Dot(vectorFromPointToVector, n) / Vector3.Dot(n, n);
            Vector3 projection = vectorFromPointToVector - distance * n;
            Vector3 vrel_plane = projection + p; //Проекция относительной скорости тел в плоскость контакта 
            float vrel_plane_len = vrel_plane.Length;
            if (vrel_plane_len > SubZero) //Если есть значимая перпендикулярная составляющая относительной скорости в плоскости контакта
            {
                Vector3 t = vrel_plane / vrel_plane_len; //Нормаль в плоскости контакта (тангенциальная составляющая)

                Vector3 tmpa = a.ii * Vector3.Cross(Vector3.Cross(ra, t), ra);
                Vector3 tmpb = b.ii * Vector3.Cross(Vector3.Cross(rb, t), rb);

                if (a.Static)
                {
                    tmpa = new Vector3(0, 0, 0);
                }

                if (b.Static)
                {
                    tmpb = new Vector3(0, 0, 0);
                }

                float ndotg = Vector3.Dot(n, g_dir); //определим насколько вектор нормали контакта отклонился от вектора гравитации
                float Jt;
                if (Math.Abs(ndotg) > max_slope)
                    //Простейшая имитация трения покоя через полное ограничение движения в перпендикулярном направлении
                    Jt = (-vrel_plane_len) / (a.im + b.im + Vector3.Dot(tmpa, t) + Vector3.Dot(tmpb, t));
                else
                    //Простейшая имитация трения скольжения через частичное ограничение движения в перпендикулярном направлении
                    Jt = (-vrel_plane_len * kf) / (a.im + b.im + Vector3.Dot(tmpa, t) + Vector3.Dot(tmpb, t));

                adv += a.im * Jt * t;
                bdv -= b.im * Jt * t;

                adw += a.ii * Vector3.Cross(ra, Jt * t);
                bdw += b.ii * Vector3.Cross(rb, -Jt * t);
            }

            a.v += adv;
            a.w += adw;
            b.v += bdv;
            b.w += bdw;
        }

        private void UpdateWorldVertices()
        {
            Matrix4 model = ModelMatrix;
            Vector3 temp = Vector3.Zero;

            int vertexCount = _vertices.Length / 6; // 6 компонентов на вершину

            for (int i = 0; i < vertexCount; i++)
            {
                Vector3 localVertex = new Vector3(
                    _vertices[i * 6],
                    _vertices[i * 6 + 1],
                    _vertices[i * 6 + 2]);

                Vector4 worldVertex = Vector4.TransformRow(new Vector4(localVertex, 1.0f), model);

                WorldVertices[i] = new Vector3(worldVertex.X, worldVertex.Y, worldVertex.Z);
                temp += WorldVertices[i];
            }

            MidPoint = temp / vertexCount;
        }

        public void Render(Shader shader)
        {
            shader.Use();

            // Передача матрицы модели в шейдер
            shader.SetMatrix4("model", ModelMatrix);

            // Передача цвета в шейдер
            shader.SetVector3("objectColor", Color);

            // Привязка VAO и отрисовка
            GL.BindVertexArray(_vao);
            GL.DrawElements(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedInt, 0);
        }

        public CrossInfo3D IsCollidingWith(RigidBody body)
        {
            CrossInfo3D ci = new CrossInfo3D();
            ci.Cross = GJK_EPA_BCP.CheckIntersection(WorldVertices, body.WorldVertices, out ci.MidPoint, out ci.Deep, out ci.Out);
            return ci;
        }

        public void Dispose()
        {
            GL.DeleteVertexArray(_vao);
            GL.DeleteBuffer(_vbo);
            GL.DeleteBuffer(_ebo);
        }
    }


    //Класс содержащий информацию о пересечении двух тел   
    public class CrossInfo3D : ICloneable
    {
        //Эти переменные заполняются при тестировании на пересечение тел
        public bool Cross = false;     //Факт пересечения
        public float Deep;             //Глубина проникновения
        public Vector3 Out;            //Направление перемещения данного тела (единичный вектор) для выхода из пересечения, он же нормаль в точке контакта
        public Vector3 MidPoint;       //Средняя точка пересечения

        public CrossInfo3D()
        {
            Deep = 0;
            Out = new Vector3(0, 0, 0);
            MidPoint = new Vector3(0, 0, 0);
        }

        //Копирует объект
        public CrossInfo3D(CrossInfo3D ci)
        {
            Cross = ci.Cross;
            Deep = ci.Deep;
            Out = new Vector3(ci.Out.X, ci.Out.Y, ci.Out.Z);
            MidPoint = new Vector3(ci.MidPoint.X, ci.MidPoint.Y, ci.MidPoint.Z);
        }

        public object Clone()
        {
            return new CrossInfo3D(this);
        }

        public void Clear()
        {
            Cross = false;
            Deep = 0;
            Out = new Vector3(0, 0, 0);
            MidPoint = new Vector3(0, 0, 0);
        }
    }

}
