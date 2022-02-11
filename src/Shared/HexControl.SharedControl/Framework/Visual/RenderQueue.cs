﻿using System.Diagnostics;
using HexControl.SharedControl.Framework.Drawing;

namespace HexControl.SharedControl.Framework.Visual;

internal class RenderQueue
{
    private readonly VisualElement _element;
    private readonly SemaphoreSlim _lock;
    private readonly Stopwatch _sw;

    private bool _isDoingIo;
    private IRenderContext _latestContext = null!;
    private long _previousTicks;
    private bool _requestedRender;

    public RenderQueue(VisualElement element)
    {
        _element = element;
        _sw = Stopwatch.StartNew();
        _lock = new SemaphoreSlim(1, 1);
    }
    
    public async Task<bool> StartIOTask()
    {
        if (_isDoingIo)
        {
            return false;
        }

        _isDoingIo = true;
        await _lock.WaitAsync();
        return true;
    }

    public void StopIOTask()
    {
        _lock.Release();
        _isDoingIo = false;
    }

    public async Task Render(IRenderContext context)
    {
        while (true)
        {
            var acquired = await _lock.WaitAsync(0);
            if (!acquired)
            {
                _requestedRender = true;
                _latestContext = context;
                return;
            }

            _latestContext = context;

            while (_sw.ElapsedMilliseconds - _previousTicks < 12)
            {
                await Task.Delay(1);
            }

            _previousTicks = _sw.ElapsedMilliseconds;
            _element.InvokeRender(_latestContext);
            _lock.Release();

            if (_requestedRender)
            {
                _requestedRender = false;
                continue;
            }

            break;
        }
    }
}