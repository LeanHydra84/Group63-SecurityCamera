using System.Net.WebSockets;
using System.Reflection.Metadata.Ecma335;

namespace SecurityCameraServer;

public class CameraViewerSession : SocketHandlerBase
{
    public LoginSession? Session { get; }
    public CameraSession Camera { get; }
    public int Fps { get; set; }

    private int InvFpsMilliseconds => (int)((1.0f / (float)Fps) * 1000);

    private bool isFrameReady;
    
    public CameraViewerSession(WebSocket socket, LoginSession? session, CameraSession camera, int fps) : base(socket)
    {
        Session = session;
        Camera = camera;

        Fps = Math.Min(fps, 30);
        isFrameReady = false;
    }

    public void MarkFrameReady()
    {
        isFrameReady = true;
    }

    public override async Task HandleAsync()
    {
        CancellationToken token = CancellationTokenSource.Token;

        while (!token.IsCancellationRequested)
        {
            await Task.Delay(InvFpsMilliseconds, token);
            if (isFrameReady)
            {
                byte[]? bytes = Camera.CurrentSnapshot;
                if (bytes == null)
                {
                    Console.WriteLine("Camera Snapshot buffer was null unexpectedly. Ending session.");
                    EndSession();
                    return;
                }

                Console.WriteLine("Sending frame...");
                await Socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Binary, true, token);
            }
        }
        
    }
    
}