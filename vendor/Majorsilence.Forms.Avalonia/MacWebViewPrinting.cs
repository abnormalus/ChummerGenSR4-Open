using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Majorsilence.Forms.Backends
{
    // WKWebView.createPDF captures the screen document as a continuous page. AppKit's
    // print operation instead applies print CSS, paper size and pagination.
    internal static class MacWebViewPrinting
    {
        private const string ObjC = "/usr/lib/libobjc.A.dylib";
        private const string AppKit = "/System/Library/Frameworks/AppKit.framework/AppKit";
        [DllImport(ObjC)] private static extern IntPtr objc_getClass(string name);
        [DllImport(ObjC)] private static extern IntPtr sel_registerName(string name);
        [DllImport(ObjC, EntryPoint = "objc_msgSend")] private static extern IntPtr Send(IntPtr target, IntPtr selector);
        [DllImport(ObjC, EntryPoint = "objc_msgSend")] private static extern IntPtr Send(IntPtr target, IntPtr selector, IntPtr argument);
        [DllImport(ObjC, EntryPoint = "objc_msgSend")] private static extern void SetObject(IntPtr target, IntPtr selector, IntPtr value, IntPtr key);
        [DllImport(ObjC, EntryPoint = "objc_msgSend")] private static extern void SetBool(IntPtr target, IntPtr selector, byte value);
        [DllImport(ObjC, EntryPoint = "objc_msgSend")] private static extern void SetNumber(IntPtr target, IntPtr selector, double value);
        [DllImport(ObjC, EntryPoint = "objc_msgSend")] private static extern byte GetBool(IntPtr target, IntPtr selector);
        [DllImport(ObjC, EntryPoint = "objc_msgSend")] private static extern byte Responds(IntPtr target, IntPtr selector, IntPtr argument);
        [DllImport(ObjC)] private static extern IntPtr objc_allocateClassPair(IntPtr superclass, string name, nuint extraBytes);
        [DllImport(ObjC)] private static extern void objc_registerClassPair(IntPtr type);
        [DllImport(ObjC)] private static extern byte class_addMethod(IntPtr type, IntPtr selector, IntPtr implementation, string encoding);
        [DllImport(ObjC, EntryPoint = "objc_msgSend")] private static extern void RunModal(IntPtr target, IntPtr selector, IntPtr window, IntPtr callback, IntPtr callbackSelector, IntPtr context);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void CompletedCallback(IntPtr self, IntPtr command, IntPtr operation, byte success, IntPtr context);
        private static readonly CompletedCallback Callback = Completed;
        private static IntPtr _callbackClass;
        private static void Completed(IntPtr self, IntPtr command, IntPtr operation, byte success, IntPtr context)
        {
            // Native callback must never throw across the Objective-C boundary.
            var handle = GCHandle.FromIntPtr(context);
            if (handle.Target is TaskCompletionSource<bool> completion) completion.TrySetResult(success != 0);
        }
        private static IntPtr CallbackClass()
        {
            if (_callbackClass != IntPtr.Zero) return _callbackClass;
            _callbackClass = objc_allocateClassPair(objc_getClass("NSObject"), "ChummerWebPrintCompletion", 0);
            if (_callbackClass == IntPtr.Zero) throw new InvalidOperationException("Unable to create print completion delegate.");
            if (class_addMethod(_callbackClass, Selector("printOperation:didRun:contextInfo:"), Marshal.GetFunctionPointerForDelegate(Callback), "v@:@B^v") == 0)
                throw new InvalidOperationException("Unable to register print completion callback.");
            objc_registerClassPair(_callbackClass);
            return _callbackClass;
        }

        private static IntPtr Selector(string value) => sel_registerName(value);
        private static IntPtr Constant(IntPtr library, string name) => Marshal.ReadIntPtr(NativeLibrary.GetExport(library, name));
        private static IntPtr String(string value)
        {
            IntPtr utf8 = Marshal.StringToCoTaskMemUTF8(value);
            try { return Send(objc_getClass("NSString"), Selector("stringWithUTF8String:"), utf8); }
            finally { Marshal.FreeCoTaskMem(utf8); }
        }

        // Caller marshals to the UI thread. Never changes the shared print preferences.
        internal static Task<Stream> ExportPdfAsync(IntPtr view) => RunAsync(view, true);
        internal static async Task ShowPrintUIAsync(IntPtr view) { using var result = await RunAsync(view, false); }

        private static async Task<Stream> RunAsync(IntPtr view, bool export)
        {
            if (view == IntPtr.Zero || Responds(view, Selector("respondsToSelector:"), Selector("printOperationWithPrintInfo:")) == 0)
                throw new PlatformNotSupportedException("The native browser does not expose a printable WKWebView.");
            IntPtr library = NativeLibrary.Load(AppKit);
            IntPtr info = IntPtr.Zero;
            string path = Path.Combine(Path.GetTempPath(), "chummer-print-" + Guid.NewGuid().ToString("N") + ".pdf");
            try
            {
                info = Send(Send(objc_getClass("NSPrintInfo"), Selector("sharedPrintInfo")), Selector("copy"));
                Send(info, Selector("setJobDisposition:"), Constant(library, export ? "NSPrintSaveJob" : "NSPrintSpoolJob"));
                if (export)
                {
                    IntPtr url = Send(objc_getClass("NSURL"), Selector("fileURLWithPath:"), String(path));
                    SetObject(Send(info, Selector("dictionary")), Selector("setObject:forKey:"), url, Constant(library, "NSPrintJobSavingURL"));
                }
                SetBool(info, Selector("setHorizontallyCentered:"), 0);
                SetBool(info, Selector("setVerticallyCentered:"), 0);
                foreach (string margin in new[] { "setLeftMargin:", "setRightMargin:", "setTopMargin:", "setBottomMargin:" })
                    SetNumber(info, Selector(margin), 24);
                IntPtr operation = Send(view, Selector("printOperationWithPrintInfo:"), info);
                if (operation == IntPtr.Zero) throw new InvalidOperationException("Unable to prepare native PDF printing.");
                SetBool(operation, Selector("setShowsPrintPanel:"), export ? (byte)0 : (byte)1);
                SetBool(operation, Selector("setShowsProgressPanel:"), 0);
                var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                var callbackHandle = GCHandle.Alloc(completion);
                IntPtr callback = Send(Send(CallbackClass(), Selector("alloc")), Selector("init"));
                try
                {
                    RunModal(operation, Selector("runOperationModalForWindow:delegate:didRunSelector:contextInfo:"),
                        Send(view, Selector("window")), callback, Selector("printOperation:didRun:contextInfo:"), GCHandle.ToIntPtr(callbackHandle));
                    bool success = await completion.Task;
                    if (export && (!success || !File.Exists(path))) throw new IOException("The native PDF print operation failed.");
                }
                finally { callbackHandle.Free(); Send(callback, Selector("release")); }
                return export ? new MemoryStream(File.ReadAllBytes(path), false) : Stream.Null;
            }
            finally
            {
                if (info != IntPtr.Zero) Send(info, Selector("release"));
                NativeLibrary.Free(library);
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }
}
