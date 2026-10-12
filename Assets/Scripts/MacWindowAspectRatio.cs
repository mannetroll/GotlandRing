#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

public class MacWindowAspectRatio : MonoBehaviour
{
 const string ObjectiveC = "/usr/lib/libobjc.A.dylib";

 [StructLayout(LayoutKind.Sequential)]
 struct Size { public double width, height; }
 [StructLayout(LayoutKind.Sequential)]
 struct Rect { public double x, y, width, height; }

 [DllImport(ObjectiveC)]
 static extern IntPtr objc_getClass(string name);
 [DllImport(ObjectiveC)]
 static extern IntPtr sel_registerName(string name);
 [DllImport(ObjectiveC, EntryPoint = "objc_msgSend")]
 static extern IntPtr Send(IntPtr receiver, IntPtr selector);
 [DllImport(ObjectiveC, EntryPoint = "objc_msgSend")]
 static extern void SetSize(IntPtr receiver, IntPtr selector, Size size);
 [DllImport(ObjectiveC, EntryPoint = "objc_msgSend")]
 static extern Rect GetRect(IntPtr receiver, IntPtr selector);
 [DllImport(ObjectiveC, EntryPoint = "objc_msgSend")]
 static extern void SetFrame(IntPtr receiver, IntPtr selector, Rect frame, [MarshalAs(UnmanagedType.I1)] bool display);

 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Initialize() => new GameObject("Window aspect ratio").AddComponent<MacWindowAspectRatio>();

 IEnumerator Start()
 {
  if (Application.isBatchMode) { Destroy(gameObject); yield break; }
  var application = Send(objc_getClass("NSApplication"), sel_registerName("sharedApplication"));
  var mainWindow = sel_registerName("mainWindow");
  IntPtr window;
  // AppKit may not have made the player window main yet during startup.
  while ((window = Send(application, mainWindow)) == IntPtr.Zero) yield return null;
  SetSize(window, sel_registerName("setContentAspectRatio:"), new Size { width = 16, height = 9 });
  if (!Screen.fullScreen && !Array.Exists(Environment.GetCommandLineArgs(), a => a == "-screen-width" || a == "-screen-height"))
  {
   var screen = Send(window, sel_registerName("screen"));
   var available = GetRect(screen, sel_registerName("visibleFrame"));
   var frame = GetRect(window, sel_registerName("frame"));
   var content = GetRect(Send(window, sel_registerName("contentView")), sel_registerName("frame"));
   double borderWidth = frame.width - content.width, borderHeight = frame.height - content.height;
   // AppKit geometry is in points, so Retina scaling needs no special case.
   double units = Math.Floor(Math.Min((available.width * .8 - borderWidth) / 16, (available.height * .8 - borderHeight) / 9));
   frame.width = units * 16 + borderWidth;
   frame.height = units * 9 + borderHeight;
   frame.x = available.x + (available.width - frame.width) * .5;
   frame.y = available.y + (available.height - frame.height) * .5;
   SetFrame(window, sel_registerName("setFrame:display:"), frame, true);
   yield return null;
   var actual = GetRect(window, sel_registerName("frame"));
   Debug.Log($"WINDOW_STARTUP available={available.x},{available.y},{available.width},{available.height} frame={actual.x},{actual.y},{actual.width},{actual.height} target=80% centred aspect=16:9");
  }
  Destroy(gameObject);
 }
}
#endif
