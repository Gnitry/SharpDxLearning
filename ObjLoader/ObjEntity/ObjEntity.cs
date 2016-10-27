using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using D3D = SharpDX.Direct3D11;
using DXGI = SharpDX.DXGI;

namespace ObjLoader.ObjEntity
{
    public class ObjEntity : IDrawEntity
    {
        private D3D.Buffer _vertexBuffer;
        private D3D.VertexBufferBinding _vertexBinding;
        private D3D.InputLayout _inputLayout;
        private D3D.VertexShader _vertexShader;
        private D3D.PixelShader _pixelShader;
        private D3D.Buffer _indexBuffer;

        private Vector3[] _vertices =
        {
            new Vector3(0.0f, 0f, -0.5f),
            new Vector3(0.5f, 0.5f, 1.5f),
            new Vector3(0.5f, 0f, 1.0f),
        };

        private uint[] _indexes = { 0, 1, 2 };

        public void InitDraw(DrawManager drawMan)
        {
            _vertexBuffer = D3D.Buffer.Create<Vector3>(drawMan.Device, D3D.BindFlags.VertexBuffer, _vertices);
            _vertexBinding = new D3D.VertexBufferBinding(_vertexBuffer, Utilities.SizeOf<Vector3>(), 0);
            _indexBuffer = D3D.Buffer.Create<uint>(drawMan.Device, D3D.BindFlags.IndexBuffer, _indexes);


            using (var byteCode = ShaderBytecode.CompileFromFile(@"ObjEntity\objEntity.fx", "vs", "vs_4_0", ShaderFlags.Debug))
            {
                _vertexShader = new D3D.VertexShader(drawMan.Device, byteCode);
                _inputLayout = new D3D.InputLayout(drawMan.Device, byteCode, new D3D.InputElement[]
                {
                    new D3D.InputElement("POSITION", 0, DXGI.Format.R32G32B32_Float, 0, 0),
                });
            }

            using (var byteCode = ShaderBytecode.CompileFromFile(@"ObjEntity\objEntity.fx", "ps", "ps_4_0", ShaderFlags.Debug))
            {
                _pixelShader = new D3D.PixelShader(drawMan.Device, byteCode);
            }
        }

        public void DeinitDraw()
        {
            _vertexBuffer.Dispose();
            _indexBuffer.Dispose();
            _pixelShader.Dispose();
            _vertexShader.Dispose();
            _inputLayout.Dispose();
        }

        public void Render(DrawManager drawMan)
        {
            drawMan.Context.InputAssembler.InputLayout = _inputLayout;
            drawMan.Context.VertexShader.Set(_vertexShader);
            drawMan.Context.PixelShader.Set(_pixelShader);

            drawMan.Context.InputAssembler.SetVertexBuffers(0, _vertexBinding);
            drawMan.Context.InputAssembler.SetIndexBuffer(_indexBuffer, DXGI.Format.R32_UInt, 0);
            drawMan.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            drawMan.Context.DrawIndexed(_indexes.Length, 0, 0);
        }
    }
}
