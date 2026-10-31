using UnityEngine;

public static class GameUi
{
    public static readonly Color Background=new Color32(20,23,29,255);
    public static readonly Color Surface=new Color32(31,35,43,255);
    public static readonly Color Text=new Color32(235,239,247,255);
    public static readonly Color Muted=new Color32(156,167,185,255);
    public static readonly Color Accent=new Color32(104,166,255,255);
    static GUISkin skin;
    static GUIStyle panel,shadow,primary,selected,quiet,heading,caption;

    public static void Apply()
    {
        if(!skin)Create();
        GUI.skin=skin;
    }
    static void Create()
    {
        skin=Object.Instantiate(GUI.skin);
        skin.name="Gotland dark theme";skin.hideFlags=HideFlags.HideAndDontSave;
        skin.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        skin.settings.cursorColor=Accent;skin.settings.selectionColor=new Color(.24f,.43f,.7f,.65f);
        skin.label.font=skin.font;skin.label.fontSize=17;skin.label.padding=new RectOffset(0,0,0,0);
        skin.label.normal.textColor=Text;
        var edge=new Color32(62,70,84,255);
        Configure(skin.button,new Color32(46,52,63,255),new Color32(59,69,84,255),new Color32(36,66,104,255),edge,10);
        skin.button.fontSize=17;skin.button.alignment=TextAnchor.MiddleCenter;
        skin.button.padding=new RectOffset(14,14,5,5);skin.button.margin=new RectOffset(3,3,3,3);
        Configure(skin.box,Surface,Surface,Surface,edge,14);
        skin.box.normal.textColor=Text;skin.box.padding=new RectOffset(16,16,12,12);
        Configure(skin.textField,new Color32(22,26,33,255),new Color32(27,33,43,255),new Color32(22,26,33,255),edge,8);
        skin.textField.focused.background=Rounded(new Color32(22,26,33,255),Accent,8);
        skin.textField.fontSize=17;skin.textField.alignment=TextAnchor.MiddleLeft;skin.textField.padding=new RectOffset(12,12,5,5);
        skin.textArea=new GUIStyle(skin.textField){wordWrap=true,alignment=TextAnchor.UpperLeft};
        primary=new GUIStyle(skin.button);
        Configure(primary,new Color32(49,113,210,255),new Color32(65,132,232,255),new Color32(38,92,176,255),new Color32(90,149,235,255),10);
        selected=new GUIStyle(skin.button);
        Configure(selected,new Color32(34,54,81,255),new Color32(42,65,96,255),new Color32(38,71,113,255),Accent,12);
        quiet=new GUIStyle(skin.button);
        Configure(quiet,new Color32(31,35,43,0),new Color32(55,62,76,255),new Color32(36,66,104,255),Color.clear,8);
        quiet.padding=new RectOffset();quiet.normal.textColor=Muted;
        panel=new GUIStyle(skin.box);
        shadow=new GUIStyle(skin.box);shadow.normal.background=Rounded(new Color(0,0,0,.28f),Color.clear,16);
        heading=new GUIStyle(skin.label){fontSize=28,fontStyle=FontStyle.Bold};
        caption=new GUIStyle(skin.label){fontSize=15,wordWrap=true};caption.normal.textColor=Muted;
        Configure(skin.toggle,new Color32(31,35,43,255),new Color32(43,50,63,255),new Color32(34,67,108,255),edge,8);
        skin.toggle.padding=new RectOffset(14,14,4,4);skin.toggle.fontSize=17;skin.toggle.alignment=TextAnchor.MiddleLeft;
        skin.toggle.onNormal.background=Rounded(new Color32(34,67,108,255),Accent,8);
        skin.toggle.onHover.background=skin.toggle.onNormal.background;
        skin.toggle.onActive.background=skin.toggle.onNormal.background;
        skin.toggle.onFocused.background=skin.toggle.onNormal.background;
        Configure(skin.horizontalSlider,new Color32(58,65,79,255),new Color32(58,65,79,255),new Color32(58,65,79,255),Color.clear,3);
        skin.horizontalSlider.border=new RectOffset(3,3,3,3);
        skin.horizontalSlider.fixedHeight=6;skin.horizontalSlider.margin=new RectOffset(0,0,8,0);
        Configure(skin.horizontalSliderThumb,Accent,new Color32(152,197,255,255),Color.white,Color.clear,9);
        skin.horizontalSliderThumb.border=new RectOffset(10,10,10,10);
        skin.horizontalSliderThumb.fixedWidth=18;skin.horizontalSliderThumb.fixedHeight=18;
        skin.horizontalSliderThumb.margin=new RectOffset(0,0,-6,0);
        Configure(skin.verticalScrollbar,Color.clear,Color.clear,Color.clear,Color.clear,5);
        skin.verticalScrollbar.fixedWidth=10;
        Configure(skin.verticalScrollbarThumb,new Color32(76,85,102,255),new Color32(103,116,138,255),Accent,Color.clear,5);
        skin.verticalScrollbarThumb.border=new RectOffset(6,6,6,6);
        skin.verticalScrollbarThumb.fixedWidth=8;skin.verticalScrollbarThumb.fixedHeight=0;
        skin.verticalScrollbarThumb.margin=new RectOffset(1,1,0,0);
        skin.verticalScrollbarThumb.padding=new RectOffset(0,0,8,8);
        Configure(skin.horizontalScrollbar,Color.clear,Color.clear,Color.clear,Color.clear,5);
        skin.horizontalScrollbar.fixedHeight=10;
        Configure(skin.horizontalScrollbarThumb,new Color32(76,85,102,255),new Color32(103,116,138,255),Accent,Color.clear,5);
        skin.horizontalScrollbarThumb.border=new RectOffset(6,6,6,6);
        skin.horizontalScrollbarThumb.fixedHeight=8;skin.horizontalScrollbarThumb.fixedWidth=0;
        foreach(var arrow in new[]{skin.verticalScrollbarUpButton,skin.verticalScrollbarDownButton,skin.horizontalScrollbarLeftButton,skin.horizontalScrollbarRightButton})
        {Configure(arrow,Color.clear,Color.clear,Color.clear,Color.clear,0);arrow.fixedWidth=arrow.fixedHeight=0;arrow.margin=new RectOffset();arrow.padding=new RectOffset();}
        skin.scrollView.normal.background=null;
    }
    static void Configure(GUIStyle style,Color normal,Color hover,Color active,Color edge,float radius)
    {
        style.font=skin.font;style.border=new RectOffset(16,16,16,16);
        style.normal.background=Rounded(normal,edge,radius);style.normal.textColor=Text;
        style.hover.background=Rounded(hover,edge,radius);style.hover.textColor=Color.white;
        style.active.background=Rounded(active,Accent,radius);style.active.textColor=Color.white;
        style.focused.background=Rounded(normal,Accent,radius);style.focused.textColor=Text;
        style.onNormal.background=Rounded(active,Accent,radius);style.onNormal.textColor=Color.white;
        style.onHover.background=style.onNormal.background;style.onHover.textColor=Color.white;
        style.onActive.background=style.active.background;style.onActive.textColor=Color.white;
        style.onFocused.background=style.onNormal.background;style.onFocused.textColor=Color.white;
    }
    static Texture2D Rounded(Color fill,Color border,float radius)
    {
        const int size=48;
        var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name="UI rounded surface",hideFlags=HideFlags.HideAndDontSave,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
        var pixels=new Color[size*size];
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            var q=new Vector2(Mathf.Abs(x+.5f-size*.5f),Mathf.Abs(y+.5f-size*.5f))-Vector2.one*(size*.5f-radius-1);
            float distance=new Vector2(Mathf.Max(q.x,0),Mathf.Max(q.y,0)).magnitude+Mathf.Min(Mathf.Max(q.x,q.y),0)-radius;
            var color=Color.Lerp(fill,border,border.a>0?Mathf.Clamp01(distance+1.6f):0);
            color.a*=Mathf.Clamp01(.5f-distance);
            // IMGUI's texture shader displays these pixels as linear values in a linear project.
            pixels[y*size+x]=QualitySettings.activeColorSpace==ColorSpace.Linear?color.linear:color;
        }
        texture.SetPixels(pixels);texture.Apply(false,true);return texture;
    }
    public static void Panel(Rect rect,bool elevated=false)
    {
        if(Event.current.type!=EventType.Repaint)return;
        var color=GUI.color;GUI.color=Color.white;
        if(elevated)shadow.Draw(new Rect(rect.x-3,rect.y+5,rect.width+6,rect.height+6),GUIContent.none,false,false,false,false);
        panel.Draw(rect,GUIContent.none,false,false,false,false);GUI.color=color;
    }
    public static void Dialog(Rect rect)
    {
        var color=GUI.color;GUI.color=new Color(0,0,0,.62f);
        GUI.DrawTexture(new Rect(-Screen.width,-Screen.height,Screen.width*3,Screen.height*3),Texture2D.whiteTexture);
        GUI.color=Color.white;Panel(rect,true);GUI.color=color;
    }
    public static bool PrimaryButton(Rect rect,string text,GUIStyle style=null)
    {
        primary.fontSize=style?.fontSize??17;return GUI.Button(rect,text,primary);
    }
    public static bool QuietButton(Rect rect,GUIContent text,GUIStyle style=null)
    {
        quiet.fontSize=style?.fontSize??17;return GUI.Button(rect,text,quiet);
    }
    public static GUIStyle Selected(GUIStyle style)
    {
        selected.fontSize=style.fontSize;selected.alignment=style.alignment;return selected;
    }
    public static bool Switch(Rect rect,bool value,string text)
    {
        GUI.Label(new Rect(rect.x,rect.y,rect.width-80,rect.height),text);
        selected.fontSize=17;selected.alignment=TextAnchor.MiddleCenter;
        return GUI.Toggle(new Rect(rect.xMax-64,rect.y,64,rect.height),value,value?"On":"Off",value?selected:skin.button);
    }
    public static int Tabs(Rect rect,int current,string[] names,int fontSize=17)
    {
        float width=(rect.width-4*(names.Length-1))/names.Length;
        for(int i=0;i<names.Length;i++)
        {
            var style=i==current?selected:quiet;style.fontSize=fontSize;style.alignment=TextAnchor.MiddleCenter;
            if(GUI.Toggle(new Rect(rect.x+i*(width+4),rect.y,width,rect.height),i==current,names[i],style))current=i;
        }
        return current;
    }
    public static GUIStyle Heading=>heading;
    public static GUIStyle Caption=>caption;
}
