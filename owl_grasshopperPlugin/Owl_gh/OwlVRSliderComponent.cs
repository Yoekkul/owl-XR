
using System;
using Eto.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace OwlVR{
    public class OwlVRSliderComponent : GH_Component
    {
        public OwlVRSliderComponent()
            : base("Owl Slider", "SL",
            "Provedes a remotely controllable slider",
            "OwlXR", "Visualization")
        {
            // 1. register input event handler interest at Network
            Owl_RH_Network.Instance.Command_Received_EVT += Receive_Action;
        }

        int from = 0;
        int to = 10;
        int step = 1;

        int current = 5;

        public override Guid ComponentGuid => new Guid("0019da31-6eae-4163-9f6d-f7c43181cae2");

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            // Hand Left/Right
            pManager.AddIntegerParameter("From", "F", "Start", GH_ParamAccess.item);
            pManager.AddIntegerParameter("To", "T", "Start", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Step", "S", "Size of the step for interval", GH_ParamAccess.item);
            pManager[0].Optional = true;
            pManager[1].Optional = true;
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddIntegerParameter("Out","O","The current value in the inteval",GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // GH_Interval interval = null;
            DA.GetData(0, ref from);
            DA.GetData(0, ref to);
            DA.GetData(0, ref step);


            //if (DA.GetData(0, ref from)) { return; }

            if (current<from){
                current = from;
            }
            if (current>to){
                current = to;
            }

            DA.SetData(0,current);
            
        }


        private void Receive_Action(string action){
            //TODO x/y left/right/any
            if(action == "grip"){
                if(current+step <= to){
                    current+=step;
                }
            }else if(action == "pinch") {
                if(current-step >= from) { 
                    current-=step;
                }
            }
            Grasshopper.Instances.ActiveCanvas.Document.ScheduleSolution(5, (ghDoc) => ghDoc.ExpireSolution());


            // 1. check if the action is the one we are expecting (x/y && hand)
            // 2 Update the outputed value
        }


        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => (System.Drawing.Bitmap)Owl_gh.Properties.Resources.ResourceManager.GetObject("remote-slider");

    }
}