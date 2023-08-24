using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;

namespace IPConfig.Helpers;

public static class ClipboardHelper
{
    private const int CLIPBRD_E_CANT_OPEN = unchecked((int)0x800401D0);

    public static async Task SetTextAsync(string text)
    {
        for (int i = 0; i < 10; i++)
        {
            try
            {
                Clipboard.SetDataObject(text, true);

                return;
            }
            catch (COMException ex) when (ex.ErrorCode == CLIPBRD_E_CANT_OPEN)
            { }

            await Task.Delay(100);
        }
    }
}
