using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using FileFormatWavefront;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using D3D = SharpDX.Direct3D11;
using DXGI = SharpDX.DXGI;

namespace ObjLoader.Building
{
    public class Building : IDrawEntity
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct VsInput
        {
            public Vector3 Pos;

            public VsInput(Vector3 pos)
            {
                Pos = pos;
            }
        }

        private D3D.Buffer _vertexBuffer;
        private D3D.VertexBufferBinding _vertexBinding;
        private D3D.InputLayout _inputLayout;
        private D3D.VertexShader _vertexShader;
        private D3D.PixelShader _pixelShader;
        private D3D.Buffer _indexBuffer;

        private readonly VsInput[] _vertices;

        private readonly uint[] _facesIndexes;
        private D3D.Buffer _wvpBuffer;
        private Matrix _wvp;
        private Matrix _proj;
        private Matrix _view;
        private D3D.GeometryShader _geometryShader;

        public Building(string pathToFile)
        {
            var obj = FileFormatObj.Load(pathToFile, false);

            // Get vertices.
            _vertices = obj.Model.Vertices.Select(v =>
            {
                var pos = new Vector3(v.x / 5, v.y / 5, (float)(v.z / 5 + 0.5));
                return new VsInput(pos);
            }).ToArray();

            // Get triangles indexes.
            var triangles = new List<uint>();
            foreach (var face in obj.Model.UngroupedFaces)
            {
                for (var i = 1; i < face.Indices.Count - 1; i++)
                {
                    triangles.Add((uint)face.Indices[0].vertex);
                    triangles.Add((uint)face.Indices[i].vertex);
                    triangles.Add((uint)face.Indices[i + 1].vertex);
                }
            }

            // Get edges.
            var edges = new Dictionary<Tuple<uint, uint>, uint[]>();
            uint[] foundIndexes;
            for (int i = 0; i < triangles.Count; i += 3)
            {
                var sides = new[]
                {
                    new[] { triangles[i + 0], triangles[i + 1] }.OrderBy(u => u).Concat(new [] {triangles[i+2]}).ToArray(),
                    new[] { triangles[i + 1], triangles[i + 2] }.OrderBy(u => u).Concat(new [] {triangles[i+0]}).ToArray(),
                    new[] { triangles[i + 2], triangles[i + 0] }.OrderBy(u => u).Concat(new [] {triangles[i+1]}).ToArray(),
                };

                foreach (var side in sides)
                {
                    var tuple = new Tuple<uint, uint>(side[0], side[1]);

                    if (edges.TryGetValue(tuple, out foundIndexes))
                    {
                        foundIndexes[1] = side[2];
                    }
                    else
                    {
                        foundIndexes = new[] { side[2], uint.MaxValue };
                    }
                    edges[tuple] = foundIndexes;
                }
            }

            // Fill adjacency.
            var trianglesWAdj = new List<uint>(triangles.Count * 3);
            for (int i = 0; i < triangles.Count; i += 3)
            {
                var sides = new[]
                {
                    new[] { triangles[i + 0], triangles[i + 1] }.OrderBy(u => u).Concat(new [] {triangles[i+2]}).Concat(new [] {triangles[i+0]}).ToArray(),
                    new[] { triangles[i + 1], triangles[i + 2] }.OrderBy(u => u).Concat(new [] {triangles[i+0]}).Concat(new [] {triangles[i+1]}).ToArray(),
                    new[] { triangles[i + 2], triangles[i + 0] }.OrderBy(u => u).Concat(new [] {triangles[i+1]}).Concat(new [] {triangles[i+2]}).ToArray(),
                };

                foreach (var side in sides)
                {
                    // Add real point.
                    trianglesWAdj.Add(side[3]);

                    // Add adjacent point.
                    if (!edges.TryGetValue(new Tuple<uint, uint>(side[0], side[1]), out foundIndexes))
                    {
                        trianglesWAdj.Add(uint.MaxValue);
                    }
                    else
                    {
                        foundIndexes = foundIndexes.Except(new[] { side[2] }).ToArray();
                        trianglesWAdj.Add(foundIndexes.First());
                    }
                }
            }

            _facesIndexes = trianglesWAdj.ToArray();
        }

        public void InitDraw(DrawManager drawMan)
        {
            _vertexBuffer = D3D.Buffer.Create<VsInput>(drawMan.Device, D3D.BindFlags.VertexBuffer, _vertices);
            _vertexBinding = new D3D.VertexBufferBinding(_vertexBuffer, Utilities.SizeOf<VsInput>(), 0);
            _indexBuffer = D3D.Buffer.Create<uint>(drawMan.Device, D3D.BindFlags.IndexBuffer, _facesIndexes);

            using (var bytecode = ShaderBytecode.CompileFromFile(@"Building\building.fx", "Vs", "vs_4_0", ShaderFlags.Debug))
            {
                _vertexShader = new D3D.VertexShader(drawMan.Device, bytecode);
                _inputLayout = new D3D.InputLayout(drawMan.Device, bytecode, new[]
                {
                    new D3D.InputElement("POSITION", 0, DXGI.Format.R32G32B32_Float, 0, 0),
                });
            }

            using (var bytecode = ShaderBytecode.CompileFromFile(@"Building\building.fx", "Ps", "ps_4_0", ShaderFlags.Debug))
            {
                _pixelShader = new D3D.PixelShader(drawMan.Device, bytecode);
            }

            using (var bytecode = ShaderBytecode.CompileFromFile(@"Building\building.fx", "Gs", "gs_4_0", ShaderFlags.Debug))
            {
                _geometryShader = new D3D.GeometryShader(drawMan.Device, bytecode);
            }

            _wvpBuffer = D3D.Buffer.Create(drawMan.Device, D3D.BindFlags.ConstantBuffer, ref _wvp);

            _view = SharpDX.Matrix.LookAtLH(new Vector3(0, 0, -5), new Vector3(0, 0, 0), Vector3.UnitY);
            _proj = SharpDX.Matrix.PerspectiveFovLH((float)Math.PI / 4.0f, (float)(drawMan.Width / drawMan.Height), 0.1f, 100.0f);

            CalcWvp(drawMan);
        }

        private void CalcWvp(DrawManager drawMan)
        {
            var periodX = 8000.0;
            var periodY = 16000.0;
            var periodZ = 32000.0;
            _wvp = Matrix.RotationY((float)(2 * Math.PI * (drawMan.Time.ElapsedMilliseconds % periodX) / periodX))
                * Matrix.RotationX((float)(2 * Math.PI * (drawMan.Time.ElapsedMilliseconds % periodY) / periodY))
                * Matrix.RotationZ((float)(2 * Math.PI * (drawMan.Time.ElapsedMilliseconds % periodZ) / periodZ))
                * _view
                * _proj;
            _wvp.Transpose();
        }

        public void DeinitDraw()
        {
            _wvpBuffer.Dispose();
            _vertexBuffer.Dispose();
            _indexBuffer.Dispose();
            _pixelShader.Dispose();
            _geometryShader.Dispose();
            _vertexShader.Dispose();
            _inputLayout.Dispose();
        }

        public void Render(DrawManager drawMan)
        {
            // Set wvp.
            CalcWvp(drawMan);
            drawMan.Context.UpdateSubresource(ref _wvp, _wvpBuffer);

            // Draw front faces.
            drawMan.Context.InputAssembler.InputLayout = _inputLayout;
            drawMan.Context.VertexShader.Set(_vertexShader);
            drawMan.Context.GeometryShader.Set(_geometryShader);
            drawMan.Context.PixelShader.Set(_pixelShader);
            drawMan.Context.VertexShader.SetConstantBuffer(0, _wvpBuffer);
            drawMan.Context.InputAssembler.SetVertexBuffers(0, _vertexBinding);
            drawMan.Context.InputAssembler.SetIndexBuffer(_indexBuffer, DXGI.Format.R32_UInt, 0);
            drawMan.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleListWithAdjacency;
            drawMan.Context.DrawIndexed(_facesIndexes.Length, 0, 0);
        }
    }
}
