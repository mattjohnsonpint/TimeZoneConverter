using System.Runtime.CompilerServices;

namespace TimeZoneConverter.Tests
{
    public static class ModuleInit
    {
        [ModuleInitializer]
        public static void Init()
        {
            VerifierSettings.AutoVerify();
        }
    }
}
