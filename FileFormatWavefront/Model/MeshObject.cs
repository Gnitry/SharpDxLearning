using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileFormatWavefront.Model
{
    /// <summary>
    /// Represents 3d object.
    /// </summary>
    public class MeshObject
    {
        public string Name { get; }
        private readonly List<Face> faces = new List<Face>();

        public MeshObject(string name)
        {
            Name = name;
        }
        internal void AddFace(Face face)
        {
            faces.Add(face);
        }

        /// <summary>
        /// Gets the faces.
        /// </summary>
        public ReadOnlyCollection<Face> Faces
        {
            get { return faces.AsReadOnly(); }
        }
    }
}
