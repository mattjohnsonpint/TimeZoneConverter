using System.Runtime.CompilerServices;

namespace TimeZoneConverter.Tests
{
    public static class ModuleInit
    {
        [ModuleInitializer]
        public static void Init()
        {
            VerifierSettings.AutoVerify(includeBuildServer: false);
        }
    }
}

#if NETFRAMEWORK
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class ModuleInitializerAttribute : Attribute
    {
    }
}
#endif
