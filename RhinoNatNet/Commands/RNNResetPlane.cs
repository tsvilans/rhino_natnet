using System;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;

namespace RhinoNatNet
{
    [System.Runtime.InteropServices.Guid("eaaafeb2-27e1-420a-b490-ed0f7da733fd")]
    public class RNNResetPlane : Command
    {
        static RNNResetPlane _instance;
        public RNNResetPlane()
        {
            _instance = this;
        }

        public static RNNResetPlane Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "RNNResetPlane"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            RNNPlugin.Instance.rnn.SetPlane(Plane.WorldXY);
            return Result.Success;
        }
    }
}
