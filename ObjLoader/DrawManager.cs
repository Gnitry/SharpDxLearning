using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Graphics.Display;
using Windows.UI.Composition;
using Windows.UI.Xaml.Controls;
using SharpDX;
using SharpDX.Direct3D;
using CompositionTarget = Windows.UI.Xaml.Media.CompositionTarget;
using D3D = SharpDX.Direct3D11;
using DXGI = SharpDX.DXGI;

namespace ObjLoader
{
    public class DrawManager : IDisposable
    {
        private SwapChainPanel _panel;
        private ViewportF _viewport;
        private List<IDrawEntity> _entities = new List<IDrawEntity>();

        public D3D.Device4 Device { get; private set; }
        public D3D.DeviceContext3 Context { get; private set; }
        public DXGI.SwapChain1 SwapChain { get; private set; }

        public double Width { get; private set; }
        public double Height { get; private set; }

        public DrawManager(SwapChainPanel panel, params IDrawEntity[] entities)
        {
            _panel = panel;
            _entities = entities.ToList();
        }

        private void CompositionTarget_Rendering(object sender, object e)
        {
            foreach (var drawEntity in _entities)
            {
                drawEntity.Render(this);
            }

            SwapChain.Present(1, DXGI.PresentFlags.None);
        }

        public void Dispose()
        {
            Deinit();
        }

        public void Add(IDrawEntity entity)
        {
            _entities.Add(entity);
            entity.InitDraw(this);
        }

        public void Init()
        {
            Width = _panel.RenderSize.Width;
            Height = _panel.RenderSize.Height;

            using (var defDevice = new D3D.Device(DriverType.Hardware, D3D.DeviceCreationFlags.Debug))
            {
                Device = defDevice.QueryInterface<D3D.Device4>();
            }
            Context = Device.ImmediateContext.QueryInterface<D3D.DeviceContext3>();

            var pixelScale = DisplayInformation.GetForCurrentView().LogicalDpi / 96.0;
            var swapChainDesc = new DXGI.SwapChainDescription1()
            {
                AlphaMode = DXGI.AlphaMode.Ignore,
                BufferCount = 2,
                Flags = DXGI.SwapChainFlags.None,
                Format = DXGI.Format.B8G8R8A8_UNorm,
                Height = (int)(Height * pixelScale),
                Width = (int)(Width * pixelScale),
                SampleDescription = new DXGI.SampleDescription(1, 0),
                Scaling = DXGI.Scaling.Stretch,
                Stereo = false,
                SwapEffect = DXGI.SwapEffect.FlipSequential,
                Usage = DXGI.Usage.BackBuffer | DXGI.Usage.RenderTargetOutput
            };

            using (var dxgiDevice = Device.QueryInterface<DXGI.Device>())
            {
                using (var factory = dxgiDevice.Adapter.GetParent<DXGI.Factory4>())
                {
                    SwapChain = new DXGI.SwapChain1(factory, Device, ref swapChainDesc);
                }
            }

            using (var nativePanel = ComObject.As<DXGI.ISwapChainPanelNative2>(_panel))
            {
                nativePanel.SwapChain = SwapChain;
            }

            _viewport = new ViewportF(0, 0, (float)Width, (float)Height, 0, 1);

            foreach (var drawEntity in _entities)
            {
                drawEntity.InitDraw(this);
            }

            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        public void Deinit()
        {
            if (Device == null) return;
            CompositionTarget.Rendering -= CompositionTarget_Rendering;

            foreach (var drawEntity in _entities)
            {
                drawEntity.DeinitDraw();
            }

            SwapChain.Dispose();
            SwapChain = null;
            Context.Dispose();
            Context = null;
            Device.Dispose();
            Device = null;
        }
    }
}
