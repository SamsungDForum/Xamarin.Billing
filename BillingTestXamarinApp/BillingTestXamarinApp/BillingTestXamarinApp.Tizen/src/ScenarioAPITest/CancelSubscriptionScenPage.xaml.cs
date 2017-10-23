using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Security.Cryptography;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using Tizen.Applications;
using TizenTV = Tizen.TV;
using Newtonsoft.Json.Linq;

using static Tizen.Log;

using Tizen.TV.Service.Billing;

namespace BillingTestXamarinApp.Tizen
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class CancelSubscriptionScenPage : ContentPage
	{
        private SynchronizationContext m_thisContext;
        private BillingPlugin m_pIBilling;

        private const string m_strAppId = "3201504002021";
        private  string m_strCountryCode = TizenTV.Environment.SmartHubConfig.Country;
        private const string m_strUserUID = "710000072682";
        private const string m_strSecurityKey = "YxE757K+aDWHJXa0QMnL5AJmItefoEizvv8L7WPJAMs=";
        private BillingRequestServerType m_requestServerType;

        private Stack<string> m_CancellableSubscriptionIdStack;

        public CancelSubscriptionScenPage ()
		{
			InitializeComponent ();

            InitBillingPlugin();
            InitSynchronizationContext();
            SetBillingServerType();
            InitUIComponent();            
        }

        protected override void OnAppearing()
        {
            m_thisContext.Post(state => {
                GetPurchaseListBtn.Focus();
            }, null);
        }

        private void ShowLoadingScreen()
        {
            if (LoadingScreen.IsRunning != true)
            {
                LoadingScreen.IsRunning = true; LoadingScreen.IsVisible = true;
            }
        }

        private void HideLoadingScreen()
        {
            LoadingScreen.IsVisible = false; LoadingScreen.IsRunning = false;
        }

        private void PrintText(string strText)
        {
            OutputLabel.Text = strText;
        }

        private void Request_Purchase_list_Clicked(object sender, EventArgs e)
        {
            m_pIBilling.RequestAPIEventHandler += new BillingRequestAPICallbackEventHandler(RequestPurchaseListCallbackEvent);

            int ItemType = 2;
            int PageNumber = 1;

            string strCheckValue = GetCheckValue(m_strAppId + m_strUserUID + m_strCountryCode + ItemType + PageNumber, m_strSecurityKey);

            bool bRet = m_pIBilling.GetPurchaseList(m_strAppId, m_strUserUID, m_strCountryCode, PageNumber, strCheckValue, m_requestServerType);
            if (bRet)
            {
                ShowLoadingScreen();
            }
            else
            {
                PrintText("Oops. Get Purchase List fail!");
            }
        }

        private void Request_Cancel_Subscription_Clicked(object sender, EventArgs e)
        {
            Info("BILLING_CS", "");

            if (m_CancellableSubscriptionIdStack == null)
            {
                PrintText("There is no Cancellable invoice Obj");
            }
            else if(m_CancellableSubscriptionIdStack.Count != 0)
            {
                m_pIBilling.RequestAPIEventHandler += new BillingRequestAPICallbackEventHandler(RequestCancelSubscriptionCallbackEvent);

                string popInvoice = m_CancellableSubscriptionIdStack.Pop();

                NumberOfCancellableSubscription.Text = m_CancellableSubscriptionIdStack.Count.ToString();

                bool bRet = m_pIBilling.CancelSubscription(m_strAppId, m_strUserUID, popInvoice, m_strCountryCode, m_requestServerType);
                if (bRet)
                {
                    ShowLoadingScreen();
                }
                else
                {
                    
                }
            }
        }

        private void RequestPurchaseListCallbackEvent(object sender, BillingRequestAPICallbackEventArgs e)
        {
            Info("BILLING_CS", "");

            HideLoadingScreen();

            m_pIBilling.RequestAPIEventHandler -= RequestPurchaseListCallbackEvent;
            //https://www.newtonsoft.com/json/help/html/QueryJsonLinq.htm
            JObject PurchaseListObj = JObject.Parse(e.Result);

            var CancellableSubscriptionId = from p in PurchaseListObj["InvoiceDetails"].Children()["SubscriptionInfo"]
                                            where (string)p["SubsStatus"] == "00"
                                            select (string)p["SubscriptionId"];

            m_CancellableSubscriptionIdStack = new Stack<string>(CancellableSubscriptionId);

            if (m_CancellableSubscriptionIdStack.Count == 0)
            {
                m_thisContext.Post(state => { PrintText("There is no Cancellable Subscription!\nYou should subscribe subscription item by using \"BuyItem\" API for test this sceanrio!"); }, null);
            }
            else
            {
                m_thisContext.Post(state => {
                    CancelSubscriptionBtn.IsEnabled = true;
                    NumberOfCancellableSubscription.Text = m_CancellableSubscriptionIdStack.Count.ToString();
                    PrintText("You got the " + NumberOfCancellableSubscription.Text + " Cancellable subscription ID!\nLet's try to use \"CancelSubscription\" API");
                }, null);
            }

        }

        private void RequestCancelSubscriptionCallbackEvent(object sender, BillingRequestAPICallbackEventArgs e)
        {
            Info("BILLING_CS", "");

            m_pIBilling.RequestAPIEventHandler -= RequestCancelSubscriptionCallbackEvent;

            if(m_CancellableSubscriptionIdStack.Count == 0)
            {
                m_thisContext.Post(state =>
                {
                    HideLoadingScreen();
                    PrintText("All of Cancellable subscription was cancelled.");
                    NumberOfCancellableSubscription.Text = m_CancellableSubscriptionIdStack.Count.ToString();
                }, null);
            }

            Request_Cancel_Subscription_Clicked(null, null);
        }

        private string GetCheckValue(string strMsg, string strKey)
        {
            var encoding = new System.Text.ASCIIEncoding();
            byte[] byteKey = encoding.GetBytes(strKey);
            byte[] dataToHmac = encoding.GetBytes(strMsg);

            HMACSHA256 hmac = new HMACSHA256(byteKey);
            return Convert.ToBase64String(hmac.ComputeHash(dataToHmac));
        }

        private void SetBillingServerType()
        {
            try
            {
                TizenTV.SmartHubConfig.ServerType serverType = TizenTV.Environment.SmartHubConfig.Server;

                if (serverType == TizenTV.SmartHubConfig.ServerType.Operating)
                {
                    m_requestServerType = BillingRequestServerType.Prd;
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

        private void InitBillingPlugin()
        {
            m_pIBilling = new BillingPlugin();
        }

        private void InitSynchronizationContext()
        {
            if (TizenSynchronizationContext.Current == null)
            {
                TizenSynchronizationContext.Initialize();
            }
            m_thisContext = TizenSynchronizationContext.Current;
        }

        private void InitUIComponent()
        {
            CancelSubscriptionBtn.IsEnabled = false;
            PrintText("You need to get cancellable subscription id for testing this sceanrio.\nLet's get cancellable subscription id data by using \"GetPurchaseList\" API.");
        }
    }
}