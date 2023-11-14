using Grasshopper;
using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace OwlVR
{
    public class OwlVRInfo : GH_AssemblyInfo
    {
        public override string Name => "OwlXR";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => (System.Drawing.Bitmap)Owl_gh.Properties.Resources.ResourceManager.GetObject("geometry_sender");

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "OwlXR is the server-side component in charge of sending Rhino Geometry to a XR device and receiving commands from it";

        public override Guid Id => new Guid("10b13c0f-f9a7-4234-ba8f-af2d22063642");

        //Return a string identifying you or your company.
        public override string AuthorName => "Christopher Tibaldo";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "chris@tibaldo.ch";
    }
}