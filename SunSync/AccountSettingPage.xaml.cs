﻿using System;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;
using Newtonsoft.Json;
using SunSync.Models;
using System.IO;
using System.Threading;
using Qiniu.Util;
using Qiniu.Storage;
namespace SunSync
{
    /// <summary>
    /// Interaction logic for AccountSettingPage.xaml
    /// </summary>
    public partial class AccountSettingPage : Page
    {
        private MainWindow mainWindow;
        private string myAKSKLink = "https://portal.qiniu.com/user/key";
        public AccountSettingPage(MainWindow mainWindow)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
            this.loadAccountInfo();
            this.SettingsErrorTextBlock.Text = "";
        }

        private void BackToHome_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.mainWindow.GotoHomePage();
        }

        /// <summary>
        /// view my ak & sk button click handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ViewMyAKSK_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(this.myAKSKLink);
            }
            catch (Exception ex)
            {
                Log.Error("open ak & sk link failed, " + ex.Message);
            }
        }

        /// <summary>
        /// load ak & sk from local file
        /// </summary>
        private void loadAccountInfo()
        {
            Account acct = Account.TryLoadAccount();
            if (!string.IsNullOrEmpty(acct.AccessKey))
            {
                this.AccessKeyTextBox.Text = acct.AccessKey;
            }
            if (!string.IsNullOrEmpty(acct.SecretKey))
            {
                this.SecretKeyTextBox.Text = acct.SecretKey;
            }
        }

        /// <summary>
        /// save account settings to local file and check the validity of the settings
        /// </summary>
        private void SaveAccountSetting(object accountObj)
        {
            Account account = (Account)accountObj;
            //write settings to local file
            string accData = JsonConvert.SerializeObject(account);
            string myDocPath = Environment.CurrentDirectory;
            string appDir = System.IO.Path.Combine(myDocPath, "qsunsync");
            try
            {
                if (!Directory.Exists(appDir))
                {
                    try
                    {
                        Directory.CreateDirectory(appDir);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(string.Format("create app dir {0} failed due to {1}", appDir, ex.Message));
                        Dispatcher.Invoke(new Action(delegate
                        {
                            this.SettingsErrorTextBlock.Text = "创建本地配置路径失败";
                        }));
                    }
                }
                string accPath = System.IO.Path.Combine(appDir, "account.json");
                using (StreamWriter sw = new StreamWriter(accPath, false, Encoding.UTF8))
                {
                    sw.Write(accData);
                }
            }
            catch (Exception ex)
            {
                Log.Error("save account info to file failed, " + ex.Message);
                Dispatcher.Invoke(new Action(delegate
                {
                    this.SettingsErrorTextBlock.Text = "帐号设置写入文件失败";
                }));
            }

            //check ak & sk validity
            Mac mac = new Mac(account.AccessKey, account.SecretKey);
            //use fixed zone to avoid the uc query
            Config config = new Config();
            //init domains
            Domains domains = Domains.TryLoadDomains();
            if (domains != null && !string.IsNullOrEmpty(domains.RsDomain))
            {
                Qiniu.Storage.Config.DefaultRsHost = domains.RsDomain;
                config.Zone = new Zone
                {
                    RsHost = domains.RsDomain,
                };
            }
            else
            {
                config.Zone = Zone.ZONE_CN_East;
            }
            BucketManager bucketManager = new BucketManager(mac, config);
            StatResult statResult = bucketManager.Stat("NONE_EXIST_BUCKET", "NONE_EXIST_KEY");


            if (statResult.Code == 401)
            {
                Log.Error("ak & sk wrong");
                Dispatcher.Invoke(new Action(delegate
                {
                    this.SettingsErrorTextBlock.Text = "AK 或 SK 设置不正确";
                }));
            }
            else if (statResult.Code == 612 || statResult.Code == 631)
            {
                Log.Info("ak & sk is valid");
                Dispatcher.Invoke(new Action(delegate
                {
                    this.SettingsErrorTextBlock.Text = "AK & SK 设置正确！";
                }));
                Dispatcher.Invoke(new Action(delegate
                {
                    this.mainWindow.GotoHomePage();
                }));
            }
            else
            {
                Log.Error("stat file network error, " + statResult.Text);
                string message = null;
                if (!string.IsNullOrEmpty(statResult.RefText))
                {
                    message = string.Format("验证帐号失败 {0}", statResult.RefText);
                }
                else
                {
                    message = "验证帐号失败，网络故障！";
                }

                Dispatcher.Invoke(new Action(delegate
                {
                    this.SettingsErrorTextBlock.Text = message;
                }));
            }
        }


        /// <summary>
        /// save account settings button click handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveAccountSettings_EventHandler(object sender, System.Windows.RoutedEventArgs e)
        {
            string accessKey = this.AccessKeyTextBox.Text.Trim();
            string secretKey = this.SecretKeyTextBox.Text.Trim();
            Account account = new Account();
            account.AccessKey = accessKey;
            account.SecretKey = secretKey;
            new Thread(new ParameterizedThreadStart(this.SaveAccountSetting)).Start(account);
        }
    }
}
