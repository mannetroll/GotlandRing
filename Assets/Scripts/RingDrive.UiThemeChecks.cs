using System;
using System.Collections;
using System.IO;
using UnityEngine;

public partial class RingDrive
{
    IEnumerator UiThemePreview()
    {
        var steps=UiThemePreviewSteps();
        while(true)
        {
            try{if(!steps.MoveNext())yield break;}
            catch(Exception error){Debug.LogException(error);Application.Quit(1);yield break;}
            yield return steps.Current;
        }
    }
    IEnumerator UiThemeShot(string name)
    {
        yield return null;
        yield return new WaitForEndOfFrame();
        string folder=Environment.GetEnvironmentVariable("GOTLAND_TEST_OUTPUT");Directory.CreateDirectory(folder);
        ScreenCapture.CaptureScreenshot(Path.Combine(folder,name+".png"));
        yield return new WaitForEndOfFrame();yield return null;
        Debug.Log("UI_THEME_CAPTURE "+name);
    }
    IEnumerator UiThemePreviewSteps()
    {
        yield return null;
        {
            yield return UiThemeShot("circuit-hud");
            int previousView=view;
            view=1;
            yield return UiThemeShot("circuit-hud-bonnet");
            view=previousView;
            ToggleMouseSteering();
            yield return UiThemeShot("circuit-hud-mouse");
            SetAutopilot(true);
            yield return UiThemeShot("circuit-hud-autopilot-mouse");
            ToggleMouseSteering();
            yield return UiThemeShot("circuit-hud-autopilot");
            SetAutopilot(false);
            SetPaused(true);
            yield return UiThemeShot("circuit-menu");
            OpenSettings();settingsPage=0;draft.awdMode=true;
            yield return UiThemeShot("driving-dynamics");
            draft.awdMode=false;
            yield return UiThemeShot("arcade-settings");
            CloseSettings(false);
            OpenSettings();settingsPage=1;
            yield return UiThemeShot("view-sound");
            CloseSettings(false);
            OpenDroneSettings();
            yield return UiThemeShot("circuit-drone");
            CloseDroneSettings(false);
            OpenKeyboardHelp();
            int windowWidth=Screen.width,windowHeight=Screen.height;
            Screen.SetResolution(1280,720,FullScreenMode.Windowed);
            yield return new WaitForSecondsRealtime(.4f);
            yield return UiThemeShot("keyboard-720p");
            CloseKeyboardHelp(false);
            yield return UiThemeShot("menu-720p");
            SetPaused(false);
            yield return UiThemeShot("circuit-hud-720p");
            view=1;
            yield return UiThemeShot("circuit-hud-bonnet-720p");
            view=previousView;
            ToggleMouseSteering();SetAutopilot(true);
            yield return UiThemeShot("circuit-hud-expanded-720p");
            SetAutopilot(false);ToggleMouseSteering();
            ToggleTrainingArea();
            yield return UiThemeShot("training-hud-720p");
            ToggleTrainingArea();
            Screen.SetResolution(windowWidth,windowHeight,FullScreenMode.Windowed);
            yield return new WaitForSecondsRealtime(.4f);
            Debug.Log("UI_THEME_CHECK passed: Circuit menus, dialogs, settings, keyboard pages and 720p layout; no settings saved");
            Application.Quit(0);
        }
    }
}
