using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Geometry;
using Rhino.Commands;

namespace RhinoNatNet
{
    [System.Runtime.InteropServices.Guid("15607a88-c0d1-466c-9803-04679d9af618")]
    public class RNNGetPoints : Command
    {
        static RNNGetPoints _instance;
        public RNNGetPoints()
        {
            _instance = this;
        }

        ///<summary>The only instance of the RNNGetPoints command.</summary>
        public static RNNGetPoints Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "RNNGetPoints"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            List<Point3d> points = RNNPlugin.Instance.rnn.GetMarkers();
            Rhino.DocObjects.ObjectAttributes attr = new Rhino.DocObjects.ObjectAttributes();
            int layer_index = Rhino.RhinoDoc.ActiveDoc.Layers.FindByFullPath("RhinoNatNet::RNN_Markers", true);
            if (layer_index < 0)
            {
                RNNContext.CreateLayers();
            }
            attr.LayerIndex = RhinoDoc.ActiveDoc.Layers.FindByFullPath("RhinoNatNet::RNN_Markers", true);
            attr.Name = "RNN_Marker";

            doc.Objects.AddPoints(points, attr);
            return Result.Success;
        }
    }
}
