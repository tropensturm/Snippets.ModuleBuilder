using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Snippets.ModuleBuilder
{
    /// <summary>
    /// inspiried by https://gist.github.com/mattifestation/8958b4c18d8bca9e221b29252cfee26b
    /// 
    /// Assembly Load* calls _nLoad which is native in the clr:
    /// [MethodImplAttribute(MethodImplOptions.InternalCall)]
    /// private static extern RuntimeAssembly _nLoad(AssemblyName fileName,
    ///                                              String codeBase,
    ///                                              Evidence assemblySecurity,
    ///                                              RuntimeAssembly locationHint,
    ///                                              ref StackCrawlMark stackMark,
    ///                                              IntPtr pPrivHostBinder,
    ///                                              bool throwOnFileNotFound,
    ///                                              bool forIntrospection,
    ///                                              bool suppressSecurityChecks);
    /// 
    /// but Module LoadModule calls:
    /// [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
    /// private extern static void LoadModule(RuntimeAssembly assembly,
    ///                                       String moduleName,
    ///                                       byte[] rawModule, 
    ///                                       int cbModule,
    ///                                       byte[] rawSymbolStore, 
    ///                                       int cbSymbolStore,
    ///                                       ObjectHandleOnStack retModule);
    /// </summary>
    class Program
    {
        internal static readonly string _logHeader = "[Snippets.ModuleBuilder]";

        internal static Action<string> logInfoTxt = s => 
            System.Diagnostics.Trace.WriteLine($"{_logHeader} {s}");
        internal static Action<string, string> logInfoVar = (s,t) => 
            System.Diagnostics.Trace.WriteLine($"{_logHeader} {s} {t ?? "<null>"}");
        internal static Action<string, Exception> logEx = (s,ex) => 
            System.Diagnostics.Trace.WriteLine($"{_logHeader} {s} exception {ex.Message} inner {(ex.InnerException?.Message)??"<null>"}");

        static void Main(string[] args)
        {
#if DEBUG
            System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
#endif

            string pocPath = @"..\..\..\deployment\Snippets.PoCLibrary.dll";
            string pocType = "Core";
            string pocMethod = "WhoAmI";

            try
            {
                var module = LoadModule_CurrentDomain(pocPath, pocType);

                var t = module.GetTypes().Where(x => x.Name == pocType).FirstOrDefault();
                var method = t.GetMethod(pocMethod);
                var result = method.Invoke(null, null) as string;

                logInfoVar("result", result);
            }
            catch(Exception ex)
            {
                logEx("Main", ex);
            }
        }

        public static Module LoadModule_CurrentDomain(string targetDll, string typeName)
        {
            var domain = AppDomain.CurrentDomain;
            bool validCall = true;

            if (System.String.IsNullOrEmpty(targetDll))
            {
                logInfoTxt("argument exception targetDll");
                validCall = false;
            }
            if (System.String.IsNullOrEmpty(typeName))
            {
                logInfoTxt("argument exception typeName");
                validCall = false;
            }
            if(!System.IO.File.Exists(targetDll))
            {
                logInfoTxt("argument exception targetDll file not found");
                validCall = false;
            }

            if (!validCall)
                return null;

            try
            { 
                byte[] bytes = System.IO.File.ReadAllBytes(targetDll);

                var dynAssembly = new AssemblyName("Surrogate");
                var dynBuilder = domain.DefineDynamicAssembly(dynAssembly, System.Reflection.Emit.AssemblyBuilderAccess.Run);

                // we need 2 modules
                var dynModuleBuilder1 = dynBuilder.DefineDynamicModule("EmptySurrogate");
                var dynModuleBuilder2 = dynBuilder.DefineDynamicModule("TargetSurrogate");

                var dynTypeBuilder = dynModuleBuilder2.DefineType(typeName, TypeAttributes.Public);
                var targetType = dynTypeBuilder.CreateType();

                var target = targetType.Assembly.LoadModule("TargetSurrogate", bytes);

                return target;
            }
            catch (Exception ex)
            {
                logEx("LoadModule_CurrentDomain", ex);
            }

            return null;
        }
    }
}
