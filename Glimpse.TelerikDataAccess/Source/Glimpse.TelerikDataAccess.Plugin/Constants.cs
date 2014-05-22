using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Glimpse.Core.Extensibility;

[assembly: AssemblyTitle("Glimpse TelerikDataAccess Assembly")]
[assembly: AssemblyDescription("Telerik DataAccess plugin for Glimpse.")]
[assembly: AssemblyProduct("Glimpse.TelerikDataAccess")]
[assembly: AssemblyCopyright("Copyright © tkcode123 2014")]

[assembly: ComVisible(false)]
[assembly: Guid("7C44D0AA-C1AD-403C-CAFE-465A9A84BACB")]
[assembly: AssemblyVersion("1.0.2.0")]
[assembly: AssemblyFileVersion("1.0.2.0")]
[assembly: AssemblyInformationalVersion("1.0.2")] // Used to specify the NuGet version number at build time

[assembly: CLSCompliant(true)]
[assembly: InternalsVisibleTo("Glimpse.Test.TelerikDataAccess")]
[assembly: NuGetPackage("Glimpse.TelerikDataAccess")]

namespace Glimpse.TelerikDataAccess.Plugin
{
    internal static class Constants
    {
        internal const string DocumentationUrl4Tab = "/Glimpse.axd?n=dataaccess_html&sub=help";
    }
}
