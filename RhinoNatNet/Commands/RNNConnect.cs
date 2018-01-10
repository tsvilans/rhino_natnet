using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace RhinoNatNet
{
    [System.Runtime.InteropServices.Guid("c9c3feae-a613-4eca-a9d4-b8db8a81193e")]
    public class RNNConnect : Command
    {
        public RNNConnect()
        {
            Instance = this;
        }

        public static RNNConnect Instance
        {
            get; private set;
        }

        public override string EnglishName
        {
            get { return "RNNConnect"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            RNNPlugin.Instance.rnn.TryConnect();
            return Result.Success;
        }
    }
}
