using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using D3D = SharpDX.Direct3D11;
using DXGI = SharpDX.DXGI;

namespace ObjLoader
{
    public class GreenRect : IDrawEntity, IDisposable
    {
        private D3D.Texture2D _backBuffer;
        private D3D.Texture2D _depthBuffer;
        private D3D.DepthStencilView _depthView;
        private D3D.RenderTargetView1 _renderView;

        public void InitDraw(DrawManager drawMan)
        {
            DeinitDraw();

            _backBuffer = D3D.Resource.FromSwapChain<D3D.Texture2D>(drawMan.SwapChain, 0);
            _renderView = new D3D.RenderTargetView1(drawMan.Device, _backBuffer);

            _depthBuffer = new D3D.Texture2D(drawMan.Device, new D3D.Texture2DDescription()
            {
                ArraySize = 1,
                BindFlags = D3D.BindFlags.DepthStencil,
                CpuAccessFlags = D3D.CpuAccessFlags.None,
                Height = (int)drawMan.Height,
                Width = (int)drawMan.Width,
                Format = DXGI.Format.D24_UNorm_S8_UInt,
                Usage = D3D.ResourceUsage.Default,
                SampleDescription = new DXGI.SampleDescription(1, 0),
                MipLevels = 1,
                OptionFlags = D3D.ResourceOptionFlags.None
            });
            _depthView = new D3D.DepthStencilView(drawMan.Device, _depthBuffer);
        }

        public void DeinitDraw()
        {
            _renderView?.Dispose();
            _depthView?.Dispose();
            _backBuffer?.Dispose();
            _depthBuffer?.Dispose();
        }

        public void Render(DrawManager drawMan)
        {
            drawMan.Context.ClearDepthStencilView(_depthView, D3D.DepthStencilClearFlags.Stencil, 1, 0);
            drawMan.Context.ClearRenderTargetView(_renderView, SharpDX.Color.Green);
        }

        public void Dispose()
        {
            DeinitDraw();
        }
    }
}
