using NatNetML;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RhinoNatNet
{
    public class RNNContext
    {
        private static NatNetClientML client;

        private static List<Point3d> markers;
        private static List<float> marker_sizes;
        private static int counter;
        private static Plane plane;
        private static Transform xform;
        public static bool DisplayCoords = true;

        // shared memory stuff
        private static MemoryMappedFile mmf;
        private static MemoryMappedViewAccessor access;
        private static int buffer_size = sizeof(double) * 9;

        static Rhino.RhinoDoc doc;

        public RNNContext()
        {
            doc = Rhino.RhinoDoc.ActiveDoc;
            markers = new List<Point3d>();
            marker_sizes = new List<float>();
            counter = 0;

            SetPlane(Plane.WorldXY);
        }

        ~RNNContext()
        {
            client = null;
        }

        public static bool IsConnected { get; private set; }

        public void TryConnect()
        {
            Rhino.RhinoApp.Write("Attemping to connect to NatNet server... ");
            client = new NatNetClientML();
            int res = client.Initialize("127.0.0.1", "127.0.0.1");
            if (res != 0)
            {
                Rhino.RhinoApp.WriteLine("Failed.");
                client = null;
                IsConnected = false;
                return;
            }
            Rhino.RhinoApp.WriteLine("Success.");
            IsConnected = true;


            // testing only
            /*
            System.Random rnd = new Random();
            for (int i = 0; i < 10; ++i)
            {
                markers.Add(new Point3d((rnd.NextDouble() - 0.5) * 500.0, (rnd.NextDouble() - 0.5) * 500.0, (rnd.NextDouble() - 0.5) * 500.0));
            }
            */

            client.OnFrameReady += GetMarkersCallback;
            Rhino.Display.DisplayPipeline.PostDrawObjects += DisplayPipeline_DrawMarkers;
            Rhino.Display.DisplayPipeline.CalculateBoundingBox += DisplayPipeline_MarkersBoundingBox;

            Rhino.RhinoDoc.ActiveDoc.Views.Redraw();
        }

        private static void DisplayPipeline_MarkersBoundingBox(object sender, Rhino.Display.CalculateBoundingBoxEventArgs e)
        {
            BoundingBox bb = new BoundingBox(markers);
            bb.Union(plane.Origin);
            bb.Union(plane.Origin + plane.XAxis * 110.0);
            bb.Union(plane.Origin + plane.YAxis * 110.0);
            bb.Union(plane.Origin + plane.ZAxis * 110.0);

            e.IncludeBoundingBox(bb);
        }

        private static void DisplayPipeline_DrawMarkers(object sender, Rhino.Display.DrawEventArgs e)
        {
            counter++;

            //for (int i = 0; i < markers.Count; ++i)
            //    e.Display.DrawPoint(markers[i], Rhino.Display.PointStyle.X, (int)marker_sizes[i], System.Drawing.Color.Red);

            e.Display.DrawPoints(markers, Rhino.Display.PointStyle.X, 3, System.Drawing.Color.Red);
            if (DisplayCoords)
                for (int i = 0; i < markers.Count; ++i)
                    e.Display.Draw2dText(string.Format(CultureInfo.InvariantCulture, "   {0:0.0} {1:0.0} {2:0.0} ", markers[i].X, markers[i].Y, markers[i].Z), System.Drawing.Color.Red, markers[i], false, 12);

            e.Display.DrawArrows(new Line[]
            {
                new Line(plane.Origin, plane.Origin + plane.XAxis * 100.0),
                new Line(plane.Origin, plane.Origin + plane.YAxis * 100.0),
                new Line(plane.Origin, plane.Origin + plane.ZAxis * 100.0)
            }
            , System.Drawing.Color.Gray);

            //e.Display.DrawArrow(new Line(plane.Origin, plane.Origin + plane.XAxis * 100.0), System.Drawing.Color.Red);
            //e.Display.DrawArrow(new Line(plane.Origin, plane.Origin + plane.YAxis * 100.0), System.Drawing.Color.Green);
            //e.Display.DrawArrow(new Line(plane.Origin, plane.Origin + plane.ZAxis * 100.0), System.Drawing.Color.Blue);

            e.Display.Draw2dText("x", System.Drawing.Color.Gray, plane.Origin + plane.XAxis * 105.0, true, 12);
            e.Display.Draw2dText("y", System.Drawing.Color.Gray, plane.Origin + plane.YAxis * 105.0, true, 12);
            e.Display.Draw2dText("z", System.Drawing.Color.Gray, plane.Origin + plane.ZAxis * 105.0, true, 12);
            e.Display.Draw2dText("RhinoNatNet", System.Drawing.Color.Gray, plane.Origin, true, 12);
        }

        private static void DisplayPipeline_DrawPointcloud(object sender, Rhino.Display.DrawEventArgs e)
        {
            counter++;

            //for (int i = 0; i < markers.Count; ++i)
            //    e.Display.DrawPoint(markers[i], Rhino.Display.PointStyle.X, (int)marker_sizes[i], System.Drawing.Color.Red);

            e.Display.DrawPoints(markers, Rhino.Display.PointStyle.X, 3, System.Drawing.Color.Red);

            e.Display.DrawArrows(new Line[]
            {
                new Line(plane.Origin, plane.Origin + plane.XAxis * 100.0),
                new Line(plane.Origin, plane.Origin + plane.YAxis * 100.0),
                new Line(plane.Origin, plane.Origin + plane.ZAxis * 100.0)
            }
            , System.Drawing.Color.Gray);

            //e.Display.DrawArrow(new Line(plane.Origin, plane.Origin + plane.XAxis * 100.0), System.Drawing.Color.Red);
            //e.Display.DrawArrow(new Line(plane.Origin, plane.Origin + plane.YAxis * 100.0), System.Drawing.Color.Green);
            //e.Display.DrawArrow(new Line(plane.Origin, plane.Origin + plane.ZAxis * 100.0), System.Drawing.Color.Blue);

            e.Display.Draw2dText("x", System.Drawing.Color.Gray, plane.Origin + plane.XAxis * 105.0, true, 12);
            e.Display.Draw2dText("y", System.Drawing.Color.Gray, plane.Origin + plane.YAxis * 105.0, true, 12);
            e.Display.Draw2dText("z", System.Drawing.Color.Gray, plane.Origin + plane.ZAxis * 105.0, true, 12);
            e.Display.Draw2dText("RhinoNatNet", System.Drawing.Color.Gray, plane.Origin, true, 12);
        }

        public List<Point3d> GetMarkers()
        {
            return markers;
        }

        public static void GetMarkersCallback(FrameOfMocapData frame, NatNetClientML client)
        {
            markers = new List<Point3d>();
            for (int i = 0; i < frame.nOtherMarkers; ++i)
            {
                Point3d pt = new Point3d(frame.OtherMarkers[i].x * 1000.0, frame.OtherMarkers[i].y * 1000.0, frame.OtherMarkers[i].z * 1000.0);
                pt.Transform(xform);
                markers.Add(pt);
                marker_sizes.Add(1.0f);
                //marker_sizes.Add(frame.OtherMarkers[i].size);
            }

            Rhino.RhinoDoc.ActiveDoc.Views.Redraw();
        }

        public void SetPlane(Plane p)
        {
            plane = p;
            xform = Transform.PlaneToPlane(Plane.WorldXY, plane);
            //SetSharedMem();
            Rhino.RhinoDoc.ActiveDoc.Views.Redraw();
        }

        private void SetSharedMem()
        {
            if (mmf == null)
            {
                try
                {
                    mmf = MemoryMappedFile.CreateOrOpen("RNN_Plane", buffer_size);
                    Console.WriteLine("Created memory map.");
                }
                catch (System.IO.FileNotFoundException)
                {
                    Console.WriteLine("Failed to find shared memory block.");
                    return;
                }
            }

            using (access = mmf.CreateViewAccessor(0, buffer_size))
            {
                byte[] bytes = new byte[buffer_size];
                Array.Copy(BitConverter.GetBytes(plane.Origin.X), 0, bytes, sizeof(double) * 0, sizeof(double));
                Array.Copy(BitConverter.GetBytes(plane.Origin.Y), 0, bytes, sizeof(double) * 1, sizeof(double));
                Array.Copy(BitConverter.GetBytes(plane.Origin.Z), 0, bytes, sizeof(double) * 2, sizeof(double));
                Array.Copy(BitConverter.GetBytes(plane.XAxis.X), 0, bytes, sizeof(double) * 3, sizeof(double));
                Array.Copy(BitConverter.GetBytes(plane.XAxis.Y), 0, bytes, sizeof(double) * 4, sizeof(double));
                Array.Copy(BitConverter.GetBytes(plane.XAxis.Z), 0, bytes, sizeof(double) * 5, sizeof(double));
                Array.Copy(BitConverter.GetBytes(plane.YAxis.X), 0, bytes, sizeof(double) * 6, sizeof(double));
                Array.Copy(BitConverter.GetBytes(plane.YAxis.Y), 0, bytes, sizeof(double) * 7, sizeof(double));
                Array.Copy(BitConverter.GetBytes(plane.YAxis.Z), 0, bytes, sizeof(double) * 8, sizeof(double));

                access.WriteArray(0, bytes, 0, buffer_size);
            }
        }

        public static void CreateLayers()
        {
            #region Root Layer
            Rhino.DocObjects.Layer RNN_Layer;
            int root_index = doc.Layers.Find("RhinoNatNet", false);
            if (root_index < 0)
            {
                //Rhino.RhinoApp.WriteLine("Couldn't find RhinoNatNet layer.");
                RNN_Layer = new Rhino.DocObjects.Layer();
                RNN_Layer.Name = "RhinoNatNet";
                RNN_Layer.Color = System.Drawing.Color.DarkRed;

                root_index = doc.Layers.Add(RNN_Layer);
                RNN_Layer.CommitChanges();
            }
            //RNN_Layer = doc.Layers[root_index];
            #endregion

            #region Marker Layer
            Rhino.DocObjects.Layer RNN_Markers_Layer;

            int index = doc.Layers.Find("RNN_Markers", true);
            if (index < 0)
            {
                RNN_Markers_Layer = new Rhino.DocObjects.Layer();
                RNN_Markers_Layer.Name = "RNN_Markers";
                RNN_Markers_Layer.ParentLayerId = doc.Layers[root_index].Id;
                RNN_Markers_Layer.Color = System.Drawing.Color.Lime;

                index = doc.Layers.Add(RNN_Markers_Layer);
            }
            doc.Layers[index].ParentLayerId = doc.Layers[root_index].Id;
            doc.Layers[index].CommitChanges();

            //RNN_Markers_Layer = doc.Layers[index];
            #endregion

            #region Geometry Layer
            Rhino.DocObjects.Layer RNN_Geo_Layer;

            index = doc.Layers.Find("RNN_Geometry", true);
            if (index < 0)
            {
                RNN_Geo_Layer = new Rhino.DocObjects.Layer();
                RNN_Geo_Layer.Name = "RNN_Geometry";
                RNN_Geo_Layer.ParentLayerId = doc.Layers[root_index].Id;
                RNN_Geo_Layer.Color = System.Drawing.Color.DarkRed;

                index = doc.Layers.Add(RNN_Geo_Layer);
            }
            //RNN_Geo_Layer = doc.Layers[index];
            #endregion
        }
    }
}
