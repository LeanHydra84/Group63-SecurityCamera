using System.Net.WebSockets;

namespace SecurityCameraServer;

public static class WebSocketHelper
{
	public static async Task<string?> ReceiveFixedStringFromWebSocket(this WebSocket webSocket, int maxLength)
	{
		try
		{
			CancellationToken token = new CancellationTokenSource().Token;
			ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[maxLength]);
			var response = await webSocket.ReceiveAsync(buffer, token);
			if (!response.EndOfMessage)
			{
				await webSocket.CloseAsync(WebSocketCloseStatus.InvalidPayloadData, string.Empty, token);
				return null;
			}
			return System.Text.Encoding.Default.GetString(buffer.Slice(0, response.Count));
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			return null;
		}
	}

	public static async Task<string?> ReceiveStringFromWebSocket(this WebSocket webSocket, int chunkSize = 256 * 256)
	{
		byte[]? bytes = await ReceiveBytesFromWebSocket(webSocket, chunkSize);
		if (bytes == null) return null;
		
		return System.Text.Encoding.Default.GetString(bytes);
	}
	
	public static async Task<byte[]?> ReceiveBytesFromWebSocket(this WebSocket webSocket, int chunkSize = 256 * 256)
	{
		ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[chunkSize]);
		if (buffer.Array == null) return null;
		
		MemoryStream stream = new MemoryStream(); 
		CancellationToken token = new CancellationTokenSource().Token;
		
		try
		{
			while (!token.IsCancellationRequested)
			{
				var response = await webSocket.ReceiveAsync(buffer, token);
				stream.Write(buffer.Array, 0, response.Count);
			}
			
			return stream.ToArray();
		}
		catch (Exception)
		{
			// Console.WriteLine(e);
			return null;
		}
	}
	
}