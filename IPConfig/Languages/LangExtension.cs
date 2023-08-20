using System.Windows.Data;

namespace IPConfig.Languages;

/// <summary>
/// <see href="https://www.codinginfinity.me/posts/localization-of-a-wpf-app-the-simple-approach/">localization-of-a-wpf-app-the-simple-approach</see>
/// </summary>
public class LangExtension : Binding
{
    public LangExtension(LangKey langKey) : base("[" + langKey + "]")
    {
        Mode = BindingMode.OneWay;
        Source = LangSource.Instance;
    }
}
