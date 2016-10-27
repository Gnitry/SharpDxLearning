using System;

namespace ObjLoader.SolidEntity
{
    public class SolidEntity : IDrawEntity, IDisposable
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
            drawMan.Context.ClearRenderTargetView(drawMan.RenderView, new SharpDX.Color(0x02, 0x88, 0xd1));
        }

        public void Dispose()
        {
            DeinitDraw();
        }
    }
}
