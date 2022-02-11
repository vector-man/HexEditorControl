﻿using System.Windows;
using System.Windows.Controls;
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
    private RenderTarget? _previousTarget;
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

    public float Dpi { get; set; } = 1;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        SystemEvents.PowerModeChanged += OnPowerModeChanged;

        var source = PresentationSource.FromVisual(_hostContainer);
        if (source is not null)
        {
            var dpi = source.CompositionTarget?.TransformFromDevice.M11 ?? 1;
            UpdateDpi((float)dpi);
        }

        var window = Window.GetWindow(_hostContainer);
        if (window is null)
        {
            return;
        }

        window.DpiChanged += WindowOnDpiChanged;
    }

    private void WindowOnDpiChanged(object sender, DpiChangedEventArgs e)
    {
        var newDpi = (float)e.NewDpi.DpiScaleX;
        UpdateDpi(newDpi);
    }

    private void UpdateDpi(float newDpi)
    {
        _d2dControl.Dpi = newDpi;
        Dpi = newDpi;
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

    private void OnRender(object? sender, RenderEventArgs e)
    {
        if (!ReferenceEquals(e.RenderTarget, _previousTarget) || _factory is null || _renderContext is null)
        {
            _renderContext?.Dispose();

            _factory = new WpfD2DFactory(e.RenderTarget);
            _renderContext = new WpfD2DRenderContext(_factory, e.Factory, e.RenderTarget, _d2dControl);
            _renderContext.AttachStateProvider(_d2dControl);
        }

        _renderContext.Dpi = Dpi;

        RaiseRender(_renderContext);
        _previousTarget = e.RenderTarget;
    }

    public override void Invalidate()
    {
        _d2dControl.Invalidate();
    }
}