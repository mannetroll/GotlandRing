using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

internal static class Program
{
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]
 private static extern int MessageBox(IntPtr hwnd,string text,string caption,uint type);
 [STAThread]
 static int Main(string[] args)
 {
  try
  {
   using var payload=Assembly.GetExecutingAssembly().GetManifestResourceStream("Game.zip") ?? throw new Exception("Game payload is missing.");
   string version=Convert.ToHexString(SHA256.HashData(payload))[..16];payload.Position=0;
   string root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Mannetroll","GotlandRing");
   string destination=Path.Combine(root,version);
   using var mutex=new Mutex(false,"Local\\MannetrollGotlandRing"+version);
   bool held=false;
   try
   {
    try { held=mutex.WaitOne(TimeSpan.FromMinutes(3)); } catch(AbandonedMutexException) {held=true;}
    if(!held) throw new Exception("Another copy is still preparing the game. Please try again shortly.");
    if(!File.Exists(Path.Combine(destination,"ready.txt")))
    {
     Directory.CreateDirectory(root);
     string staging=Path.Combine(root,version+"-"+Guid.NewGuid().ToString("N"));
     Directory.CreateDirectory(staging);
     using(var archive=new ZipArchive(payload,ZipArchiveMode.Read,true)) archive.ExtractToDirectory(staging);
     if(!File.Exists(Path.Combine(staging,"GotlandRing.exe")) || !Directory.Exists(Path.Combine(staging,"GotlandRing_Data"))) throw new Exception("The extracted game is incomplete.");
     File.WriteAllText(Path.Combine(staging,"ready.txt"),version);
     Directory.Move(staging,destination);
    }
   }
   finally { if(held) mutex.ReleaseMutex(); }
   if(args.Contains("--extract-only")) return 0;
   var start=new ProcessStartInfo(Path.Combine(destination,"GotlandRing.exe")){WorkingDirectory=destination,UseShellExecute=false};
   foreach(string argument in args) start.ArgumentList.Add(argument);
   _ = Process.Start(start) ?? throw new Exception("Windows could not start the game.");
   return 0;
  }
  catch(Exception ex) {MessageBox(IntPtr.Zero,ex.Message,"Gotland Ring",0x10);return 1;}
 }
}

