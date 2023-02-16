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

        private static System.Diagnostics.Stopwatch m_timer = new System.Diagnostics.Stopwatch();

        static Rhino.RhinoDoc doc;

        public RNNContext()
        {
            doc = Rhino.RhinoDoc.ActiveDoc;
            markers = new List<Point3d>();
            marker_sizes = new List<float>();
            counter = 0;

            SetPlane(Plane.WorldXY);
            client = null;

            Rhino.Display.DisplayPipeline.PostDrawObjects += DisplayPipeline_DrawMarkers;
            Rhino.Display.DisplayPipeline.CalculateBoundingBox += DisplayPipeline_MarkersBoundingBox;

            m_timer.Start();
        }

        ~RNNContext()
        {
            client = null;
        }

        public static bool IsConnected { get; private set; }

        public void TryConnect()
        {
            if (client != null)
            {
                //client.Disconnect();
            }

            Rhino.RhinoApp.Write("Attemping to connect to NatNet server... ");
            client = new NatNetClientML();

            var cp = new NatNetClientML.ConnectParams();
            cp.ConnectionType = ConnectionType.Multicast;
            cp.LocalAddress = "192.168.1.100";
            cp.ServerAddress = "192.168.1.101";
            cp.ServerCommandPort = 1510;
            cp.ServerDataPort = 1511;

            int res = client.Connect(cp);
            if (res != 0)
            {
                Rhino.RhinoApp.WriteLine("Failed.");
                client = null;
                IsConnected = false;
                return;
            }
            Rhino.RhinoApp.WriteLine("Success.");
            IsConnected = true;

            fetchServerDescriptor();

            client.OnFrameReady += new NatNetML.FrameReadyEventHandler(fetchFrameData);

            Rhino.RhinoDoc.ActiveDoc.Views.Redraw();
        }

        static bool fetchServerDescriptor()
        {
            NatNetML.ServerDescription m_ServerDescriptor = new NatNetML.ServerDescription();
            int errorCode = client.GetServerDescription(m_ServerDescriptor);

            if (errorCode == 0)
            {
                Console.WriteLine("Success: Connected to the server\n");
                parseServerDescriptor(m_ServerDescriptor);
                return true;
            }
            else
            {
                Console.WriteLine("Error: Failed to connect. Check the connection settings.");
                return false;
            }
        }

        static void parseServerDescriptor(NatNetML.ServerDescription server)
        {
            Console.WriteLine("Server Info:");
            Console.WriteLine("\tHost: {0}", server.HostComputerName);
            Console.WriteLine("\tApplication Name: {0}", server.HostApp);
            Console.WriteLine("\tApplication Version: {0}.{1}.{2}.{3}", server.HostAppVersion[0], server.HostAppVersion[1], server.HostAppVersion[2], server.HostAppVersion[3]);
            Console.WriteLine("\tNatNet Version: {0}.{1}.{2}.{3}\n", server.NatNetVersion[0], server.NatNetVersion[1], server.NatNetVersion[2], server.NatNetVersion[3]);
        }

        static void fetchFrameData(NatNetML.FrameOfMocapData data, NatNetML.NatNetClientML client)
        {
            if (m_timer.ElapsedMilliseconds < 30)
                return;
            m_timer.Restart();

            /*  Exception handler for cases where assets are added or removed.
                Data description is re-obtained in the main function so that contents
                in the frame handler is kept minimal. */
            //if (( data.bTrackingModelsChanged == true || data.nRigidBodies != mRigidBodies.Count || data.nSkeletons != mSkeletons.Count || data.nForcePlates != mForcePlates.Count))
            //{
            //  mAssetChanged = true;
            //}

            /*  Processing and ouputting frame data every 200th frame.
                This conditional statement is included in order to simplify the program output */
            //if (data.iFrame % 20 == 0)
            //{
            /*    if (data.bRecording == false)
                    Console.WriteLine(string.Format("Frame #{0} Received:", data.iFrame));
                else if (data.bRecording == true)
                    Console.WriteLine(string.Format("[Recording] Frame #{0} Received:", data.iFrame));
            */
            
                processFrameData(data);
            //}
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

        static void processFrameData(NatNetML.FrameOfMocapData data)
        {
            var new_points = new List<Point3d>();
            var new_sizes = new List<float>();

            for (int i = 0; i < data.nOtherMarkers; ++i)
            {
                var om = data.OtherMarkers[i];
                new_points.Add(new Point3d(om.x, om.y, om.z));
                new_sizes.Add(1.0f);
            }

            for (int i = 0; i < data.nMarkers; ++i)
            {
                var m = data.LabeledMarkers[i];
                new_points.Add(new Point3d(m.x, m.y, m.z));
                new_sizes.Add(1.0f);

            }

            if (new_points.Count > 0)
            {
                markers = new_points;
                marker_sizes = new_sizes;
            }

            Rhino.RhinoDoc.ActiveDoc.Views.Redraw();
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
