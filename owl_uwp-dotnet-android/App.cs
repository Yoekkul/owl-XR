using StereoKit;

namespace StereoKitApp
{
	public class App
	{
		public SKSettings Settings => new SKSettings { 
			appName           = "OWL XR",
			assetsFolder      = "Assets",
			displayPreference = DisplayMode.MixedReality
		};

		//TODO REMOVE UNNEEDED BELOW

		Pose  cubePose = new Pose(0, 0, -0.5f, Quat.Identity);
		Model cube;
		Matrix   floorTransform = Matrix.TS(new Vec3(0, -1.5f, 0), new Vec3(30, 0.1f, 30));
		Material floorMaterial;

		//------------
		Owl_Network owl_nw;
        bool change_triggered = false;

		Material shaderMaterial;
        Matrix orgin_offset = Matrix.Identity;


        //------------

        public void Init()
		{
			// Create assets used by the app
			cube = Model.FromMesh(
				Mesh.GenerateRoundedCube(Vec3.One * 0.1f, 0.02f),
				Default.MaterialUI);

			floorMaterial = new Material(Shader.FromFile("floor.hlsl"));
			floorMaterial.Transparency = Transparency.Blend;
			//---
			owl_nw = Owl_Network.Instance;

            shaderMaterial = new Material(Shader.Default);
            shaderMaterial.Transparency = Transparency.Blend;
            shaderMaterial[MatParamName.ColorTint] = new Color(0f, 0.35f, 0.7f, 0.85f);
        }

		public void Step()
		{
			if (SK.System.displayType == Display.Opaque)
				Default.MeshCube.Draw(floorMaterial, floorTransform);

			UI.Handle("Cube", ref cubePose, cube.Bounds);
			cube.Draw(cubePose.ToMatrix());


            //-------------------
            foreach (Mesh m in owl_nw.Get_Mesh_Dictionary().Values)
            {
                m.Draw(shaderMaterial, orgin_offset * Matrix.TS(-Vec3.Up, 0.05f));
            }

            Hand hand = Input.Hand(Handed.Right);
            if (hand.IsJustPinched)
            {
                // owl_nw.send_data("pinch");
                owl_nw.send_action("pinch");
                // Log.Info("NOICE");
            }
            if (hand.IsJustGripped)
            {
                owl_nw.send_action("grip");
                //FIXME use joystick to rotate, rotate around user and not origin



                //owl_nw.send_data("grip");
            }
            ShowController(Handed.Right);
            Send_Controller_Actions();

            Player_Rotation_Motion_Control(Handed.Right);
        }


        void Send_Controller_Actions()
        {
            Controller lc = Input.Controller(Handed.Left);
            Controller rc = Input.Controller(Handed.Right);

            if (!(lc.IsTracked || rc.IsTracked)) return;
            if (lc.IsX1JustPressed)
            {
                owl_nw.send_action("lx");
            }
            else if (lc.IsX2JustPressed)
            {
                owl_nw.send_action("ly");
            }
            if (rc.IsX1JustPressed)
            {
                owl_nw.send_action("rx");
            }
            else if (rc.IsX2JustPressed)
            {
                owl_nw.send_action("ry");
            }

        }



        // Handles Rotation and motion when in VR mode (controllers plugged in)
        void Player_Rotation_Motion_Control(Handed hand)
        {
            Controller c = Input.Controller(hand);
            if (!c.IsTracked) return;

            float deadzone_allowance = 0.05f;
            float center_zone = 0.35f;  //Used to detect when we reset

            float roate_angle = 45;

            // TODO Read the next action only when the stick is released to the center

            //Rotate when the stick is pointed to the right (within threshold)
            if (Vec2.Distance(c.stick, -Vec2.UnitX) < deadzone_allowance && !change_triggered)
            {
                Log.Info("Rotate Right");
                //Renderer.CameraRoot = Matrix.R(Quat.FromAngles(15*Vec3.UnitY)*Renderer.CameraRoot.Pose.orientation);	//ROTATE WORLD ORIGIN
                Renderer.CameraRoot = Matrix.T(-Input.Head.position) * Matrix.R(Quat.FromAngles(roate_angle * Vec3.UnitY) * Renderer.CameraRoot.Pose.orientation) * Matrix.T(Input.Head.position);
                change_triggered = true;
            }
            else if (Vec2.Distance(c.stick, Vec2.UnitX) < deadzone_allowance && !change_triggered)
            {
                Log.Info("Rotate Left");
                Renderer.CameraRoot = Matrix.T(-Input.Head.position) * Matrix.R(Quat.FromAngles(-roate_angle * Vec3.UnitY) * Renderer.CameraRoot.Pose.orientation) * Matrix.T(Input.Head.position);
                change_triggered = true;
            }

            if (Vec2.DistanceSq(c.stick, Vec2.Zero) < center_zone)
            {
                change_triggered = false;
            }
            // Teleport fwd and bwd
        }


        //https://stereokit.net/Pages/StereoKit/Controller/stick.html
        void ShowController(Handed hand)
        {
            Controller c = Input.Controller(hand);
            if (!c.IsTracked) return;

            Hierarchy.Push(c.pose.ToMatrix());
            // Pick the controller color based on trackin info state
            Color color = Color.Black;
            if (c.trackedPos == TrackState.Inferred) color.g = 0.5f;
            if (c.trackedPos == TrackState.Known) color.g = 1;
            if (c.trackedRot == TrackState.Inferred) color.b = 0.5f;
            if (c.trackedRot == TrackState.Known) color.b = 1;
            Default.MeshCube.Draw(Default.Material, Matrix.S(new Vec3(3, 3, 8) * U.cm), color);

            // Show button info on the back of the controller
            Hierarchy.Push(Matrix.TR(0, 1.6f * U.cm, 0, Quat.LookAt(Vec3.Zero, new Vec3(0, 1, 0), new Vec3(0, 0, -1))));

            // Show the tracking states as text
            Text.Add(c.trackedPos == TrackState.Known ? "(pos)" : (c.trackedPos == TrackState.Inferred ? "~pos~" : "pos"), Matrix.TS(0, -0.03f, 0, 0.25f));
            Text.Add(c.trackedRot == TrackState.Known ? "(rot)" : (c.trackedRot == TrackState.Inferred ? "~rot~" : "rot"), Matrix.TS(0, -0.02f, 0, 0.25f));

            // Show the controller's buttons
            Text.Add(Input.ControllerMenuButton.IsActive() ? "(menu)" : "menu", Matrix.TS(0, -0.01f, 0, 0.25f));
            Text.Add(c.IsX1Pressed ? "(X1)" : "X1", Matrix.TS(0, 0.00f, 0, 0.25f));
            Text.Add(c.IsX2Pressed ? "(X2)" : "X2", Matrix.TS(0, 0.01f, 0, 0.25f));

            // Show the analog stick's information
            Vec3 stickAt = new Vec3(0, 0.03f, 0);
            Lines.Add(stickAt, stickAt + c.stick.XY0 * 0.01f, Color.White, 0.001f);
            if (c.IsStickClicked) Text.Add("O", Matrix.TS(stickAt, 0.25f));

            // And show the trigger and grip buttons
            Default.MeshCube.Draw(Default.Material, Matrix.TS(0, -0.015f, -0.005f, new Vec3(0.01f, 0.04f, 0.01f)) * Matrix.TR(new Vec3(0, 0.02f, 0.03f), Quat.FromAngles(-45 + c.trigger * 40, 0, 0)));
            Default.MeshCube.Draw(Default.Material, Matrix.TS(0.0149f * (hand == Handed.Right ? 1 : -1), 0, 0.015f, new Vec3(0.01f * (1 - c.grip), 0.04f, 0.01f)));

            Hierarchy.Pop();
            Hierarchy.Pop();

            // And show the pointer
            Default.MeshCube.Draw(Default.Material, c.aim.ToMatrix(new Vec3(1, 1, 4) * U.cm), Color.HSV(0, 0.5f, 0.8f).ToLinear());
        }

    }
}