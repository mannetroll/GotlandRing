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

 [DllImport(ObjectiveC)]
 static extern IntPtr objc_getClass(string name);
 [DllImport(ObjectiveC)]
 static extern IntPtr sel_registerName(string name);
 [DllImport(ObjectiveC, EntryPoint = "objc_msgSend")]
 static extern IntPtr Send(IntPtr receiver, IntPtr selector);
 [DllImport(ObjectiveC, EntryPoint = "objc_msgSend")]
 static extern void SetSize(IntPtr receiver, IntPtr selector, Size size);

 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Initialize() => new GameObject("Window aspect ratio").AddComponent<MacWindowAspectRatio>();

 IEnumerator Start()
 {
  var application = Send(objc_getClass("NSApplication"), sel_registerName("sharedApplication"));
  var mainWindow = sel_registerName("mainWindow");
  IntPtr window;
  // AppKit may not have made the player window main yet during startup.
  while ((window = Send(application, mainWindow)) == IntPtr.Zero) yield return null;
  SetSize(window, sel_registerName("setContentAspectRatio:"), new Size { width = 16, height = 9 });
  if (!Screen.fullScreen && Screen.width * 9 != Screen.height * 16)
   Screen.SetResolution(Screen.width, Mathf.RoundToInt(Screen.width * 9f / 16), FullScreenMode.Windowed);
  Destroy(gameObject);
 }
}
#endif
