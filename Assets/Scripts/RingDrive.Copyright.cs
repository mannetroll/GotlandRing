using UnityEngine;

public partial class RingDrive
{
    bool hideDrivingHud;
    bool DrivingHudActive=>!paused&&!settingsOpen&&!referenceLap&&!SceneryViewing;
    float InstrumentOverlayWidth=>Mathf.Min(600*Mathf.Min(Screen.width/1600f,Screen.height/900f),Screen.width*.48f)*(view==2?.75f:1f);
    void DrawInstrumentOverlay()
    {
        float width=InstrumentOverlayWidth;
        carModel.instruments.Draw(new Rect((Screen.width-width)*.5f,Screen.height-width/3,width,width/3));
    }
    void OnGUI()
    {
        if(hideDrivingHud&&DrivingHudActive)return;
        GameUi.Apply();
        DrawGameGUI();
        var matrix=GUI.matrix;var colour=GUI.color;bool enabled=GUI.enabled;
        GUI.matrix=Matrix4x4.identity;GUI.color=Color.white;GUI.enabled=true;
        if(!paused&&!settingsOpen&&!referenceLap&&!SceneryViewing&&(view==1||view==2)&&carModel)DrawInstrumentOverlay();
        GUI.matrix=matrix;GUI.color=colour;GUI.enabled=enabled;
    }
}
