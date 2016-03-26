/*
The MIT License (MIT)

Copyright (c) 2015 Nukeation Studios.

Permission is hereby granted, free of charge, to any person obtaining a copy 
of this software and associated documentation files (the "Software"), to deal 
in the Software without restriction, including without limitation the rights 
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
copies of the Software, and to permit persons to whom the Software is 
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in 
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS 
IN THE SOFTWARE.

https://github.com/daxpandhi/vsXen

*/

using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using vsXen.Options;

namespace vsXen
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideOptionPage(typeof(XenOptions), "Nukeation Tools", "XEN", 0, 0, true)]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "MemberCanBeInternal")]
    public sealed class vsXenPackage : Package
    {
        private XenOptions _options;
        internal static XenOptions Options;
        public const string PackageGuidString = "01fb304d-bf1c-45ee-8c37-e5fcd35177b4";

        #region Package Members

        private static DTE2 _dte;
        internal static DTE2 DTE;

        protected override void Initialize()
        {
            base.Initialize();
            try
            {
                DTE = _dte ?? (_dte = ServiceProvider.GlobalProvider.GetService(typeof(DTE)) as DTE2);
            }
            catch (Exception)
            {
            }
            try
            {
                _options = (XenOptions)GetDialogPage(typeof(XenOptions));
                Options = _options;
            }
            catch (Exception)
            {
            }
        }

        public static void ExecuteCommand(string commandName, string commandArgs = "")
        {
            var command = DTE.Commands.Item(commandName);

            if (!command.IsAvailable)
                return;

            try
            {
                DTE.ExecuteCommand(commandName, commandArgs);
            }
            catch { }
        }

        #endregion Package Members
    }
}