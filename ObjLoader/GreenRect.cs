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
        public void InitDraw(DrawManager drawMan)
        {
            DeinitDraw();
        }

        public void DeinitDraw()
        {
        }

        public void Render(DrawManager drawMan)
        {
            drawMan.Context.ClearRenderTargetView(drawMan.RenderView, SharpDX.Color.Green);
        }

        public void Dispose()
        {
            DeinitDraw();
        }
    }
}
