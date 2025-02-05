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

using Amdocs.Ginger.Common;
using Amdocs.Ginger.Common.UIElement;
using Ginger.WindowExplorer;
using GingerCore.Actions;
using GingerCore.Actions.Windows;
using GingerWPF.UserControlsLib.UCTreeView;
using System.Windows.Controls;

namespace Ginger.Drivers.Windows
{
    public class WindowsRadioButtonTreeItem : WindowsElementTreeItemBase, ITreeViewItem, IWindowExplorerTreeItem
    {
        StackPanel ITreeViewItem.Header()
        {
            return TreeViewUtils.CreateItemHeader(UIAElementInfo.ElementTitle, ElementInfo.GetElementTypeImage(eElementType.RadioButton));
        }

        ObservableList<Act> IWindowExplorerTreeItem.GetElementActions()
        {
            ObservableList<Act> list = new ObservableList<Act>();

            list.Add(new ActWindowsControl()
            {
                Description = "Select Radio Button " + UIAElementInfo.ElementTitle,
                ControlAction = ActWindowsControl.eControlAction.Select
            });

            list.Add(new ActWindowsControl()
            {
                Description = "Get Radio Button Value- " + UIAElementInfo.ElementTitle,
                ControlAction = ActWindowsControl.eControlAction.GetValue
            });

            list.Add(new ActWindowsControl()
            {
                Description = "Is Radio Button Selected- " + UIAElementInfo.ElementTitle,
                ControlAction = ActWindowsControl.eControlAction.IsSelected
            });

            list.Add(new ActWindowsControl()
            {
                Description = "Click Radio Button" + UIAElementInfo.ElementTitle,
                ControlAction = ActWindowsControl.eControlAction.Click
            });
            return list;
        }
    }
}
