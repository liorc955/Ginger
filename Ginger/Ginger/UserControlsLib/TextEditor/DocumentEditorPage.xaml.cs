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
using Amdocs.Ginger.Plugin.Core;
using Amdocs.Ginger.Repository;
using Ginger.PlugInsWindows;
using Ginger.UserControlsLib.TextEditor.Common;
using Ginger.UserControlsLib.TextEditor.Office;
using Ginger.UserControlsLib.TextEditor.VBS;
using GingerCore.GeneralLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;

namespace Ginger.UserControlsLib.TextEditor
{
    /// <summary>
    /// Interaction logic for BasicTextEditorPage.xaml
    /// </summary>
    public partial class DocumentEditorPage : Page
    {
        static List<TextEditorBase> TextEditors = null;

        public DocumentEditorPage(string FileName, bool enableEdit = true, bool RemoveToolBar = false, string UCTextEditorTitle = null)
        {
            InitializeComponent();

            TextEditorBase TE = GetTextEditorByExtension(FileName);

            if (TE != null)
            {
                ITextEditorPage p = TE.EditorPage;
                if (p != null)
                {
                    // text editor can return customized editor
                    EditorFrame.ClearAndSetContent(p);
                    p.Load(FileName);
                }
                else
                {
                    // Load regular UCtextEditor and init with TE
                    UCTextEditor UCTE = new UCTextEditor();
                    UCTE.Init(FileName, TE, enableEdit, RemoveToolBar);
                    if (UCTextEditorTitle != null)
                    {
                        UCTE.SetContentEditorTitleLabel(UCTextEditorTitle);
                    }
                    else if (!string.IsNullOrEmpty(TE.Title()))
                    {
                        UCTE.SetContentEditorTitleLabel(TE.Title());
                    }

                    EditorFrame.ClearAndSetContent(UCTE);
                }
            }
            else
            {
                if (IsTextFile(FileName))
                {
                    //Use the default UCTextEditor control
                    UCTextEditor UCTE = new UCTextEditor();
                    AutoDetectTextEditor AD = new AutoDetectTextEditor();
                    AD.ext = Path.GetExtension(FileName);
                    TE = AD;
                    UCTE.Init(FileName, TE, enableEdit, RemoveToolBar);
                    if (UCTextEditorTitle != null)
                    {
                        UCTE.SetContentEditorTitleLabel(UCTextEditorTitle);
                    }

                    EditorFrame.ClearAndSetContent(UCTE);
                }
                else
                {
                    // Just put a general shell doc                     
                    ITextEditorPage p = new OfficeDocumentPage();
                    p.Load(FileName);
                    EditorFrame.ClearAndSetContent(p);
                }
            }
        }



        internal void ShowAsWindow(string customeTitle = null)
        {
            string title;
            if (customeTitle != null)
            {
                title = customeTitle;
            }
            else
            {
                title = this.Title;
            }

            GingerCore.General.LoadGenericWindow(ref genWin, App.MainWindow, eWindowShowStyle.Free, title, this);
        }

        GenericWindow genWin;

        private bool IsTextFile(string fileName)
        {
            // Method #1
            if (IsTextFileByExtension(Path.GetExtension(fileName).ToLower()))
            {
                return true;
            }

            // Method #2
            //Not full proof but good enough
            // Check for consecutive nulls in the first 10K..
            byte[] content = File.ReadAllBytes(fileName);
            for (int i = 1; i < 10000 && i < content.Length; i++)
            {
                if (content[i] == 0x00 && content[i - 1] == 0x00)
                {
                    return false;
                }
            }
            return true;

        }

        static List<string> ExtensiosnList = null;

        static bool IsTextFileByExtension(string sExt)
        {
            //TODO: create match - ext to edit type - show icon per type
            // new class - ext, editortype, icon for TV, keep in static
            if (ExtensiosnList == null)
            {
                FillExtensionsList();

            }
            if (ExtensiosnList.Contains(sExt))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static void FillExtensionsList()
        {
            ExtensiosnList = new List<string>();
            ExtensiosnList.Add(".txt");
            ExtensiosnList.Add(".bat");
            ExtensiosnList.Add(".ahk");
            ExtensiosnList.Add(".applescript");
            ExtensiosnList.Add(".as");
            ExtensiosnList.Add(".au3");
            ExtensiosnList.Add(".bas"); ExtensiosnList.Add(".cljs");
            ExtensiosnList.Add(".cmd"); ExtensiosnList.Add(".coffee");
            ExtensiosnList.Add(".egg"); ExtensiosnList.Add(".egt");
            ExtensiosnList.Add(".erb"); ExtensiosnList.Add(".hta");
            ExtensiosnList.Add(".ibi"); ExtensiosnList.Add(".ici");
            ExtensiosnList.Add(".ijs"); ExtensiosnList.Add(".ipynb");
            ExtensiosnList.Add(".itcl"); ExtensiosnList.Add(".js");
            ExtensiosnList.Add(".jsfl"); ExtensiosnList.Add(".lua");
            ExtensiosnList.Add(".m"); ExtensiosnList.Add(".mrc");
            ExtensiosnList.Add(".ncf"); ExtensiosnList.Add(".nut");
            ExtensiosnList.Add(".php"); ExtensiosnList.Add(".pl");
            ExtensiosnList.Add(".pm"); ExtensiosnList.Add(".ps1");
            ExtensiosnList.Add(".ps1xml"); ExtensiosnList.Add(".psc1");
            ExtensiosnList.Add(".psd1"); ExtensiosnList.Add(".psm1");
            ExtensiosnList.Add(".py"); ExtensiosnList.Add(".pyc");
            ExtensiosnList.Add(".pyo"); ExtensiosnList.Add(".r");
            ExtensiosnList.Add(".rb"); ExtensiosnList.Add(".rdp");
            ExtensiosnList.Add(".scpt"); ExtensiosnList.Add(".scptd");
            ExtensiosnList.Add(".sdl"); ExtensiosnList.Add(".sh");
            ExtensiosnList.Add(".syjs"); ExtensiosnList.Add(".sypy");
            ExtensiosnList.Add(".tcl"); ExtensiosnList.Add(".vbs");
            ExtensiosnList.Add(".xpl"); ExtensiosnList.Add(".ebuild");
            ExtensiosnList.Add(".feature");
        }

        private TextEditorBase GetTextEditorByExtension(string FileName)
        {
            ScanTextEditors();

            //TODO: if there is more than one let the user pick
            string ext = Path.GetExtension(FileName).ToLower();
            foreach (TextEditorBase TE in TextEditors)
            {
                if (TE.Extensions != null)
                {
                    if (TE.Extensions.Contains(ext))
                    {
                        return TE;
                    }
                }
            }

            return null;
        }

        void ScanTextEditors()
        {
            if (TextEditors == null)
            {
                TextEditors = new List<TextEditorBase>();

                var list = from type in typeof(TextEditorBase).Assembly.GetTypes()
                           where type.IsSubclassOf(typeof(TextEditorBase))
                           select type;


                foreach (Type t in list)
                {
                    if (t == typeof(PlugInTextEditorWrapper) || t == typeof(ValueExpression.ValueExpressionEditor))
                    {
                        continue;
                    }

                    if (t != typeof(ITextEditor))
                    {
                        TextEditorBase TE = (TextEditorBase)Activator.CreateInstance(t);
                        TextEditors.Add(TE);
                    }
                }

                // Add all plugins TextEditors 
                ObservableList<PluginPackage> Plugins = WorkSpace.Instance.SolutionRepository.GetAllRepositoryItems<PluginPackage>();

                foreach (PluginPackage pluginPackage in Plugins)
                {
                    pluginPackage.PluginPackageOperations = new PluginPackageOperations(pluginPackage);

                    if (string.IsNullOrEmpty(((PluginPackageOperations)pluginPackage.PluginPackageOperations).PluginPackageInfo.UIDLL))
                    {
                        continue;
                    }

                    foreach (ITextEditor TE in PluginTextEditorHelper.GetTextFileEditors(pluginPackage))
                    {
                        PlugInTextEditorWrapper plugInTextEditorWrapper = new PlugInTextEditorWrapper(TE);
                        TextEditors.Add(plugInTextEditorWrapper);
                    }
                }
            }
        }




        public void Save()
        {
            if (EditorFrame.Content is UCTextEditor)
            {
                ((UCTextEditor)EditorFrame.Content).Save();
            }
            else
            if (EditorFrame.Content is ITextEditorPage)
            {
                ((ITextEditorPage)EditorFrame.Content).Save();
            }
            else
            {
                //TODO: fix me to call save...
            }
        }
    }
}
