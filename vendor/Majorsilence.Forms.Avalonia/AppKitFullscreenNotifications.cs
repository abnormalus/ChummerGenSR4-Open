using System.Runtime.InteropServices;

namespace Majorsilence.Forms;

/// <summary>Observes native fullscreen transitions on the AppKit UI thread.</summary>
public static class AppKitFullscreenNotifications
{
    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "sel_registerName")]
    private static extern IntPtr Selector(string name);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern nuint GetUnsigned(IntPtr receiver, IntPtr selector);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern void SendAction(IntPtr receiver, IntPtr selector, IntPtr sender);

    [DllImport("/usr/lib/libobjc.A.dylib")]
    private static extern IntPtr objc_getClass(string name);

    [DllImport("/usr/lib/libobjc.A.dylib")]
    private static extern IntPtr objc_allocateClassPair(IntPtr superclass, string name, nuint extraBytes);

    [DllImport("/usr/lib/libobjc.A.dylib")]
    private static extern void objc_registerClassPair(IntPtr type);

    [DllImport("/usr/lib/libobjc.A.dylib")]
    private static extern byte class_addMethod(IntPtr type, IntPtr selector, IntPtr implementation, string encoding);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr GetObject(IntPtr receiver, IntPtr selector);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr CreateString(IntPtr receiver, IntPtr selector, [MarshalAs(UnmanagedType.LPUTF8Str)] string text);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern void Observe(IntPtr center, IntPtr selector, IntPtr observer, IntPtr callback, IntPtr name, IntPtr window);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void NotificationCallback(IntPtr receiver, IntPtr selector, IntPtr notification);

    private static readonly Dictionary<IntPtr, Action<string>> observers = new();
    private static readonly NotificationCallback DispatchNotification = (receiver, _, notification) =>
    {
        var name = GetObject(notification, Selector("name"));
        var text = Marshal.PtrToStringUTF8(GetObject(name, Selector("UTF8String")));
        if (text != null && observers.TryGetValue(receiver, out var trace))
            trace(text);
    };

    /// <summary>Reads the current AppKit fullscreen style from a live NSWindow.</summary>
    public static bool IsFullscreen(IntPtr window)
    {
        const uint fullscreenMask = 1 << 14;
        return (GetUnsigned(window, Selector("styleMask")) & fullscreenMask) != 0;
    }

    /// <summary>Attaches the requested fullscreen notifications and returns an unsubscribe action.</summary>
    public static Action Watch(IntPtr window, Action<string> trace, params string[] notifications)
    {
        const string className = "MajorsilenceFullscreenObserver";
        var type = objc_getClass(className);
        var callback = Selector("recordFullscreenNotification:");
        if (type == IntPtr.Zero)
        {
            type = objc_allocateClassPair(objc_getClass("NSObject"), className, 0);
            if (type == IntPtr.Zero || class_addMethod(type, callback,
                    Marshal.GetFunctionPointerForDelegate(DispatchNotification), "v@:@") == 0)
                throw new InvalidOperationException("Unable to install the AppKit fullscreen observer.");
            objc_registerClassPair(type);
        }

        var center = GetObject(objc_getClass("NSNotificationCenter"), Selector("defaultCenter"));
        var observer = GetObject(type, Selector("new"));
        observers.Add(observer, trace);
        foreach (string name in notifications)
        {
            var notification = CreateString(objc_getClass("NSString"), Selector("stringWithUTF8String:"),
                "NSWindow" + name + "FullScreenNotification");
            Observe(center, Selector("addObserver:selector:name:object:"), observer, callback, notification, window);
        }
        return () =>
        {
            if (!observers.Remove(observer))
                return;
            SendAction(center, Selector("removeObserver:"), observer);
            SendAction(observer, Selector("release"), IntPtr.Zero);
        };
    }

}
