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
using System.Security.Cryptography;
using System.Text;

class Program
{

    static void Main(string[] args)
    {
        GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
        Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
        /*
        Realm s = Realm.GetInstance("bullshit.realm");
        s.Error += (e, x) =>
        {
            Console.WriteLine(x.Exception.Message);
        };

        var set = s.All<DBBeatmapInfo>().First().SetInfo;
        s.Write(() => {
            set.Beatmaps.Add(new DBBeatmapInfo() { Filename = "test123", Hash = "Weed420727" });
        });
        */
        BenchmarkRunner.Run<Benchmarks>();
        Console.WriteLine("- FIN -");
        Console.ReadLine();
    }
}

public class IsBenchmarkAttribute : Attribute
{
    public int RunCount { get; private set; } = 1;

    public IsBenchmarkAttribute(int runCount)
    {
        RunCount = runCount;
    }
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

    private MD5 md5;
    private SHA256 sha256;

    private byte[] testBeatmap;

    public Benchmarks()
    {
        md5 = MD5.Create();
        md5.Initialize();

        sha256 = SHA256.Create();
        sha256.Initialize();

        using(var fs = File.OpenRead(@"C:\Users\user\Desktop\osu!\Songs\1074598 Slax - 90's Girly Tekno Beat EP\Slax - 90's Girly Tekno Beat EP (Seamob) [Hardtaro].osu"))
        {
            testBeatmap = new byte[fs.Length];
            fs.Read(testBeatmap, 0, testBeatmap.Length);
        }
    }

    public const int QuadCount = 1_000_000;

    public const int Test = 100_000;

    private PrimitiveBatch<Vertex> safePrimitiveBatch = new PrimitiveBatch<Vertex>(4 * QuadCount, 6 * QuadCount);
    private UnsafePrimitiveBatch<Vertex> unsafePrimitiveBatch = new UnsafePrimitiveBatch<Vertex>(4 * QuadCount, 6 * QuadCount);

    /// <summary>
    /// Note to self: Indexing a pointer is as fast/faster than using -> then incrementing it
    /// </summary>

    //[Benchmark]
    public void testSafe()
    {
        for (int i = 0; i < Test; i++)
        {
            var quad = safePrimitiveBatch.GetTriangleStrip(4);

            quad[0].TextureSlot = 0;
            quad[0].Position = new Vector2(0, 0);
            quad[0].Color = new Vector4(1, 1, 1, 1);

            quad[1].TextureSlot = 0;
            quad[1].Position = new Vector2(100, 0);
            quad[1].Color = new Vector4(1, 1, 1, 1);

            quad[2].TextureSlot = 0;
            quad[2].Position = new Vector2(100, 100);
            quad[2].Color = new Vector4(1, 1, 1, 1);

            quad[3].TextureSlot = 0;
            quad[3].Position = new Vector2(0, 100);
            quad[3].Color = new Vector4(1, 1, 1, 1);
        }

        safePrimitiveBatch.Reset();
    }

    //[Benchmark]
    public void testUnsafeNoIndex()
    {
        for (int i = 0; i < Test; i++)
        {
            var quad = unsafePrimitiveBatch.GetTriangleStrip(4);

            quad->TextureSlot = 0;
            quad->Position = new Vector2(0, 0);
            quad->Color = new Vector4(1, 1, 1, 1);
            ++quad;

            quad->TextureSlot = 0;
            quad->Position = new Vector2(100, 0);
            quad->Color = new Vector4(1, 1, 1, 1);
            ++quad;

            quad->TextureSlot = 0;
            quad->Position = new Vector2(100, 100);
            quad->Color = new Vector4(1, 1, 1, 1);
            ++quad;

            quad->TextureSlot = 0;
            quad->Position = new Vector2(0, 100);
            quad->Color = new Vector4(1, 1, 1, 1);
        }

        unsafePrimitiveBatch.Reset();
    }

    //[Benchmark]
    public void testUnsafe()
    {
        for (int i = 0; i < Test; i++)
        {
            var quad = unsafePrimitiveBatch.GetTriangleStrip(4);

            quad[0].TextureSlot = 0;
            quad[0].Position = new Vector2(0, 0);
            quad[0].Color = new Vector4(1, 1, 1, 1);

            quad[1].TextureSlot = 0;
            quad[1].Position = new Vector2(100, 0);
            quad[1].Color = new Vector4(1, 1, 1, 1);

            quad[2].TextureSlot = 0;
            quad[2].Position = new Vector2(100, 100);
            quad[2].Color = new Vector4(1, 1, 1, 1);

            quad[3].TextureSlot = 0;
            quad[3].Position = new Vector2(0, 100);
            quad[3].Color = new Vector4(1, 1, 1, 1);
        }

        unsafePrimitiveBatch.Reset();
    }
}