using System;
using System.Threading;
using Tizen.Applications;
using Tizen.TV.Service.Billing;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using static Tizen.Log;
using TizenTV = Tizen.TV;

namespace BillingTestXamarinApp.Tizen
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SingleAPITest : ContentPage
    {
        public BillingPlugin m_pIBilling;
        public SynchronizationContext m_thisContext;

        private BillingRequestServerType m_requestServerType;

        public SingleAPITest()
        {
            InitializeComponent();

            InitBillingPlugin();
            InitSynchronizationContext();
            SetBillingServerType();
        }

        private void DrawResult(string text)
        {
            Info("BILLING_CS", text);
            TestResult.Text = text;
        }

        private void ShowRegisterCreditCard_Button_Clicked(object sender, EventArgs e)
        {
            bool bRet = m_pIBilling.ShowRegisterCreditCard();

            if (bRet)
            {
                TestResult.Text = "OpenDeeplink success";
            }
            else
            {
                TestResult.Text = "OpenDeeplink failed";
            }
        }

        private void ShowPurchaseHistory_Button_Clicked(object sender, EventArgs e)
        {
            string appID = "ALL";
            bool bRet = m_pIBilling.ShowPurchaseHistory(appID, BillingRequestPurchaseHistoryType.All);

            if (bRet)
            {
                TestResult.Text = "OpenDeeplink success";
            }
            else
            {
                TestResult.Text = "OpenDeeplink failed";
            }
        }

        private void ShowRegisterBenefit_Button_Clicked(object sender, EventArgs e)
        {
            bool bRet = m_pIBilling.ShowRegisterPromotionalCode();

            if (bRet)
            {
                TestResult.Text = "OpenDeeplink success";
            }
            else
            {
                TestResult.Text = "OpenDeeplink failed";
            }
        }

        private void IsServiceAvailable_Button_Clicked(object sender, EventArgs e)
        {
            bool bRet = m_pIBilling.IsServiceAvailable(m_requestServerType);

            if (bRet)
            {
                TestResult.Text = "requestAPI success";
            }
            else
            {
                TestResult.Text = "requestAPI failed";
            }
        }

        private void GetVersion_Button_Clicked(object sender, EventArgs e)
        {
            string strResult = m_pIBilling.GetVersion();

            DisplayAlert("GetVersion Result", "Version : " + strResult, "OK");

            TestResult.Text = "Version : " + strResult;
        }

        private void RequestAPICallbackEvent(object sender, BillingRequestAPICallbackEventArgs e)
        {
            Info("BILLING_CS", "");
            Info("BILLING_CS", "result : " + e.Result);

            m_thisContext.Post(state => DrawResult("e.Result : " + e.Result), null);

        }
        private void OpenDeepLinkCallbackEvent(object sender, BillingShowDeepLinkCallbackEventArgs e)
        {
            Info("BILLING_CS", "");

            Info("BILLING_CS", "result : " + e.Result);
            Info("BILLING_CS", "resultDetail : " + e.ResultDetail);

            string text = "e.Result : " + e.Result + "\n" + "e.ResultDetail : " + e.ResultDetail;
            m_thisContext.Post(state => DrawResult(text), null);
        }

        private void InitBillingPlugin()
        {
            m_pIBilling = new BillingPlugin();

            m_pIBilling.RequestAPIEventHandler += new BillingRequestAPICallbackEventHandler(RequestAPICallbackEvent);
            m_pIBilling.ShowDeepLinkEventHandler += new BillingShowDeepLinkCallbackEventHandler(OpenDeepLinkCallbackEvent);
        }

        private void InitSynchronizationContext()
        {
            if (TizenSynchronizationContext.Current == null)
            {
                TizenSynchronizationContext.Initialize();
            }
            m_thisContext = TizenSynchronizationContext.Current;
        }

        private void SetBillingServerType()
        {
            try
            {
                TizenTV.SmartHubConfig.ServerType serverType = TizenTV.Environment.SmartHubConfig.Server;

                Info("BILLING", "serverType is :" + serverType.ToString());

                if (serverType == TizenTV.SmartHubConfig.ServerType.Operating)
                {
                    m_requestServerType = BillingRequestServerType.Dev;
                }
                else if (serverType == TizenTV.SmartHubConfig.ServerType.Developement)
                {
                    m_requestServerType = BillingRequestServerType.Dev;
                }
                else
                {
                    m_requestServerType = BillingRequestServerType.Dev;
                }
            }
            catch (UnauthorizedAccessException e)
            {
                Error("BILLING", e.ToString());
            }
        }
    }
}