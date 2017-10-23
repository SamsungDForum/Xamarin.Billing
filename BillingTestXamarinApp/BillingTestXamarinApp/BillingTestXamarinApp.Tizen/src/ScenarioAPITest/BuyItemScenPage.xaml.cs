using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Security.Cryptography;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using Tizen.Applications;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using static Tizen.Log;

using TizenTV = Tizen.TV;
using Tizen.TV.Service.Billing;

namespace BillingTestXamarinApp.Tizen
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class BuyItemScenPage : ContentPage
	{
        private SynchronizationContext m_thisContext;
        private BillingPlugin m_pIBilling;
        private IList<ProductInfo> ProductInfos;

        private const string m_strAppId = "3201504002021";
        private string m_strCountryCode = TizenTV.Environment.SmartHubConfig.Country;
        private const string m_strUserUID = "710000072682";
        private const string m_strSecurityKey = "YxE757K+aDWHJXa0QMnL5AJmItefoEizvv8L7WPJAMs=";
        private BillingRequestServerType m_requestServerType;

        public BuyItemScenPage ()
		{
			InitializeComponent ();

            InitBillingPlugin();
            InitSynchronizationContext();
            SetBillingServerType();
            InitUIComponent();
        }


        protected override void OnAppearing()
        {
            m_thisContext.Post(state => { GetProductListBtn.Focus(); }, null);
        }

        private async void ShowPopup()
        {
            var action = await DisplayActionSheet("Get Product success.", "Cancel", null, "Consumable", "subscription");
        }

        private void ShowLoadingScreen()
        {
            LoadingScreen.IsRunning = true; LoadingScreen.IsVisible = true;
        }

        private void HideLoadingScreen()
        {
            LoadingScreen.IsVisible = false; LoadingScreen.IsRunning = false;
        }

        private void PrintText(string strText)
        {
            OutputLabel.Text = strText;
        }

        private void GetProductListBtnClicked(object sender, EventArgs e)
        {
            m_pIBilling.RequestAPIEventHandler += new BillingRequestAPICallbackEventHandler(RequestProductListCallbackEvent);

            string strCheckValue = GetCheckValue(m_strAppId + m_strCountryCode, m_strSecurityKey);

            bool bRet = m_pIBilling.GetProductsList(m_strAppId, m_strCountryCode, 100, 1, strCheckValue, m_requestServerType);
            
            if (bRet)
            {
                ShowLoadingScreen();
            }
            else
            {
                PrintText("Oops. GetProductsList fail!");
            }
        }

        private void BuyItemBtnClicked(object sender, EventArgs e)
        { 
            bool bRet = m_pIBilling.BuyItem(m_strAppId, m_requestServerType, MakePayDetailString());
            if (!bRet)
            {
                PrintText("Oops. BuyItem Fail!");
            }
        }

        private void RequestProductListCallbackEvent(object sender, BillingRequestAPICallbackEventArgs e)
        {
            Info("BILLING_CS", "");

            m_pIBilling.RequestAPIEventHandler -= RequestProductListCallbackEvent;

            JObject ProductListObj = JObject.Parse(e.Result);
            JArray jArray = JArray.Parse(ProductListObj.GetValue("ItemDetails").ToString());
            ProductInfos = jArray.Select(p => new ProductInfo
            {
                ItemType = (string)p["ItemType"],
                Price = (string)p["Price"],
                ItemTitle = (string)p["ItemTitle"],
                ItemID = (string)p["ItemID"],
                CurrencyID = (string)p["CurrencyID"]
            }).ToList();
            
            if(ProductInfos.Count != 0)
            {
                ProductList.Items.RemoveAt(0);

                ProductList.Items.Add("Select Product List");
                ProductList.SelectedIndex = 0;

                for (int i = 0; i < ProductInfos.Count; i++)
                {
                    ProductList.IsEnabled = true;
                    string strItemType = "";
                    switch(Int32.Parse(ProductInfos[i].ItemType))
                    {
                        case 1:
                            strItemType = "Consumable Item";
                            break;
                        case 2:
                            strItemType = "Non-Consumable Item";
                            break;
                        case 3:
                            strItemType = "Limited-period Item";
                            break;
                        case 4:
                            strItemType = "Subscription Item";
                            break;
                        default:
                            strItemType = "Item Type Error!";
                            break;
                    }
                    ProductList.Items.Add(ProductInfos[i].ItemTitle + "(" + strItemType + ")");
                }

                ProductList.SelectedIndexChanged += (insender, args) =>
                {
                    if (ProductList.SelectedIndex == 0)
                    {
                        m_thisContext.Post(state =>
                        {
                            BuyItemBtn.IsEnabled = false;
                        }, null);
                    }
                    else
                    {
                        m_thisContext.Post(state =>
                        {
                            BuyItemBtn.IsEnabled = true;
                            PrintText("let's try to \"BuyItem\" API.\nPlease click \"BuyItem\" button.");
                        }, null);
                    };
                };
                
                m_thisContext.Post(state => { HideLoadingScreen(); PrintText("You got product list. \nPlease select Product Info"); }, null);
            }
            else
            {
                m_thisContext.Post(state => { HideLoadingScreen(); PrintText("Oops! There is no Product Infos"); }, null);
            }
        }

        private void BuyItemCallbackEvent(object sender, BillingClientClosedEventArgs e)
        {
            Error("BILLING_CS", "");

            m_thisContext.Post(state => { PrintText("Callback Event Args data.\ne.Result : " + e.Result + "\ne.OptionalData : " + e.OptionalData); }, null);

            Error("BILLING_CS", "result : " + e.Result);
            Error("BILLING_CS", "optionalData : " + e.OptionalData);
        }

        private string MakePayDetailString()
        {
            ProductInfo ProductItemInfo = null;
            ProductItemInfo = ProductInfos[ProductList.SelectedIndex - 1];

            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.Formatting = Formatting.Indented;

                writer.WriteStartObject();
                writer.WritePropertyName("OrderTotal"); writer.WriteValue(ProductItemInfo.Price);
                writer.WritePropertyName("OrderItemID"); writer.WriteValue(ProductItemInfo.ItemID);
                writer.WritePropertyName("OrderTitle"); writer.WriteValue(ProductItemInfo.ItemTitle);
                writer.WritePropertyName("OrderCurrencyID"); writer.WriteValue(ProductItemInfo.CurrencyID);
                writer.WriteEndObject();
            }

            Info("BILLING_CS", sb.ToString());

            return sb.ToString();
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

                Info("BILLING", "serverType is :" + serverType.ToString());

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
            m_pIBilling.BuyItemEventHandler += new BillingClientClosedEventHandler(BuyItemCallbackEvent);
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
            BuyItemBtn.IsEnabled = false;
            PrintText("You need to product Information for useing \"BuyItem\" API.\nYou can get product information by using \"GetProductsList\" API.\nPlease click the \"Get Product List\" Button");

            ProductList.Items.Add("There is no Product Item");
            ProductList.SelectedIndex = 0;
            ProductList.IsEnabled = false;
        }
    }
}