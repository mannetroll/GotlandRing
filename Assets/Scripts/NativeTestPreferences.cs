using System;

// Native checks persist their settings separately from the player's saved game.
public static class NativeTestPreferences
{
    static readonly bool testing=Array.Exists(Environment.GetCommandLineArgs(),
        argument=>argument.StartsWith("--")&&argument.EndsWith("-test"));
    static readonly string profile=Environment.GetEnvironmentVariable("GOTLAND_TEST_PROFILE")??"default";
    public static string Key(string name)=>testing?"GotlandRing.Tests."+profile+"."+name:name;
}
