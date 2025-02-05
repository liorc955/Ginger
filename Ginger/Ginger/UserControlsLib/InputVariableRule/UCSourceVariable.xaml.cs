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
using GingerCore.Variables;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Ginger.UserControlsLib
{
    /// <summary>
    /// Interaction logic for UCSourceVariable.xaml
    /// </summary>
    public partial class UCSourceVariable : UserControl, INotifyPropertyChanged
    {
        public static DependencyProperty SourceVariabelsProperty = DependencyProperty.Register("SourceVariabels", typeof(ObservableList<VariableBase>), typeof(UCSourceVariable), new PropertyMetadata(OnSourceVariabelPropertyChanged));

        public static DependencyProperty SourceVariabelGuidProperty = DependencyProperty.Register("SourceVariabelGuid", typeof(Guid), typeof(UCSourceVariable), new PropertyMetadata(OnSourceVariabelGuidPropertyChanged));
             
        public ObservableList<VariableBase> SourceVariables
        {
            get { return (ObservableList<VariableBase>)GetValue(SourceVariabelsProperty); }
            set { SetValue(SourceVariabelsProperty, value); }
        }


        public Guid SourceVariableGuid
        {
            get { return (Guid)GetValue(SourceVariabelGuidProperty); }
            set { SetValue(SourceVariabelGuidProperty, value); }
        }
      
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private static void OnSourceVariabelPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            var control = sender as UCSourceVariable;
            if (control != null)
            {
                control.SourceVariabelsPropertyChanged((ObservableList<VariableBase>)args.NewValue);
            }
        }

        private static void OnSourceVariabelGuidPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            var control = sender as UCSourceVariable;
            if (control != null)
            {
                control.SourceVariabelGuidPropertyChanged((Guid)args.NewValue);
            }
        }
       
        private void SourceVariabelsPropertyChanged(ObservableList<VariableBase> sourceVariableList)
        {            
            SetComboboxData(sourceVariableList);
        }
    
        private void SetComboboxData(ObservableList<VariableBase> variableList)
        {
            if (variableList == null || variableList.Count == 0)
            {                
                xVariablesComboBox.IsEnabled = false;
            }

            if (variableList != null && variableList.Count > 0)
            {
                xVariablesComboBox.DisplayMemberPath = nameof(VariableBase.Name);
                xVariablesComboBox.SelectedValuePath = nameof(VariableBase.Guid);              
                xVariablesComboBox.ItemsSource = variableList.OrderBy(nameof(VariableBase.Name));
                xVariablesComboBox.SelectionChanged +=XVariablesComboBox_SelectionChanged;
            }
        }

        private void SourceVariabelGuidPropertyChanged(Guid guid)
        {   
            OnPropertyChanged(nameof(UCSourceVariable.SourceVariableGuid));                
            SetComboBoxValue(guid);
            MarkSourceVariableValidation(guid);
        }
        private void MarkSourceVariableValidation(Guid guid)
        {
            bool isValid = true;

            if (!SourceVariables.Any(x => x.Guid == guid))
            {
                isValid = false;
            }

            if (isValid == false)
            {
                this.BorderThickness = new Thickness(1);
                this.BorderBrush = Brushes.Red;
            }
            else
            {
                this.BorderThickness = new Thickness(0);
                this.BorderBrush = null;
            }
        }

        private void SetComboBoxValue(Guid guid)
        {
            xVariablesComboBox.SelectedValue = guid;
        }

        private void XVariablesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;          
            if (comboBox.Items.Count > 0)
            {
                VariableBase variableBase = (VariableBase)(comboBox).SelectedItem;
                if(variableBase!=null)
                {                   
                    SourceVariableGuid = variableBase.Guid;                   
                }                
            }
        }

        public UCSourceVariable()
        {
            InitializeComponent();
        }

        public static DataTemplate GetTemplate(string sourcevariablesProperty = "", string sourceVariableGuidProperty = "")
        {
            DataTemplate template = new DataTemplate();
            FrameworkElementFactory ucVariableType = new FrameworkElementFactory(typeof(UCSourceVariable));

            if (string.IsNullOrEmpty(sourcevariablesProperty) == false)
            {
                Binding sourcevariablesBinding = new Binding(sourcevariablesProperty);
                sourcevariablesBinding.Mode = BindingMode.OneWay;
                sourcevariablesBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                ucVariableType.SetBinding(UCSourceVariable.SourceVariabelsProperty, sourcevariablesBinding);
            }

            if (string.IsNullOrEmpty(sourceVariableGuidProperty) == false)
            {
                Binding sourcevariableGuidBinding = new Binding(sourceVariableGuidProperty);        
                sourcevariableGuidBinding.Mode = BindingMode.TwoWay;
                sourcevariableGuidBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                ucVariableType.SetBinding(UCSourceVariable.SourceVariabelGuidProperty, sourcevariableGuidBinding);
            }          
                    
            template.VisualTree = ucVariableType;
            return template;
        }

    }
}
