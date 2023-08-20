using System;
using System.IO;
using System.Windows;

namespace IPConfig.Helpers;

public static class ResourceHelper
{
    public static Stream? GetResourceStream(string path)
    {
        try
        {
            var stream = Application.GetResourceStream(new(path, UriKind.RelativeOrAbsolute)).Stream;

            return stream;
        }
        catch (IOException)
        {
            return null;
        }
    }
}
