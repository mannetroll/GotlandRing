using System;
using System.Collections;
using System.IO;
using UnityEngine;

public partial class RingDrive
{
    static void ModeCheck(bool condition,string message){if(!condition)throw new Exception(message);}
    IEnumerator KeyboardHelpCheck()
    {
        Application.runInBackground=true;
        bool saved=PlayerPrefs.HasKey(DrivingKeyBindings.SaveKey);
        string previous=PlayerPrefs.GetString(DrivingKeyBindings.SaveKey);
        var steps=KeyboardHelpCheckSteps();
        bool passed=true;
        while(true)
        {
            try{if(!steps.MoveNext())break;}
            catch(Exception error){Debug.LogException(error);passed=false;break;}
            yield return steps.Current;
        }
        if(saved)PlayerPrefs.SetString(DrivingKeyBindings.SaveKey,previous);
        else PlayerPrefs.DeleteKey(DrivingKeyBindings.SaveKey);
        PlayerPrefs.Save();
        Application.Quit(passed?0:1);
    }
    IEnumerator KeyboardHelpCheckSteps()
    {
        yield return null;
        SetPaused(true);keyBindings=new DrivingKeyBindings();
        OpenKeyboardHelp();
        ModeCheck(!keyboardDraft.TryBind(KeyCode.C,KeyCode.W,out var conflict)&&conflict.Contains("Accelerator"),"duplicate key reports the conflicting action");
        ModeCheck(keyboardDraft.TryBind(KeyCode.C,KeyCode.F8,out _),"camera accepts a free key");
        CloseKeyboardHelp(false);
        ModeCheck(Keys.Resolve(KeyCode.C)==KeyCode.C&&paused,"Cancel leaves active bindings unchanged and stays paused");
        OpenKeyboardHelp();
        keyboardDraft.TryBind(KeyCode.C,KeyCode.F8,out _);
        keyboardDraft.TryBind(KeyCode.UpArrow,KeyCode.F9,out _);
        CloseKeyboardHelp(true);
        keyBindings=DrivingKeyBindings.Load();
        ModeCheck(Keys.Resolve(KeyCode.C)==KeyCode.F8&&Keys.Resolve(KeyCode.UpArrow)==KeyCode.F9,"saved camera and alternate movement keys reload");
        ModeCheck(KeyHint(KeyCode.C)=="F8"&&KeyHint(KeyCode.UpArrow)=="F9","on-screen hints use saved bindings");
        OpenKeyboardHelp();
        ModeCheck(keyboardDraft.TryBind(KeyCode.C,KeyCode.None,out _)&&keyboardDraft.TryBind(KeyCode.J,KeyCode.F8,out _),"clearing a binding frees its key for another action");
        ModeCheck(!keyboardDraft.TryBind(KeyCode.K,KeyCode.Escape,out _),"Escape stays available for leaving menus");
        CloseKeyboardHelp(false);
        ModeCheck(Keys.Resolve(KeyCode.C)==KeyCode.F8&&Keys.Resolve(KeyCode.J)==KeyCode.J,"Cancel discards a reassignment without changing active controls");
        OpenKeyboardHelp();keyboardDraft=new DrivingKeyBindings();CloseKeyboardHelp(true);
        keyBindings=DrivingKeyBindings.Load();
        ModeCheck(Keys.Resolve(KeyCode.C)==KeyCode.C&&Keys.Resolve(KeyCode.UpArrow)==KeyCode.UpArrow,"Restore defaults saves and reloads");
        const string checks="KEYBOARD_HELP_CHECK passed: conflicts, cancellation, saved bindings reload, alternate movement, cleared bindings, reserved Escape and default restore";
        if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)
        {
            Debug.Log(checks+"; rendered captures skipped: no graphics device");
            yield break;
        }
        string folder=Environment.GetEnvironmentVariable("GOTLAND_TEST_OUTPUT");Directory.CreateDirectory(folder);
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(folder,"escape-menu.png"));
        yield return new WaitForEndOfFrame();yield return null;
        OpenKeyboardHelp();
        for(int page=0;page<2;page++)
        {
            keyboardPage=page;keyboardScroll=Vector2.zero;
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(folder,new[]{"keyboard-help.png","circuit-keys.png"}[page]));
            yield return new WaitForEndOfFrame();yield return null;
        }
        keyboardPage=0;keyboardScroll=new Vector2(0,480);
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(folder,"shared-shortcuts.png"));
        yield return new WaitForEndOfFrame();yield return null;
        Debug.Log(checks+"; two rendered help pages");
    }
}
