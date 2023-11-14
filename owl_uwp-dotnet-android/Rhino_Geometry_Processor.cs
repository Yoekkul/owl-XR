using StereoKit;
using System;
using System.Collections.Generic;
using System.Text;

namespace StereoKitApp
{
    internal class Rhino_Geometry_processor
    {

        public static Mesh Get_Mesh_From_JSON(string json)
        {
            return Get_Mesh((Rhino.Geometry.GeometryBase)Rhino.Geometry.GeometryBase.FromJSON(json));
        }

        public static Mesh Get_Mesh(Rhino.Geometry.GeometryBase geometry)
        {

            Mesh m = Mesh.Quad;

            if (geometry is Rhino.Geometry.Mesh)
            {
                m = Rhino_To_StereoKit_Mesh((Rhino.Geometry.Mesh)geometry);
            }

            return m;
        }

        private static Mesh Rhino_To_StereoKit_Mesh(Rhino.Geometry.Mesh r_mesh)
        {
            Vertex[] verts = new Vertex[r_mesh.Vertices.Count];
            uint[] indices = new uint[r_mesh.Faces.Count * 3 * 2];


            for (int i = 0; i < verts.Length; i++)
            {
                Vec3 normals = new Vec3(r_mesh.Normals[i].X, r_mesh.Normals[i].Z, r_mesh.Normals[i].Y);
                Vec3 vertex = new Vec3(r_mesh.Vertices[i].X, r_mesh.Vertices[i].Z, r_mesh.Vertices[i].Y);
                verts[i] = new Vertex(vertex, normals);
            }
            Log.Info("Verts.Length=" + verts.Length.ToString());
            for (int i = 0; i < r_mesh.Faces.Count; i++)
            {

                indices[6 * i] = (uint)r_mesh.Faces[i].A;
                indices[6 * i + 1] = (uint)r_mesh.Faces[i].D;
                indices[6 * i + 2] = (uint)r_mesh.Faces[i].B;

                indices[6 * i + 3] = (uint)r_mesh.Faces[i].C;
                indices[6 * i + 4] = (uint)r_mesh.Faces[i].B;
                indices[6 * i + 5] = (uint)r_mesh.Faces[i].D;
            }

            Mesh m = Mesh.Quad;

            m.SetData(verts, indices);

            return m;
        }
    }
}
