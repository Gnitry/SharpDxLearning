using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using D3D = SharpDX.Direct3D11;

namespace ObjLoader.Triangle
{
    public class Triangle : IDrawEntity
    {
        private D3D.Buffer _vertexBuffer;
        private D3D.VertexShader _vertexShader;
        private D3D.PixelShader _pixelShader;
        private D3D.VertexBufferBinding _vertexBinding;
        private D3D.InputLayout _inputLayout;

        private readonly Vector4[] _vertexes = new[]
            {
                new Vector4(-0.5f, 0.0f, 0.0f, 1.0f),
                new Vector4(0.0f, 0.5f, 0.0f, 1.0f),
                new Vector4(0.5f, 0.0f, 0.0f, 1.0f),
            };

        public void InitDraw(DrawManager drawMan)
        {
            using (var byteCode = ShaderBytecode.CompileFromFile(@"Triangle\triangle.fx", "vs", "vs_4_0", ShaderFlags.Debug))
            {
                _vertexShader = new D3D.VertexShader(drawMan.Device, byteCode);

                var inputElements = new D3D.InputElement[]
                {
                    new D3D.InputElement("POSITION", 0, Format.R32G32B32A32_Float, 0, 0),
                };
                _inputLayout = new D3D.InputLayout(drawMan.Device, byteCode, inputElements);
            }

            using (var byteCode = ShaderBytecode.CompileFromFile(@"Triangle\triangle.fx", "ps", "ps_4_0", ShaderFlags.Debug))
            {
                _pixelShader = new D3D.PixelShader(drawMan.Device, byteCode);
            }

            _vertexBuffer = D3D.Buffer.Create(drawMan.Device, D3D.BindFlags.VertexBuffer, _vertexes);
            _vertexBinding = new D3D.VertexBufferBinding(_vertexBuffer, Utilities.SizeOf<Vector4>(), 0);
        }

        public void DeinitDraw()
        {
            _vertexBuffer.Dispose();
            _vertexShader.Dispose();
            _pixelShader.Dispose();
        }

        public void Render(DrawManager drawMan)
        {
            drawMan.Context.InputAssembler.SetVertexBuffers(0, _vertexBinding);
            drawMan.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            drawMan.Context.InputAssembler.InputLayout = _inputLayout;
            drawMan.Context.VertexShader.Set(_vertexShader);
            drawMan.Context.PixelShader.Set(_pixelShader);
            drawMan.Context.Draw(_vertexes.Length, 0);
        }
    }
}
