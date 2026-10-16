/*
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.IO;
using System.Reflection;

namespace SongRequestBot;

public class UserProperties
{
    public required string Username { get; set; }
    public int Tokens { get; set; } = 10;
    public bool Banned {get; set;} = false;
}

public class Users
{
    public required List<UserProperties> UserList = [
        new UserProperties {
            Username="Buffer",
            Tokens=-1,
        }
    ];
}

public class UserClassesTwitch
{
    public UserClassesTwitch()
    {
        string outPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "VidIdList");
        JsonSerializerOptions options = new()
        {
            WriteIndented = true
        };
        string VidIdJson = JsonSerializer.Serialize(TwitchClientAPI.VidIdList, options);
        File.WriteAllText(outPath, VidIdJson);
    }
}
*/