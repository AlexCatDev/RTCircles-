using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Easy2D;
using Realms;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using RTCircles;
class Program
{

    static void Main(string[] args)
    {
        GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
        Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;

        /*
        DBBeatmapSetInfo lol = new DBBeatmapSetInfo();
        lol.Foldername = "veryfoldername";

        lol.Beatmaps.Add(new DBBeatmapInfo() { Filename = "test123", Hash = "Weed420", SetInfo = lol });
        */
        Realm s = Realm.GetInstance("bullshit.realm");
        s.Error += (e, x) =>
        {
            Console.WriteLine(x.Exception.Message);
        };

        var set = s.All<DBBeatmapInfo>().First().SetInfo;
        s.Write(() => {
            set.Beatmaps.Add(new DBBeatmapInfo() { Filename = "test123", Hash = "Weed420727" });
        });

        /*
        DBObject k = new DBObject();
        k.ID = 0;
        k.Keys.Add("Penis");

        Realm s = Realm.GetInstance("test.db");
        s.Error += (e, x) =>
        {
            Console.WriteLine(x.Exception.Message);
        };

        var obj = s.Find<DBObject>(0).Freeze();

        new Thread(() =>
        {
            Console.WriteLine(obj.ID);
        }).Start();
        */

        //BenchmarkRunner.Run<Benchmarks>();
        Console.WriteLine("- FIN -");
        Console.ReadLine();
    }
}

public class IsBenchmarkAttribute : Attribute
{

}

public static class Benchmarker
{
    public static void Run<T>()
    {
        var objectInstance = Activator.CreateInstance(typeof(T));

        var allMethods = typeof(T).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var methodsToRun = new List<MethodInfo>();

        foreach (var method in allMethods)
        {
            var att = method.GetCustomAttribute<IsBenchmarkAttribute>();

            if (att == null)
                continue;

            methodsToRun.Add(method);
        }

        Stopwatch sw = new Stopwatch();

        foreach (var method in methodsToRun)
        {
            Console.WriteLine($"Running: {method.Name}");

            //Warmup
            for (int i = 0; i < 16; i++)
                method.Invoke(objectInstance, null);

            sw.Start();

            for (int i = 0; i < 4096; i++)
            {
                method.Invoke(objectInstance, null);
            }

            sw.Stop();

            double time = ((double)sw.ElapsedTicks / Stopwatch.Frequency) * (1000 * 1000);

            sw.Reset();

            Console.WriteLine($"Completed 4096 Operations in: {time:F4} Microseconds");
            Console.WriteLine($"TIME: {(time/4096):F4} Microseconds");
        }
    }
}

[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public unsafe class Benchmarks
{
    public const int QuadCount = 1_000_000;

    public const int Test = 100_000;

    private PrimitiveBatch<Vertex> safePrimitiveBatch = new PrimitiveBatch<Vertex>(4 * QuadCount, 6 * QuadCount);
    private UnsafePrimitiveBatch<Vertex> unsafePrimitiveBatch = new UnsafePrimitiveBatch<Vertex>(4 * QuadCount, 6 * QuadCount);

    /// <summary>
    /// Note to self: Indexing a pointer is as fast/faster than using -> then incrementing it
    /// </summary>

    [Benchmark]
    public void testSafe()
    {
        for (int i = 0; i < Test; i++)
        {
            var quad = safePrimitiveBatch.GetTriangleStrip(4);

            quad[0].TextureSlot = 0;
            quad[0].Position = new OpenTK.Mathematics.Vector2(0, 0);
            quad[0].Color = new OpenTK.Mathematics.Vector4(1, 1, 1, 1);

            quad[1].TextureSlot = 0;
            quad[1].Position = new OpenTK.Mathematics.Vector2(100, 0);
            quad[1].Color = new OpenTK.Mathematics.Vector4(1, 1, 1, 1);

            quad[2].TextureSlot = 0;
            quad[2].Position = new OpenTK.Mathematics.Vector2(100, 100);
            quad[2].Color = new OpenTK.Mathematics.Vector4(1, 1, 1, 1);

            quad[3].TextureSlot = 0;
            quad[3].Position = new OpenTK.Mathematics.Vector2(0, 100);
            quad[3].Color = new OpenTK.Mathematics.Vector4(1, 1, 1, 1);
        }

        safePrimitiveBatch.Reset();
    }

    [Benchmark]
    public void testUnsafeNoIndex()
    {
        for (int i = 0; i < Test; i++)
        {
            var quad = unsafePrimitiveBatch.GetTriangleStrip(4);

            quad->TextureSlot = 0;
            quad->Position = new OpenTK.Mathematics.Vector2(0, 0);
            quad->Color = new OpenTK.Mathematics.Vector4(1, 1, 1, 1);
            ++quad;

            quad->TextureSlot = 0;
            quad->Position = new OpenTK.Mathematics.Vector2(100, 0);
            quad->Color = new OpenTK.Mathematics.Vector4(1, 1, 1, 1);
            ++quad;

            quad->TextureSlot = 0;
            quad->Position = new OpenTK.Mathematics.Vector2(100, 100);
            quad->Color = new OpenTK.Mathematics.Vector4(1, 1, 1, 1);
            ++quad;

            quad->TextureSlot = 0;
            quad->Position = new OpenTK.Mathematics.Vector2(0, 100);
            quad->Color = new OpenTK.Mathematics.Vector4(1, 1, 1, 1);
        }

        unsafePrimitiveBatch.Reset();
    }

    [Benchmark]
    public void testUnsafe()
    {
        for (int i = 0; i < Test; i++)
        {
            var quad = unsafePrimitiveBatch.GetTriangleStrip(4);

            quad[0].TextureSlot = 0;
            quad[0].Position = new OpenTK.Mathematics.Vector2(0, 0);
            quad[0].Color = new OpenTK.Mathematics.Vector4(1, 1, 1, 1);

            quad[1].TextureSlot = 0;
            quad[1].Position = new OpenTK.Mathematics.Vector2(100, 0);
            quad[1].Color = new OpenTK.Mathematics.Vector4(1, 1, 1, 1);

            quad[2].TextureSlot = 0;
            quad[2].Position = new OpenTK.Mathematics.Vector2(100, 100);
            quad[2].Color = new OpenTK.Mathematics.Vector4(1, 1, 1, 1);

            quad[3].TextureSlot = 0;
            quad[3].Position = new OpenTK.Mathematics.Vector2(0, 100);
            quad[3].Color = new OpenTK.Mathematics.Vector4(1, 1, 1, 1);
        }

        unsafePrimitiveBatch.Reset();
    }

    public void testRectangle()
    {
        Rectangle rect = new Rectangle();

        rect.X = 15;
        rect.Y = 30;
        rect.Width = 69;
        rect.Height = 727;

        Console.WriteLine($"X: {rect.X} Y: {rect.Y} Width: {rect.Width} Height: {rect.Height}");

        Console.WriteLine($"Pos: {rect.Position} Size: {rect.Size}");

        Console.WriteLine($"Vec4: {rect.Xyzw}");

        Console.WriteLine("SizeInBytes: " + Marshal.SizeOf(rect));
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct Rectangle
    {
        [FieldOffset(0)]
        public float X;
        [FieldOffset(4)]
        public float Y;
        [FieldOffset(8)]
        public float Width;
        [FieldOffset(12)]
        public float Height;

        [FieldOffset(0)]
        public Vector2 Position;

        [FieldOffset(8)]
        public Vector2 Size;

        [FieldOffset(0)]
        public Vector4 Xyzw;
    }
}