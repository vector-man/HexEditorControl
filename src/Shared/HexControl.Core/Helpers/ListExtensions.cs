﻿namespace HexControl.Core.Helpers;

internal static class ListExtensions
{
    public static List<T> Clone<T>(this IReadOnlyList<T> list) where T : ICloneable<T>
    {
        var clonedList = new List<T>(list.Count);
        for (var i = 0; i < list.Count; i++)
        {
            clonedList.Add(list[i].Clone());
        }

        return clonedList;
    }

    public static T[] CloneAll<T>(this T[] array) where T : ICloneable<T>
    {
        var clonedList = new T[array.Length];
        for (var i = 0; i < array.Length; i++)
        {
            clonedList[i] = array[i].Clone();
        }

        return clonedList;
    }
}