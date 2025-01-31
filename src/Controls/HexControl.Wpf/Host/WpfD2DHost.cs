﻿#if !SKIA_RENDER
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HexControl.Core.Helpers;
using HexControl.Renderer.Direct2D;
using HexControl.Wpf.D2D;
using HexControl.Wpf.Host.Controls;
using Microsoft.Win32;
using SharpDX.Direct2D1;

namespace HexControl.Wpf.Host;

internal class WpfD2DHost : WpfControl
{
    private readonly D2DControl _d2dControl;
    private readonly Grid _hostContainer;
    private D2DRenderFactory? _factory;
    private WpfD2DRenderContext? _renderContext;

    public WpfD2DHost(Grid hostContainer, D2DControl d2dControl) : base(hostContainer)
    {
        _hostContainer = hostContainer;

        hostContainer.Loaded += OnLoaded;
        hostContainer.Unloaded += OnUnloaded;

        _d2dControl = d2dControl;
        _d2dControl.Render += OnRender;

        hostContainer.Children.Add(_d2dControl);
    }

    private float _dpi = 1;

    public ResizeMode ResizeMode
    {
        get => _d2dControl.ResizeMode;
        set => _d2dControl.ResizeMode = value;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        SystemEvents.PowerModeChanged += OnPowerModeChanged;

        var window = Window.GetWindow(_hostContainer);
        if (window is null)
        {
            return;
        }

        UpdateDpi(VisualTreeHelper.GetDpi(window));
        window.DpiChanged += WindowOnDpiChanged;
    }

    private void WindowOnDpiChanged(object sender, DpiChangedEventArgs e)
    {
        UpdateDpi(e.NewDpi);
    }

    private void UpdateDpi(DpiScale scale)
    {
        _d2dControl.Dpi = (float)scale.DpiScaleX;
        _dpi = (float)scale.DpiScaleX;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;

        var window = Window.GetWindow(_hostContainer);
        if (window is null)
        {
            return;
        }

        window.DpiChanged -= WindowOnDpiChanged;
    }

    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode is PowerModes.Resume)
        {
            _d2dControl.CreateAndBindTargets();
            Invalidate();
        }
    }

    private void OnRender(Factory factory, RenderTarget renderTarget, bool newSurface)
    {
        if (newSurface || _factory is null || _renderContext is null)
        {
            Disposer.SafeDispose(ref _renderContext);

            _factory = new WpfD2DRenderFactory(renderTarget);
            _renderContext = new WpfD2DRenderContext(_factory, factory, renderTarget, _d2dControl);
            _renderContext.AttachStateProvider(_d2dControl);
        }

        _renderContext.Dpi = _dpi;
        RaiseRender(_renderContext, newSurface);
    }

    public override void Invalidate()
    {
        _d2dControl.Invalidate();
    }
}
#endif