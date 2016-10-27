using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Graphics.Display;
using Windows.UI.Composition;
using Windows.UI.Xaml.Controls;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Mathematics.Interop;
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
        private D3D.Texture2D _depthBuffer;
        private D3D.Texture2D _backBuffer;

        public D3D.Device4 Device { get; private set; }
        public D3D.DeviceContext3 Context { get; private set; }
        public DXGI.SwapChain1 SwapChain { get; private set; }
        public D3D.RenderTargetView1 RenderView { get; private set; }
        public D3D.DepthStencilView DepthView { get; private set; }

        public double Width { get; private set; }
        public double Height { get; private set; }

        public DrawManager(SwapChainPanel panel, params IDrawEntity[] entities)
        {
            _panel = panel;
            _entities = entities.ToList();
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

            // Initialize device, context and swap chain.

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
                Width = (int)(Width * pixelScale),
                Height = (int)(Height * pixelScale),
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

            // Initialize depth and render views.

            _depthBuffer = new D3D.Texture2D(Device, new D3D.Texture2DDescription()
            {
                Format = DXGI.Format.D24_UNorm_S8_UInt,
                ArraySize = 1,
                MipLevels = 1,
                Width = swapChainDesc.Width,
                Height = swapChainDesc.Height,
                SampleDescription = new DXGI.SampleDescription(1, 0),
                BindFlags = D3D.BindFlags.DepthStencil,
            });
            DepthView = new D3D.DepthStencilView(Device, _depthBuffer, new D3D.DepthStencilViewDescription()
            {
                Dimension = D3D.DepthStencilViewDimension.Texture2D
            });

            _backBuffer = D3D.Resource.FromSwapChain<D3D.Texture2D>(SwapChain, 0);
            RenderView = new D3D.RenderTargetView1(Device, _backBuffer);

            // Initialize viewport.

            _viewport = new ViewportF(0, 0, (float)Width, (float)Height, 0, 1);
            Context.Rasterizer.SetViewport(_viewport);

            // Initialize others.

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
            _backBuffer.Dispose();
            _backBuffer = null;
            RenderView.Dispose();
            RenderView = null;
            _depthBuffer.Dispose();
            _depthBuffer = null;
            DepthView.Dispose();
            DepthView = null;
        }

        private void CompositionTarget_Rendering(object sender, object e)
        {
            Context.OutputMerger.SetRenderTargets(DepthView, RenderView);

            Context.ClearDepthStencilView(DepthView, D3D.DepthStencilClearFlags.Depth, 1.0f, 0);
            Context.ClearRenderTargetView(RenderView, Color.Black);

            foreach (var drawEntity in _entities)
            {
                drawEntity.Render(this);
            }

            SwapChain.Present(1, DXGI.PresentFlags.None);
        }
    }
}
