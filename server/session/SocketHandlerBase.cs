using System.Net.WebSockets;

namespace SecurityCameraServer;

public abstract class SocketHandlerBase(WebSocket webSocket)
{
	public WebSocket Socket { get; } = webSocket;

	public virtual void EndSession()
	{
		Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
	}
}