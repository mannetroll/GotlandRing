using UnityEngine;

public partial class RingDrive
{
    DrivingKeyBindings keyBindings,keyboardDraft;
    DrivingKeyBindings Keys=>keyBindings??=DrivingKeyBindings.Load();
    bool keyboardHelpOpen;
    int keyboardPage;
    Vector2 keyboardScroll;
    KeyCode keyboardCapture;
    string keyboardMessage="";
    bool DriveKey(KeyCode original)=>Keys.Held(original);
    bool DriveKeyDown(KeyCode original)=>Keys.Down(original);
    string KeyHint(KeyCode original)=>Keys.Hint(original);

    void OpenKeyboardHelp()
    {
        keyboardDraft=Keys.Copy();keyboardHelpOpen=true;
        keyboardCapture=KeyCode.None;keyboardMessage="";keyboardScroll=Vector2.zero;
    }
    void CloseKeyboardHelp(bool apply)
    {
        if(apply){keyBindings=keyboardDraft;keyBindings.Save();ResetMouseSteering();}
        keyboardHelpOpen=false;keyboardCapture=KeyCode.None;
    }
    void DrawKeyboardHelp()
    {
        var ev=Event.current;
        if(keyboardCapture!=KeyCode.None&&ev.type==EventType.KeyDown)
        {
            if(ev.keyCode==KeyCode.Escape){keyboardCapture=KeyCode.None;keyboardMessage="";}
            else if(ev.keyCode!=KeyCode.None&&ev.keyCode!=KeyCode.LeftShift&&ev.keyCode!=KeyCode.RightShift&&
                ev.keyCode!=KeyCode.LeftControl&&ev.keyCode!=KeyCode.RightControl&&
                ev.keyCode!=KeyCode.LeftCommand&&ev.keyCode!=KeyCode.RightCommand&&
                ev.keyCode!=KeyCode.LeftAlt&&ev.keyCode!=KeyCode.RightAlt)
            {
                var key=ev.keyCode==KeyCode.KeypadPlus?KeyCode.KeypadPlus:ev.character=='+'?KeyCode.Plus:ev.keyCode;
                if(keyboardDraft.TryBind(keyboardCapture,key,out keyboardMessage))keyboardCapture=KeyCode.None;
            }
            ev.Use();
        }
        GameUi.Dialog(new Rect(220,50,1160,800));
        GUI.color=Color.white;
        GUI.Label(new Rect(260,75,1080,48),"Keyboard help & setup",modeTitle);
        GUI.Label(new Rect(260,127,1080,28),"Click a key, then press its replacement. Save applies to Gotland Ring and future launches.",modeSecondary);
        int page=GameUi.Tabs(new Rect(260,170,1080,34),keyboardPage,new[]{"Driving & view","Gotland Ring"});
        if(page!=keyboardPage){keyboardPage=page;keyboardScroll=Vector2.zero;keyboardCapture=KeyCode.None;keyboardMessage="";}
        GUI.Label(new Rect(260,211,720,24),"CONTROL / HELP",modeEyebrow);
        GUI.Label(new Rect(1040,211,130,24),"PRIMARY",modeEyebrow);
        GUI.Label(new Rect(1190,211,130,24),"ALTERNATE",modeEyebrow);
        int count=0;foreach(var control in DrivingKeyBindings.Controls)if(control.page==keyboardPage)count++;
        keyboardScroll=GUI.BeginScrollView(new Rect(250,239,1100,455),keyboardScroll,new Rect(0,0,1070,count*48));
        float y=0;
        foreach(var control in DrivingKeyBindings.Controls)
        {
            if(control.page!=keyboardPage)continue;
            GUI.Label(new Rect(10,y+3,260,28),control.name,modeBody);
            GUI.Label(new Rect(275,y+3,490,42),control.help,modeSecondary);
            DrawKeyboardBinding(control.primary,790,y);
            if(control.alternate!=KeyCode.None)DrawKeyboardBinding(control.alternate,940,y);
            y+=48;
        }
        GUI.EndScrollView();
        GUI.Label(new Rect(260,704,1080,46),keyboardCapture!=KeyCode.None
            ?"Press a key now. Escape cancels this assignment. "+keyboardMessage
            :keyboardMessage.Length>0?keyboardMessage:"Mouse: look · Right-click: centre · Escape: menu / back",modeBody);
        if(GUI.Button(new Rect(260,770,235,44),"Restore defaults",modeButton))
        {keyboardDraft=new DrivingKeyBindings();keyboardCapture=KeyCode.None;keyboardMessage="Defaults restored in this panel. Save to apply.";}
        if(GUI.Button(new Rect(850,770,235,44),"Cancel [Esc]",modeButton))CloseKeyboardHelp(false);
        if(GameUi.PrimaryButton(new Rect(1105,770,235,44),"Save & back",modeButton))CloseKeyboardHelp(true);
    }
    void DrawKeyboardBinding(KeyCode original,float x,float y)
    {
        if(GUI.Button(new Rect(x,y,100,34),keyboardCapture==original?"Press key…":keyboardDraft.Hint(original),keyboardCapture==original?GameUi.Selected(modeButton):modeButton))
        {keyboardCapture=original;keyboardMessage="";}
        if(GameUi.QuietButton(new Rect(x+102,y,28,34),new GUIContent("×","Clear binding"),modeButton))
        {keyboardDraft.TryBind(original,KeyCode.None,out keyboardMessage);keyboardCapture=KeyCode.None;}
    }
}
