using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;
using Rhino.FileIO;
using System.IO;

namespace OwlVR
{
    public class OwlVRComponent : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public OwlVRComponent()
          : base("Geometry Sender", "GS",
            "Provides geometry to XR clients",
            "OwlXR", "Visualization")
        {
           var srcIpEndPoint = new IPEndPoint(IPAddress.Any, 10501);
            //var ipEndPoint = new IPEndPoint(IPAddress.Parse("10.0.2.2"), 10500);

            //handler = new TcpClient();
            //handler.Connect(ipEndPoint);

            //listener = new TcpListener(srcIpEndPoint);
            Owl_RH_Network nw = Owl_RH_Network.Instance;
        }
        TcpClient handler;
        TcpListener listener;
        NetworkStream stream = null;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGeometryParameter("Geometry", "geo", "Receives geometry to be sent to the VR devices", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GeometryBase data = null;
            if (!DA.GetData(0, ref data)) { return; }

            Owl_RH_Network.Instance.Send_Json(data.ToJSON(new SerializationOptions()));

            Console.WriteLine($"Message sent");
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => (System.Drawing.Bitmap)Owl_gh.Properties.Resources.ResourceManager.GetObject("geometry_sender");

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("0019da31-6eae-4163-9f6d-f7c43181cae1");
    }
}