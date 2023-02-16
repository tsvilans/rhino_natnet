using System;
using System.Xml;
using System.Collections;
using System.Collections.Generic;

using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NatNetML;
using Rhino;
using Rhino.Geometry;

namespace RhinoNatNet
{
    public class RigidBody
    {
        public List<Point3d> Points;
        public Plane ReferencePlane
        {
            get
            {
                return _referencePlane;
            }
            set
            {
                _transform = Transform.PlaneToPlane(value, Plane.WorldXY);
                _referencePlane = value;
            }
        }

        private Transform _transform;
        private Plane _referencePlane;

        public int ID;
        public string Name;

        public RigidBody()
        {
            Points = new List<Point3d>();
            ReferencePlane = Plane.WorldXY;
            ID = 0;
            Name = "RigidBody";
        }

        private void _addPropertyNode(XmlDocument doc, XmlNode parent, string name, string value, string default_value = "")
        {
            var propertyNode = doc.CreateElement("property");
            var nameNode = doc.CreateElement("name");
            nameNode.InnerText = name;
            propertyNode.AppendChild(nameNode);

            var valueNode = doc.CreateElement("value");
            valueNode.InnerText = value;
            propertyNode.AppendChild(valueNode);

            if (!string.IsNullOrEmpty(default_value))
            {
                var defaultvalueNode = doc.CreateElement("defaultValue");
                defaultvalueNode.InnerText = default_value;
                propertyNode.AppendChild(defaultvalueNode);
            }

            parent.AppendChild(propertyNode);
        }

        public void ExportMotive(string filepath)
        {
            var scale = RhinoMath.UnitScale(RhinoDoc.ActiveDoc.ModelUnitSystem, UnitSystem.Meters);
            var xPoints = new List<Point3d>(Points);
            for (int i = 0; i < xPoints.Count; ++i)
            {
                xPoints[i].Transform(_transform);
                xPoints[i] = xPoints[i] * scale;
            }


            XmlDocument doc = new XmlDocument();

            var root = doc.CreateElement("Profile");
            root.SetAttribute("version", "1");

            var nodeassets = doc.CreateElement("NodeAssets");

            var rbNode = doc.CreateElement("rigid_body");
            rbNode.SetAttribute("version", "1.1");
            rbNode.SetAttribute("id", ID.ToString());

            var markersNode = doc.CreateElement("markers");

            for (int i = 0; i < xPoints.Count; ++i)
            {
                var markerNode = doc.CreateElement("marker");
                markerNode.SetAttribute("id", i.ToString());

                var positionNode = doc.CreateElement("position");
                positionNode.InnerText = string.Format("{0:0.######},{1:0.######},{2:0.######}", xPoints[i].X, xPoints[i].Y, xPoints[i].Z);
                markerNode.AppendChild(positionNode);

                var sizeNode = doc.CreateElement("size");
                sizeNode.InnerText = "0.012";
                markerNode.AppendChild(sizeNode);

                var residualNode = doc.CreateElement("residual");
                residualNode.InnerText = "0.0";
                markerNode.AppendChild(residualNode);

                var idNode = doc.CreateElement("label_id");
                idNode.InnerText = i.ToString();
                markerNode.AppendChild(idNode);

                markersNode.AppendChild(markerNode);
            }

            rbNode.AppendChild(markersNode);

            var propertiesNode = doc.CreateElement("properties");
            _addPropertyNode(doc, propertiesNode, "NodeName", Name);
            _addPropertyNode(doc, propertiesNode, "Color", "8454079");
            _addPropertyNode(doc, propertiesNode, "DisplayLabel", "true");
            _addPropertyNode(doc, propertiesNode, "MaxMarkerDeflection", "0.003");
            _addPropertyNode(doc, propertiesNode, "Smoothing", "0.135");
            _addPropertyNode(doc, propertiesNode, "AcquisitionFrames", "1");

            for(int i = 0; i < Points.Count; ++i)
            {
                _addPropertyNode(doc, propertiesNode, string.Format("MarkerLocation{0}", i),
                    string.Format("{0:0.######},{1:0.######},{2:0.######}", xPoints[i].X, xPoints[i].Y, xPoints[i].Z),
                    string.Format("{0:0.######},{1:0.######},{2:0.######}", 0, 0, 0));
            }

            rbNode.AppendChild(propertiesNode);
            nodeassets.AppendChild(rbNode);
            root.AppendChild(nodeassets);

            var docNode = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(docNode);
            doc.AppendChild(root);

            doc.Save(filepath);
        }
    }

    public class RnnClient
    {
        /*  [NatNet] Network connection configuration    */
        public NatNetClientML mNatNet;    // The client instance

        public double Scale { get; set; }
        private double mScale = 1.0;
        private Transform mTransform;

        public string LocalIP
        {
            get { return mStrLocalIP; }
            set { mStrLocalIP = value; }
        }
        private string mStrLocalIP = "127.0.0.1";   // Local IP address (string)

        public string RemoteIP
        {
            get { return mStrServerIP; }
            set { mStrServerIP = value; }
        }
        private string mStrServerIP = "127.0.0.1";  // Server IP address (string)


        public bool IsConnected
        {
            get { return mIsConnected; }
        }

        bool mIsConnected = false;

        private bool mDiscovering = false;

        private ConnectionType mConnectionType = ConnectionType.Multicast; // Multicast or Unicast mode

        /*  List for saving each of datadescriptors */
        private List<NatNetML.DataDescriptor> mDataDescriptor = new List<NatNetML.DataDescriptor>();

        /*  Lists and Hashtables for saving data descriptions   */
        public Hashtable mHtSkelRBs = new Hashtable();
        public List<NatNetML.RigidBody> mRigidBodies = new List<NatNetML.RigidBody>();
        public List<Skeleton> mSkeletons = new List<Skeleton>();
        public List<ForcePlate> mForcePlates = new List<ForcePlate>();
        public List<Device> mDevices = new List<Device>();
        public List<Camera> mCameras = new List<Camera>();

        private FrameOfMocapData mFrame = null;
        public FrameOfMocapData Frame
        {
            get { return mFrame; }
        }

        public RnnClient()
        {
            mScale = RhinoMath.UnitScale(UnitSystem.Meters, RhinoDoc.ActiveDoc.ModelUnitSystem);
            mTransform = Transform.PlaneToPlane(new Plane(Point3d.Origin, -Vector3d.YAxis, Vector3d.XAxis), Plane.WorldXY);
        }

        public void GetFrame()
        {
            mFrame = new FrameOfMocapData(mNatNet.GetLastFrameOfData());
        }

        public void GetMarkers(out Point3d[] positions, out int[] ids)
        {
            if (mFrame == null)
            {
                positions = new Point3d[0];
                ids = new int[0];
            }

            positions = new Point3d[mFrame.nMarkers];
            ids = new int[mFrame.nMarkers];

            for (int i = 0; i < mFrame.nMarkers; ++i)
            {
                var marker = mFrame.LabeledMarkers[i];
                positions[i] = new Point3d(marker.x, marker.y, marker.z) * mScale;
                positions[i].Transform(mTransform);

                ids[i] = marker.ID;
            }
        }

        public void GetOtherMarkers(out Point3d[] positions, out int[] ids)
        {
            if (mFrame == null)
            {
                positions = new Point3d[0];
                ids = new int[0];
            }

            positions = new Point3d[mFrame.nOtherMarkers];
            ids = new int[mFrame.nOtherMarkers];

            for (int i = 0; i < mFrame.nOtherMarkers; ++i)
            {
                var marker = mFrame.OtherMarkers[i];
                positions[i] = new Point3d(marker.x, marker.y, marker.z) * mScale;
                positions[i].Transform(mTransform);
                ids[i] = marker.ID;
            }
        }

        public void GetRigidBodies(out Plane[] planes, out int[] ids)
        {
            if (mFrame == null)
            {
                planes = new Plane[0];
                ids = new int[0];
            }

            planes = new Plane[mFrame.nRigidBodies];
            ids = new int[mFrame.nRigidBodies];

            for (int i = 0; i < mFrame.nRigidBodies; ++i)
            {
                var rb = mFrame.RigidBodies[i];
                var quat = new Quaternion(rb.qw, rb.qx, rb.qy, rb.qz);

                quat.GetRotation(out Plane plane);
                plane.Translate(new Vector3d(rb.x, rb.y, rb.z) * mScale);
                plane.Transform(mTransform);

                planes[i] = plane;
                ids[i] = rb.ID;
            }
        }

        public bool CheckValid()
        {
            if (mNatNet == null)
                return false;
            return true;
        }

        public void FetchDataDescriptor()
        {
            fetchDataDescriptor();
        }

        public void SetAddresses(DiscoveredServer server)
        {
            mStrLocalIP = server.LocalAddress.ToString();
            mStrServerIP = server.ServerAddress.ToString();

            mDiscovering = false;
        }

        public int Discover(int maxTime = 2000)
        {
            NatNetServerDiscovery discovery = new NatNetServerDiscovery();
            discovery.OnServerDiscovered += SetAddresses;
            mDiscovering = true;
            discovery.StartDiscovery();

            int result = 0;

            int elapsedTime = 0;
            int timeStep = 50;
            while (mDiscovering)
            {
                System.Threading.Thread.Sleep(timeStep);
                elapsedTime += timeStep;

                if (elapsedTime > maxTime)
                {
                    result = 1;
                    break;
                }
            }
            discovery.EndDiscovery();

            return result;
        }

        public bool Connect(bool useEventHandler=false)
        {
            connectToServer(mStrServerIP, mStrLocalIP, mConnectionType);
            mIsConnected = fetchServerDescriptor();
            if (!IsConnected) return IsConnected;

            //fetchDataDescriptor();                  //Fetch and parse data descriptor

            if (useEventHandler)
                mNatNet.OnFrameReady += new NatNetML.FrameReadyEventHandler(fetchFrameData);
            
            return IsConnected;
        }

        public bool Disconnect()
        {
            if (IsConnected)
            {
                mNatNet.OnFrameReady -= fetchFrameData;

                /*  Clearing Saved Descriptions */
                mRigidBodies.Clear();
                mSkeletons.Clear();
                mHtSkelRBs.Clear();
                mForcePlates.Clear();
                mNatNet.Disconnect();
                mNatNet = null;

                mIsConnected = false;
                return true;
            }
            return false;
        }

        /// <summary>
        /// [NatNet] parseFrameData will be called when a frame of Mocap
        /// data has is received from the server application.
        ///
        /// Note: This callback is on the network service thread, so it is
        /// important to return from this function quickly as possible 
        /// to prevent incoming frames of data from buffering up on the
        /// network socket.
        ///
        /// Note: "data" is a reference structure to the current frame of data.
        /// NatNet re-uses this same instance for each incoming frame, so it should
        /// not be kept (the values contained in "data" will become replaced after
        /// this callback function has exited).
        /// </summary>
        /// <param name="data">The actual frame of mocap data</param>
        /// <param name="client">The NatNet client instance</param>
        void fetchFrameData(FrameOfMocapData data, NatNetClientML client)
        {
            mFrame = new FrameOfMocapData(data);
            //mFrame.CopyArrays(data);
            //processFrameData(mFrame);
        }

        void processFrameData(FrameOfMocapData data)
        {
            /*  Parsing Rigid Body Frame Data   */
            for (int i = 0; i < mRigidBodies.Count; i++)
            {
                int rbID = mRigidBodies[i].ID;              // Fetching rigid body IDs from the saved descriptions

                for (int j = 0; j < data.nRigidBodies; j++)
                {
                    if (rbID == data.RigidBodies[j].ID)      // When rigid body ID of the descriptions matches rigid body ID of the frame data.
                    {
                        NatNetML.RigidBody rb = mRigidBodies[i];                // Saved rigid body descriptions
                        RigidBodyData rbData = data.RigidBodies[j];    // Received rigid body descriptions

                        if (rbData.Tracked == true)
                        {
                            RhinoApp.WriteLine("\tRigidBody ({0}):", rb.Name);
                            RhinoApp.WriteLine("\t\tpos ({0:N3}, {1:N3}, {2:N3})", rbData.x, rbData.y, rbData.z);

                            // Rigid Body Euler Orientation
                            float[] quat = new float[4] { rbData.qx, rbData.qy, rbData.qz, rbData.qw };
                            float[] eulers = new float[3];

                            eulers = NatNetClientML.QuatToEuler(quat, NATEulerOrder.NAT_XYZr); //Converting quat orientation into XYZ Euler representation.
                            double xrot = RadiansToDegrees(eulers[0]);
                            double yrot = RadiansToDegrees(eulers[1]);
                            double zrot = RadiansToDegrees(eulers[2]);

                            RhinoApp.WriteLine("\t\tori ({0:N3}, {1:N3}, {2:N3})", xrot, yrot, zrot);
                        }
                        else
                        {
                            RhinoApp.WriteLine("\t{0} is not tracked in current frame", rb.Name);
                        }
                    }
                }
            }

            /* Parsing Skeleton Frame Data  */
            for (int i = 0; i < mSkeletons.Count; i++)      // Fetching skeleton IDs from the saved descriptions
            {
                int sklID = mSkeletons[i].ID;

                for (int j = 0; j < data.nSkeletons; j++)
                {
                    if (sklID == data.Skeletons[j].ID)      // When skeleton ID of the description matches skeleton ID of the frame data.
                    {
                        Skeleton skl = mSkeletons[i];              // Saved skeleton descriptions
                        SkeletonData sklData = data.Skeletons[j];  // Received skeleton frame data

                        RhinoApp.WriteLine("\tSkeleton ({0}):", skl.Name);
                        RhinoApp.WriteLine("\t\tSegment count: {0}", sklData.nRigidBodies);

                        /*  Now, for each of the skeleton segments  */
                        for (int k = 0; k < sklData.nRigidBodies; k++)
                        {
                            NatNetML.RigidBodyData boneData = sklData.RigidBodies[k];

                            /*  Decoding skeleton bone ID   */
                            int skeletonID = HighWord(boneData.ID);
                            int rigidBodyID = LowWord(boneData.ID);
                            int uniqueID = skeletonID * 1000 + rigidBodyID;
                            int key = uniqueID.GetHashCode();

                            NatNetML.RigidBody bone = (NatNetML.RigidBody)mHtSkelRBs[key];   //Fetching saved skeleton bone descriptions

                            //Outputting only the hip segment data for the purpose of this sample.
                            if (k == 0)
                                RhinoApp.WriteLine(string.Format("\t\t{0:N3}: pos({1:N3}, {2:N3}, {3:N3})", 
                                    bone.Name, boneData.x, boneData.y, boneData.z));
                        }
                    }
                }
            }

            /*  Parsing Force Plate Frame Data  */
            for (int i = 0; i < mForcePlates.Count; i++)
            {
                int fpID = mForcePlates[i].ID;                  // Fetching force plate IDs from the saved descriptions

                for (int j = 0; j < data.nForcePlates; j++)
                {
                    if (fpID == data.ForcePlates[j].ID)         // When force plate ID of the descriptions matches force plate ID of the frame data.
                    {
                        NatNetML.ForcePlate fp = mForcePlates[i];                // Saved force plate descriptions
                        NatNetML.ForcePlateData fpData = data.ForcePlates[i];    // Received forceplate frame data

                        RhinoApp.WriteLine("\tForce Plate ({0}):", fp.Serial);

                        // Here we will be printing out only the first force plate "subsample" (index 0) that was collected with the mocap frame.
                        for (int k = 0; k < fpData.nChannels; k++)
                        {
                            RhinoApp.WriteLine("\t\tChannel {0}: {1}", fp.ChannelNames[k], fpData.ChannelData[k].Values[0]);
                        }
                    }
                }
            }
            RhinoApp.WriteLine("\n");
        }

        void connectToServer(string serverIPAddress, string localIPAddress, NatNetML.ConnectionType connectionType)
        {
            /*  [NatNet] Instantiate the client object  */
            mNatNet = new NatNetML.NatNetClientML();

            /*  [NatNet] Checking verions of the NatNet SDK library  */
            int[] verNatNet = new int[4];           // Saving NatNet SDK version number
            verNatNet = mNatNet.NatNetVersion();
            RhinoApp.WriteLine(string.Format("NatNet SDK Version: {0}.{1}.{2}.{3}", 
                verNatNet[0], verNatNet[1], verNatNet[2], verNatNet[3]));

            /*  [NatNet] Connecting to the Server    */

            NatNetClientML.ConnectParams connectParams = new NatNetClientML.ConnectParams();
            connectParams.ConnectionType = connectionType;
            connectParams.ServerAddress = serverIPAddress;
            connectParams.LocalAddress = localIPAddress;

            RhinoApp.WriteLine("\nConnecting...");
            RhinoApp.WriteLine("\tServer IP Address: {0}", serverIPAddress);
            RhinoApp.WriteLine("\tLocal IP address : {0}", localIPAddress);
            RhinoApp.WriteLine("\tConnection Type  : {0}", connectionType);
            RhinoApp.WriteLine("\n");

            mNatNet.Connect(connectParams);
        }

        bool fetchServerDescriptor()
        {
            NatNetML.ServerDescription m_ServerDescriptor = new NatNetML.ServerDescription();
            int errorCode = mNatNet.GetServerDescription(m_ServerDescriptor);

            if (errorCode == 0)
            {
                RhinoApp.WriteLine("Success: Connected to the server\n");
                parseSeverDescriptor(m_ServerDescriptor);
                return true;
            }
            else
            {
                RhinoApp.WriteLine("Error: Failed to connect. Check the connection settings.");
                RhinoApp.WriteLine("Program terminated (Enter ESC to exit)");
                return false;
            }
        }

        void parseSeverDescriptor(NatNetML.ServerDescription server)
        {
            RhinoApp.WriteLine("Server Info:");
            RhinoApp.WriteLine("\tHost               : {0}", server.HostComputerName);
            RhinoApp.WriteLine("\tApplication Name   : {0}", server.HostApp);
            RhinoApp.WriteLine(string.Format("\tApplication Version: {0}.{1}.{2}.{3}", 
                server.HostAppVersion[0], server.HostAppVersion[1], server.HostAppVersion[2], server.HostAppVersion[3]));
            RhinoApp.WriteLine(string.Format("\tNatNet Version     : {0}.{1}.{2}.{3}\n", 
                server.NatNetVersion[0], server.NatNetVersion[1], server.NatNetVersion[2], server.NatNetVersion[3]));
        }

        void fetchDataDescriptor()
        {
            /*  [NatNet] Fetch Data Descriptions. Instantiate objects for saving data descriptions and frame data    */
            bool result = mNatNet.GetDataDescriptions(out mDataDescriptor);
            if (result)
            {
                RhinoApp.WriteLine("Success: Data Descriptions obtained from the server.");
                parseDataDescriptor(mDataDescriptor);
            }
            else
            {
                RhinoApp.WriteLine("Error: Could not get the Data Descriptions");
            }
            RhinoApp.WriteLine("\n");
        }

        void parseDataDescriptor(List<NatNetML.DataDescriptor> description)
        {
            //  [NatNet] Request a description of the Active Model List from the server. 
            //  This sample will list only names of the data sets, but you can access 
            int numDataSet = description.Count;
            RhinoApp.WriteLine("Total {0} data sets in the capture:", numDataSet);

            for (int i = 0; i < numDataSet; ++i)
            {
                int dataSetType = description[i].type;
                // Parse Data Descriptions for each data sets and save them in the delcared lists and hashtables for later uses.
                switch (dataSetType)
                {
                    case ((int)NatNetML.DataDescriptorType.eMarkerSetData):
                        NatNetML.MarkerSet mkset = (NatNetML.MarkerSet)description[i];
                        RhinoApp.WriteLine("\tMarkerSet ({0})", mkset.Name);
                        break;


                    case ((int)NatNetML.DataDescriptorType.eRigidbodyData):
                        NatNetML.RigidBody rb = (NatNetML.RigidBody)description[i];
                        RhinoApp.WriteLine("\tRigidBody ({0})", rb.Name);

                        // Saving Rigid Body Descriptions
                        mRigidBodies.Add(rb);
                        break;


                    case ((int)NatNetML.DataDescriptorType.eSkeletonData):
                        NatNetML.Skeleton skl = (NatNetML.Skeleton)description[i];
                        RhinoApp.WriteLine("\tSkeleton ({0}), Bones:", skl.Name);

                        //Saving Skeleton Descriptions
                        mSkeletons.Add(skl);

                        // Saving Individual Bone Descriptions
                        for (int j = 0; j < skl.nRigidBodies; j++)
                        {

                            RhinoApp.WriteLine("\t\t{0}. {1}", j + 1, skl.RigidBodies[j].Name);
                            int uniqueID = skl.ID * 1000 + skl.RigidBodies[j].ID;
                            int key = uniqueID.GetHashCode();
                            mHtSkelRBs.Add(key, skl.RigidBodies[j]); //Saving the bone segments onto the hashtable
                        }
                        break;


                    case ((int)NatNetML.DataDescriptorType.eForcePlateData):
                        NatNetML.ForcePlate fp = (NatNetML.ForcePlate)description[i];
                        RhinoApp.WriteLine("\tForcePlate ({0})", fp.Serial);

                        // Saving Force Plate Channel Names
                        mForcePlates.Add(fp);

                        for (int j = 0; j < fp.ChannelCount; j++)
                        {
                            RhinoApp.WriteLine("\t\tChannel {0}: {1}", j + 1, fp.ChannelNames[j]);
                        }
                        break;

                    case ((int)NatNetML.DataDescriptorType.eDeviceData):
                        NatNetML.Device dd = (NatNetML.Device)description[i];
                        RhinoApp.WriteLine("\tDeviceData ({0})", dd.Serial);

                        // Saving Device Data Channel Names
                        mDevices.Add(dd);

                        for (int j = 0; j < dd.ChannelCount; j++)
                        {
                            RhinoApp.WriteLine("\t\tChannel {0}: {1}", j + 1, dd.ChannelNames[j]);
                        }
                        break;
                        
                    case ((int)NatNetML.DataDescriptorType.eCameraData):
                        // Saving Camera Names
                        NatNetML.Camera camera = (NatNetML.Camera)description[i];
                        RhinoApp.WriteLine("\tCamera: ({0})", camera.Name);

                        // Saving Force Plate Channel Names
                        mCameras.Add(camera);
                        break;
                        

                    default:
                        // When a Data Set does not match any of the descriptions provided by the SDK.
                        RhinoApp.WriteLine("\tError: Invalid Data Set - dataSetType = " + dataSetType);
                        break;
                }
            }
        }

        static double RadiansToDegrees(double dRads)
        {
            return dRads * (180.0f / Math.PI);
        }

        static int LowWord(int number)
        {
            return number & 0xFFFF;
        }

        static int HighWord(int number)
        {
            return ((number >> 16) & 0xFFFF);
        }
    } // End. ManagedClient class
}
