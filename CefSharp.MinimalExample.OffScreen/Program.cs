﻿// Copyright © 2010-2021 The CefSharp Authors. All rights reserved.
//
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.

using CefSharp.MinimalExample.OffScreen.Extensions;
using CefSharp.MinimalExample.OffScreen.Models;
using CefSharp.OffScreen;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CefSharp.MinimalExample.OffScreen
{
    /// <summary>
    /// CefSharp.OffScreen Minimal Example
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Asynchronous demo using CefSharp.OffScreen
        /// Loads google.com, uses javascript to fill out the search box then takes a screenshot which is opened
        /// in the default image viewer.
        /// For a synchronous demo see <see cref="MainSync(string[])"/> below.
        /// </summary>
        /// <param name="args">args</param>
        /// <returns>exit code</returns>
        public static int Main(string[] args)
        {
#if ANYCPU
            //Only required for PlatformTarget of AnyCPU
            CefRuntime.SubscribeAnyCpuAssemblyResolver();
#endif

            const string testUrl = "https://www.google.com/";

            Console.WriteLine("This example application will load {0}, take a screenshot, and save it to your desktop.", testUrl);
            Console.WriteLine("You may see Chromium debugging output, please wait...");
            Console.WriteLine();

            //Console apps don't have a SynchronizationContext, so to ensure our await calls continue on the main thread we use a super simple implementation from
            //https://devblogs.microsoft.com/pfxteam/await-synchronizationcontext-and-console-apps/
            //Continuations will happen on the main thread. Cef.Initialize/Cef.Shutdown must be called on the same Thread.
            //The Nito.AsyncEx.Context Nuget package has a more advanced implementation
            //should you wish to use a pre-build implementation.
            //https://github.com/StephenCleary/AsyncEx/blob/8a73d0467d40ca41f9f9cf827c7a35702243abb8/doc/AsyncContext.md#console-example-using-asynccontext
            //NOTE: This is only required if you use await

            AsyncContext.Run(async delegate
            {
                var settings = new CefSettings()
                {
                    //By default CefSharp will use an in-memory cache, you need to specify a Cache Folder to persist data
                    CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CefSharp\\Cache")
                };

                //Perform dependency check to make sure all relevant resources are in our output directory.
                var success = await Cef.InitializeAsync(settings, performDependencyCheck: true, browserProcessHandler: null);

                if (!success)
                {
                    throw new Exception("Unable to initialize CEF, check the log file.");
                }

                // Create the CefSharp.OffScreen.ChromiumWebBrowser instance
                using (var browser = new ChromiumWebBrowser("https://www.cars.com/profile"))
                {
                    var initialLoadResponse = await browser.WaitForInitialLoadAsync();

                    if (!initialLoadResponse.Success)
                    {
                        throw new Exception(string.Format("Page load failed with ErrorCode:{0}, HttpStatusCode:{1}", initialLoadResponse.ErrorCode, initialLoadResponse.HttpStatusCode));
                    }

                    await browser.CarsComCheckWhetherSignedInOrNotAsync();

                    var result = new List<ParentModel>();
                    result.Add(await browser.CarsComGetParentAsync(new SearchParametersModel
                    {
                        Car = "used",
                        Make = "toyota",
                        Model = "toyota-corolla",
                        MaxPrice = "100000",
                        Distance = "all",
                        Zip = "94596"
                    }));
                    result.Add(await browser.CarsComGetParentAsync(new SearchParametersModel
                    {
                        Car = "used",
                        Make = "toyota",
                        Model = "toyota-corolla_hybrid",
                        MaxPrice = "100000",
                        Distance = "all",
                        Zip = "94596"
                    }));

                    var jsonString = JsonConvert.SerializeObject(result);

                    // File path to save our screenshot e.g. C:\Users\{username}\Desktop\CefSharp screenshot.png
                    var jsonPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "result.json");

                    Console.WriteLine();
                    Console.WriteLine("Json ready. Saving to {0}", jsonPath);

                    File.WriteAllText(jsonPath, jsonString);

                    Console.WriteLine("Json saved. Launching your default json viewer...");

                    // Tell Windows to launch the saved image.
                    Process.Start(new ProcessStartInfo(jsonPath)
                    {
                        // UseShellExecute is false by default on .NET Core.
                        UseShellExecute = true
                    });
                }

                // Wait for user to press a key before exit
                Console.ReadKey();

                // Clean up Chromium objects. You need to call this in your application otherwise
                // you will get a crash when closing.
                Cef.Shutdown();
            });

            return 0;
        }

        /// <summary>
        /// Synchronous demo using CefSharp.OffScreen
        /// Loads google.com, uses javascript to fill out the search box then takes a screenshot which is opened
        /// in the default image viewer.
        /// For a asynchronous demo see <see cref="Main(string[])"/> above.
        /// To use this demo simply delete the <see cref="Main(string[])"/> method and rename this method to Main.
        /// </summary>
        /// <param name="args">args</param>
        /// <returns>exit code</returns>
        public static int MainSync(string[] args)
        {
#if ANYCPU
            //Only required for PlatformTarget of AnyCPU
            CefRuntime.SubscribeAnyCpuAssemblyResolver();
#endif

            const string testUrl = "https://www.google.com/";

            Console.WriteLine("This example application will load {0}, take a screenshot, and save it to your desktop.", testUrl);
            Console.WriteLine("You may see Chromium debugging output, please wait...");
            Console.WriteLine();

            var settings = new CefSettings()
            {
                //By default CefSharp will use an in-memory cache, you need to specify a Cache Folder to persist data
                CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CefSharp\\Cache")
            };

            //Perform dependency check to make sure all relevant resources are in our output directory.
            Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);

            // Create the offscreen Chromium browser.
            var browser = new ChromiumWebBrowser(testUrl);

            EventHandler<LoadingStateChangedEventArgs> handler = null;

            handler = (s, e) =>
            {
                // Check to see if loading is complete - this event is called twice, one when loading starts
                // second time when it's finished
                if (!e.IsLoading)
                {
                    // Remove the load event handler, because we only want one snapshot of the page.
                    browser.LoadingStateChanged -= handler;

                    var scriptTask = browser.EvaluateScriptAsync("document.querySelector('[name=q]').value = 'CefSharp Was Here!'");

                    scriptTask.ContinueWith(t =>
                    {
                        if(!t.Result.Success)
                        {
                            throw new Exception("EvaluateScriptAsync failed:" + t.Result.Message);
                        }

                        //Give the browser a little time to render
                        Thread.Sleep(500);
                        // Wait for the screenshot to be taken.
                        var task = browser.CaptureScreenshotAsync();
                        task.ContinueWith(x =>
                        {
                            // File path to save our screenshot e.g. C:\Users\{username}\Desktop\CefSharp screenshot.png
                            var screenshotPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "CefSharp screenshot.png");

                            Console.WriteLine();
                            Console.WriteLine("Screenshot ready. Saving to {0}", screenshotPath);

                            var bitmapAsByteArray = x.Result;

                            // Save the Bitmap to the path.
                            File.WriteAllBytes(screenshotPath, bitmapAsByteArray);

                            Console.WriteLine("Screenshot saved.  Launching your default image viewer...");

                            // Tell Windows to launch the saved image.
                            Process.Start(new ProcessStartInfo(screenshotPath)
                            {
                                // UseShellExecute is false by default on .NET Core.
                                UseShellExecute = true
                            });

                            Console.WriteLine("Image viewer launched.  Press any key to exit.");
                        }, TaskScheduler.Default);
                    });
                }
            };

            // An event that is fired when the first page is finished loading.
            // This returns to us from another thread.
            browser.LoadingStateChanged += handler;

            // We have to wait for something, otherwise the process will exit too soon.
            Console.ReadKey();

            // Clean up Chromium objects. You need to call this in your application otherwise
            // you will get a crash when closing.
            //The ChromiumWebBrowser instance will be disposed
            Cef.Shutdown();

            return 0;
        }
    }
}
