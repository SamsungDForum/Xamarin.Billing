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
    public partial class VerifyAndApplyPurchaseScenPage : ContentPage
    {
        private SynchronizationContext m_thisContext;
        private BillingPlugin m_pIBilling;

        private const string m_strAppId = "3201504002021";
        private string m_strCountryCode = TizenTV.Environment.SmartHubConfig.Country;
        private const string m_strUserUID = "710000072682";
        private const string m_strSecurityKey = "YxE757K+aDWHJXa0QMnL5AJmItefoEizvv8L7WPJAMs=";
        private BillingRequestServerType m_requestServerType;

        private Stack<string> m_unAppliedInvoiceStack;
        private Stack<string> m_verifiedInvoiceStack;

        public VerifyAndApplyPurchaseScenPage()
        {
            InitializeComponent();

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

        private void Request_Verify_Purchase_Clicked(object sender, EventArgs e)
        {
            Info("BILLING_CS", "");

            if (m_unAppliedInvoiceStack == null)
            {
                PrintText("There is no UnAppliedInvoice obj");
            }
            else if(m_unAppliedInvoiceStack.Count != 0)
            {
                m_pIBilling.RequestAPIEventHandler += new BillingRequestAPICallbackEventHandler(RequestVerifyInvoiceCallbackEvent);

                string popInvoice = m_unAppliedInvoiceStack.Pop();

                NumberOfUnAppliedPurchase.Text = m_unAppliedInvoiceStack.Count.ToString();

                bool bRet = m_pIBilling.VerifyInvoice(m_strAppId, m_strUserUID, popInvoice, m_strCountryCode, m_requestServerType);

                if (bRet)
                {
                    ShowLoadingScreen();
                }
                else
                {
                    
                }
            }
        }

        private void Request_Apply_Product_Clicked(object sender, EventArgs e)
        {
            Info("BILLING_CS", "");

            if (m_verifiedInvoiceStack == null)
            {
                m_pIBilling.RequestAPIEventHandler -= RequestApplyInvoiceCallbackEvent;
                PrintText("There is no Verified invoice obj");
            }
            else if(m_verifiedInvoiceStack.Count != 0)
            {
                m_pIBilling.RequestAPIEventHandler += new BillingRequestAPICallbackEventHandler(RequestApplyInvoiceCallbackEvent);

                string popInvoice = m_verifiedInvoiceStack.Pop();

                NumberOfVerifiedPurchase.Text = m_verifiedInvoiceStack.Count.ToString();

                bool bRet = m_pIBilling.ApplyInvoice(m_strAppId, m_strUserUID, popInvoice, m_strCountryCode, m_requestServerType);

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

            var UnAppliedInvoices = from p in PurchaseListObj["InvoiceDetails"]
                                    where ((bool)p["AppliedStatus"] == false) && ((bool)p["CancelStatus"] == false)
                                    select (string)p["InvoiceID"];

            if(UnAppliedInvoices.Count() == 0)
            {
                m_thisContext.Post(state => { PrintText("There is no unapplied product!\nYou should buy item by using \"BuyItem\" API for test this sceanrio!"); }, null);
            }
            else
            {
                m_unAppliedInvoiceStack = new Stack<string>(UnAppliedInvoices);

                m_thisContext.Post(state => {
                    VerifyPurchaseBtn.IsEnabled = true;
                    NumberOfUnAppliedPurchase.Text = m_unAppliedInvoiceStack.Count.ToString();
                    PrintText("You got the " + NumberOfUnAppliedPurchase.Text + " unapplied purchase invoice ID!\nLet's try to use \"VerifyInvoice\" API");
                }, null);
            }

        }

        private void RequestVerifyInvoiceCallbackEvent(object sender, BillingRequestAPICallbackEventArgs e)
        {
            Info("BILLING_CS", "");
            m_pIBilling.RequestAPIEventHandler -= RequestVerifyInvoiceCallbackEvent;

            JObject APIResponse = JObject.Parse(e.Result);
            string strVerifiedInvoiceID = APIResponse.Property("InvoiceID").Value.ToString();

            if(m_verifiedInvoiceStack == null)
            {
                m_verifiedInvoiceStack = new Stack<string>();
            }

            if (m_verifiedInvoiceStack.Contains(strVerifiedInvoiceID))
            {
                //it is already exist in stack
            }
            else
            {
                m_verifiedInvoiceStack.Push(strVerifiedInvoiceID);
            }


            if (m_unAppliedInvoiceStack.Count == 0)
            {
                m_thisContext.Post(state => {
                    HideLoadingScreen();
                    ApplyPurchaseBtn.IsEnabled = true;
                    PrintText("Verified product list is ready. let's apply this list by using \"ApplyInvoice\" API.");
                    NumberOfVerifiedPurchase.Text = m_verifiedInvoiceStack.Count.ToString();
                }, null);

            }

            Request_Verify_Purchase_Clicked(null, null);
        }

        private void RequestApplyInvoiceCallbackEvent(object sender, BillingRequestAPICallbackEventArgs e)
        {
            Info("BILLING_CS", "");

            m_pIBilling.RequestAPIEventHandler -= RequestApplyInvoiceCallbackEvent;

            if (m_verifiedInvoiceStack.Count == 0)
            {
                m_thisContext.Post(state =>
                {
                    HideLoadingScreen();
                    PrintText("All of Verified product was applied.");
                    NumberOfUnAppliedPurchase.Text = m_verifiedInvoiceStack.Count.ToString();
                }, null);
            }

            Request_Apply_Product_Clicked(null, null);
        }
        
        private string GetCheckValue(string strMsg, string strKey)
        {
            var encoding = new System.Text.ASCIIEncoding();
            byte[] byteKey = encoding.GetBytes(strKey);
            byte[] dataToHmac = encoding.GetBytes(strMsg);

            HMACSHA256 hmac = new HMACSHA256(byteKey);
            return Convert.ToBase64String(hmac.ComputeHash(dataToHmac));
        }

        private void InitBillingPlugin()
        {
            m_pIBilling = new BillingPlugin();

            m_pIBilling.RequestAPIEventHandler += new BillingRequestAPICallbackEventHandler(RequestPurchaseListCallbackEvent);
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
            VerifyPurchaseBtn.IsEnabled = false;
            ApplyPurchaseBtn.IsEnabled = false;

            PrintText("You need to get unapplied product invoice id for testing this sceanrio.\nLet's get unapplied product invoice data by using \"GetPurchaseList\" API.");
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
    }
}