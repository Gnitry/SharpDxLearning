using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.IO;
using SharpDX.Mathematics.Interop;
using D3D = SharpDX.Direct3D11;
using DXGI = SharpDX.DXGI;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace UwpTriangle
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private D3D.Device3 _device;
        private D3D.DeviceContext3 _context;
        private DXGI.SwapChain3 _swapChain;
        private D3D.DepthStencilView _depthStencilView;
        private D3D.Texture2D _backBuffer;
        private D3D.RenderTargetView1 _renderView;
        private D3D.InputLayout _inputLayout;
        private D3D.Buffer _vertexBuffer;
        private D3D.VertexBufferBinding _vertexBinding;
        private Vector4[] _vertices;
        private D3D.VertexShader _vertexShader;
        private D3D.PixelShader _pixelShader;
        private Stopwatch _timer;
        private D3D.Buffer _constantBuffer;

        public MainPage()
        {
            this.InitializeComponent();
        }

        private void SwapChainPanel_OnLoaded(object sender, RoutedEventArgs e)
        {
            using (var defDevice = new D3D.Device(DriverType.Hardware, D3D.DeviceCreationFlags.Debug))
            {
                _device = defDevice.QueryInterface<D3D.Device3>();
            }
            _context = _device.ImmediateContext3;

            var pixelScale = DisplayInformation.GetForCurrentView().LogicalDpi / 96.0f;
            var swapChainDesc = new DXGI.SwapChainDescription1()
            {
                AlphaMode = DXGI.AlphaMode.Ignore,
                BufferCount = 2,
                Flags = DXGI.SwapChainFlags.None,
                Format = DXGI.Format.B8G8R8A8_UNorm,
                Width = (int)(panel.RenderSize.Width * pixelScale),
                Height = (int)(panel.RenderSize.Height * pixelScale),
                SampleDescription = new DXGI.SampleDescription(1, 0),
                Scaling = DXGI.Scaling.Stretch,
                Stereo = false,
                SwapEffect = DXGI.SwapEffect.FlipSequential,
                Usage = DXGI.Usage.BackBuffer | DXGI.Usage.RenderTargetOutput
            };

            using (var dxgiDevice = _device.QueryInterface<DXGI.Device3>())
            {
                var factory = dxgiDevice.Adapter.GetParent<DXGI.Factory4>();
                using (var tmpSwapChain = new DXGI.SwapChain1(factory, _device, ref swapChainDesc))
                {
                    _swapChain = tmpSwapChain.QueryInterface<DXGI.SwapChain3>();
                }
            }

            using (var nativeObject = ComObject.As<DXGI.ISwapChainPanelNative>(panel))
            {
                nativeObject.SwapChain = _swapChain;
            }

            using (var depthBuffer = new D3D.Texture2D(_device, new D3D.Texture2DDescription()
            {
                Format = DXGI.Format.D24_UNorm_S8_UInt,
                ArraySize = 1,
                MipLevels = 1,
                Width = swapChainDesc.Width,
                Height = swapChainDesc.Height,
                SampleDescription = new DXGI.SampleDescription(1, 0),
                BindFlags = D3D.BindFlags.DepthStencil,
            }))
            {
                _depthStencilView = new D3D.DepthStencilView(_device, depthBuffer, new D3D.DepthStencilViewDescription()
                {
                    Dimension = D3D.DepthStencilViewDimension.Texture2D
                });
            }

            _backBuffer = D3D.Resource.FromSwapChain<D3D.Texture2D>(_swapChain, 0);
            _renderView = new D3D.RenderTargetView1(_device, _backBuffer);

            var viewport = new ViewportF(0, 0, (float)panel.RenderSize.Width, (float)panel.RenderSize.Height, 0.0f, 1.0f);
            _context.Rasterizer.SetViewport(viewport);

            ShaderBytecode shaderBytecode;
            using (shaderBytecode = ShaderBytecode.CompileFromFile("shaders.fx", "vs", "vs_4_0", ShaderFlags.Debug))
            {
                _vertexShader = new D3D.VertexShader(_device, shaderBytecode);
            }

            using (var byteCode = ShaderBytecode.CompileFromFile(@"shaders.fx", "ps", "ps_4_0", ShaderFlags.Debug))
            {
                _pixelShader = new D3D.PixelShader(_device, byteCode);
            }

            D3D.InputElement[] inputElements =
            {
                new D3D.InputElement("POSITION", 0, DXGI.Format.R32G32B32A32_Float, 0, 0),
            };
            _inputLayout = new D3D.InputLayout(_device, shaderBytecode, inputElements);

            _vertices = new[]
            {
                new Vector4(-0.5f, 0.0f, 0.0f, 1.0f),
                new Vector4(0.0f, 0.5f, 0.5f, 1.0f),
                new Vector4(0.5f, 0.0f, 0.0f, 1.0f),
            };
            _vertexBuffer = D3D.Buffer.Create(_device, D3D.BindFlags.VertexBuffer, _vertices);
            _vertexBinding = new D3D.VertexBufferBinding(_vertexBuffer, Utilities.SizeOf<Vector4>(), 0);

            _constantBuffer = new SharpDX.Direct3D11.Buffer(
                _device,
                Utilities.SizeOf<SharpDX.Matrix>(),
                D3D.ResourceUsage.Default,
                D3D.BindFlags.ConstantBuffer,
                D3D.CpuAccessFlags.None,
                D3D.ResourceOptionFlags.None,
                0);

            _timer = new Stopwatch();
            _timer.Start();

            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        private void CompositionTarget_Rendering(object sender, object e)
        {
            _context.OutputMerger.SetRenderTargets(_depthStencilView, _renderView);
            _context.ClearDepthStencilView(_depthStencilView, D3D.DepthStencilClearFlags.Depth, 1.0f, 0);
            _context.ClearRenderTargetView(_renderView, new RawColor4(1f, 0.5f, 0.2f, 1f));

            var view = SharpDX.Matrix.LookAtLH(new Vector3(0, 0, -5), new Vector3(0, 0, 0), Vector3.UnitY);
            var proj = SharpDX.Matrix.PerspectiveFovLH((float)Math.PI / 4.0f, (float)(panel.RenderSize.Width / panel.RenderSize.Height), 0.1f, 100.0f);
            var viewProj = SharpDX.Matrix.Multiply(view, proj);
            var period = 2000.0;
            var time = (float)(_timer.ElapsedMilliseconds % period);

            var worldViewProj = SharpDX.Matrix.Scaling(1) * SharpDX.Matrix.RotationX(0.0f * time)
                                * SharpDX.Matrix.RotationY((float)(2 * Math.PI * time / period)) * SharpDX.Matrix.RotationZ(1 * time * 0.0f)
                                * viewProj;
            worldViewProj.Transpose();

            _context.InputAssembler.SetVertexBuffers(0, _vertexBinding);
            _context.InputAssembler.InputLayout = _inputLayout;
            _context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            _context.VertexShader.SetConstantBuffer(0, _constantBuffer);
            _context.VertexShader.Set(_vertexShader);
            _context.PixelShader.Set(_pixelShader);
            _context.Draw(_vertices.Length, 0);

            // Update Constant Buffer
            _context.UpdateSubresource(ref worldViewProj, _constantBuffer, 0);

            _swapChain.Present(1, DXGI.PresentFlags.None);
        }
    }
}
