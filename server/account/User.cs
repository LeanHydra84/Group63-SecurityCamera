using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;

namespace SecurityCameraServer;

[Serializable]
public class User
{
    public int ID { get; set; }
    public string? EMail { get; set; }
    public string? Name { get; set; }
    public string? PasswordHash { get; private set; }

    public List<Camera>? Cameras { get; set; }

    [NotMapped]
    public SecureHash<SHA256>? PasswordHashData
    {
        get => SecureHash<SHA256>.FromHexString(PasswordHash);
        set => PasswordHash = value?.ToString();
    }

}