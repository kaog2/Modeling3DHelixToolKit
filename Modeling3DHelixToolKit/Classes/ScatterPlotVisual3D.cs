using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Modeling3DHelixToolKit
{
    public class ScatterPlotVisual3D : ModelVisual3D
    {
        private readonly ModelVisual3D visualChild;

        // todo: make Dependency properties
        public double IntervalX { get; set; }
        public double IntervalY { get; set; }
        public double IntervalZ { get; set; }
        public double FontSize { get; set; }
        //public double SphereSize { get; set; }
        public double LineThickness { get; set; }

        Brush SurfaceBrush = GradientBrushes.RainbowStripes;
        public ScatterPlotVisual3D()
        {
            IntervalX = 10;
            IntervalY = 40;
            IntervalZ = 50;
            FontSize = 0.06;
            //SphereSize = 0.06;
            LineThickness = 0.01;

            visualChild = new ModelVisual3D();
            Children.Add(visualChild);
        }

        public Model3D CreateModel(Point3D[] Points, Material material, double sphereSize, int thetaDiv, int phiDiv)
        {
            var plotModel = new Model3DGroup();
            
            var scatterMeshBuilder = new MeshBuilder(true, true);

            for (var i = 0; i < Points.Length; ++i)
            {
                scatterMeshBuilder.AddSphere(Points[i], sphereSize, thetaDiv, phiDiv);
            }

            //var scatterModel = new GeometryModel3D(scatterMeshBuilder.ToMesh(), MaterialHelper.CreateMaterial(SurfaceBrush, null, null, 1, 0));
            var scatterModel = new GeometryModel3D(scatterMeshBuilder.ToMesh(), material);
            scatterModel.BackMaterial = scatterModel.Material;

            // create bounding box with axes indications
            var axesMeshBuilder = new MeshBuilder();

            //var axesModel = new GeometryModel3D(axesMeshBuilder.ToMesh(), Materials.Red);

            plotModel.Children.Add(scatterModel);
            //plotModel.Children.Add(axesModel);

            return plotModel;
        }
    }
}
