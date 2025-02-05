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
using Amdocs.Ginger.Common.InterfacesLib;
using Amdocs.Ginger.Repository;
using Ginger.SolutionGeneral;
using Ginger.Variables;
using GingerCore;
using GingerCore.Actions;
using GingerCore.Platforms;
using GingerCore.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Ginger.AnalyzerLib
{
    public class AnalyzeBusinessFlow : AnalyzerItemBase
    {
        static string MissingTargetApp = GingerDicser.GetTermResValue(eTermResKey.BusinessFlow) + " is missing target Application(s)";
        static string MissingActivities = GingerDicser.GetTermResValue(eTermResKey.BusinessFlow) + " is missing " + GingerDicser.GetTermResValue(eTermResKey.Activities);
        public static readonly string LegacyOutPutValidationDescription = "Output validation contains legacy operators which are very slow and does not work on Linux";
        private static Regex rxVarPattern = new Regex(@"{(\bVar Name=)\w+\b[^{}]*}", RegexOptions.Compiled);
        public BusinessFlow mBusinessFlow { get; set; }
        private Solution mSolution { get; set; }

        public List<ActReturnValue> ReturnValues { get; set; } = new List<ActReturnValue>();
        public static List<AnalyzerItemBase> Analyze(Solution Solution, BusinessFlow BusinessFlow, bool includeMandatoryInputsAnalyze = true)
        {
            // Put all tests on BF here
            List<AnalyzerItemBase> IssuesList = new List<AnalyzerItemBase>();

            // Check BF have Target Apps            
            if (BusinessFlow.TargetApplications == null || !BusinessFlow.TargetApplications.Any())
            {
                AnalyzeBusinessFlow ABF = new AnalyzeBusinessFlow();
                ABF.Description = MissingTargetApp;
                ABF.UTDescription = "MissingTargetApp";
                ABF.Details = GingerDicser.GetTermResValue(eTermResKey.BusinessFlow) + " doesn't have Target Application(s) defined";
                ABF.HowToFix = "Open the " + GingerDicser.GetTermResValue(eTermResKey.BusinessFlow) + " in solution tab and add target apps";
                ABF.CanAutoFix = AnalyzerItemBase.eCanFix.Yes;   //  take it from solution 
                ABF.FixItHandler = FixMissingTargetApp;
                ABF.Status = AnalyzerItemBase.eStatus.NeedFix;
                ABF.IssueType = eType.Error;
                ABF.ItemParent = "NA";
                ABF.mBusinessFlow = BusinessFlow;
                ABF.ItemName = BusinessFlow.Name;
                ABF.Impact = "Might be executed on wrong application if solution have more than 1 target application";
                ABF.mSolution = Solution;
                ABF.ItemClass = "BusinessFlow";
                ABF.Severity = eSeverity.High;
                ABF.Selected = true;
                IssuesList.Add(ABF);
            }

            // Check BF have activities
            if (!BusinessFlow.Activities.Any())
            {
                AnalyzeBusinessFlow ABF = new AnalyzeBusinessFlow();
                ABF.UTDescription = "MissingActivities";
                ABF.Description = MissingActivities;
                ABF.Details = GingerDicser.GetTermResValue(eTermResKey.BusinessFlow) + " doesn't have " + GingerDicser.GetTermResValue(eTermResKey.Activities);
                ABF.HowToFix = "Open the " + GingerDicser.GetTermResValue(eTermResKey.BusinessFlow) + " and add " + GingerDicser.GetTermResValue(eTermResKey.Activities) + " or remove this " + GingerDicser.GetTermResValue(eTermResKey.BusinessFlow);
                ABF.CanAutoFix = AnalyzerItemBase.eCanFix.No;    // we can autofix by delete, but don't want to
                ABF.Status = AnalyzerItemBase.eStatus.NeedFix;

                ABF.IssueType = eType.Warning;
                ABF.ItemParent = "NA";
                ABF.mBusinessFlow = BusinessFlow;
                ABF.Severity = eSeverity.Medium;
                ABF.ItemClass = "BusinessFlow";
                IssuesList.Add(ABF);
            }

            //check for mandatory inputs without value:
            if (includeMandatoryInputsAnalyze)
            {
                IssuesList.AddRange(AnalyzeForMissingMandatoryInputValues(BusinessFlow));
            }

            // Check BF have actions with legacy outputValidation

            AnalyzeBusinessFlow OutputValidationIssue = GetOutputvalidationErros(BusinessFlow);
            if (OutputValidationIssue.ReturnValues.Count > 0)
            {
                OutputValidationIssue.ItemName = BusinessFlow.Name;
                OutputValidationIssue.Description = LegacyOutPutValidationDescription;
                OutputValidationIssue.IssueType = eType.Warning;
                OutputValidationIssue.mBusinessFlow = BusinessFlow;
                OutputValidationIssue.Severity = eSeverity.Medium;
                OutputValidationIssue.ItemClass = "BusinessFlow";
                OutputValidationIssue.CanAutoFix = eCanFix.Maybe;
                OutputValidationIssue.Status = AnalyzerItemBase.eStatus.NeedFix;
                OutputValidationIssue.FixItHandler = FixOutputValidationIssue;
                IssuesList.Add(OutputValidationIssue);
            }

            IssuesList.AddRange(AnalyzeForInputValuesRules(BusinessFlow));

            return IssuesList;
        }

        public static List<AnalyzerItemBase> AnalyzeForInputValuesRules(BusinessFlow businessFlow)
        {
            List<AnalyzerItemBase> issuesList = new List<AnalyzerItemBase>();
            ObservableList<VariableBase> bfInputVariables = businessFlow.GetBFandActivitiesVariabeles(true, true);
            foreach (InputVariableRule ivr in businessFlow.InputVariableRules)
            {
                if (ivr.Active)
                {
                    VariableBase sVariable = bfInputVariables.FirstOrDefault(v => v.Guid == ivr.SourceVariableGuid);
                    VariableBase tVariable = bfInputVariables.FirstOrDefault(v => v.Guid == ivr.TargetVariableGuid);
                    if (sVariable == null || tVariable == null)
                    {
                        string vtype = string.Empty;
                        if (tVariable == null)
                        {
                            vtype = "Target ";
                        }
                        else if (sVariable == null)
                        {
                            vtype = "Source ";
                        }
                        AnalyzeBusinessFlow AA = new AnalyzeBusinessFlow();
                        AA.Description =  "The '" + vtype + GingerDicser.GetTermResValue(eTermResKey.Variable) + "' is missing in Input Variable Rule";
                        AA.UTDescription = "MissingVariable";
                        AA.Details = "The '" + vtype + GingerDicser.GetTermResValue(eTermResKey.Variable) + "' Does not exist In Input Variable Rule";
                        AA.HowToFix = " Create new " + GingerDicser.GetTermResValue(eTermResKey.Variable) + " or Delete it from the input variable rule";
                        AA.CanAutoFix = AnalyzerItemBase.eCanFix.No;
                        AA.IssueType = eType.Error;
                        AA.Impact = GingerDicser.GetTermResValue(eTermResKey.BusinessFlow) + " will fail due to missing " + GingerDicser.GetTermResValue(eTermResKey.Variable);
                        AA.Severity = eSeverity.High;
                        AA.IssueCategory = eIssueCategory.MissingVariable;
                        AA.ItemParent = businessFlow.Name;
                        AA.ItemClass = "InputVariableRule";
                        AA.Status = eStatus.NeedFix;
                        issuesList.Add(AA);
                    }
                    else if (sVariable!=null && sVariable.GetType() == typeof(VariableSelectionList))
                    {
                        if (((VariableSelectionList)sVariable).OptionalValuesList.Where(x => x.Value == ivr.TriggerValue).Count() < 1)
                        {
                            AnalyzeBusinessFlow AA = new AnalyzeBusinessFlow();
                            AA.Description =  "The 'Source " + GingerDicser.GetTermResValue(eTermResKey.Variable) + "' trigger value used in Input Variable Rule is  missing in Selection List " +
                                                GingerDicser.GetTermResValue(eTermResKey.Variable) + ": " + sVariable.Name;
                            AA.UTDescription = "MissingVariableValue";
                            AA.Details = "The 'Source " + GingerDicser.GetTermResValue(eTermResKey.Variable) + "' trigger value used in Input Variable Rule is  missing in Selection List " +
                                                GingerDicser.GetTermResValue(eTermResKey.Variable)+ ": " + sVariable.Name;
                            AA.HowToFix = " Change 'Source " + GingerDicser.GetTermResValue(eTermResKey.Variable)  + " Trigger Value in Input Variable Rule";
                            AA.CanAutoFix = AnalyzerItemBase.eCanFix.No;
                            AA.IssueType = eType.Error;
                            AA.Impact = GingerDicser.GetTermResValue(eTermResKey.BusinessFlow) + " will fail due to missing " + GingerDicser.GetTermResValue(eTermResKey.Variable) + " Value";
                            AA.Severity = eSeverity.High;
                            AA.IssueCategory = eIssueCategory.MissingVariable;
                            AA.ItemParent = businessFlow.Name;
                            AA.ItemClass = "InputVariableRule";
                            AA.Status = eStatus.NeedFix;
                            issuesList.Add(AA);
                        }
                    }
                    else if (tVariable!=null && tVariable.GetType() == typeof(VariableSelectionList) && (ivr.OperationType == InputVariableRule.eInputVariableOperation.SetValue || ivr.OperationType == InputVariableRule.eInputVariableOperation.SetOptionalValues))
                    {
                        if (((VariableSelectionList)tVariable).OptionalValuesList.Where(x => x.Value == ivr.OperationValue).Count() < 1)
                        {
                            AnalyzeBusinessFlow AA = new AnalyzeBusinessFlow();
                            AA.Description =  "The 'Target " + GingerDicser.GetTermResValue(eTermResKey.Variable) + "' trigger value used in Input Variable Rule is  missing in Selection List " +
                                                GingerDicser.GetTermResValue(eTermResKey.Variable) + ": " + tVariable.Name;
                            AA.UTDescription = "MissingVariableValue";
                            AA.Details = "The 'Target " + GingerDicser.GetTermResValue(eTermResKey.Variable) + "' trigger value used in Input Variable Rule is  missing in Selection List " +
                                                GingerDicser.GetTermResValue(eTermResKey.Variable)+ ": " + tVariable.Name;
                            AA.HowToFix = " Change 'Target " + GingerDicser.GetTermResValue(eTermResKey.Variable)  + " Trigger Value in Input Variable Rule";
                            AA.CanAutoFix = AnalyzerItemBase.eCanFix.No;
                            AA.IssueType = eType.Error;
                            AA.Impact = GingerDicser.GetTermResValue(eTermResKey.BusinessFlow) + " will fail due to missing " + GingerDicser.GetTermResValue(eTermResKey.Variable) + " Value";
                            AA.Severity = eSeverity.High;
                            AA.IssueCategory = eIssueCategory.MissingVariable;
                            AA.ItemParent = businessFlow.Name;
                            AA.ItemClass = "InputVariableRule";
                            AA.Status = eStatus.NeedFix;
                            issuesList.Add(AA);
                        }
                    } 
                }
            }
            return issuesList;
        }

        public static List<AnalyzerItemBase> AnalyzeForMissingMandatoryInputValues(BusinessFlow businessFlow)
        {
            List<AnalyzerItemBase> issuesList = new List<AnalyzerItemBase>();
            foreach (VariableBase var in businessFlow.GetBFandActivitiesVariabeles(includeParentDetails: true, includeOnlySetAsInputValue: true, includeOnlyMandatoryInputs: true))
            {
                if (var.SetAsInputValue && var.MandatoryInput && string.IsNullOrWhiteSpace(var.Value) && var.MappedOutputType == VariableBase.eOutputType.None)
                {
                    AnalyzeBusinessFlow mandatoryInputIssue = new AnalyzeBusinessFlow();
                    mandatoryInputIssue.Description = string.Format("Mandatory Input {0} is missing a value", GingerDicser.GetTermResValue(eTermResKey.Variable));
                    mandatoryInputIssue.UTDescription = "MissingMandatoryInputValue";
                    mandatoryInputIssue.Details = string.Format("The {0} '{1}' was marked as 'Mandatory Input' but it has empty value and no dynamic mapped value.", GingerDicser.GetTermResValue(eTermResKey.Variable), var.Name);
                    mandatoryInputIssue.HowToFix = string.Format("Set a value to the {0} before starting the execution.", GingerDicser.GetTermResValue(eTermResKey.Variable));
                    mandatoryInputIssue.CanAutoFix = AnalyzerItemBase.eCanFix.No;
                    mandatoryInputIssue.FixItHandler = null;
                    mandatoryInputIssue.Status = AnalyzerItemBase.eStatus.NeedFix;
                    mandatoryInputIssue.IssueType = eType.Error;
                    mandatoryInputIssue.ItemParent = var.ParentName;
                    mandatoryInputIssue.mBusinessFlow = businessFlow;
                    mandatoryInputIssue.ItemName = var.Name;
                    mandatoryInputIssue.Impact = "Execution probably will fail due to missing input value.";
                    mandatoryInputIssue.mSolution = WorkSpace.Instance.Solution;
                    mandatoryInputIssue.ItemClass = GingerDicser.GetTermResValue(eTermResKey.Variable);
                    mandatoryInputIssue.Severity = eSeverity.High;
                    mandatoryInputIssue.Selected = true;
                    issuesList.Add(mandatoryInputIssue);
                }
            }
            return issuesList;
        }

        private static void FixOutputValidationIssue(object sender, EventArgs e)
        {
            AnalyzeBusinessFlow ABF = (AnalyzeBusinessFlow)sender;

            int issueCount = ABF.ReturnValues.Count;
            int FixedIssueCount = 0;
            foreach (ActReturnValue ARV in ABF.ReturnValues)
            {
                if (!string.IsNullOrEmpty(ARV.Expected) && !ARV.Expected.Contains("{"))
                {
                    ARV.Operator = Amdocs.Ginger.Common.Expressions.eOperator.Equals;
                }
                else
                {

                    if (!string.IsNullOrEmpty(ARV.Expected) && ARV.Expected.StartsWith(@"{Var "))
                    {
                        MatchCollection matches = rxVarPattern.Matches(ARV.Expected);
                        if (matches.Count == 1)
                        {
                            if (matches[0].Value.Equals(ARV.Expected))
                            {
                                FixedIssueCount++;
                                ARV.Operator = Amdocs.Ginger.Common.Expressions.eOperator.Equals;
                            }
                        }


                    }
                }
            }

            if (FixedIssueCount == 0)
            {
                ABF.Status = eStatus.CannotFix;
            }
            else if (FixedIssueCount < issueCount)
            {
                ABF.Status = eStatus.PartiallyFixed;
            }
            else
            {
                ABF.Status = eStatus.Fixed;
            }
        }

        private static AnalyzeBusinessFlow GetOutputvalidationErros(BusinessFlow businessFlow)
        {
            AnalyzeBusinessFlow ABF = new AnalyzeBusinessFlow();

            IEnumerable<ObservableList<IAct>> ActionList = businessFlow.Activities.Select(x => x.Acts);

            foreach (ObservableList<IAct> ActList in ActionList)
            {
                foreach (Act action in ActList)
                {
                    ABF.ReturnValues.AddRange(action.ActReturnValues.Where(x => x.Operator == Amdocs.Ginger.Common.Expressions.eOperator.Legacy));
                }
            }
            return ABF;
        }

        private static void FixMissingTargetApp(object sender, EventArgs e)
        {
            // Take the target app from the solution
            AnalyzeBusinessFlow ABF = (AnalyzeBusinessFlow)sender;
            // Do recheck
            if (!ABF.mBusinessFlow.TargetApplications.Any())
            {
                if (!ABF.mSolution.ApplicationPlatforms.Any())
                {
                    Reporter.ToUser(eUserMsgKey.MissingTargetApplication, "The default Application Platform Info is missing, please go to Solution level to add at least one Target Application.");
                    return;
                }
                string SAN = ABF.mSolution.ApplicationPlatforms[0].AppName;
                ABF.mBusinessFlow.TargetApplications.Add(new TargetApplication() { AppName = SAN });
                ABF.Status = eStatus.Fixed;
            }
        }
    }
}
