using System;
using System.Collections.Generic;
using System.Text;

namespace RazeContent
{
    internal static class SafeDispose
    {
        internal static void Dispose(ref IDisposable dsp)
        {
            if (dsp == null)
                return;

            dsp.Dispose();
            dsp = null;
        }
    }
}
