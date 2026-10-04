using System;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;

namespace NOVR.Gamepad
{
    internal sealed class ViGEmSink : IGamepadSink
    {
        private ViGEmClient? _client;
        private IXbox360Controller? _pad;
        public int UserIndex => _pad?.UserIndex ?? -1;
        public ViGEmSink()
        {
            try
            {
                // Pinned client contains Costura x86/x64 native resources. Its loader
                // uses absolute LoadLibraryEx, without changing the DLL search path.
                _client = new ViGEmClient();
                _pad = _client.CreateXbox360Controller();
                _pad.AutoSubmitReport = false;
                _pad.Connect();
                Send(default);
            }
            catch { Dispose(); throw; }
        }
        public void Send(PadReport report)
        {
            var pad = _pad ?? throw new ObjectDisposedException(nameof(ViGEmSink));
            pad.ButtonState = (ushort)report.Buttons;
            pad.LeftThumbX = report.LeftX; pad.LeftThumbY = report.LeftY;
            pad.RightThumbX = report.RightX; pad.RightThumbY = report.RightY;
            pad.LeftTrigger = report.LeftTrigger; pad.RightTrigger = report.RightTrigger;
            pad.SubmitReport();
        }
        public void Dispose()
        {
            var pad = _pad; _pad = null;
            var client = _client; _client = null;
            // Every stage is attempted even if the bus vanished during removal.
            try { if (pad != null) { try { pad.Disconnect(); } finally { ((IDisposable)pad).Dispose(); } } }
            finally { client?.Dispose(); }
        }
    }
}
