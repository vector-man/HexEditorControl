﻿using Factory = SharpDX.Direct2D1.Factory;

namespace HexControl.WinUI.Host;

internal class WinUIRenderContext : D2DRenderContext
{
    private readonly SwapChain _swapChain;

    public WinUIRenderContext(D2DRenderFactory factory, Factory d2dFactory, SwapChain swapChain, RenderTarget context)
        : base(factory, d2dFactory, context)
    {
        _swapChain = swapChain;
    }

    public override void End(SharedRectangle? dirtyRect)
    {
        if (!CanRender)
        {
            return;
        }

        base.End(dirtyRect);
        _swapChain.Present(0, PresentFlags.None);
    }
}