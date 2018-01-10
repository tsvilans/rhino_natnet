using NatNetML;
using System;
using System.Collections;

using Rhino.PlugIns;
using System.Collections.Generic;
using Rhino.Geometry;
using System.IO.MemoryMappedFiles;
using Rhino.UI;
using System.Globalization;

namespace RhinoNatNet
{
    public class RNNPlugin : Rhino.PlugIns.PlugIn

    {

        internal RNNContext rnn;

        public RNNPlugin()
        {
            if (Instance == null) Instance = this;
            if (rnn == null) rnn = new RNNContext();
        }

        ~RNNPlugin()
        {
            //if (client != null && IsConnected)
            //    client.Uninitialize();
        }



        public override object GetPlugInObject()
        {
            return Instance;
        }

        ///<summary>Gets the only instance of the RNNPlugin plug-in.</summary>
        public static RNNPlugin Instance
        {
            get; private set;
        }

        // You can override methods here to change the plug-in behavior on
        // loading and shut down, add options pages to the Rhino _Option command
        // and mantain plug-in wide options in a document.

        protected override LoadReturnCode OnLoad(ref string errorMessage)
        {

            rnn.TryConnect();
            Rhino.RhinoApp.WriteLine(this.Name, this.Version);

            return base.OnLoad(ref errorMessage);
        }


    }
}