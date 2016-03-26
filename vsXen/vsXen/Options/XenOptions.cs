using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace vsXen.Options
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [CLSCompliant(false), ComVisible(true)]
    [SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
    public class XenOptions : UIElementDialogPage
    {
        private XenOptionsDialog dialog;
        protected override System.Windows.UIElement Child => dialog;
        public static readonly string localPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\Nukeation Studios\\VSX\\";
        public OptionsCore Core
        { get; set; } = new OptionsCore();

        public XenOptions()
        {
            dialog = new XenOptionsDialog
                     {
                         _OptionsCore = Core
                     };
            Directory.CreateDirectory(localPath);
        }

        public override void SaveSettingsToStorage()
        {
            Core = dialog._OptionsCore;
            HelperFunctions.SaveTagsToFile(localPath + "xen.xml", Core);
            base.SaveSettingsToStorage();
        }

        public override void LoadSettingsFromStorage()
        {
            base.LoadSettingsFromStorage();
            try
            {
                Core = HelperFunctions.LoadTagsFromFile(localPath + "xen.xml") ?? new OptionsCore();
            }
            catch (Exception)
            {
                //Core = new OptionsCore();
            }
            finally
            {
                dialog._OptionsCore = Core;
            }
        }
    }
}