﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using amdocs.ginger.GingerCoreNET;
using Amdocs.Ginger.Common;
using Amdocs.Ginger.Common.Enums;
using Amdocs.Ginger.Common.GlobalSolutionLib;
using Amdocs.Ginger.Common.Repository;
using Amdocs.Ginger.CoreNET.GlobalSolutionLib;
using Amdocs.Ginger.Repository;
using Ginger.SolutionGeneral;
using Ginger.SolutionWindows.TreeViewItems;
using Ginger.WizardLib;
using GingerCore;
using GingerCore.DataSource;
using GingerCore.Environments;
using GingerCore.Variables;
using GingerCoreNET.SolutionRepositoryLib.RepositoryObjectsLib.PlatformsLib;
using GingerWPF.UserControlsLib.UCTreeView;
using GingerWPF.WizardLib;
namespace Ginger.GlobalSolutionLib.ImportItemWizardLib
{
    class ImportItemWizard : WizardBase
    {
        public GlobalSolution.eImportFromType ImportFromType = GlobalSolution.eImportFromType.LocalFolder;
        public string SolutionFolder { get; set; } 
        public string EncryptionKey { get; set; } 
        public List<object> SelectedItems { get; set; }

        public List<string> ItemTypesList = Enum.GetNames(typeof(GlobalSolution.eImportItemType)).ToList();

        public ObservableList<GlobalSolutionItem> ItemTypeListToImport = null;
        public ObservableList<GlobalSolutionItem> ItemsListToImport = null;
        public ObservableList<GlobalSolutionItem> SelectedItemTypeListToImport = null;
        public List<VariableBase> VariableListToImport = new List<VariableBase>();
        public List<EnvApplication> EnvAppListToImport = new List<EnvApplication>();

        NewRepositorySerializer newRepositorySerializer = new NewRepositorySerializer();
        public ImportItemWizard()
        {
            AddPage(Name: "Introduction", Title: "Introduction", SubTitle: "Global Solution Introduction", Page: new WizardIntroPage("/GlobalSolutionLib/ImportItemWizardLib/ImportItemIntro.md"));
            
            AddPage(Name: "Select Item Source Type", Title: "Select Item Source Type", SubTitle: "Choose ...", Page: new SelectItemImportTypePage());

            AddPage(Name: "Select Item Types", Title: "Select Item Types", SubTitle: "Choose ...", Page: new SelectItemTypesToImportPage());

            AddPage(Name: "Select Solution Items", Title: "Select Solution Items", SubTitle: "Select Solution Items...", Page: new SelectItemFromSolutionPage());

            AddPage(Name: "Solution Items Dependancy Validation", Title: "Solution Items Dependancy Validation", SubTitle: "Solution Items Dependancy Validation...", Page: new ItemDependancyPage());

        }

        public override string Title { get { return "Import Global Cross Solution Wizard"; } }

        public override void Finish()
        {
            if (!string.IsNullOrEmpty(SolutionFolder))
            {
                foreach (GlobalSolutionItem itemToAdd in SelectedItemTypeListToImport.Where(x => x.Selected).ToList())
                {
                    string sourceFile = itemToAdd.ItemFullPath;
                    string targetFile = string.Empty;
                    //Get subdirectory path
                    string path = Path.GetDirectoryName(itemToAdd.ItemFullPath);
                    string folderPath = path.Replace(SolutionFolder, "");

                    if (string.IsNullOrEmpty(itemToAdd.ItemNewName))
                    {
                        targetFile = Path.Combine(WorkSpace.Instance.SolutionRepository.SolutionFolder, folderPath, Path.GetFileName(itemToAdd.ItemExtraInfo));
                    }
                    else
                    {
                        string newFileName = GlobalSolutionUtils.Instance.GetUniqFileName(Path.Combine(WorkSpace.Instance.SolutionRepository.SolutionFolder, folderPath, Path.GetFileName(itemToAdd.ItemExtraInfo)));
                        targetFile = Path.Combine(WorkSpace.Instance.SolutionRepository.SolutionFolder, folderPath, newFileName);
                    }

                    switch (itemToAdd.ItemType)
                    {
                        case GlobalSolution.eImportItemType.Documents:
                            AddItemToSolution(sourceFile, targetFile, false, itemToAdd);
                            break;
                        case GlobalSolution.eImportItemType.Environments:
                            GlobalSolutionUtils.Instance.EnvParamsToReEncrypt(sourceFile, itemToAdd);
                            AddItemToSolution(sourceFile, targetFile, true, itemToAdd);
                            break;
                        case GlobalSolution.eImportItemType.BusinessFlows:
                        case GlobalSolution.eImportItemType.SharedRepositoryActivities:
                            GlobalSolutionUtils.Instance.VariablesToReEncrypt(sourceFile, itemToAdd);
                            AddItemToSolution(sourceFile, targetFile, true, itemToAdd);
                            break;

                        case GlobalSolution.eImportItemType.DataSources:
                        case GlobalSolution.eImportItemType.SharedRepositoryActivitiesGroup:
                        case GlobalSolution.eImportItemType.SharedRepositoryActions:
                        case GlobalSolution.eImportItemType.SharedRepositoryVariables:
                        case GlobalSolution.eImportItemType.APIModels:
                        case GlobalSolution.eImportItemType.POMModels:
                        case GlobalSolution.eImportItemType.Agents:
                        case GlobalSolution.eImportItemType.TargetApplication:
                        case GlobalSolution.eImportItemType.Variables:
                            AddItemToSolution(sourceFile, targetFile, true, itemToAdd);
                            break;
                        
                        default:
                            break;
                    }
                }

                //Add env params and dbs to this solution from the list
                if (EnvAppListToImport.Count > 0)
                {
                    GlobalSolutionUtils.Instance.AddEnvDependanciesToSolution(EnvAppListToImport);
                }
            }
        }

        void AddItemToSolution(string sourceFile, string targetFile, bool saveAsRepo, GlobalSolutionItem itemToImport)
        {
            if (itemToImport.ItemType == GlobalSolution.eImportItemType.Variables)
            {
                //Add Global Variables from the list
                if (VariableListToImport.Count > 0)
                {
                    foreach (VariableBase vb in VariableListToImport)
                    {
                        if (WorkSpace.Instance.Solution.Variables.Where(x => x.Name == vb.Name).FirstOrDefault() == null)
                        {
                            WorkSpace.Instance.Solution.AddVariable(vb);
                        }
                    }
                    WorkSpace.Instance.Solution.SaveSolution();
                }
                return;
            }
            if (itemToImport.ItemType == GlobalSolution.eImportItemType.TargetApplication)
            {
                string[] filePaths = Directory.GetFiles(Path.Combine(SolutionFolder), "Ginger.Solution.xml", SearchOption.AllDirectories);
                Solution solution = (Solution)newRepositorySerializer.DeserializeFromFile(filePaths[0]);
                ApplicationPlatform applicationPlatform  = solution.ApplicationPlatforms.Where(x=>x.AppName == itemToImport.ItemName).FirstOrDefault();

                ApplicationPlatform appPlatform = WorkSpace.Instance.Solution.ApplicationPlatforms.Where(x => x.AppName == applicationPlatform.AppName && x.Platform == applicationPlatform.Platform).FirstOrDefault();
                if (appPlatform == null)
                {
                    WorkSpace.Instance.Solution.ApplicationPlatforms.Add(applicationPlatform);
                }
                return;
            }
            RepositoryItemBase repoItemToImport = null;
            if (itemToImport.ItemImportSetting == GlobalSolution.eImportSetting.Replace)
            {
                if (GlobalSolutionUtils.Instance.IsGingerRepositoryItem(sourceFile))
                {
                    RepositoryItemBase repositoryItem = newRepositorySerializer.DeserializeFromFile(sourceFile);
                    RepositoryItemBase repoItem = GlobalSolutionUtils.Instance.GetRepositoryItemByGUID(itemToImport, repositoryItem);
                    if (repoItem != null)
                    {
                        WorkSpace.Instance.SolutionRepository.MoveSharedRepositoryItemToPrevVersion(repoItem);
                        if (itemToImport.ItemType == GlobalSolution.eImportItemType.DataSources)
                        {
                            DataSourceBase dataSource = (DataSourceBase)repoItem;
                            string dsFile = WorkSpace.Instance.SolutionRepository.ConvertSolutionRelativePath(dataSource.FilePath);
                            GlobalSolutionUtils.Instance.KeepBackupAndDeleteFile(dsFile);
                        }
                    }
                }
                else
                {
                    GlobalSolutionUtils.Instance.KeepBackupAndDeleteFile(targetFile);
                }
            }

            if (saveAsRepo)
            {
                repoItemToImport = newRepositorySerializer.DeserializeFromFile(sourceFile);
                repoItemToImport.ContainingFolder = Path.GetDirectoryName(targetFile);
                repoItemToImport.FilePath = targetFile;
                if (!string.IsNullOrEmpty(itemToImport.ItemNewName))
                {
                    repoItemToImport.ItemName = itemToImport.ItemNewName;
                }
                if (itemToImport.ItemType == GlobalSolution.eImportItemType.DataSources)
                {
                    DataSourceBase dataSource = (DataSourceBase)repoItemToImport;
                    sourceFile = dataSource.FilePath.Replace("~", SolutionFolder);
                    string dsFile = WorkSpace.Instance.SolutionRepository.ConvertSolutionRelativePath(dataSource.FilePath);
                    string directory = Path.GetDirectoryName(dsFile);
                    string ext = Path.GetExtension(dsFile);
                    string fileName = Path.GetFileName(dsFile);

                    string newFile = string.Empty;
                    if (!string.IsNullOrEmpty(itemToImport.ItemNewName))
                    {
                        newFile = Path.Combine(directory, itemToImport.ItemNewName + ext);
                    }
                    else
                    {
                        newFile = Path.Combine(directory, fileName);
                    }
                    dataSource.FilePath = WorkSpace.Instance.SolutionRepository.ConvertFullPathToBeRelative(newFile);
                    //
                    File.Copy(sourceFile, newFile);
                }
                
                WorkSpace.Instance.SolutionRepository.AddRepositoryItem(repoItemToImport);
            }
            else
            {
                if (!Directory.Exists(Path.GetDirectoryName(targetFile)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(targetFile));
                }
                File.Copy(sourceFile, targetFile);
            }
        }

        
    }
}