using System;
using System.Linq;
using UnityEngine;

[Serializable]
public sealed class DrivingKeyBindings
{
    public sealed class Control
    {
        public readonly string name,help;
        public readonly int page;
        public readonly KeyCode primary,alternate;
        public Control(int page,string name,string help,KeyCode primary,KeyCode alternate=KeyCode.None)
        {this.page=page;this.name=name;this.help=help;this.primary=primary;this.alternate=alternate;}
    }

    public static readonly Control[] Controls={
        new(0,"Accelerator","Drive forward",KeyCode.W,KeyCode.UpArrow),
        new(0,"Brake","Slow down or stop",KeyCode.S,KeyCode.DownArrow),
        new(0,"Steer left","Turn left",KeyCode.A,KeyCode.LeftArrow),
        new(0,"Steer right","Turn right",KeyCode.D,KeyCode.RightArrow),
        new(0,"Reverse","Release the accelerator first",KeyCode.X),
        new(0,"Steering input","Switch keyboard / mouse steering",KeyCode.U),
        new(0,"Camera","Cycle cockpit, bonnet, chase and drone",KeyCode.C),
        new(0,"Show / hide HUD","Driving only",KeyCode.J),
        new(0,"Desktop screenshot","PNG with embedded info-box metadata; driving only",KeyCode.K),
        new(0,"Mute sound","Toggle driving sound",KeyCode.M),
        new(0,"Fullscreen","Switch fullscreen / windowed in either driving mode",KeyCode.F),
        new(0,"View / driving settings","Open the settings panel",KeyCode.F3),
        new(0,"Handling","Switch AWD / Arcade",KeyCode.H),
        new(0,"Car model","Switch car where available",KeyCode.Y),
        new(0,"Paint","Change paint where available",KeyCode.T),
        new(0,"Autopilot","Toggle circuit autopilot",KeyCode.P),
        new(0,"Recover car","Return to track",KeyCode.R),
        new(0,"POI / scenery views","Cycle circuit scenery views",KeyCode.V),
        new(1,"Training area","Switch circuit / slide-practice area",KeyCode.Z),
        new(1,"Reference lap","Open / close the reference replay",KeyCode.O),
        new(1,"Restart lap","Restart circuit lap; rewind reference replay",KeyCode.Home),
        new(1,"Circuit capture","Save a circuit / scenery / replay screenshot",KeyCode.F2)
    };
    static readonly KeyCode[] Slots=Controls.SelectMany(c=>c.alternate==KeyCode.None?new[]{c.primary}:new[]{c.primary,c.alternate}).ToArray();
    public static string SaveKey=>NativeTestPreferences.Key("GotlandRing.KeyboardBindings");
    public KeyCode[] keys=(KeyCode[])Slots.Clone();

    public KeyCode Resolve(KeyCode original)=>keys[Array.IndexOf(Slots,original)];
    public string Hint(KeyCode original)=>Label(Resolve(original));
    public DrivingKeyBindings Copy()=>new(){keys=(KeyCode[])keys.Clone()};
    public bool Held(KeyCode original){var key=Resolve(original);return key!=KeyCode.None&&Input.GetKey(key);}
    public bool Down(KeyCode original)
    {
        var key=Resolve(original);
        return key!=KeyCode.None&&(Input.GetKeyDown(key)||(key==KeyCode.Plus&&!Input.GetKeyDown(KeyCode.KeypadPlus)&&Input.inputString.Contains("+")));
    }
    public bool TryBind(KeyCode original,KeyCode replacement,out string message)
    {
        if(replacement==KeyCode.Escape){message="Escape is reserved for menus and cancelling.";return false;}
        int slot=Array.IndexOf(Slots,original);
        int conflict=replacement==KeyCode.None?-1:Array.IndexOf(keys,replacement);
        if(conflict>=0&&conflict!=slot)
        {
            var action=Controls.First(c=>c.primary==Slots[conflict]||c.alternate==Slots[conflict]);
            message=$"{Label(replacement)} is already assigned to {action.name}. Clear or change that binding first.";
            return false;
        }
        keys[slot]=replacement;message="";return true;
    }
    public string ToJson()=>JsonUtility.ToJson(this);
    public static DrivingKeyBindings FromJson(string json)
    {
        var result=JsonUtility.FromJson<DrivingKeyBindings>(json);
        if(result?.keys==null||result.keys.Length!=Slots.Length||
            result.keys.Any(k=>k==KeyCode.Escape||!Enum.IsDefined(typeof(KeyCode),k))||
            result.keys.Where(k=>k!=KeyCode.None).Distinct().Count()!=result.keys.Count(k=>k!=KeyCode.None))
            throw new ArgumentException("Invalid keyboard bindings");
        return result;
    }
    public static DrivingKeyBindings Load()
    {
        if(!PlayerPrefs.HasKey(SaveKey))return new();
        try{return FromJson(PlayerPrefs.GetString(SaveKey));}
        catch(ArgumentException e){Debug.LogWarning("Keyboard bindings reset: "+e.Message);return new();}
    }
    public void Save(){PlayerPrefs.SetString(SaveKey,ToJson());PlayerPrefs.Save();}
    public static string Label(KeyCode key)=>key switch
    {
        KeyCode.None=>"Unassigned",KeyCode.UpArrow=>"Up",KeyCode.DownArrow=>"Down",
        KeyCode.LeftArrow=>"Left",KeyCode.RightArrow=>"Right",KeyCode.Plus=>"+",
        KeyCode.KeypadPlus=>"Num +",KeyCode.Return=>"Enter",KeyCode.KeypadEnter=>"Num Enter",
        KeyCode.BackQuote=>"`",KeyCode.Equals=>"=",KeyCode.Minus=>"-",KeyCode.Space=>"Space",
        _=>key.ToString()
    };
}
