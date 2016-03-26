using System;
using System.Windows;

namespace vsXen.Options
{
    public partial class XenOptionsDialog
    {
        public OptionsCore _OptionsCore { get; set; }

        public XenOptionsDialog()
        {
            InitializeComponent();
            Loaded += SAOptionsDialog_Loaded;
        }

        private void SAOptionsDialog_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_OptionsCore == null)
                {
                    _OptionsCore = new OptionsCore();
                }

                pg.SelectedObject = _OptionsCore;
            }
            catch (Exception)
            {
                _OptionsCore = new OptionsCore();
                pg.SelectedObject = _OptionsCore;
            }
        }
    }
}