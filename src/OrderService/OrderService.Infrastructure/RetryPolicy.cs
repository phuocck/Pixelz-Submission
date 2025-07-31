using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderService.Infrastructure
{
    public static class RetryPolicy
    {
        public static async Task RetryAsync(
        Func<Task> action,
        int maxRetries = 3,
        TimeSpan? delay = null,
        string? operationName = null)
        {
            delay ??= TimeSpan.FromMilliseconds(300);

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    await action();
                    return; // success
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    Console.WriteLine($"[Retry] {operationName ?? "Operation"} failed on attempt {attempt}. Retrying... Exception: {ex.Message}");
                    await Task.Delay(delay.Value);
                }
            }

            // Final attempt without catch
            await action(); // let exception bubble
        }

        public static async Task<T> RetryAsync<T>(
            Func<Task<T>> func,
            int maxRetries = 3,
            TimeSpan? delay = null,
            string? operationName = null)
        {
            delay ??= TimeSpan.FromMilliseconds(300);

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    return await func();
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    Console.WriteLine($"[Retry] {operationName ?? "Operation"} failed on attempt {attempt}. Retrying... Exception: {ex.Message}");
                    await Task.Delay(delay.Value);
                }
            }

            return await func(); // last chance (no catch)
        }
    }
}
