using System;
using System.Threading;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using Tizen.Applications;

namespace BillingTestXamarinApp.Tizen
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class SelectPaymentScenarioPage : ContentPage
	{
        private SynchronizationContext m_thisContext;

		public SelectPaymentScenarioPage ()
		{
			InitializeComponent ();

            if(TizenSynchronizationContext.Current == null)
            {
                TizenSynchronizationContext.Initialize();
            }
            m_thisContext = TizenSynchronizationContext.Current;
            BuyItemScenBtn.Focused += BuyItemScenBtn_Focused;
            VerifyAndApplyScenBtn.Focused += VerifyAndApplyScenBtn_Focused;
            CancelSubBtn.Focused += CancelSubBtn_Focused;
		}

        private void CancelSubBtn_Focused(object sender, FocusEventArgs e)
        {
            m_thisContext.Post((state) => { DetailInfomationArea.Text = "It is Cancel Subscription Scenario"; }, null);
        }

        private void VerifyAndApplyScenBtn_Focused(object sender, FocusEventArgs e)
        {
            m_thisContext.Post((state) => { DetailInfomationArea.Text = "It is Verify and Apply Product Scenario"; }, null);
        }

        private void BuyItemScenBtn_Focused(object sender, FocusEventArgs e)
        {
            m_thisContext.Post((state) => { DetailInfomationArea.Text = "It is BuyItem Scenario\nYou can get Product List and use it for \"BuyItem\" API!"; }, null);
            
        }

        private async void BuyItemClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new BuyItemScenPage());
        }

        private async void VerifyAndApplyClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new VerifyAndApplyPurchaseScenPage());
        }

        private async void CancelSubClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new CancelSubscriptionScenPage());
        }
    }
}