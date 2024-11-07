using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace TextSpammer
{
    class Program
    {
        // Constants for the low-level keyboard hook
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;

        // Virtual key codes
        private const int VK_RETURN = 0x0D;  // Enter key
        private const int VK_ESCAPE = 0x1B;  // Escape key

        // Delegate and hook handle
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        // Flags to control spamming
        private static volatile bool _isSpamming = false;
        private static volatile bool _stopSpamming = false;

        // Spamming thread
        private static Thread spamThread;

        // User input storage for spamming
        private static string _spamMessage;
        private static int _spamCount;
        private static int _delay;

        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Text Spammer\n");

                // Collect user inputs
                Console.Write("Enter the message you want to spam: ");
                _spamMessage = Console.ReadLine();

                Console.Write("Enter the number of times to spam: ");
                while (!int.TryParse(Console.ReadLine(), out _spamCount) || _spamCount < 1)
                {
                    Console.WriteLine("Enter a positive number.");
                    Console.Write("Enter the number of times to spam: ");
                }

                Console.Write("Enter delay between messages in milliseconds: ");
                while (!int.TryParse(Console.ReadLine(), out _delay) || _delay < 0)
                {
                    Console.WriteLine("Enter a positive number.");
                    Console.Write("Enter delay between messages in milliseconds: ");
                }

                Console.WriteLine("\nInstructions:");
                Console.WriteLine("- Press 'Enter' to start spamming.");
                Console.WriteLine("- Press 'Escape' to stop spamming.");
                Console.WriteLine("- Ensure the target application is focused when spamming starts.\n");

                // Set the global keyboard hook
                _hookID = SetHook(_proc);

                Console.WriteLine("Text Spammer is running. Waiting for key presses...");

                // Keep the main thread alive to listen for key presses
                Application.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred: {ex.Message}");
            }
            finally
            {
                // Ensure the hook is uninstalled when the application exits
                if (_hookID != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(_hookID);
                }
            }
        }

        // Spamming logic
        static void StartSpamming(string message, int count, int delay)
        {
            _isSpamming = true;
            _stopSpamming = false;

            Console.WriteLine("Spamming started. Press 'Escape' to stop.");

            for (int i = 0; i < count; i++)
            {
                if (_stopSpamming)
                {
                    Console.WriteLine("\nSpamming stopped by user.");
                    break;
                }

                SpamMessage(message);
                Console.WriteLine($"Message {i + 1}/{count} sent.");
                Thread.Sleep(delay);
            }

            if (!_stopSpamming)
            {
                Console.WriteLine("Spamming completed.");
            }

            _isSpamming = false;
        }

        // Sends the spam message followed by an Enter key press.
        static void SpamMessage(string message)
        {
            // Send the message
            SendKeys.SendWait(message);
            // Send the Enter key to send the message
            SendKeys.SendWait("{ENTER}");
        }

        // Sets the low-level keyboard hook.
        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        // Hook callback method to process key events.
        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);

                if (vkCode == VK_RETURN)
                {
                    // Start spamming when Enter is pressed
                    if (!_isSpamming)
                    {
                        Console.WriteLine("Enter key pressed. Starting spamming.");
                        spamThread = new Thread(() => StartSpamming(
                            _spamMessage, _spamCount, _delay));
                        spamThread.Start();
                    }
                }
                else if (vkCode == VK_ESCAPE)
                {
                    // Stop spamming when Escape is pressed
                    if (_isSpamming)
                    {
                        Console.WriteLine("\nEscape key pressed. Stopping spamming.");
                        _stopSpamming = true;
                    }
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        // Windows API functions
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
