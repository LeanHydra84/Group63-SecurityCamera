using System;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore.Storage.Json;
using SQLitePCL;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Identity;
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
    public static CameraController CameraManager { get; }

    static Application()
    {
        Sessions = new UserSessionHandler();
        Database = new DatabaseController();
        Accounts = new AccountController();
        CameraManager = new CameraController();
        ActiveCameras = new CameraSessionHandler();
        CameraViewers = new CameraViewerSessionHandler();
    }

    public static void Main(string[] args)
    {
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

        // Camera
        app.MapPost("/registercamera", RegisterCamera).WithName("Register Camera").WithOpenApi();

        app.MapGet("/getimage", GetSnapshot);
        // app.MapGet("/requeststream", RequestStream);
        // app.MapPost("/connect", ConnectCamera).WithName("Connect camera").WithOpenApi();

        app.Map("/connectuser", async context =>
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        });

        app.Map("/connectcamera", async context =>
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();

                string cameraGuid = string.Empty;
                try
                {
                    CancellationToken token = new CancellationTokenSource().Token;
                    ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[256]);
                    var response = await webSocket.ReceiveAsync(buffer, token);
                
                    cameraGuid = System.Text.Encoding.Default.GetString(buffer.Slice(0, response.Count));
                    Console.WriteLine($"Received camera GUID: {cameraGuid}");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return;
                }

                Console.WriteLine(context.RequestAborted);

                InitCameraConnection(webSocket, cameraGuid);
                Console.WriteLine("WebSocket opened");
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        });
        
        app.Run();
    }

    private static IResult DefaultResponse()
    {
        return TypedResults.Ok("Default Response");
    }

    private static int InitCameraConnection(WebSocket webSocket, string? cameraGuid)
    {
        if (cameraGuid == null) return StatusCodes.Status400BadRequest;
        
        Camera? camera = Database.GetCamera(cameraGuid);
        if (camera == null) return StatusCodes.Status400BadRequest;
        
        Console.WriteLine("Camera connection initiated... websocket connected");

        ActiveCameras.RegisterSession(camera, webSocket);
        
        return StatusCodes.Status201Created;
    }
    
    // Account
    private static IResult Login(
        [FromHeader(Name = "Email")] string? email,
        [FromHeader(Name = "Password")] string? password)
    {
        if (email == null || password == null)
            return TypedResults.BadRequest("Missing 'Email' and/or 'Password' headers");
        var result = Sessions.Login(email, password);
        if (result == null) return TypedResults.Unauthorized();
        return TypedResults.Ok(new LoginResponse(result, email));
    }

    private static IResult Logout(
        [FromHeader(Name = "Email")] string? email,
        [FromHeader(Name = "Authentication")] string? authentication
    )
    {
        if (email == null || authentication == null)
            return TypedResults.BadRequest("Missing 'Email' and/or 'Password' headers");
        if (!Sessions.ValidateAuthentication(email, authentication))
            return TypedResults.Unauthorized();

        _ = Sessions.Logout(email);
        return TypedResults.Ok();
    }

    private static IResult CreateUser(
        [FromHeader(Name = "Email")] string? email,
        [FromHeader(Name = "Password")] string? password,
        [FromBody] string? name
    )
    {
        if (email == null || name == null || password == null) return TypedResults.BadRequest();
        Accounts.CreateUser(email, name, password);
        return Login(email, password);
    }

    private static IResult CheckAuthenticationValidity(
        [FromHeader(Name = "Email")] string? email,
        [FromHeader(Name = "Authentication")] string? authentication
    )
    {
        if (email == null || authentication == null) return TypedResults.BadRequest();
        bool success = Sessions.ValidateAuthentication(email, authentication);
        if (success) return TypedResults.Ok();
        else return TypedResults.Unauthorized();
    }
    
    private static IResult ChangePassword(
        [FromHeader(Name = "Email")] string? email,
        [FromHeader(Name = "Authentication")] string? authentication,
        [FromBody] string? newPassword
    )
    {
        if (email == null || authentication == null) return TypedResults.BadRequest();
        if (!Sessions.ValidateAuthentication(email, authentication)) return TypedResults.Unauthorized();

        if (newPassword == null) return TypedResults.BadRequest("Missing body");
        bool success = Accounts.ResetPassword(email, newPassword);

        if (success) return TypedResults.Ok("Password changed");
        return TypedResults.BadRequest();
    }

    // Cameras
    private static IResult RegisterCamera(
        [FromHeader(Name = "Email")] string? email,
        [FromHeader(Name = "Authentication")] string? authentication,
        [FromBody] string? jsonBody
    )
    {
        if (email == null || authentication == null)
            return TypedResults.BadRequest("Missing user or authentication");

        if (!Sessions.ValidateAuthentication(email, authentication))
        {
            return TypedResults.Unauthorized();
        }

        if (jsonBody == null) return TypedResults.BadRequest("Missing body");
        var data = JsonConvert.DeserializeObject<RegisterCameraData>(jsonBody);
        if (data == null) return TypedResults.BadRequest("Malformed body");

        User? user = Database.GetLightUserFromEmail(email);
        Camera? camera = CameraManager.RegisterNewCamera(user, data);
        if (camera != null)
        {
            var clr = new CameraListResponse(camera.ID, camera.CameraGuid, camera.Name);
            string asJson = JsonConvert.SerializeObject(clr);
            return TypedResults.Ok(asJson);
        }
        
        return TypedResults.Forbid();
    }


    
    private static IResult GetAllCameras(
        [FromHeader(Name = "Email")] string? email,
        [FromHeader(Name = "Authentication")] string? authentication
    )
    {
        if (email == null || authentication == null) return TypedResults.BadRequest();
        if (!Sessions.ValidateAuthentication(email, authentication))
        {
            return TypedResults.Unauthorized();
        }
        
        List<Camera>? cameras = Database.GetLightAllCameras(email);
        if (cameras == null) return TypedResults.NotFound();

        var organized = cameras.Select(a => new CameraListResponse(a.ID, a.CameraGuid, a.Name)).ToList();
        string asJson = JsonConvert.SerializeObject(organized);

        return TypedResults.Ok(asJson);
    }

    private static async Task<IResult> GetSnapshot(
        [FromHeader(Name = "cameraGuid")] string? cameraGuid,
        [FromHeader(Name = "Authentication")] string? authentication
    )
    {
        if (authentication == null) return TypedResults.BadRequest();
        if (cameraGuid == null) return TypedResults.BadRequest();

        var session = ActiveCameras.GetSession(cameraGuid);
        if (session == null) return TypedResults.NotFound();

        if (!Sessions.ValidateAuthentication(session.Camera.Owner.EMail, authentication))
        {
            return TypedResults.Unauthorized();
        }

        // byte[]? snapshot = await session.RequestSnapshotAsync();
        // if (snapshot == null) return TypedResults.NotFound();

        string asJson = JsonConvert.SerializeObject(new SnapshotResponse("jpg", session.CurrentSnapshot));

        if (session.CurrentSnapshot == null) return TypedResults.NotFound();
        return TypedResults.Ok(asJson);
    }

    private static IResult RequestStream(
        [FromHeader(Name = "Email")] string? email,
        [FromHeader(Name = "Authentication")] string? authentication,
        [FromBody] string? jsonBody
    )
    {
        if (email == null || authentication == null) return TypedResults.BadRequest();
        if (!Sessions.ValidateAuthentication(email, authentication))
        {
            return TypedResults.Unauthorized();
        }
        
        // begin stream
        
        return TypedResults.NotFound();
    }

    private record LoginResponse(string authentication, string email);
    private record CameraListResponse(int id, string GUID, string? name);

    private record SnapshotResponse(string format, byte[] bytes);

}

