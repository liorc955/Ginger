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

using Amdocs.Ginger.Common.UIElement;
using GingerCore.Drivers.AndroidADB;
using GingerWPF.UserControlsLib.UCTreeView;

namespace Ginger.WindowExplorer.Android
{
    public class AndroidElementInfoConverter
    {
        internal static ITreeViewItem GetTreeViewItemFor(ElementInfo EI)
        {
            // TODO verify if pl.Name = ElementInfo

            AndroidElementInfo AEI = (AndroidElementInfo)EI;

            switch (AEI.ElementType)
            {
                case "android.widget.Button":
                    return new AndroidWidgetButtonTreeItem() { AndroidElementInfo = AEI };
                case "android.widget.CheckBox":
                    return new AndroidWidgetCheckBoxTreeItem() { AndroidElementInfo = AEI };
                case "android.widget.TextView":
                    return new AndroidWidgetTextViewTreeItem() { AndroidElementInfo = AEI };
                case "android.widget.ImageView":
                    return new AndroidWidgetImageTreeItem() { AndroidElementInfo = AEI };
                case "android.widget.EditText":
                    return new AndroidWidgetEditTextTreeItem() { AndroidElementInfo = AEI };

                default:
                    // return simple basic Android TVI
                    return new AndroidElementTreeItemBase() { AndroidElementInfo = AEI };
            }
        }
    }
}
