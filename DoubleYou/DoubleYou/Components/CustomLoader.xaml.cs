using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DoubleYou.Components
{
    public sealed partial class CustomLoader : UserControl
    {
        public CustomLoader()
        {
            this.InitializeComponent();
        }

        public void SetLoaderActive(bool isActive)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                if (isActive)
                {
                    MainGrid.Visibility = Visibility.Visible;
                }
                else
                {
                    MainGrid.Visibility = Visibility.Collapsed;
                }
            });
        }
    }
}
