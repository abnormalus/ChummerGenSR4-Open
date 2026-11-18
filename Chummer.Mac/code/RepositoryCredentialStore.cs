#nullable enable
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Chummer;

internal sealed record RepositoryConnection(string Name, string Folder, string? Token, DateTimeOffset? ExpiresAt, long Id, string Branch)
{
    public override string ToString() => Name;
}

internal static class RepositoryCredentialStore
{
    private const string Key = "ChummerGenSR4.CharacterRepository";
    private const uint Generic = 1;
    private const int MacNotFound = -25300;
    private const int WindowsNotFound = 1168;
    private const string Security = "/System/Library/Frameworks/Security.framework/Security";
    private const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    private static readonly byte[] Service = Encoding.UTF8.GetBytes(Key);
    private static readonly byte[] Account = Encoding.UTF8.GetBytes("GitHub");

    internal static RepositoryConnection? Read()
    {
        byte[]? bytes = System.OperatingSystem.IsMacOS() ? ReadMac() :
            System.OperatingSystem.IsWindows() ? ReadWindows() : null;
        if (bytes == null) return null;
        try { return JsonSerializer.Deserialize<RepositoryConnection>(bytes); }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }

    internal static void Save(RepositoryConnection connection)
    {
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(connection);
        try
        {
            if (System.OperatingSystem.IsMacOS()) WriteMac(bytes);
            else if (System.OperatingSystem.IsWindows()) WriteWindows(bytes);
            else throw new IOException("Protected GitHub credential storage is unavailable on this platform.");
        }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }

    internal static void Remove()
    {
        if (System.OperatingSystem.IsMacOS())
        {
            if (!FindMac(out var item)) return;
            try { CheckMac(SecKeychainItemDelete(item)); }
            finally { CFRelease(item); }
        }
        else if (System.OperatingSystem.IsWindows() && !CredDelete(Key, Generic, 0))
        {
            int error = Marshal.GetLastPInvokeError();
            if (error != WindowsNotFound) throw StoreError(error);
        }
    }

    private static IOException StoreError(int code) =>
        new($"Chummer could not access your protected GitHub credential (system error {code}). Unlock your credential store and try again.");

    private static void CheckMac(int status)
    {
        if (status != 0) throw StoreError(status);
    }

    private static bool FindMac(out IntPtr item)
    {
        int status = SecKeychainFindItem(IntPtr.Zero, Service.Length, Service, Account.Length, Account,
            IntPtr.Zero, IntPtr.Zero, out item);
        if (status == MacNotFound) return false;
        CheckMac(status);
        return true;
    }

    private static byte[]? ReadMac()
    {
        int status = SecKeychainFindPassword(IntPtr.Zero, Service.Length, Service, Account.Length, Account,
            out int length, out IntPtr data, IntPtr.Zero);
        if (status == MacNotFound) return null;
        CheckMac(status);
        try { return Copy(data, length); }
        finally { SecKeychainItemFreeContent(IntPtr.Zero, data); }
    }

    private static void WriteMac(byte[] bytes)
    {
        if (!FindMac(out var item))
        {
            CheckMac(SecKeychainAddGenericPassword(IntPtr.Zero, Service.Length, Service, Account.Length, Account,
                bytes.Length, bytes, IntPtr.Zero));
            return;
        }
        try { CheckMac(SecKeychainItemModifyContent(item, IntPtr.Zero, bytes.Length, bytes)); }
        finally { CFRelease(item); }
    }

    private static byte[] Copy(IntPtr data, int length)
    {
        var bytes = new byte[length];
        Marshal.Copy(data, bytes, 0, length);
        return bytes;
    }

    private static byte[]? ReadWindows()
    {
        if (!CredRead(Key, Generic, 0, out var pointer))
        {
            int error = Marshal.GetLastPInvokeError();
            if (error == WindowsNotFound) return null;
            throw StoreError(error);
        }
        try
        {
            var credential = Marshal.PtrToStructure<WindowsCredential>(pointer);
            return Copy(credential.Blob, credential.BlobSize);
        }
        finally { CredFree(pointer); }
    }

    private static void WriteWindows(byte[] bytes)
    {
        var pinned = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try
        {
            var credential = new WindowsCredential
            {
                Type = Generic, Target = Key, Blob = pinned.AddrOfPinnedObject(), BlobSize = bytes.Length,
                Persist = 2, User = "GitHub" // Persist locally for this Windows user across sign-ins.
            };
            if (!CredWrite(ref credential, 0)) throw StoreError(Marshal.GetLastPInvokeError());
        }
        finally { pinned.Free(); }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WindowsCredential
    {
        internal uint Flags, Type;
        internal string Target;
        internal IntPtr Comment;
        internal System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        internal int BlobSize;
        internal IntPtr Blob;
        internal uint Persist, AttributeCount;
        internal IntPtr Attributes, TargetAlias;
        internal string User;
    }

    [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredRead(string target, uint type, uint flags, out IntPtr credential);
    [DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredWrite(ref WindowsCredential credential, uint flags);
    [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredDelete(string target, uint type, uint flags);
    [DllImport("advapi32.dll", ExactSpelling = true)]
    private static extern void CredFree(IntPtr credential);

    [DllImport(Security, EntryPoint = "SecKeychainFindGenericPassword")]
    private static extern int SecKeychainFindItem(IntPtr keychain, int serviceLength, byte[] service,
        int accountLength, byte[] account, IntPtr passwordLength, IntPtr password, out IntPtr item);
    [DllImport(Security, EntryPoint = "SecKeychainFindGenericPassword")]
    private static extern int SecKeychainFindPassword(IntPtr keychain, int serviceLength, byte[] service,
        int accountLength, byte[] account, out int passwordLength, out IntPtr password, IntPtr item);
    [DllImport(Security)]
    private static extern int SecKeychainAddGenericPassword(IntPtr keychain, int serviceLength, byte[] service,
        int accountLength, byte[] account, int passwordLength, byte[] password, IntPtr item);
    [DllImport(Security)]
    private static extern int SecKeychainItemModifyContent(IntPtr item, IntPtr attributes, int length, byte[] data);
    [DllImport(Security)]
    private static extern int SecKeychainItemDelete(IntPtr item);
    [DllImport(Security)]
    private static extern int SecKeychainItemFreeContent(IntPtr attributes, IntPtr data);
    [DllImport(CoreFoundation)]
    private static extern void CFRelease(IntPtr value);
}
