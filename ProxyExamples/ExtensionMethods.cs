﻿// <copyright file="ExtensionMethods.cs" company="Jim Evans">
// Copyright © 2018 Jim Evans
// Licensed under the Apache 2.0 license, as found in the LICENSE file accompanying this source code.
// </copyright>

namespace ProxyExamples
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using FryProxy.Writers;
    using OpenQA.Selenium;
    using WebDriverExampleUtilities;

    /// <summary>
    /// A class of extension methods for a WebDriver instance.
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// The default timeout for navigation.
        /// </summary>
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Navigates to a specified URL, returning the HTTP status code of the navigation.
        /// </summary>
        /// <param name="driver">The driver used to navigate to the URL.</param>
        /// <param name="targetUrl">The URL to navigate to.</param>
        /// <returns>The HTTP status code of the navigation.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", MessageId = "1#", Justification = "As a test sample project, specifying strings for URLs is okay.")]
        public static int NavigateTo(this IWebDriver driver, string targetUrl)
        {
            return NavigateTo(driver, targetUrl, DefaultTimeout);
        }

        /// <summary>
        /// Navigates to a specified URL, returning the HTTP status code of the navigation.
        /// </summary>
        /// <param name="driver">The driver used to navigate to the URL.</param>
        /// <param name="targetUrl">The URL to navigate to.</param>
        /// <param name="timeout">A <see cref="TimeSpan"/> structure for the time out of the navigation.</param>
        /// <returns>The HTTP status code of the navigation.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", MessageId = "1#", Justification = "As a test sample project, specifying strings for URLs is okay.")]
        public static int NavigateTo(this IWebDriver driver, string targetUrl, TimeSpan timeout)
        {
            return NavigateTo(driver, targetUrl, timeout, false);
        }

        /// <summary>
        /// Navigates to a specified URL, returning the HTTP status code of the navigation.
        /// </summary>
        /// <param name="driver">The driver used to navigate to the URL.</param>
        /// <param name="targetUrl">The URL to navigate to.</param>
        /// <param name="timeout">A <see cref="TimeSpan"/> structure for the time out of the navigation.</param>
        /// <param name="printDebugInfo"><see langword="true"/> to print debugging information to the console;
        /// otherwise, <see langword="false"/>.</param>
        /// <returns>The HTTP status code of the navigation.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", MessageId = "1#", Justification = "As a test sample project, specifying strings for URLs is okay.")]
        public static int NavigateTo(this IWebDriver driver, string targetUrl, TimeSpan timeout, bool printDebugInfo)
        {
            if (driver == null)
            {
                throw new ArgumentNullException("driver", "Driver cannot be null");
            }

            if (string.IsNullOrEmpty(targetUrl))
            {
                throw new ArgumentException("URL cannot be null or the empty string.", "targetUrl");
            }

            int responseCode = 0;
            DateTime endTime = DateTime.Now.Add(timeout);

            // Attach the event handler, perform the navigation, and wait for
            // the status code to be non-zero, or to timeout. Then detach the
            // event handler and return the response code.
            ProxyManager.Instance.Proxy.OnResponseSent = context =>
            {
                if (printDebugInfo)
                {
                    Console.WriteLine("DEBUG: Received response for resource with URL {0}", context.RequestHeader.RequestURI);
                }

                if (context.RequestHeader.RequestURI == targetUrl)
                {
                    responseCode = context.ResponseHeader.StatusCode;
                    if (printDebugInfo)
                    {
                        Console.WriteLine("DEBUG: Found response for {0}, setting response code.", context.RequestHeader.RequestURI);
                    }
                }
            };

            driver.Url = targetUrl;
            while (responseCode == 0 && DateTime.Now < endTime)
            {
                System.Threading.Thread.Sleep(100);
            }

            ProxyManager.Instance.Proxy.OnResponseSent = null;
            return responseCode;
        }

        /// <summary>
        /// Clicks on a link that is expected to navigate to a new URL, returning
        /// the HTTP status code of the navigation.
        /// </summary>
        /// <param name="element">The element clicked on to perform the navigation.</param>
        /// <returns>The HTTP status code of the navigation.</returns>
        public static int ClickNavigate(this IWebElement element)
        {
            return ClickNavigate(element, DefaultTimeout);
        }

        /// <summary>
        /// Clicks on a link that is expected to navigate to a new URL, returning
        /// the HTTP status code of the navigation.
        /// </summary>
        /// <param name="element">The element clicked on to perform the navigation.</param>
        /// <param name="timeout">A <see cref="TimeSpan"/> structure for the time out of the navigation.</param>
        /// <returns>The HTTP status code of the navigation.</returns>
        public static int ClickNavigate(this IWebElement element, TimeSpan timeout)
        {
            return ClickNavigate(element, timeout, false);
        }

        /// <summary>
        /// Clicks on a link that is expected to navigate to a new URL, returning
        /// the HTTP status code of the navigation.
        /// </summary>
        /// <param name="element">The element clicked on to perform the navigation.</param>
        /// <param name="timeout">A <see cref="TimeSpan"/> structure for the time out of the navigation.</param>
        /// <param name="printDebugInfo"><see langword="true"/> to print debugging information to the console;
        /// otherwise, <see langword="false"/>.</param>
        /// <returns>The HTTP status code of the navigation.</returns>
        public static int ClickNavigate(this IWebElement element, TimeSpan timeout, bool printDebugInfo)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element", "Element cannot be null.");
            }

            int responseCode = 0;
            string targetUrl = string.Empty;

            // Attach the event handler, perform the navigation, and wait for
            // the status code to be non-zero, or to timeout. Then detach the
            // event handler and return the response code.
            ProxyManager.Instance.Proxy.OnResponseSent = context =>
            {
                // For the first session of the click, the URL should be the URL 
                // requested by the the element click.
                if (string.IsNullOrEmpty(targetUrl))
                {
                    targetUrl = context.RequestHeader.RequestURI;
                    if (printDebugInfo)
                    {
                        Console.WriteLine("DEBUG: Element click navigating to {0}", targetUrl);
                    }
                }

                // This algorithm could be much more sophisticated based on your needs.
                // In our case, we'll only look for responses where the content type is
                // HTML, and that the URL of the session matches our current target URL
                // Note that we also only set the response code if it's not already been
                // set.
                if (context.ResponseHeader.EntityHeaders.ContentType.Contains("text/html") &&
                    context.RequestHeader.RequestURI == targetUrl &&
                    responseCode == 0)
                {
                    // If the response code is a redirect, get the URL of the redirect,
                    // so that we can look for the next response from the session for that
                    // URL.
                    if (context.ResponseHeader.StatusCode >= 300 && context.ResponseHeader.StatusCode < 400)
                    {
                        targetUrl = context.ResponseHeader.Location;
                        if (printDebugInfo)
                        {
                            Console.WriteLine("DEBUG: Navigation redirected with code of {0} from {1} to {2}", context.ResponseHeader.StatusCode, context.RequestHeader.RequestURI, targetUrl);
                        }
                    }
                    else
                    {
                        responseCode = context.ResponseHeader.StatusCode;
                        if (printDebugInfo)
                        {
                            Console.WriteLine("DEBUG: Got final status code of {0} for URL {1}", context.ResponseHeader.StatusCode, targetUrl);
                        }
                    }
                }
            };

            DateTime endTime = DateTime.Now.Add(timeout);
            targetUrl = element.GetAttribute("href");
            element.Click();
            while (responseCode == 0 && DateTime.Now < endTime)
            {
                System.Threading.Thread.Sleep(100);
            }

            ProxyManager.Instance.Proxy.OnResponseSent = null;
            return responseCode;
        }

        /// <summary>
        /// Navigates to a specified URL, injecting code to capture JavaScript errors on the page.
        /// </summary>
        /// <param name="driver">The driver used to navigate to the URL.</param>
        /// <param name="targetUrl">The URL to navigate to.</param>
        /// <exception cref="ArgumentNullException">Thrown if the driver instance or URL is null.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", MessageId = "1#", Justification = "As a test sample project, specifying strings for URLs is okay.")]
        public static void NavigateToWithErrorDetection(this IWebDriver driver, string targetUrl)
        {
            if (driver == null)
            {
                throw new ArgumentNullException("driver", "Driver cannot be null");
            }

            if (string.IsNullOrEmpty(targetUrl))
            {
                throw new ArgumentNullException("targetUrl", "URL cannot be null or empty string");
            }

            ProxyManager.Instance.Proxy.OnResponseReceived = context =>
            {
                if (context.RequestHeader.RequestURI == targetUrl &&
                    context.ResponseHeader.EntityHeaders.ContentType.Contains("html"))
                {
                    byte[] buffer;
                    using (var stream = new MemoryStream())
                    {
                        new HttpMessageWriter(stream).Write(context.ResponseHeader, context.ServerStream);
                        buffer = stream.ToArray();
                    }
                    string headerString = context.ResponseHeader.ToString();
                    string responseBody = Encoding.UTF8.GetString(buffer);
                    responseBody = responseBody.Remove(0, headerString.Length);

                    string errorScript = "window.__webdriver_javascript_errors = []; window.onerror = function(errorMsg, url, line) { window.__webdriver_javascript_errors.push(errorMsg + ' (found at ' + url + ', line ' + line + ')'); };";
                    Regex regex = new Regex("<head.*>", RegexOptions.IgnoreCase);
                    Match match = regex.Match(responseBody);
                    string replacement = match.Value + "<script>" + errorScript + "</script>";
                    string newBody = regex.Replace(responseBody, replacement, 1);
                    byte[] responseBuffer = Encoding.UTF8.GetBytes(newBody);
                    using (var stream = new MemoryStream(responseBuffer))
                    {
                        // Must reset the Content-Length header to the appropriate new value.
                        context.ResponseHeader.EntityHeaders.ContentLength = stream.Length;
                        new HttpMessageWriter(context.ClientStream).Write(context.ResponseHeader, stream, stream.Length);
                        context.ClientStream.Flush();
                    }
                }
            };

            driver.Url = targetUrl;
            ProxyManager.Instance.Proxy.OnResponseReceived = null;
        }

        /// <summary>
        /// Gets the JavaScript errors on the current page.
        /// </summary>
        /// <param name="driver">The driver used to retrieve the errors.</param>
        /// <returns>A list of all JavaScript errors captured on the page.</returns>
        public static IList<string> GetJavaScriptErrors(this IWebDriver driver)
        {
            return GetJavaScriptErrors(driver, DefaultTimeout);
        }

        /// <summary>
        /// Gets the JavaScript errors on the current page.
        /// </summary>
        /// <param name="driver">The driver used to retrieve the errors.</param>
        /// <param name="timeout">A <see cref="TimeSpan"/> structure for the time out of the retrieval.</param>
        /// <returns>A list of all JavaScript errors captured on the page.</returns>
        public static IList<string> GetJavaScriptErrors(this IWebDriver driver, TimeSpan timeout)
        {
            string errorRetrievalScript = "var errorList = window.__webdriver_javascript_errors; window.__webdriver_javascript_errors = []; return errorList;";
            DateTime endTime = DateTime.Now.Add(timeout);
            List<string> errorList = new List<string>();
            IJavaScriptExecutor executor = driver as IJavaScriptExecutor;
            ReadOnlyCollection<object> returnedList = executor.ExecuteScript(errorRetrievalScript) as ReadOnlyCollection<object>;
            while (returnedList == null && DateTime.Now < endTime)
            {
                System.Threading.Thread.Sleep(250);
                returnedList = executor.ExecuteScript(errorRetrievalScript) as ReadOnlyCollection<object>;
            }

            if (returnedList == null)
            {
                return null;
            }
            else
            {
                foreach (object returnedError in returnedList)
                {
                    errorList.Add(returnedError.ToString());
                }
            }

            return errorList;
        }
    }
}
