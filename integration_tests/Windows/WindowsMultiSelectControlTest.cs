﻿//Licensed under the Apache License, Version 2.0 (the "License");
//you may not use this file except in compliance with the License.
//See the NOTICE file distributed with this work for additional
//information regarding copyright ownership.
//You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//See the License for the specific language governing permissions and
//limitations under the License.

using Appium.Integration.Tests.Helpers;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;
using System;
using System.Globalization;
using System.Threading;

namespace Appium.Integration.Tests.Windows
{
    public class WindowsMultiSelectControlTest
    {
        private WindowsDriver<WindowsElement> driver;
        protected const string WindowsApplicationDriverUrl = "http://127.0.0.1:4723/wd/hub";
        protected static WindowsDriver<WindowsElement> AlarmClockSession;
        protected static WindowsDriver<WindowsElement> DesktopSession;

        [OneTimeSetUp]
        public void Setup()
        {
            // Launch the AlarmClock app
            DesiredCapabilities appCapabilities = new DesiredCapabilities();
            appCapabilities.SetCapability("app", "Microsoft.WindowsAlarms_8wekyb3d8bbwe!App");
            AlarmClockSession =
                new WindowsDriver<WindowsElement>(new Uri(WindowsApplicationDriverUrl), appCapabilities);
            appCapabilities.SetCapability("platformName", "Windows");

            Assert.IsNotNull(AlarmClockSession);
            AlarmClockSession.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(2);

            // Create a session for Desktop
            DesiredCapabilities desktopCapabilities = new DesiredCapabilities();
            desktopCapabilities.SetCapability("app", "Root");
            DesktopSession =
                new WindowsDriver<WindowsElement>(new Uri(WindowsApplicationDriverUrl), desktopCapabilities);
            Assert.IsNotNull(DesktopSession);

            // Ensure app is started in the default main page
            ReturnToMainPage();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            ReturnToMainPage();
            SwitchToAlarmTab();

            // Delete all created alarms
            var alarmEntries = AlarmClockSession.FindElementsByName("Windows Application Driver Test Alarm");
            foreach (var alarmEntry in alarmEntries)
            {
                AlarmClockSession.Mouse.ContextClick(alarmEntry.Coordinates);
                AlarmClockSession.FindElementByName("Delete").Click();
            }

            AlarmClockSession.Dispose();
            AlarmClockSession = null;
        }

        [Test]
        public void FullMultiSelectScenario()
        {
            // Read the current local time
            SwitchToWorldClockTab();
            string localTimeText = ReadLocalTime();
            Assert.IsTrue(localTimeText.Length > 0);

            // Add an alarm at 1 minute after local time
            SwitchToAlarmTab();
            AddAlarm(localTimeText);
            var alarmEntries = AlarmClockSession.FindElementsByName("Windows Application Driver Test Alarm");
            Assert.IsTrue(alarmEntries.Count > 0);
        }

        public static void SwitchToAlarmTab()
        {
            AlarmClockSession.FindElementByAccessibilityId("AlarmPivotItem").Click();
        }

        public void SwitchToWorldClockTab()
        {
            AlarmClockSession.FindElementByAccessibilityId("WorldClockPivotItem").Click();
        }

        public string ReadLocalTime()
        {
            string localTimeText = "";
            AppiumWebElement worldClockPivotItem =
                AlarmClockSession.FindElementByAccessibilityId("WorldClockPivotItem");
            if (worldClockPivotItem != null)
            {
                localTimeText = worldClockPivotItem.FindElementByClassName("ClockCardItem").Text;
                var timeStrings = localTimeText.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries);

                foreach (string timeString in timeStrings)
                {
                    // Get the time. E.g. "11:32 AM" from "Local time, Monday, February 22, 2016, 11:32 AM, "
                    if (timeString.Contains(":"))
                    {
                        localTimeText = timeString;
                        break;
                    }
                }
            }

            return localTimeText;
        }

        public void AddAlarm(string timeText)
        {
            if (timeText.Length > 0)
            {
                // Create a test alarm 1 minute after the read local time
                DateTimeFormatInfo fi = CultureInfo.CurrentUICulture.DateTimeFormat;
                DateTime alarmTime = DateTime.Parse(timeText, fi);
                alarmTime = alarmTime.AddMinutes(1.0);
                string hourString = alarmTime.ToString("%h", fi);
                string minuteString = alarmTime.ToString("mm", fi);
                string period = alarmTime.ToString("tt", fi);

                AlarmClockSession.FindElementByAccessibilityId("AddAlarmButton").Click();
                AlarmClockSession.FindElementByAccessibilityId("AlarmNameTextBox").Clear();
                AlarmClockSession.FindElementByAccessibilityId("AlarmNameTextBox")
                    .SendKeys("Windows Application Driver Test Alarm");
                WindowsElement periodSelector = null;
                try
                {
                    periodSelector = AlarmClockSession.FindElementByAccessibilityId("PeriodSelector");
                }
                catch (NoSuchElementException)
                {
                    hourString = alarmTime.ToString("HH", fi);
                }
                periodSelector?.FindElementByName(period).Click();
                AlarmClockSession.FindElementByAccessibilityId("HourSelector").FindElementByName(hourString).Click();
                AlarmClockSession.FindElementByAccessibilityId("MinuteSelector").FindElementByName(minuteString)
                    .Click();
                Thread.Sleep(500);
                AlarmClockSession.FindElementByAccessibilityId("AlarmSaveButton").Click();
            }
        }

        public void DismissNotification()
        {
            try
            {
                AppiumWebElement newNotification = DesktopSession.FindElementByName("New notification");
                Assert.IsTrue(newNotification.FindElementByAccessibilityId("MessageText").Text
                    .Contains("Windows Application Driver Test Alarm"));
                newNotification.FindElementByName("Dismiss").Click();
            }
            catch
            {
            }
        }

        private static void ReturnToMainPage()
        {
            // Try to return to main page in case application is started in nested view
            try
            {
                AppiumWebElement backButton = null;
                do
                {
                    backButton = AlarmClockSession.FindElementByAccessibilityId("Back");
                    backButton.Click();
                } while (backButton != null);
            }
            catch
            {
            }
        }
    }
}