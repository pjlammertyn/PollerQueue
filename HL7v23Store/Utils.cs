using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HL7v23Store
{
    internal static class Utils
    {
        /// <summary>
        /// Wraps sharing violations that could occur on a file IO operation.
        /// </summary>
        /// <param name="action">The action to execute. May not be null.</param>
        /// <param name="exceptionsCallback">The exceptions callback. May be null.</param>
        /// <param name="retryCount">The retry count.</param>
        /// <param name="waitTime">The wait time in milliseconds.</param>
        internal static async Task WrapSharingViolations(this Func<Task> action, int retryCount = 10, int waitTime = 100, WrapSharingViolationsExceptionsCallback exceptionsCallback = null)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            for (int i = 0; i < retryCount; i++)
            {   
                ExceptionDispatchInfo capturedException = null;
                try
                {
                    await Task.Run(action).ConfigureAwait(false);
                    return;
                }
                catch (IOException ioe)
                {
                    capturedException = ExceptionDispatchInfo.Capture(ioe);
                }
                if (capturedException != null)
                {
                    if ((IsSharingViolation(capturedException.SourceException as IOException)) && (i < (retryCount - 1)))
                    {
                        var wait = true;
                        if (exceptionsCallback != null)
                            wait = exceptionsCallback(capturedException.SourceException as IOException, i, retryCount, waitTime);
                        if (wait)
                            await Task.Delay(waitTime);
                    }
                    else
                        capturedException.Throw();
                }
            }
        }

        /// <summary>
        /// Defines a sharing violation wrapper delegate for handling exception.
        /// </summary>
        internal delegate bool WrapSharingViolationsExceptionsCallback(IOException ioe, int retry, int retryCount, int waitTime);

        /// <summary>
        /// Determines whether the specified exception is a sharing violation exception.
        /// </summary>
        /// <param name="exception">The exception. May not be null.</param>
        /// <returns>
        ///     <c>true</c> if the specified exception is a sharing violation exception; otherwise, <c>false</c>.
        /// </returns>
        internal static bool IsSharingViolation(IOException exception)
        {
            if (exception == null)
                throw new ArgumentNullException("exception");

            int hr = GetHResult(exception, 0);
            return (hr == -2147024864); // 0x80070020 ERROR_SHARING_VIOLATION

        }

        /// <summary>
        /// Gets the HRESULT of the specified exception.
        /// </summary>
        /// <param name="exception">The exception to test. May not be null.</param>
        /// <param name="defaultValue">The default value in case of an error.</param>
        /// <returns>The HRESULT value.</returns>
        internal static int GetHResult(IOException exception, int defaultValue)
        {
            if (exception == null)
                throw new ArgumentNullException("exception");

            try
            {
                return (int)exception.GetType().GetProperty("HResult",
                    BindingFlags.NonPublic | BindingFlags.Instance).GetValue(exception, null);
            }
            catch
            {
                return defaultValue;
            }
        }

        internal static V Maybe<T, V>(this T t, Func<T, V> selector)
        {
            return t != null ? selector(t) : default(V);
        }

        internal static DateTime ToDatetime(this string value, string format, DateTime defaultDate)
        {
            if (string.IsNullOrWhiteSpace(value))
                return defaultDate;

            DateTime ret;
            if (DateTime.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out ret))
                return ret;

            return defaultDate;
        }

        internal static decimal ToDecimal(this string value)
        {
            decimal ret = decimal.Zero;

            if (string.IsNullOrWhiteSpace(value))
                return ret;

            if (decimal.TryParse(value, out ret))
                return ret;

            return ret;
        }

        internal static bool IsNullOrEmpty(this string value)
        {
            return string.IsNullOrEmpty(value);
        }

        internal static int ToInt32(this string value)
        {
            Int32 result = 0;

            if (!value.IsNullOrEmpty())
                Int32.TryParse(value, out result);

            return result;
        }
    }
}
