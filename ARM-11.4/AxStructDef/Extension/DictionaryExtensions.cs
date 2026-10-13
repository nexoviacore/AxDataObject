using System;
using System.Collections.Generic;
using System.Linq; 
namespace AxStructDef.Extension; 

public static class DictionaryExtensions
{
    public static string GetName(this Dictionary<string, string> dict, int index)
    {
        if (index < 0 || index >= dict.Count)
            throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
        return dict.Keys.ElementAt(index);
    }

    public static string GetValueFromIndex(this Dictionary<string, string> dict, int index)
    {
        if (index < 0 || index >= dict.Count)
            throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
        return dict.Values.ElementAt(index);
    }
}