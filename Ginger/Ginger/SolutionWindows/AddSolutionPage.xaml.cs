#region License
/*
Copyright © 2014-2023 European Support Limited

Licensed under the Apache License, Version 2.0 (the "License")
you may not use this file except in compliance with the License.
You may obtain a copy of the License at 

http://www.apache.org/licenses/LICENSE-2.0 

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS, 
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
See the License for the specific language governing permissions and 
limitations under the License. 
*/
#endregion

using amdocs.ginger.GingerCoreNET;
using Amdocs.Ginger.Common;
using Amdocs.Ginger.Repository;
using Ginger.Reports;
using Ginger.SolutionGeneral;
using GingerCore;
using GingerCore.DataSource;
using GingerCoreNET.SolutionRepositoryLib.RepositoryObjectsLib.PlatformsLib;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Ginger.SolutionWindows
{
    /// <summary>
    /// Interaction logic for AddSolutionPage.xaml
    /// </summary>
    public partial class AddSolutionPage : Page
    {
        Solution mSolution;
        GenericWindow _pageGenericWin = null;

        public AddSolutionPage(Solution s)
        {
            InitializeComponent();
            mSolution = s;
            GingerCore.GeneralLib.BindingHandler.ObjFieldBinding(SolutionNameTextBox, TextBox.TextProperty, s, nameof(Solution.Name));
            GingerCore.GeneralLib.BindingHandler.ObjFieldBinding(SolutionFolderTextBox, TextBox.TextProperty, s, nameof(Solution.Folder));
            UCEncryptionKey.mSolution = mSolution;
            UCEncryptionKey.EncryptionKeyPasswordBox.PasswordChanged += EncryptionKeyBox_Changed;
            GingerCore.General.FillComboFromEnumObj(MainPlatformComboBox, s.MainPlatform);
        }

        public bool IsUploadSolutionToSourceControl { get; set; }

        private void EncryptionKeyBox_Changed(object sender, RoutedEventArgs e)
        {
            UCEncryptionKey.CheckKeyCombination();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //TODO: replace with robot message
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                //check name and folder inputs exists
                if (SolutionNameTextBox.Text.Trim() == string.Empty || SolutionFolderTextBox.Text.Trim() == string.Empty
                        || ApplicationTextBox.Text.Trim() == string.Empty
                            || MainPlatformComboBox.SelectedItem == null || MainPlatformComboBox.SelectedItem.ToString() == "Null"
                            || UCEncryptionKey.EncryptionKeyPasswordBox.Password.Trim() == string.Empty)
                {
                    Mouse.OverrideCursor = null;
                    Reporter.ToUser(eUserMsgKey.MissingAddSolutionInputs);
                    return;
                }

                Regex regex = new Regex(@"^.*(?=.{8,16})(?=.*\d)(?=.*[a-z])(?=.*[A-Z])(?=.*[!*@#$%^&+=]).*$");
                if (!UCEncryptionKey.CheckKeyCombination())
                {
                    Mouse.OverrideCursor = null;
                    UCEncryptionKey.EncryptionKeyPasswordBox.Password = "";
                    return;
                }

                mSolution.ApplicationPlatforms = new ObservableList<ApplicationPlatform>();
                ApplicationPlatform MainApplicationPlatform = new ApplicationPlatform();
                MainApplicationPlatform.AppName = ApplicationTextBox.Text;
                MainApplicationPlatform.Platform = (ePlatformType)MainPlatformComboBox.SelectedValue;
                mSolution.ApplicationPlatforms.Add(MainApplicationPlatform);
                mSolution.EncryptionKey = UCEncryptionKey.EncryptionKeyPasswordBox.Password;
                //TODO: check AppName and platform validity - not empty + app exist in list of apps


                //make sure main folder exist
                if (!System.IO.Directory.Exists(mSolution.Folder))
                {
                    System.IO.Directory.CreateDirectory(mSolution.Folder);
                }

                //create new folder with solution name
                mSolution.Folder = Path.Combine(mSolution.Folder, mSolution.Name);
                if (!System.IO.Directory.Exists(mSolution.Folder))
                {
                    System.IO.Directory.CreateDirectory(mSolution.Folder);
                }

                //check solution not already exist
                if (System.IO.File.Exists(System.IO.Path.Combine(mSolution.Folder, @"Ginger.Solution.xml")) == false)
                {
                    mSolution.FilePath = System.IO.Path.Combine(mSolution.Folder, @"Ginger.Solution.xml");
                    mSolution.SolutionOperations.SaveEncryptionKey();
                    mSolution.SolutionOperations.SaveSolution(false);
                }
                else
                {
                    //solution already exist
                    Mouse.OverrideCursor = null;
                    Reporter.ToUser(eUserMsgKey.SolutionAlreadyExist);
                    return;
                }

                WorkSpace.Instance.OpenSolution(mSolution.Folder);

                //Create default items                
                AddFirstAgentForSolutionForApplicationPlatfrom(MainApplicationPlatform);
                App.OnAutomateBusinessFlowEvent(BusinessFlowWindows.AutomateEventArgs.eEventType.UpdateAppAgentsMapping, null);
                AddDefaultDataSource();
                AddDeafultReportTemplate();
                GingerCoreNET.GeneralLib.General.CreateDefaultEnvironment();
                WorkSpace.Instance.SolutionRepository.AddRepositoryItem(WorkSpace.Instance.GetNewBusinessFlow("Flow 1", true));
                mSolution.SolutionOperations.SetReportsConfigurations();

                //Save again to keep all defualt configurations setup
                mSolution.SolutionOperations.SaveSolution(false);
                //show success message to user
                Mouse.OverrideCursor = null;
                if (Reporter.ToUser(eUserMsgKey.UploadSolutionToSourceControl, mSolution.Name) == eUserMsgSelection.Yes)
                {
                    IsUploadSolutionToSourceControl = true;
                }
                _pageGenericWin.Close();
            }
            catch (Exception ex)
            {
                Mouse.OverrideCursor = null;
                Reporter.ToUser(eUserMsgKey.AddSolutionFailed, ex.Message);
            }
        }

        private void AddDeafultReportTemplate()
        {
            HTMLReportConfiguration r = new HTMLReportConfiguration();
            HTMLReportConfigurationOperations reportConfigurationOperations = new HTMLReportConfigurationOperations(r);
            r.HTMLReportConfigurationOperations = reportConfigurationOperations;

            HTMLReportConfiguration reportTemplate = new HTMLReportConfiguration("Default", true, reportConfigurationOperations);
            HTMLReportConfigurationOperations reportTemplateConfigurationOperations = new HTMLReportConfigurationOperations(reportTemplate);
            reportTemplate.HTMLReportConfigurationOperations = reportTemplateConfigurationOperations;

            WorkSpace.Instance.SolutionRepository.AddRepositoryItem(reportTemplate);
        }

        void AddFirstAgentForSolutionForApplicationPlatfrom(ApplicationPlatform MainApplicationPlatform)
        {
            Agent agent = new Agent();
            AgentOperations agentOperations = new AgentOperations(agent);
            agent.AgentOperations = agentOperations;

            agent.Name = MainApplicationPlatform.AppName + " - Agent 1";
            switch (MainApplicationPlatform.Platform)
            {
                case ePlatformType.ASCF:
                    agent.DriverType = Agent.eDriverType.ASCF;
                    break;
                case ePlatformType.DOS:
                    agent.DriverType = Agent.eDriverType.DOSConsole;
                    break;
                case ePlatformType.Mobile:
                    agent.DriverType = Agent.eDriverType.Appium;
                    break;
                case ePlatformType.PowerBuilder:
                    agent.DriverType = Agent.eDriverType.PowerBuilder;
                    break;
                case ePlatformType.Unix:
                    agent.DriverType = Agent.eDriverType.UnixShell;
                    break;
                case ePlatformType.Web:
                    agent.DriverType = Agent.eDriverType.SeleniumChrome;
                    break;
                case ePlatformType.WebServices:
                    agent.DriverType = Agent.eDriverType.WebServices;
                    break;
                case ePlatformType.Windows:
                    agent.DriverType = Agent.eDriverType.WindowsAutomation;
                    break;
                case ePlatformType.Java:
                    agent.DriverType = Agent.eDriverType.JavaDriver;
                    break;
                default:
                    Reporter.ToUser(eUserMsgKey.StaticWarnMessage, "No default driver set for first agent");
                    break;
            }

            agent.AgentOperations.InitDriverConfigs();
            WorkSpace.Instance.SolutionRepository.AddRepositoryItem(agent);
        }

        void AddDefaultDataSource()
        {
            //byte[] obj= Properties.Resources.GingerDataSource;

            //if(!File.Exists(System.IO.Path.Combine(mSolution.Folder, @"DataSources\GingerDataSource.mdb")))
            //{
            //    Directory.CreateDirectory(System.IO.Path.Combine(mSolution.Folder, "DataSources"));
            //    System.IO.FileStream fs = new System.IO.FileStream(System.IO.Path.Combine(mSolution.Folder, @"DataSources\GingerDataSource.mdb"), System.IO.FileMode.Create, System.IO.FileAccess.Write);
            //    fs.Write(obj, 0, obj.Count());
            //    fs.Close();
            //    fs.Dispose();
            //}         

            //DataSourceBase a = new AccessDataSource();
            //a.Name = "GingerDataSource";             
            //a.FilePath = @"~\DataSources\GingerDataSource.mdb";
            //a.DSType = DataSourceBase.eDSType.MSAccess;
            //RepositoryFolder<DataSourceBase> dsTargetFolder = WorkSpace.Instance.SolutionRepository.GetRepositoryItemRootFolder<DataSourceBase>();
            //dsTargetFolder.AddRepositoryItem(a);

            // TODO: Try not to use resources, we can put the file in folder and copy
            // adding LiteDB while adding solution
            byte[] litedbobj = Properties.Resources.LiteDB;

            if (!File.Exists(System.IO.Path.Combine(mSolution.Folder, @"DataSources\LiteDB.db")))
            {
                Directory.CreateDirectory(System.IO.Path.Combine(mSolution.Folder, "DataSources"));
                System.IO.FileStream fs = new System.IO.FileStream(System.IO.Path.Combine(mSolution.Folder, @"DataSources\LiteDB.db"), System.IO.FileMode.Create, System.IO.FileAccess.Write);
                fs.Write(litedbobj, 0, litedbobj.Count());
                fs.Close();
                fs.Dispose();
            }

            DataSourceBase lite = new GingerCoreNET.DataSource.GingerLiteDB();
            lite.Name = "DefaultDataSource";
            lite.FilePath = @"~\DataSources\LiteDB.db";
            lite.DSType = DataSourceBase.eDSType.LiteDataBase;

            RepositoryFolder<DataSourceBase> dsTargetFolder1 = WorkSpace.Instance.SolutionRepository.GetRepositoryItemRootFolder<DataSourceBase>();
            dsTargetFolder1.AddRepositoryItem(lite);

        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new System.Windows.Forms.FolderBrowserDialog();
            dlg.Description = "Select Solution folder";
            dlg.RootFolder = Environment.SpecialFolder.MyComputer;
            if (mSolution.Folder != string.Empty)
            {
                dlg.SelectedPath = mSolution.Folder;
            }

            dlg.ShowNewFolderButton = true;
            System.Windows.Forms.DialogResult result = dlg.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                SolutionFolderTextBox.Text = dlg.SelectedPath;
            }
        }

        public void ShowAsWindow(eWindowShowStyle windowStyle = eWindowShowStyle.Dialog)
        {
            Button createSolBtn = new Button();
            createSolBtn.Content = "Create";
            createSolBtn.Click += new RoutedEventHandler(OKButton_Click);
            GingerCore.General.LoadGenericWindow(ref _pageGenericWin, App.MainWindow, windowStyle, this.Title, this, new ObservableList<Button> { createSolBtn });
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            if (mSolution.ApplicationPlatforms == null)
            {
                mSolution.ApplicationPlatforms = new ObservableList<ApplicationPlatform>();
            }

            mSolution.ApplicationPlatforms.Clear();
            AddApplicationPage AAP = new AddApplicationPage(mSolution);
            AAP.ShowAsWindow();

            if (mSolution.ApplicationPlatforms.Any())
            {
                ApplicationTextBox.Text = mSolution.ApplicationPlatforms[0].AppName;
                MainPlatformComboBox.SelectedValue = mSolution.ApplicationPlatforms[0].Platform;
            }
        }
    }
}
