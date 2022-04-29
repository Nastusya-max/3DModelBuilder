using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.Media;
using ModelBuilder.Builders.DualContouring;
using ModelBuilder.Core.Extensions;
using ModelBuilder.Core.Interfaces;
using ModelBuilder.Core.Models;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace ModelBuilder.Surfaces.DicomSurface.UnitTests
{
    //class SphereFunction : ISurfaceFunction
    //{
    //    public Vector3 GetNormal(in Point point)
    //    {
    //        var l = MathF.Sqrt(MathF.Pow(point.X, 2) + MathF.Pow(point.Y, 2) + MathF.Pow(point.Z, 2));
    //        return new Vector3(point.X / l, point.Y / l, point.Z / l);
    //    }

    //    public float GetValue(in Point point)
    //    {
    //        return MathF.Sqrt(MathF.Pow(point.X, 2) + MathF.Pow(point.Y, 2) + MathF.Pow(point.Z, 2)) - 2.5f;
    //    }
    //}

    //public class TorFunction : ApproximatingNormalSurfaceFunction
    //{
    //    public TorFunction() : base(0.01f)
    //    {
    //    }

    //    public override Vector3 GetNormal(in Point point)
    //    {
    //        var t = base.GetNormal(point);
    //        lock (DicomSurface.normals)
    //        {
    //            DicomSurface.normals.Add(new KeyValuePair<Point, Vector3>(point, t));
    //        }

    //        return t;
    //    }

    //    public override float GetValue(in Point point)
    //    {
    //        var R = 60;
    //        var r = 30;
    //        return MathF.Pow(MathF.Pow(point.X, 2) + MathF.Pow(point.Y, 2) + MathF.Pow(point.Z, 2) + MathF.Pow(R, 2) - MathF.Pow(r, 2), 2) - 4.0f * MathF.Pow(R, 2) * (MathF.Pow(point.X, 2) + MathF.Pow(point.Y, 2));
    //    }
    //}

    public class Tests
    {
        [SetUp]
        public void Setup()
        {
            new DicomSetupBuilder()
                .RegisterServices(x => x.AddImageManager<ImageSharpImageManager>())
                .Build();
        }


        [Test]
        public async Task Build_WhenFunctionWithSharpEdgesWasUsed_ShouldBuildCorrectModel()
        {
            Assert.Pass();
        }

        [Test]
        public async Task Build_WhenFunctionWithSmoothEdgesWasUsed_ShouldBuildCorrectModel()
        {
            Assert.Pass();
        }

        [Test]
        public async Task GetValue_WhenPointOutsideTheModelWasPassed_ShouldReturnPositiveValue()
        {
            Assert.Pass();
        }

        [Test]
        public async Task GetValue_WhenPointInsideTheModelWasPassed_ShouldReturnNegativeValue()
        {
            Assert.Pass();
        }

        [Test]
        public async Task GetValue_WhenPointOnTheModelWasPassed_ShouldReturnZeroValue()
        {
            Assert.Pass();
        }

        [Test]
        public async Task GetNormal_WhenPointWasPassed_ShouldReturnCorrectNormal()
        {
            Assert.Pass();
        }

        [Test]
        public async Task Test1()
        {
            var opt = DualContouringBuilderOptions.Default;
            opt.ChangePointSelectionMode = ChangePointSelectionMode.BestPointSelection;
            opt.CellSize = 2.5f;
            //opt.MaxDegreeOfParallelism = 1;

            var builder = new DualContouringBuilder(opt);

            var dir = await DicomDirectory.OpenAsync(@"E:\scull\DICOMDIR");

            var suropt = DicomSurfaceOptions.Default;
            suropt.NormalSelectionAccuracyInsideLayer = 2.5f;
            suropt.ActivationThreshold = 200;

            var reader = new DicomDirectoryReader(dir, suropt);
            var pat = reader.GetAllPatients();

            var surface = await reader.GetSurfaceAsync("1.2.840.113619.2.55.3.2831185921.880.1606495147.595");

            //var tests = new TorFunction();
            var testb = new BuildBounds
            {
                MinZ = -100,
                MinX = -100,
                MinY = -100,
                MaxX = 100,
                MaxY = 100,
                MaxZ = 100,
            };

            var mesh = builder.Build(surface, surface.SurfaceBounds);
            mesh.WriteToObj(@"C:\Users\Junjinjen\Desktop\test.obj");

            var lines = File.ReadAllLines(@"C:\Users\Junjinjen\Desktop\test.obj");

            var vertices = lines.Where(x => x.StartsWith('v'));
            var indices = lines.Where(x => x.StartsWith('f'));

            var normals = DicomSurface.normals.Select(x => new KeyValuePair<Point, Vector3>(x.Key, x.Value * 2))
                .Select(x => new { x.Key, Value = new Point(x.Key.X + x.Value.X, x.Key.Y + x.Value.Y, x.Key.Z + x.Value.Z) })
                .SelectMany(x => new[]
                {
                    $"v {x.Key.X.ToString(CultureInfo.InvariantCulture)} {x.Key.Y.ToString(CultureInfo.InvariantCulture)} {x.Key.Z.ToString(CultureInfo.InvariantCulture)}",
                    $"v {x.Value.X.ToString(CultureInfo.InvariantCulture)} {x.Value.Y.ToString(CultureInfo.InvariantCulture)} {x.Value.Z.ToString(CultureInfo.InvariantCulture)}"
                });

            var maxIndex = indices.SelectMany(x => x.Remove(0, 2).Split(' '))
                .Select(x => int.Parse(x))
                .Max();
            maxIndex++;

            var normalindices = DicomSurface.normals.Select(x => $"l {maxIndex++} {maxIndex++}");

            File.WriteAllLines(@"C:\Users\Junjinjen\Desktop\test.obj", vertices.Concat(normals).Concat(indices).Concat(normalindices));
        }
    }
}