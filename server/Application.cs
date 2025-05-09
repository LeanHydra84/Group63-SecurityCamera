using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SecurityCameraServer;

public static class Application
{
    // Session handlers
    public static UserSessionHandler Sessions { get; }
    public static CameraSessionHandler ActiveCameras { get; }
    public static CameraViewerSessionHandler CameraViewers { get; }
    
    // Database
    public static DatabaseController Database { get; }
    public static AccountController Accounts { get; }
    
    public static string? DatabaseConnectionString { get; private set; }

    static Application()
    {
        Sessions = new UserSessionHandler();
        Accounts = new AccountController();
        Database = new DatabaseController();
        ActiveCameras = new CameraSessionHandler();
        CameraViewers = new CameraViewerSessionHandler();
    }

    public static void Main(string[] args)
    {
        Console.CancelKeyPress += CleanupAllSockets;
        
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddCors(options =>
            options.AddPolicy(name: "AllowReactLocalServer", policy =>
            {
                policy
                    .AllowAnyHeader()
                    .AllowAnyOrigin()
                    .AllowAnyMethod();
                //policy.WithOrigins("https://localhost:3000", "http://localhost:3000", "localhost:3000");
            }
        ));

        var app = builder.Build();
        
        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            Console.WriteLine("IsDevelopment");
        }

        app.UseWebSockets();
        app.UseHttpsRedirection();
        app.UseCors("AllowReactLocalServer");

        // Default
        app.MapGet("/", DefaultResponse).WithOpenApi();

        // User
        app.MapPost("/login", Login).WithName("Login").WithOpenApi();
        app.MapPost("/logout", Logout).WithName("Logout").WithOpenApi();
        app.MapPost("/newuser", CreateUser).WithName("New User").WithOpenApi();
        app.MapPost("/resetpassword", ChangePassword).WithName("Reset Password").WithOpenApi();
        app.MapGet("/validate", CheckAuthenticationValidity).WithName("Validate Authentication").WithOpenApi();
        app.MapGet("/getcams", GetAllCameras).WithName("Get Cameras").WithOpenApi();
        
        // Camera
        app.MapPost("/registercamera", RegisterCamera).WithName("Register Camera").WithOpenApi();

        app.MapGet("/getimage", GetSnapshot);
        app.Map("/connect", async context =>
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                Console.WriteLine("User request incoming...");
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                string? jsonRequest = await webSocket.ReceiveFixedStringFromWebSocket(128);
                if (jsonRequest == null) return;
                
                // Console.WriteLine(jsonRequest);
                // await webSocket.SendAsync(new ArraySegment<byte>("Here are bytes"u8.ToArray()), WebSocketMessageType.Text, true, CancellationToken.None);
                //
                var request = JsonConvert.DeserializeObject<ViewStreamRequest>(jsonRequest);
                if (request == null) return;
                if (request.CameraGuid == null) return;
                
                Camera? camera = Database.GetCamera(request.CameraGuid);
                if (camera == null) return;

                if (!camera.IsPublic)
                {
                    if (request.Authentication == null) return;
                    User? user = camera.Owner;
                    if (user == null) return;
                    bool validated = Sessions.ValidateAuthentication(user.Username, request.Authentication);
                    if (!validated) return;
                }
                
                CameraSession? session = ActiveCameras.GetSession(request.CameraGuid);
                if (session == null) return;
                
                var cvs = CameraViewers.RegisterSession(null, session, webSocket, request.RequestedFps);
                await cvs.HandleAsync();
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        });

        app.Map("/connect_camera", async context =>
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                string? cameraGuid = await webSocket.ReceiveFixedStringFromWebSocket(64);
                
                CameraSession? session = InitCameraConnection(webSocket, cameraGuid);
                if (session == null) return;

                Console.WriteLine($"[SERVER] Camera '{cameraGuid}' connected");
                
                await session.HandleAsync();
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        });
        
        app.Run();
    }

    private static void CleanupAllSockets(object? sender, ConsoleCancelEventArgs e)
    {
        ActiveCameras.DisposeAll();
        CameraViewers.DisposeAll();
    }
    
    private static void SaveImageToFileEvent(object? sender, EventArgs _)
    {
        if (sender is not CameraSession session) return;
        if (session.CurrentSnapshot == null) return;

        string fileName = Random.Shared.Next().ToString() + "_" + DateTime.Now.ToFileTimeUtc();
        var file = File.Open($"C:\\Databases\\SCC\\{fileName}.jpg", FileMode.Create);
        file.Write(session.CurrentSnapshot);
        file.Close();
    }

    private static IResult DefaultResponse()
    {
        return TypedResults.Ok("Default Response");
    }

    private static CameraSession? InitCameraConnection(WebSocket webSocket, string? cameraGuid)
    {
        if (cameraGuid == null) return null;
        
        Camera? camera = Database.GetCamera(cameraGuid);
        if (camera == null) return null;
        
        Console.WriteLine("Camera connection initiated... websocket connected");

        return ActiveCameras.RegisterSession(camera, webSocket);
    }
    
    // Account
    private static IResult Login(
        [FromHeader(Name = "username")] string? username,
        [FromHeader(Name = "password")] string? password)
    {
        if (username == null || password == null)
            return TypedResults.BadRequest("Missing 'username' and/or 'password' headers");
        var result = Sessions.Login(username, password);
        if (result == null) return TypedResults.Unauthorized();
        return TypedResults.Ok(new LoginResponse(result, username));
    }

    private static IResult Logout(
        [FromHeader(Name = "username")] string? username,
        [FromHeader(Name = "authentication")] string? authentication
    )
    {
        if (username == null || authentication == null)
            return TypedResults.BadRequest("Missing 'username' and/or 'password' headers");
        if (!Sessions.ValidateAuthentication(username, authentication))
            return TypedResults.Unauthorized();

        _ = Sessions.Logout(username);
        return TypedResults.Ok();
    }

    private static IResult CreateUser(
        [FromHeader(Name = "username")] string? username,
        [FromHeader(Name = "password")] string? password,
        [FromBody] string? name
    )
    {
        if (username == null || name == null || password == null) return TypedResults.BadRequest();
        Accounts.CreateUser(username, name, password);
        return Login(username, password);
    }

    private static IResult CheckAuthenticationValidity(
        [FromHeader(Name = "username")] string? username,
        [FromHeader(Name = "authentication")] string? authentication
    )
    {
        if (username == null || authentication == null) return TypedResults.BadRequest();
        bool success = Sessions.ValidateAuthentication(username, authentication);
        if (success) return TypedResults.Ok();
        else return TypedResults.Unauthorized();
    }
    
    private static IResult ChangePassword(
        [FromHeader(Name = "username")] string? username,
        [FromHeader(Name = "authentication")] string? authentication,
        [FromBody] string? newPassword
    )
    {
        if (username == null || authentication == null) return TypedResults.BadRequest();
        if (!Sessions.ValidateAuthentication(username, authentication)) return TypedResults.Unauthorized();

        if (newPassword == null) return TypedResults.BadRequest("Missing body");
        bool success = Accounts.ResetPassword(username, newPassword);

        if (success) return TypedResults.Ok("Password changed");
        return TypedResults.BadRequest();
    }

    // Cameras
    private static IResult RegisterCamera(
        [FromHeader(Name = "username")] string? username,
        [FromHeader(Name = "authentication")] string? authentication,
        [FromBody] string? jsonBody
    )
    {
        if (username == null || authentication == null)
            return TypedResults.BadRequest("Missing user or authentication");

        if (!Sessions.ValidateAuthentication(username, authentication))
        {
            return TypedResults.Unauthorized();
        }

        if (jsonBody == null) return TypedResults.BadRequest("Missing body");
        var data = JsonConvert.DeserializeObject<RegisterCameraRequest>(jsonBody);
        if (data == null) return TypedResults.BadRequest("Malformed body");

        User? user = Database.GetLightUserFromUsername(username);
        if (user == null) return TypedResults.NotFound();
        
        Camera? camera = Database.RegisterNewCamera(user, data);
        if (camera != null)
        {
            var clr = new CameraListResponse(camera.ID, camera.CameraGuid, camera.Name);
            string asJson = JsonConvert.SerializeObject(clr);
            return TypedResults.Ok(asJson);
        }
        
        return TypedResults.Forbid();
    }

    private static IResult GetAllCameras(
        [FromHeader(Name = "username")] string? username,
        [FromHeader(Name = "Authentication")] string? authentication
    )
    {
        if (username == null || authentication == null) return TypedResults.BadRequest();
        if (!Sessions.ValidateAuthentication(username, authentication))
        {
            return TypedResults.Unauthorized();
        }
        
        List<Camera>? cameras = Database.GetLightAllCameras(username);
        if (cameras == null) return TypedResults.NotFound();

        var organized = cameras.Select(a => new CameraListResponse(a.ID, a.CameraGuid, a.Name)).ToList();
        string asJson = JsonConvert.SerializeObject(organized);

        return TypedResults.Ok(asJson);
    }

    private static IResult GetSnapshot(
        [FromHeader(Name = "cameraGuid")] string? cameraGuid,
        [FromHeader(Name = "Authentication")] string? authentication
    )
    {
        if (authentication == null) return TypedResults.BadRequest();
        if (cameraGuid == null) return TypedResults.BadRequest();

        var session = ActiveCameras.GetSession(cameraGuid);
        if (session == null) return TypedResults.NotFound();

        if (!Sessions.ValidateAuthentication(session.Camera.Owner.Username, authentication))
        {
            return TypedResults.Unauthorized();
        }

        // byte[]? snapshot = await session.RequestSnapshotAsync();
        // if (snapshot == null) return TypedResults.NotFound();
        if (session.CurrentSnapshot == null)
            return TypedResults.NotFound();
        string asJson = JsonConvert.SerializeObject(new SnapshotResponse("jpg", session.CurrentSnapshot));
        
        if (session.CurrentSnapshot == null) return TypedResults.NotFound();
        return TypedResults.Ok(asJson);
    }

    private record LoginResponse([JsonProperty("authentication")] string Authentication, [JsonProperty("username")] string Username);
    private record CameraListResponse([JsonProperty("id")] int Id, [JsonProperty("guid")] string Guid, [JsonProperty("name")] string? Name);
    private record SnapshotResponse([JsonProperty("format")] string Format, [JsonProperty("bytes")] byte[] Bytes);

}

