using UnityEngine;

public partial class RingDrive
{
    bool droneSettingsOpen;
    float droneHeightDraft,droneDistanceDraft;
    void OpenDroneSettings()
    {
        droneHeightDraft=dynamics.droneHeight;droneDistanceDraft=dynamics.droneDistance;
        droneSettingsOpen=true;
    }
    void CloseDroneSettings(bool apply)
    {
        if(apply)
        {
            dynamics.droneHeight=droneHeightDraft;dynamics.droneDistance=droneDistanceDraft;
            dynamics.Save();lookYaw=lookPitch=0;UpdateCameraPose();
        }
        droneSettingsOpen=false;
    }
    void DrawDroneSettings()
    {
        GameUi.Dialog(new Rect(400,185,800,530));
        GUI.color=Color.white;
        GUI.Label(new Rect(440,225,720,50),"Drone camera",modeTitle);
        GUI.Label(new Rect(440,295,720,45),"An elevated follow view. Mouse looks around; right-click recenters.",modeSecondary);
        GUI.Label(new Rect(440,360,720,30),$"Height above car: {droneHeightDraft:0} m",modeBody);
        droneHeightDraft=Mathf.Round(GUI.HorizontalSlider(new Rect(440,400,720,25),droneHeightDraft,5,60));
        GUI.Label(new Rect(440,450,720,30),$"Following distance: {droneDistanceDraft:0} m",modeBody);
        droneDistanceDraft=Mathf.Round(GUI.HorizontalSlider(new Rect(440,490,720,25),droneDistanceDraft,5,120));
        GUI.Label(new Rect(440,540,720,45),$"{KeyHint(KeyCode.C)} selects the drone view. Apply saves these settings for future drives.",modeSecondary);
        if(GUI.Button(new Rect(440,630,220,44),"Restore defaults",modeButton)){var defaults=new DrivingSettings();droneHeightDraft=defaults.droneHeight;droneDistanceDraft=defaults.droneDistance;}
        if(GUI.Button(new Rect(690,630,220,44),"Cancel [Esc]",modeButton))CloseDroneSettings(false);
        if(GameUi.PrimaryButton(new Rect(940,630,220,44),"Apply",modeButton))CloseDroneSettings(true);
    }
}
