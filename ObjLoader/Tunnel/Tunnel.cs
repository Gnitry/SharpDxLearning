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
using SharpDX.Mathematics.Interop;
using D3D = SharpDX.Direct3D11;
using DXGI = SharpDX.DXGI;

namespace ObjLoader.Building
{
    public class Tunnel : IDrawEntity
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct VsInput
        {
            public Vector4 Pos;

            public VsInput(Vector4 pos)
            {
                Pos = pos;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ColorsBufferStruct
        {
            public Color4 FaceColor;
            public Color4 OutlineColor;
        }

        private D3D.Buffer _vertexBuffer;
        private D3D.VertexBufferBinding _vertexBinding;
        private D3D.InputLayout _inputLayout;
        private D3D.VertexShader _vertexShader;
        private D3D.PixelShader _pixelShader;
        private D3D.Buffer _indexBuffer;
        private ColorsBufferStruct _colors;

        private readonly VsInput[] _vertices;

        private readonly uint[] _indexes;
        private D3D.Buffer _wvpBuffer;
        private Matrix _wvp;
        private Matrix _proj;
        private Matrix _view;
        private D3D.GeometryShader _geometryShader;
        private D3D.Buffer _colorsBuffer;

        public Tunnel(string pathToFile)
        {
            var obj = FileFormatObj.Load(pathToFile, false);

            // Get vertices.
            _vertices = obj.Model.Vertices.Select(v =>
            {
                var scale = 10f;
                var pos = new Vector4(v.x / scale, v.y / scale, v.z / scale, 1.0f);
                return new VsInput(pos);
            }).ToArray();

            var indicies = new List<uint>();

            // Iterate groups.
            foreach (var meshObject in obj.Model.MeshObjects)
            {
                var groupIndicies = new List<uint>();

                // Get triangles indexes.
                foreach (var face in meshObject.Faces)
                {
                    var faceIndicies = face.Indices.ToList();
                    for (int i = faceIndicies.Count - 2; i >= 0; i--) if (faceIndicies[i].vertex == faceIndicies[i + 1].vertex) faceIndicies.RemoveAt(i + 1);
                    if (faceIndicies.Count > 3 && faceIndicies.First().vertex == faceIndicies.Last().vertex) faceIndicies.RemoveAt(faceIndicies.Count);
                    for (var i = 1; i < faceIndicies.Count - 1; i++)
                    {
                        groupIndicies.Add((uint)faceIndicies[0].vertex);
                        groupIndicies.Add((uint)faceIndicies[i].vertex);
                        groupIndicies.Add((uint)faceIndicies[i + 1].vertex);
                    }
                }

                // Get edges.
                var edges = new Dictionary<Tuple<uint, uint>, uint[]>();
                uint[] foundIndexes;
                for (int i = 0; i < groupIndicies.Count; i += 3)
                {
                    var sides = new[]
                    {
                    new[] { groupIndicies[i + 0], groupIndicies[i + 1] }.OrderBy(u => u).Concat(new [] {groupIndicies[i+2]}).ToArray(),
                    new[] { groupIndicies[i + 1], groupIndicies[i + 2] }.OrderBy(u => u).Concat(new [] {groupIndicies[i+0]}).ToArray(),
                    new[] { groupIndicies[i + 2], groupIndicies[i + 0] }.OrderBy(u => u).Concat(new [] {groupIndicies[i+1]}).ToArray(),
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
                var indexesWAdj = new List<uint>(groupIndicies.Count * 2);
                for (int i = 0; i < groupIndicies.Count; i += 3)
                {
                    var sides = new[]
                    {
                    new[] { groupIndicies[i + 0], groupIndicies[i + 1] }.OrderBy(u => u).Concat(new [] {groupIndicies[i+2]}).Concat(new [] {groupIndicies[i+0]}).ToArray(),
                    new[] { groupIndicies[i + 1], groupIndicies[i + 2] }.OrderBy(u => u).Concat(new [] {groupIndicies[i+0]}).Concat(new [] {groupIndicies[i+1]}).ToArray(),
                    new[] { groupIndicies[i + 2], groupIndicies[i + 0] }.OrderBy(u => u).Concat(new [] {groupIndicies[i+1]}).Concat(new [] {groupIndicies[i+2]}).ToArray(),
                };

                    foreach (var side in sides)
                    {
                        // Add real point.
                        indexesWAdj.Add(side[3]);

                        // Add adjacent point.
                        if (!edges.TryGetValue(new Tuple<uint, uint>(side[0], side[1]), out foundIndexes))
                        {
                            indexesWAdj.Add(uint.MaxValue);
                        }
                        else
                        {
                            foundIndexes = foundIndexes.Except(new[] { side[2] }).ToArray();
                            indexesWAdj.Add(foundIndexes.First());
                        }
                    }
                }

                for (int i = 0; i < indexesWAdj.Count; i += 6)
                {
                    if (indexesWAdj[i + 1] == uint.MaxValue) indexesWAdj[i + 1] = indexesWAdj[i + 0];
                    if (indexesWAdj[i + 3] == uint.MaxValue) indexesWAdj[i + 3] = indexesWAdj[i + 2];
                    if (indexesWAdj[i + 5] == uint.MaxValue) indexesWAdj[i + 5] = indexesWAdj[i + 4];
                }

                indicies.AddRange(indexesWAdj.ToArray());
            }
            _indexes = indicies.ToArray();
        }

        public void InitDraw(DrawManager drawMan)
        {
            _vertexBuffer = D3D.Buffer.Create<VsInput>(drawMan.Device, D3D.BindFlags.VertexBuffer, _vertices);
            _vertexBinding = new D3D.VertexBufferBinding(_vertexBuffer, Utilities.SizeOf<VsInput>(), 0);
            _indexBuffer = D3D.Buffer.Create<uint>(drawMan.Device, D3D.BindFlags.IndexBuffer, _indexes);

            using (var bytecode = ShaderBytecode.CompileFromFile(@"Tunnel\outlined.fx", "Vs", "vs_4_0", ShaderFlags.Debug | ShaderFlags.SkipOptimization))
            {
                _vertexShader = new D3D.VertexShader(drawMan.Device, bytecode);
                _inputLayout = new D3D.InputLayout(drawMan.Device, bytecode, new[]
                {
                    new D3D.InputElement("POSITION", 0, DXGI.Format.R32G32B32A32_Float, 0, 0),
                });
            }

            using (var bytecode = ShaderBytecode.CompileFromFile(@"Tunnel\outlined.fx", "Ps", "ps_4_0", ShaderFlags.Debug | ShaderFlags.SkipOptimization))
            {
                _pixelShader = new D3D.PixelShader(drawMan.Device, bytecode);
            }

            using (var bytecode = ShaderBytecode.CompileFromFile(@"Tunnel\outlined.fx", "Gs", "gs_4_0", ShaderFlags.Debug | ShaderFlags.SkipOptimization))
            {
                _geometryShader = new D3D.GeometryShader(drawMan.Device, bytecode);
            }

//            using (var effectByteCode = ShaderBytecode.CompileFromFile(@"Tunnel\outlined.fx", "fx_5_0", ShaderFlags.None, EffectFlags.None))
//            {
//                var _effect = new D3D.Effect(drawMan.Device, effectByteCode);
//            }

            _wvpBuffer = D3D.Buffer.Create(drawMan.Device, D3D.BindFlags.ConstantBuffer, ref _wvp);

            _colors.FaceColor = drawMan.RawBackColor;
            //            _colors.FaceColor.Green = 1.0f;
            //            _colors.FaceColor.Alpha = 1.0f;
            _colors.OutlineColor = Color4.White;
            _colorsBuffer = D3D.Buffer.Create(drawMan.Device, D3D.BindFlags.ConstantBuffer, ref _colors);
            drawMan.Context.UpdateSubresource(ref _colors, _colorsBuffer);

            _view = Matrix.LookAtLH(new Vector3(0, 0, -5), new Vector3(0, 0, 0), Vector3.UnitY);
            _proj = Matrix.PerspectiveFovLH((float)Math.PI / 4.0f, (float)(drawMan.Width / drawMan.Height), 0.1f, 100.0f);

            CalcWvp(drawMan);
        }

        private void CalcWvp(DrawManager drawMan)
        {
            float k = 2;
            var periodX = 8000.0 * k;
            var periodY = 16000.0 * k;
            var periodZ = 32000.0 * k;
            _wvp =
                Matrix.RotationY((float)(2 * Math.PI * (drawMan.Time.ElapsedMilliseconds % periodY) / periodY))
                * Matrix.RotationX((float)(2 * Math.PI * (drawMan.Time.ElapsedMilliseconds % periodX) / periodX))
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
            drawMan.Context.GeometryShader.SetConstantBuffer(0, _colorsBuffer);
            drawMan.Context.InputAssembler.SetVertexBuffers(0, _vertexBinding);
            drawMan.Context.InputAssembler.SetIndexBuffer(_indexBuffer, DXGI.Format.R32_UInt, 0);
            drawMan.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleListWithAdjacency;
            drawMan.Context.DrawIndexed(_indexes.Length, 0, 0);
        }
    }
}
