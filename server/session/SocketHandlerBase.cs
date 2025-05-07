using System.Net.WebSockets;

namespace SecurityCameraServer;

public abstract class SocketHandlerBase(WebSocket webSocket)
{
	public WebSocket Socket { get; } = webSocket;
	public CancellationTokenSource CancellationTokenSource { get; } = new();

	public abstract Task HandleAsync();
	
	public virtual void EndSession()
	{
		CancellationTokenSource.Cancel();
		Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
	}
}