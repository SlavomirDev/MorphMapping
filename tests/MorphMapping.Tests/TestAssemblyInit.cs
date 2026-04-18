using System.Globalization;
using System.Runtime.CompilerServices;

namespace MorphMapping.Tests
{
    /// <summary>
    /// Pins the thread culture for every test in this assembly so assertions that rely on
    /// culture-sensitive formatting (e.g. <c>decimal.ToString("F2")</c> producing "9,99" vs
    /// "9.99") stay stable across developer machines and CI runners. Without this, tests
    /// produced comma decimal separators on Windows-Russian hosts and dot separators on the
    /// ubuntu-latest runner, causing false CI failures.
    /// </summary>
    internal static class TestAssemblyInit
    {
        [ModuleInitializer]
        internal static void Init()
        {
            var culture = CultureInfo.GetCultureInfo("ru-RU");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }
    }
}
