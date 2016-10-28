using System;
using System.Collections.Generic;
using System.Linq;
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
        private D3D.Buffer _vertexBuffer;
        private D3D.VertexBufferBinding _vertexBinding;
        private D3D.InputLayout _inputLayout;
        private D3D.VertexShader _vertexShader;
        private D3D.PixelShader _pixelShader;
        private D3D.Buffer _indexBuffer;

        private readonly Vector3[] _vertices;

        private readonly uint[] _facesIndexes;
        private D3D.Buffer _wvpBuffer;
        private Matrix _wvp;
        private Matrix _proj;
        private Matrix _view;

        public Building(string pathToFile)
        {
            var obj = FileFormatObj.Load(pathToFile, false);
            _vertices = obj.Model.Vertices.Select(v => new Vector3(v.x / 5, v.y / 5, (float)(v.z / 5 + 0.5))).ToArray();

            var indexes = new List<uint>();
            foreach (var face in obj.Model.UngroupedFaces)
            {
                for (var i = 1; i < face.Indices.Count - 1; i++)
                {
                    indexes.Add((uint)face.Indices[0].vertex);
                    indexes.Add((uint)face.Indices[i].vertex);
                    indexes.Add((uint)face.Indices[i + 1].vertex);
                }
            }
            _facesIndexes = indexes.ToArray();
        }

        public void InitDraw(DrawManager drawMan)
        {
            _vertexBuffer = D3D.Buffer.Create<Vector3>(drawMan.Device, D3D.BindFlags.VertexBuffer, _vertices);
            _vertexBinding = new D3D.VertexBufferBinding(_vertexBuffer, Utilities.SizeOf<Vector3>(), 0);
            _indexBuffer = D3D.Buffer.Create<uint>(drawMan.Device, D3D.BindFlags.IndexBuffer, _facesIndexes);

            using (var byteCode = ShaderBytecode.CompileFromFile(@"Building\building.fx", "vs", "vs_4_0", ShaderFlags.Debug))
            {
                _vertexShader = new D3D.VertexShader(drawMan.Device, byteCode);
                _inputLayout = new D3D.InputLayout(drawMan.Device, byteCode, new D3D.InputElement[]
                {
                    new D3D.InputElement("POSITION", 0, DXGI.Format.R32G32B32_Float, 0, 0),
                });
            }

            using (var byteCode = ShaderBytecode.CompileFromFile(@"Building\building.fx", "ps", "ps_4_0", ShaderFlags.Debug))
            {
                _pixelShader = new D3D.PixelShader(drawMan.Device, byteCode);
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
            drawMan.Context.PixelShader.Set(_pixelShader);
            drawMan.Context.VertexShader.SetConstantBuffer(0, _wvpBuffer);
            drawMan.Context.InputAssembler.SetVertexBuffers(0, _vertexBinding);
            drawMan.Context.InputAssembler.SetIndexBuffer(_indexBuffer, DXGI.Format.R32_UInt, 0);
            drawMan.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            drawMan.Context.DrawIndexed(_facesIndexes.Length, 0, 0);

            // Draw edges.
            drawMan.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.LineStrip;
            drawMan.Context.DrawIndexed();
        }
    }
}
