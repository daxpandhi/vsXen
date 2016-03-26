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

using System;
using System.IO;
using System.Xml.Serialization;
using Microsoft.VisualStudio.Shell.Interop;

namespace vsXen.Options
{
    internal static class HelperFunctions
    {
        internal static OptionsCore LoadTagsFromFile(string file)
        {
            try
            {
                return (OptionsCore)new XmlSerializer(typeof(OptionsCore))
                    .Deserialize(new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite));
            }
            catch (Exception)
            {
                return null;
            }

        }

        internal static void SaveTagsToFile(string file, object _object)
        {
            new XmlSerializer(_object.GetType())
                .Serialize(new FileStream(file, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite), _object);
        }

        internal static void WriteOnOutputWindow(IVsOutputWindow provider, string text)
        {
            if (null == provider)
            {
                return;
            }

            IVsOutputWindow outputWindow = provider;

            Guid guidGeneral = Microsoft.VisualStudio.VSConstants.GUID_OutWindowGeneralPane;
            IVsOutputWindowPane windowPane;
            if (Microsoft.VisualStudio.ErrorHandler.Failed(outputWindow.GetPane(ref guidGeneral, out windowPane)) ||
                (null == windowPane))
            {
                Microsoft.VisualStudio.ErrorHandler.Failed(outputWindow.CreatePane(ref guidGeneral, "General", 1, 0));
                if (Microsoft.VisualStudio.ErrorHandler.Failed(outputWindow.GetPane(ref guidGeneral, out windowPane)) ||
                (null == windowPane))
                {
                    return;
                }
                Microsoft.VisualStudio.ErrorHandler.Failed(windowPane.Activate());
            }

            if (Microsoft.VisualStudio.ErrorHandler.Failed(windowPane.OutputString(text)))
            {
            }
        }
    }
}