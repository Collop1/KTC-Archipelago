using System;
using System.Reflection;
using Newtonsoft.Json;

namespace APKingdom2Crown;

public static class JsonHelper
{
    private static readonly MethodInfo SerializeMethod;

    static JsonHelper()
    {
        // Get the SerializeObject(object) method from the game’s Newtonsoft
        SerializeMethod = typeof(JsonConvert).GetMethod(
            "SerializeObject",
            new Type[] { typeof(object) }
        );

        if (SerializeMethod == null)
        {
            throw new Exception("JsonConvert.SerializeObject(object) not found in the game DLL!");
        }
    }

    public static string Serialize(object obj)
    {
        return (string)SerializeMethod.Invoke(null, new object[] { obj });
    }
}
