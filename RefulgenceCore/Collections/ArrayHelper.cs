namespace Refulgence.Collections;

public static class ArrayHelper
{
    public static T[] NewOrEmpty<T>(int length)
        => length == 0
            ? []
            : new T[length];
}
