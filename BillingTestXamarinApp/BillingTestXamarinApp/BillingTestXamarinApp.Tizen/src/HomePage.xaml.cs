using System;
using System.Threading;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using Tizen.Applications;

namespace BillingTestXamarinApp.Tizen
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class HomePage : ContentPage
    {
        SynchronizationContext m_thisContext;

        public HomePage()
        {
            InitializeComponent();
            InitSynchronizationContext();
        }

        protected override void OnAppearing()
        {
            m_thisContext.Post(state => { SingleEventBtn.Focus(); }, null);
        }
        private async void SingleEvent_Button_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new SingleAPITest());
        }

        private async void PaymentScenario_Button_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new SelectPaymentScenarioPage());
        }

        private void Button_Focused(object sender, FocusEventArgs e)
        {
            Button focusedBtn = (Button)sender;
            if(SingleEventBtn == focusedBtn)
            {
                m_thisContext.Post(state =>
                {
                    DetailInfo.Text = "You can test the following feature.\nShowRegisterCreditCard, ShowPurchaseHistory, ShowRegisterBenefit deeplink feature.\nIsServiceAvailable, GetVersion API.";
                }, null);
            }
            else
            {
                m_thisContext.Post(state =>
                {
                    DetailInfo.Text = "You can test the following Scenario.\nBuyItem Scenario.\nVerify and Apply Purchase Scenario.\nCancel Subscription Scenario.";
                }, null);
            }
        }

        private void InitSynchronizationContext()
        {
            if (TizenSynchronizationContext.Current == null)
            {
                TizenSynchronizationContext.Initialize();
            }
            m_thisContext = TizenSynchronizationContext.Current;
        }
    }
}