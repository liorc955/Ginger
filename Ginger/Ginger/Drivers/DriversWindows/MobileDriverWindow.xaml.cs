﻿using Amdocs.Ginger.Common;
using Amdocs.Ginger.Common.Enums;
using Amdocs.Ginger.CoreNET.Drivers.CoreDrivers.Mobile;
using Amdocs.Ginger.CoreNET.Drivers.DriversWindow;
using Amdocs.Ginger.UserControls;
using GingerCore;
using GingerCore.Drivers;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Ginger.Drivers.DriversWindows
{
    /// <summary>
    /// Interaction logic for UpdatedAppiumDriverWindow.xaml
    /// </summary>
    public partial class MobileDriverWindow : Window
    {

        IMobileDriverWindow mDriver;
        Agent mAgent;
        bool mIsMousePressed;
        bool mIsItDragAction;
        bool mSelfClosing;
        bool mUserClosing;
        long mRectangleStartPoint_X;
        long mRectangleStartPoint_Y;
        System.Windows.Point mMouseStartPoint;
        System.Windows.Point mMouseEndPoint;
        public BusinessFlow mBF;
        eAutoScreenshotRefreshMode mDeviceAutoScreenshotRefreshMode;
        bool mWindowIsOpen = true;

        public MobileDriverWindow(DriverBase driver, Agent agent)
        {
            InitializeComponent();

            mDriver = (IMobileDriverWindow)driver;
            mAgent = agent;

            ((DriverBase)mDriver).DriverMessageEvent += MobileDriverWindow_DriverMessageEvent;           
        }

        #region Events
        private void MobileDriverWindow_DriverMessageEvent(object sender, DriverMessageEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                switch (e.DriverMessageType)
                {
                    case DriverBase.eDriverMessageType.DriverStatusChanged:
                        if (mDriver.IsDeviceConnected)
                        {
                            xLoadingPnl.Visibility = Visibility.Collapsed;
                            xDeviceScreenshotCanvas.Visibility = Visibility.Visible;
                            xLoadingLbl.Content = "Loading Device Screenshot...";
                            RefreshDeviceScreenshotAsync();
                            SetOrientationButton();
                            DoContinualDeviceScreenshotRefresh();
                        }
                        else
                        {
                            if (!mSelfClosing)
                            {
                                DoSelfClose();
                            }
                        }
                        break;

                    case DriverBase.eDriverMessageType.ActionPerformed:
                        if (mDeviceAutoScreenshotRefreshMode == eAutoScreenshotRefreshMode.PostOperation)
                        {
                            RefreshDeviceScreenshotAsync(100);
                        }
                        break;
                }
            });
        }

       
        private void xDeviceScreenshotImage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetDeviceButtonsLocation();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetDeviceButtonsLocation();
        }


        private void xDeviceScreenshotImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //if (mDriver.GetAppType() == eAppType.Web)//TODO: why not working for Web?
            //{
            //    e.Handled = true;
            //    return;
            //}
            try
            {
                mMouseStartPoint = e.GetPosition((System.Windows.Controls.Image)sender);
            }
            catch
            {
                mMouseStartPoint = e.GetPosition((System.Windows.Shapes.Rectangle)sender);
                //convert to image scale
                mMouseStartPoint.X = mMouseStartPoint.X + mRectangleStartPoint_X;
                mMouseStartPoint.Y = mMouseStartPoint.Y + mRectangleStartPoint_Y;
            }
            mIsMousePressed = true;
        }

        private void xDeviceScreenshotImage_MouseMove(object sender, MouseEventArgs e)
        {
            //if (mDriver.GetAppType() == eAppType.Web)
            //{
            //    e.Handled = true;
            //    return;
            //}
            if (mIsMousePressed == true)
            {
                //it's a drag
                mIsItDragAction = true;
            }
        }

        private void xDeviceScreenshotImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //if (mDriver.GetAppType() == eAppType.Web)
            //{
            //    e.Handled = true;
            //    return;
            //}

            try
            {
                mMouseEndPoint = e.GetPosition((System.Windows.Controls.Image)sender);
            }
            catch
            {
                mMouseEndPoint = e.GetPosition((System.Windows.Shapes.Rectangle)sender);
                //convert to image scale
                mMouseEndPoint.X = mMouseEndPoint.X + mRectangleStartPoint_X;
                mMouseEndPoint.Y = mMouseEndPoint.Y + mRectangleStartPoint_Y;
            }
            mIsMousePressed = false;

            if (mIsItDragAction == true)
            {
                //do drag
                mIsItDragAction = false;
                DeviceScreenshotImageMouseDragAsync(mMouseStartPoint, mMouseEndPoint);
            }
            else
            {
                //do click
                DeviceScreenshotImageMouseClickAsync(mMouseEndPoint);
            }
        }

        private void xDeviceScreenshotImage_MouseEnter(object sender, MouseEventArgs e)
        {
            //if (mDriver.GetAppType() == eAppType.Web)
            //{
            //    e.Handled = true;
            //    return;
            //}

            Mouse.OverrideCursor = System.Windows.Input.Cursors.Hand;
        }

        private void xDeviceScreenshotImage_MouseLeave(object sender, MouseEventArgs e)
        {
            //if (mDriver.GetAppType() == eAppType.Web)
            //{
            //    e.Handled = true;
            //    return;
            //}

            Mouse.OverrideCursor = null;
        }

        private void xRefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshDeviceScreenshotAsync();
        }

        bool mConfigIsOn = false;
        private void xConfigurationsBtn_Click(object sender, RoutedEventArgs e)
        {
            SetConfigurationsPanelView(!mConfigIsOn);
        }

        bool mPinIsOn = false;
        private void xPinBtn_Click(object sender, RoutedEventArgs e)
        {
            if (mPinIsOn)
            {
                //undock
                this.Topmost = false;
                xPinBtn.ButtonStyle = FindResource("$ImageButtonStyle_WhiteSmoke") as Style;
                xPinBtn.ToolTip = "Dock Window";
            }
            else
            {
                //dock
                this.Topmost = true;
                xPinBtn.ButtonStyle = FindResource("$ImageButtonStyle_Pink") as Style;
                xPinBtn.ToolTip = "Undock Window";
            }

            mPinIsOn = !mPinIsOn;
        }

        private void xOrientationBtn_Click(object sender, RoutedEventArgs e)
        {
            if (mDriver.GetOrientation() == eDeviceOrientation.Landscape)
            {
                mDriver.SwitchToPortrait();
            }
            else
            {
                mDriver.SwitchToLandscape();
            }
            if (mDeviceAutoScreenshotRefreshMode == eAutoScreenshotRefreshMode.PostOperation)
            {
                RefreshDeviceScreenshotAsync();
            }
            SetOrientationButton();
        }

        private void xBackBtn_Click(object sender, RoutedEventArgs e)
        {
            mDriver.PerformBackButtonPress();
            if (mDeviceAutoScreenshotRefreshMode == eAutoScreenshotRefreshMode.PostOperation)
            {
                RefreshDeviceScreenshotAsync(100);
            }
        }

        private void xHomeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (mDriver.GetAppType() == eAppType.Web)
            {
                Reporter.ToUser(eUserMsgKey.StaticWarnMessage, "Operation not supported for this mobile OS or application type.");
                return;
            }

            mDriver.PerformHomeButtonPress();
            if (mDeviceAutoScreenshotRefreshMode == eAutoScreenshotRefreshMode.PostOperation)
            {
                RefreshDeviceScreenshotAsync(100);
            }
        }

        private void xMenuBtn_Click(object sender, RoutedEventArgs e)
        {
            if (mDriver.GetAppType() == eAppType.Web)
            {
                Reporter.ToUser(eUserMsgKey.StaticWarnMessage, "Operation not supported for this mobile OS or application type.");
                return;
            }

            mDriver.PerformMenuButtonPress();
            if (mDeviceAutoScreenshotRefreshMode == eAutoScreenshotRefreshMode.PostOperation)
            {
                RefreshDeviceScreenshotAsync(100);
            }
        }

        
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (((Window)sender).IsKeyboardFocused)
            {
                mUserClosing = true;

                if (!mSelfClosing)
                {
                    if (Reporter.ToUser(eUserMsgKey.StaticQuestionsMessage, "Close Mobile Agent?") == eUserMsgSelection.Yes)
                    {
                        mAgent.Close();
                    }
                }
            }
            mWindowIsOpen = false;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitWindowLook();            
        }

        private void xTrackActionsChK_Unchecked(object sender, RoutedEventArgs e)
        {
            mDeviceAutoScreenshotRefreshMode = eAutoScreenshotRefreshMode.Live;
            if (this.IsLoaded)
            {
                xRefreshButton.Visibility = Visibility.Collapsed;
            }
        }

        private void xContinualRdBtn_Checked(object sender, RoutedEventArgs e)
        {
            mDeviceAutoScreenshotRefreshMode = eAutoScreenshotRefreshMode.Live;
            if (this.IsLoaded)
            {
                xRefreshWaitingRatePnl.Visibility = Visibility.Visible;
                xRefreshButton.Visibility = Visibility.Collapsed;
            }
        }

        private void xPostOperationRdBtn_Checked(object sender, RoutedEventArgs e)
        {
            mDeviceAutoScreenshotRefreshMode = eAutoScreenshotRefreshMode.PostOperation;
            if (this.IsLoaded)
            {
                xRefreshWaitingRatePnl.Visibility = Visibility.Visible;
                xRefreshButton.Visibility = Visibility.Visible;
            }
        }

        private void xDisabledRdBtn_Checked(object sender, RoutedEventArgs e)
        {
            mDeviceAutoScreenshotRefreshMode = eAutoScreenshotRefreshMode.Disabled;
            if (this.IsLoaded)
            {
                xRefreshWaitingRatePnl.Visibility = Visibility.Collapsed;
                xRefreshButton.Visibility = Visibility.Visible;
            }
        }
        #endregion Events


        #region Functions
        public void InitWindowLook()
        {
            try
            {
                //General
                if (mDriver.GetDevicePlatformType() == eDevicePlatformType.Android)
                {
                    this.Icon = ImageMakerControl.GetImageSource(eImageType.AndroidOutline);
                }
                else
                {
                    this.Icon = ImageMakerControl.GetImageSource(eImageType.IosOutline);
                }
                this.Title = string.Format("{0} Device View", mAgent.Name);
                this.Width = 320;
                this.Height = 650;
                xLoadingLbl.Content = "Connecting to Device...";

                //Configurations
                SetConfigurationsPanelView(false);
                //set refresh mode by what configured on driver      
                mDeviceAutoScreenshotRefreshMode = mDriver.DeviceAutoScreenshotRefreshMode;
                switch (mDeviceAutoScreenshotRefreshMode)
                {
                    case eAutoScreenshotRefreshMode.Live:
                        xLiveRdBtn.IsChecked = true;
                        break;
                    case eAutoScreenshotRefreshMode.PostOperation:
                        xPostOperationRdBtn.IsChecked = true;
                        break;
                    case eAutoScreenshotRefreshMode.Disabled:
                        xDisabledRdBtn.IsChecked = true;
                        break;
                }

                xConfigurationsFrame.Visibility = System.Windows.Visibility.Collapsed;
                xConfigurationsCol.Width = new GridLength(0);

                //Main tool bar
                xRefreshButton.ButtonStyle = FindResource("$ImageButtonStyle_WhiteSmoke") as Style;
                xPinBtn.ButtonStyle = FindResource("$ImageButtonStyle_WhiteSmoke") as Style;
                xConfigurationsBtn.ButtonStyle = FindResource("$ImageButtonStyle_WhiteSmoke") as Style;
                xPortraiteBtn.ButtonStyle = FindResource("$ImageButtonStyle_WhiteSmoke") as Style;
                xLandscapeBtn.ButtonStyle = FindResource("$ImageButtonStyle_WhiteSmoke") as Style;
                xPinBtn_Click(null, null);

                //Loading Pnl
                xDeviceScreenshotCanvas.Visibility = Visibility.Collapsed;
                xLoadingPnl.Visibility = Visibility.Visible;
                if (mDriver.GetDevicePlatformType() == eDevicePlatformType.Android)
                {
                    xLoadingImage.ImageType = eImageType.AndroidWhite;// ImageMakerControl.GetImageSource(eImageType.Processing, width: 50, toolTip:"Loading Device...", foreground: (System.Windows.Media.SolidColorBrush)FindResource("$BackgroundColor_WhiteSmoke"));
                }
                else
                {
                    xLoadingImage.ImageType = eImageType.IosWhite;
                }

                //Device buttons panel
                xHomeBtn.ButtonStyle = FindResource("$ImageButtonStyle_WhiteSmoke") as Style;
                xMenuBtn.ButtonStyle = FindResource("$ImageButtonStyle_WhiteSmoke") as Style;
                xBackButton.ButtonStyle = FindResource("$ImageButtonStyle_WhiteSmoke") as Style;
                switch (mDriver.GetDevicePlatformType())
                {
                    case eDevicePlatformType.Android:
                        //if (mDriver.GetAppType() == eAppType.Web)
                        //{
                        //    //browser mode- show buttons but disabled
                        //    xBackButton.IsEnabled = false;
                        //    xMenuBtn.IsEnabled = false;
                        //    xHomeBtn.IsEnabled = false;
                        //}
                        break;
                    case eDevicePlatformType.iOS:
                        //only middle button 
                        xBackButton.Visibility = Visibility.Collapsed;
                        xMenuBtn.Visibility = Visibility.Collapsed;
                        //if (mDriver.GetAppType() == eAppType.Web)
                        //{
                        //    xHomeBtn.IsEnabled = false;
                        //}
                        break;
                }
                //fliping the back icon to fit look on mobile
                xBackButton.xButtonImage.RenderTransformOrigin = new Point(0.5, 0.5);
                ScaleTransform flipTrans = new ScaleTransform();
                flipTrans.ScaleX = -1;
                xBackButton.xButtonImage.RenderTransform = flipTrans;
            }
            catch (Exception ex)
            {
                Reporter.ToLog(eLogLevel.ERROR, "Error occurred while designing the Mobile window initial look", ex);
            }
        }

        private void SetDeviceButtonsLocation()
        {
            if (xDeviceScreenshotCanvas != null)
            {
                //correct the device buttons location
                if (xDeviceScreenshotImage != null && xDeviceScreenshotImage.ActualWidth > 0)
                {
                    double emptySpace = ((xDeviceScreenshotCanvas.ActualWidth - xDeviceScreenshotImage.ActualWidth) / 2);
                    Thickness margin = xBackButton.Margin;
                    margin.Right = 10 + emptySpace;
                    xBackButton.Margin = margin;
                    margin = xMenuBtn.Margin;
                    margin.Left = 10 + emptySpace;
                    xMenuBtn.Margin = margin;
                }
            }
        }

        private void SetOrientationButton()
        {
            eDeviceOrientation currentOrientation = mDriver.GetOrientation();
            if (currentOrientation == eDeviceOrientation.Landscape)
            {
                xPortraiteBtn.Visibility = Visibility.Visible;
                xLandscapeBtn.Visibility = Visibility.Collapsed;
            }
            else
            {
                xPortraiteBtn.Visibility = Visibility.Collapsed;
                xLandscapeBtn.Visibility = Visibility.Visible;
            }
        }

        private void DoContinualDeviceScreenshotRefresh()
        {
            Task.Run(() =>
            {
                while(mWindowIsOpen && mDriver.IsDeviceConnected)
                {
                    if (mDeviceAutoScreenshotRefreshMode == eAutoScreenshotRefreshMode.Live)
                    {
                        RefreshDeviceScreenshotAsync(200).Wait();
                    }
                }
            });
        }

        private async Task<bool> RefreshDeviceScreenshotAsync(int waitingTimeInMiliSeconds = 0)
        {
            try
            {
                if (!xDeviceScreenshotImage.IsVisible)
                {
                    return false;
                }

                int waitingRatio = 1;
                if (mDeviceAutoScreenshotRefreshMode != eAutoScreenshotRefreshMode.Live)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        xRefreshButton.ButtonImageType = Amdocs.Ginger.Common.Enums.eImageType.Processing;
                        xRefreshButton.ButtonStyle = FindResource("$ImageButtonStyle_WhiteSmoke") as Style;
                        xRefreshButton.ToolTip = "Refreshing...";
                    });
                }

                await Task.Run(() =>
                {
                    //wait before taking screen shot
                    if (waitingTimeInMiliSeconds > 0)
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            waitingRatio = int.Parse(xRefreshWaitingRateCombo.Text.ToString());
                        });
                        Thread.Sleep(waitingTimeInMiliSeconds * (waitingRatio));
                    }

                    //take screen shot
                    try
                    {
                        byte[] imageByteArray = mDriver.GetScreenshotImage();
                        if (imageByteArray == null || imageByteArray.Length == 0)
                        {
                            Reporter.ToUser(eUserMsgKey.MobileRefreshScreenShotFailed, "Failed to get the screenshot from the device.");
                        }
                        else
                        {
                            var image = new BitmapImage();
                            using (var mem = new MemoryStream(imageByteArray))
                            {
                                image.BeginInit();
                                image.CacheOption = BitmapCacheOption.OnLoad; // here
                                image.StreamSource = mem;
                                image.EndInit();
                            }
                            image.Freeze();
                            this.Dispatcher.Invoke(() =>
                                {
                                    xDeviceScreenshotImage.Source = image;
                                });
                        }

                        return true;
                    }
                    catch (Exception ex)
                    {                       
                        this.Dispatcher.Invoke(() =>
                        {
                            if (!mDriver.IsDeviceConnected)
                            {
                                if (!mSelfClosing)
                                {
                                    DoSelfClose();
                                }
                            }
                            else
                            {
                                Reporter.ToUser(eUserMsgKey.MobileRefreshScreenShotFailed, string.Format("Failed to update the device screenshot, Error:{0}", ex.Message));
                                if (mDeviceAutoScreenshotRefreshMode == eAutoScreenshotRefreshMode.Live)
                                {
                                    xPostOperationRdBtn.IsChecked = true;
                                }
                            }
                        });
                        return false;
                    }
                });

                return false;
            }
            finally
            {
                if (mDeviceAutoScreenshotRefreshMode != eAutoScreenshotRefreshMode.Live)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        xRefreshButton.ButtonImageType = Amdocs.Ginger.Common.Enums.eImageType.Refresh;
                        xRefreshButton.ButtonStyle = FindResource("$ImageButtonStyle_WhiteSmoke") as Style;
                        xRefreshButton.ToolTip = "Refresh Device Screenshot";
                    });
                }
            }                
        }

        private System.Windows.Point GetPointOnMobile(System.Windows.Point pointOnImage)
        {
            System.Windows.Point pointOnMobile = new System.Windows.Point();
            double ratio_X = 1;
            double ratio_Y = 1;

            if (mDriver.GetDevicePlatformType() == eDevicePlatformType.iOS && mDriver.GetAppType() == eAppType.NativeHybride)
            {
                ratio_X = (xDeviceScreenshotImage.Source.Width / 2) / xDeviceScreenshotImage.ActualWidth;
                ratio_Y = (xDeviceScreenshotImage.Source.Height / 2) / xDeviceScreenshotImage.ActualHeight;
            }
            else
            {
                ratio_X = xDeviceScreenshotImage.Source.Width / xDeviceScreenshotImage.ActualWidth;
                ratio_Y = xDeviceScreenshotImage.Source.Height / xDeviceScreenshotImage.ActualHeight;
            }

            //pointOnMobile.X = (long)(pointOnImage.X * ratio_X);
            //pointOnMobile.Y = (long)(pointOnImage.Y * ratio_Y);
            pointOnMobile.X = (int)(pointOnImage.X * ratio_X);
            pointOnMobile.Y = (int)(pointOnImage.Y * ratio_Y);

            return pointOnMobile;
        }

        private async void DeviceScreenshotImageMouseClickAsync(System.Windows.Point clickedPoint)
        {
            try
            {
                //calculate clicked X,Y
                System.Windows.Point pointOnMobile = GetPointOnMobile(clickedPoint);
                long pointOnMobile_X = (long)pointOnMobile.X;
                long pointOnMobile_Y = (long)pointOnMobile.Y;

                //click the element
                await Task.Run(() => { mDriver.PerformTap(pointOnMobile_X, pointOnMobile_Y); });

                //update the screen
                if (mDeviceAutoScreenshotRefreshMode == eAutoScreenshotRefreshMode.PostOperation)
                {
                    RefreshDeviceScreenshotAsync(100);
                }
            }
            catch (Exception ex)
            {
                Reporter.ToUser(eUserMsgKey.StaticWarnMessage, string.Format("Failed to perform tap operation, error: '{0}'", ex.Message));
            }
        }

        private async void DeviceScreenshotImageMouseDragAsync(System.Windows.Point startPoint, System.Windows.Point endPoint)
        {
            try
            {
                //Perform drag
                await Task.Run(() => {
                    mDriver.PerformDrag(new System.Drawing.Point((int)startPoint.X, (int)startPoint.Y), new System.Drawing.Point((int)endPoint.X, (int)endPoint.Y));
                });

                //update the screen
                if (mDeviceAutoScreenshotRefreshMode == eAutoScreenshotRefreshMode.PostOperation)
                {
                    RefreshDeviceScreenshotAsync(100);
                }
            }
            catch (Exception ex)
            {
                Reporter.ToUser(eUserMsgKey.StaticWarnMessage, string.Format("Failed to perform drag operation, error: '{0}'", ex.Message));
            }
        }

        private void SetConfigurationsPanelView(bool show)
        {
            if (show == true)
            {
                xConfigurationsFrame.Visibility = System.Windows.Visibility.Visible;
                xConfigurationsSplitter.Visibility = System.Windows.Visibility.Visible;
                xConfigurationsCol.Width = new GridLength(220);
                this.Width = this.Width + Convert.ToDouble(xConfigurationsCol.Width.ToString());
                xConfigurationsBtn.ButtonStyle = FindResource("$ImageButtonStyle_Pink") as Style;
                xConfigurationsBtn.ToolTip = "Hide Configurations";
            }
            else
            {
                xConfigurationsFrame.Visibility = System.Windows.Visibility.Collapsed;
                xConfigurationsSplitter.Visibility = System.Windows.Visibility.Collapsed;
                this.Width = this.Width - xConfigurationsCol.ActualWidth;
                xConfigurationsCol.Width = new GridLength(0);
                xConfigurationsBtn.ButtonStyle = FindResource("$ImageButtonStyle_WhiteSmoke") as Style;
                xConfigurationsBtn.ToolTip = "Show Configurations";
            }
            mConfigIsOn = show;
        }

        private void DoSelfClose()
        {
            mSelfClosing = true;
            if (!mUserClosing)
            {
                this.Close();
            }
        }
        #endregion Functions


    }
}