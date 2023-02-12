using System.Runtime.CompilerServices;
using VerifyTests.DiffPlex;

namespace TimeZoneConverter.Tests
{
    public static class ModuleInit
    {
        [ModuleInitializer]
        public static void Init()
        {
            VerifyDiffPlex.Initialize(OutputType.Compact);
        }
    }
}

#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class ModuleInitializerAttribute : Attribute
    {
    }
}
#endif
