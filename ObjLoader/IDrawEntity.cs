using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjLoader
{
    public interface IDrawEntity
    {
        void InitDraw(DrawManager drawMan);

        void DeinitDraw();

        void Render(DrawManager drawMan);
    }
}
