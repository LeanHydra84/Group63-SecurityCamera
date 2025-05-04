using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;

// public class SecureSHA256 : SecureHash<SHA256>
// {
//     public SecureSHA256(object data) : base(data) { }
// }

// public class SecureSHA512 : SecureHash<SHA512>
// {
//     public SecureSHA512(object data) : base(data) { }
// }

namespace SecurityCameraServer
{
    [Serializable]
    public class SecureHash<T> where T : HashAlgorithm
    {
        private static T hashFunction;
        public static int HashSizeInBytes { get; }

        static SecureHash()
        {
            var typeCreateMethod = typeof(T).GetMethod("Create", new Type[0]);
            if (typeCreateMethod == null)
            {
                throw new Exception("Supplied HashAlgorithm has no 'Create' method!");
            }
            object? potentiallyNullHasher = typeCreateMethod.Invoke(null, new object[0]);
            if (potentiallyNullHasher == null)
            {
                throw new Exception("Supplied HashAlgorithm 'Create' method returned null");
            }
            hashFunction = (T)potentiallyNullHasher;

            var hashSizeField = typeof(T).GetField("HashSizeInBytes");
            if (hashSizeField == null)
            {
                throw new Exception("Supplied HashAlgorithm 'HashSizeInBytes' field not found");
            }
            object? hsb = (int?)hashSizeField.GetRawConstantValue();
            if (hsb == null)
            {
                throw new Exception("Supplied HashAlgorithm 'HashSizeInBytes' const field somehow is null (it is an int)");
            }
            HashSizeInBytes = (int)hsb;
        }

        public static SecureHash<T>? FromHexString(string? data)
        {
            if (data == null) return null;
            if (data.Length != 2 * HashSizeInBytes)
                return null;

            var bytes = Convert.FromHexString(data.AsSpan());
            return new SecureHash<T>(bytes);
        }

        private byte[] hash;

        private byte[] FormatObjectAsByteArray(object obj)
        {
            return JsonSerializer.SerializeToUtf8Bytes(obj);
        }

        public SecureHash(object data)
        {
            byte[] bytes = FormatObjectAsByteArray(data);
            hash = hashFunction.ComputeHash(bytes);
        }

        private SecureHash(byte[] bytes)
        {
            hash = bytes;
        }

        public override string ToString()
        {
            IEnumerable<string> asStrings = hash.Select(a => a.ToString("x2"));
            return string.Concat(asStrings);
        }


        public override int GetHashCode()
        {
            // convert N bit cryptographic hash to 32 bit hash for use in table
            // does not need to be secure
            // takes the first 4 bytes of the hash (or stops if hash is shorter than 32 bits for some reason)
            int value = 0;
            for (int i = 0; i < 4; i++)
            {
                if (i >= hash.Length)
                    break;
                value |= hash[i] << (i * 8);
            }
            return value;
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is SecureHash<T>)
            {
                return this == ((SecureHash<T>)obj);
            }
            return false;
        }

        public static bool operator ==(SecureHash<T> left, SecureHash<T> right)
        {

            return left.hash.SequenceEqual(right.hash);
        }

        public static bool operator !=(SecureHash<T> left, SecureHash<T> right)
        {
            return !left.hash.SequenceEqual(right.hash);
        }

    }
}