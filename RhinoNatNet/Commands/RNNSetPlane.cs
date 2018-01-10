using System;
using Rhino;
using Rhino.Commands;
using Rhino.Input.Custom;
using Rhino.Geometry;
using Rhino.Input;

namespace RhinoNatNet
{
    [System.Runtime.InteropServices.Guid("ee02b832-a7b1-4e7a-9369-01f01ba07174")]
    public class RNNSetPlane : Command
    {
        static RNNSetPlane _instance;
        public RNNSetPlane()
        {
            _instance = this;
        }

        public static RNNSetPlane Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "RNNSetPlane"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            Point3d pt0;
            using (GetPoint getPointAction = new GetPoint())
            {
                getPointAction.SetCommandPrompt("Please select the plane origin.");

                if (getPointAction.Get() != GetResult.Point)
                {
                    RhinoApp.WriteLine("No plane origin was selected.");
                    return getPointAction.CommandResult();
                }
                pt0 = getPointAction.Point();
            }

            Point3d pt1;
            using (GetPoint getPointAction = new GetPoint())
            {
                getPointAction.SetCommandPrompt("Please select a point on the new X-axis.");
                if (getPointAction.Get() != GetResult.Point)
                {
                    RhinoApp.WriteLine("No point was selected.");
                    return getPointAction.CommandResult();
                }
                pt1 = getPointAction.Point();
            }

            Point3d pt2;
            using (GetPoint getPointAction = new GetPoint())
            {
                getPointAction.SetCommandPrompt("Please select a point on the new Y-axis.");
                if (getPointAction.Get() != GetResult.Point)
                {
                    RhinoApp.WriteLine("No point was selected.");
                    return getPointAction.CommandResult();
                }
                pt2 = getPointAction.Point();
            }

            Plane plane = new Plane(pt0, pt1, pt2);
            RNNPlugin.Instance.rnn.SetPlane(plane);
            return Result.Success;
        }
    }
}
