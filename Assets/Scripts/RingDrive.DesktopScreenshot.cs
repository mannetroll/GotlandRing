using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public partial class RingDrive
{
    IEnumerator SaveDesktopScreenshot()
    {
        yield return new WaitForEndOfFrame();
        var captured=DateTimeOffset.Now;
        var image=ScreenCapture.CaptureScreenshotAsTexture();
        try
        {
            var png=image.EncodeToPNG();
            var description=CircuitScreenshotInfo();
            var cameraAngles=cam.transform.eulerAngles;
            using var output=new MemoryStream();
            // Unity's PNG ends with the 12-byte IEND chunk. Insert UTF-8 metadata before it.
            output.Write(png,0,png.Length-12);
            WriteScreenshotText(output,"Software",$"Gotland Ring v{Application.version}");
            WriteScreenshotText(output,"Creation Time",captured.UtcDateTime.ToString("R",CultureInfo.InvariantCulture));
            WriteScreenshotText(output,"Description",description);
            WriteScreenshotText(output,"HUD",hideDrivingHud?"Hidden":"Visible");
            WriteScreenshotText(output,"Vehicle",$"{carModel.displayName} · {carModel.PaintName}");
            WriteScreenshotText(output,"Camera",FormattableString.Invariant(
                $"View {view} · Heading {cameraAngles.y:F3}° · Pitch {cameraAngles.x:F3}° · Roll {cameraAngles.z:F3}° · FOV {cam.fieldOfView:F3}°"));
            output.Write(png,png.Length-12,12);
            string desktop=Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string filename="GotlandRing-"+captured.ToString("yyyy-MM-dd_HH-mm-ss-fffffff",CultureInfo.InvariantCulture)+".png";
            string path=Path.Combine(desktop,filename);
            using var file=new FileStream(path,FileMode.CreateNew,FileAccess.Write);
            output.Position=0;output.CopyTo(file);
            Debug.Log("SCREENSHOT saved with embedded metadata: "+path);
        }
        catch(Exception error) when(error is IOException||error is UnauthorizedAccessException)
        {
            Debug.LogError("Could not save screenshot to Desktop: "+error.Message);
        }
        finally {Destroy(image);}
    }

    string CircuitScreenshotInfo()
    {
        return $"{(drivingSurface.Training?"TRAINING AREA / SLIDE PRACTICE":"GOTLAND RING / OPEN PRACTICE")}\n"
            +$"{carModel.displayName} / {carModel.PaintName}\n"
            +$"Speed {velocity.magnitude*3.6f:0} km/h · Gear {(reversing?"R":gear.ToString())}\n"
            +$"{rpm:0} RPM · Boost {boost*.9f:0.00} bar · Handling {(dynamics.awdMode?"AWD":"Arcade")}\n"
            +$"Autopilot {(autopilotEnabled?"ON":"OFF")} · Mouse steering {(mouseSteering?"ON":"OFF")}\n"
            +(dynamics.awdMode?$"Side slip {Mathf.Abs(awd.SideslipDegrees):0.0}°\n":"")
            +(autopilotEnabled?$"Target {pilotControls.TargetSpeed*3.6f:0} km/h · Gas {pilotControls.Throttle*100:0}% · Brake {pilotControls.Brake*100:0}%\n":"")
            +(drivingSurface.Training?"800 × 800 m / ASPHALT\n":$"Lap {lap} · Time {Format(Time.time-lapStart)} · Best {(best>0?Format(best):"—")} · Track {length/1000:0.000} km\n")
            +fpsText;
    }

    static void WriteScreenshotText(Stream output,string keyword,string text)
    {
        // iTXt: keyword, null, compression flag/method, empty language and translated keyword, UTF-8 text.
        byte[] data=Encoding.UTF8.GetBytes("iTXt"+keyword+"\0\0\0\0\0"+text);
        WriteScreenshotUInt(output,(uint)(data.Length-4));
        output.Write(data,0,data.Length);
        uint crc=0xffffffff;
        foreach(byte value in data)
        {
            crc^=value;
            for(int bit=0;bit<8;bit++)crc=(crc>>1)^((crc&1)!=0?0xedb88320u:0u);
        }
        WriteScreenshotUInt(output,~crc);
    }

    static void WriteScreenshotUInt(Stream output,uint value)
    {
        output.WriteByte((byte)(value>>24));output.WriteByte((byte)(value>>16));
        output.WriteByte((byte)(value>>8));output.WriteByte((byte)value);
    }
}
