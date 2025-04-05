using System.Windows;

namespace TF2CrowdControl
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            this.ccvm.Closed();
        }

        private void log_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // don't auto-scroll if the user was accessing the log when the new text came in.
            double closeEnoughToEnd = 15;
            if ((logScroller.VerticalOffset + closeEnoughToEnd) < logScroller.ScrollableHeight)
                return;

            // auto-scroll the log
            logScroller.ScrollToEnd();
        }
    }

}