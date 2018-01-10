using System;
using Rhino;
using Rhino.Commands;

namespace RhinoNatNet.Commands
{
    [System.Runtime.InteropServices.Guid("28b1e659-c9dd-4f77-85ab-de38f0cefab4")]
    public class RNNToggleNumberDisplay : Command
    {
        static RNNToggleNumberDisplay _instance;
        public RNNToggleNumberDisplay()
        {
            _instance = this;
        }

        public static RNNToggleNumberDisplay Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "RNNToggleDisplayNumbers"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            RNNContext.DisplayCoords = !RNNContext.DisplayCoords;
            Rhino.RhinoDoc.ActiveDoc.Views.Redraw();
            return Result.Success;
        }
    }
}
