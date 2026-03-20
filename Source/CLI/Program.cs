using System.Runtime.InteropServices;
using System.Net.Http;
using System.Media;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Windows_Task_Dialog_Generator;
using Microsoft.Toolkit.Uwp.Notifications;

namespace Modern_Windows_Message_Box_Generator.CLI;

internal static partial class Program
{
    [LibraryImport("user32.dll", EntryPoint = "SendMessageW")]
    private static partial IntPtr SendMessage(IntPtr hWnd, uint Msg, UIntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    public struct FLASHWINFO
    {
        public uint cbSize;
        public IntPtr hwnd;
        public uint dwFlags;
        public uint uCount;
        public uint dwTimeout;
    }

    public const uint FLASHW_ALL = 3;
    public const uint FLASHW_TIMERNOFG = 12;

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool FlashWindowEx(ref FLASHWINFO pwfi);

    [LibraryImport("user32.dll")]
    private static partial IntPtr GetActiveWindow();

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetForegroundWindow(IntPtr hWnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_SHOWWINDOW = 0x0040;

    [STAThread]
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: --title \"...\" --message \"...\" [--type ok|okcancel|...] [--icon info|warning|...] [--timeout ms] [--flash] [--ding] [--toast]");
            return;
        }

        ApplicationConfiguration.Initialize();

        string title = "";
        string message = "";
        string type = "ok";
        string icon = "info";
        string heading = "";
        string footer = "";
        string details = "";
        string checkbox = "";
        int timeout = 0;
        bool useClassic = false;
        string callbackUrl = "";
        bool flash = false;
        bool useOVRToolkit = false;
        bool useDialog = false;

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i].ToLower();
            if (arg.StartsWith("--"))
            {
                var cmd = arg.Substring(2);
                switch (cmd)
                {
                    case "title": if (i + 1 < args.Length) title = args[++i]; break;
                    case "message": if (i + 1 < args.Length) message = args[++i]; break;
                    case "heading": if (i + 1 < args.Length) heading = args[++i]; break;
                    case "footer": if (i + 1 < args.Length) footer = args[++i]; break;
                    case "details": if (i + 1 < args.Length) details = args[++i]; break;
                    case "checkbox": if (i + 1 < args.Length) checkbox = args[++i]; break;
                    case "type": if (i + 1 < args.Length) type = args[++i].ToLower(); break;
                    case "icon": if (i + 1 < args.Length) icon = args[++i].ToLower(); break;
                    case "timeout": if (i + 1 < args.Length && int.TryParse(args[++i], out var t)) timeout = t; break;
                    case "classic": useClassic = true; break;
                    case "callback": if (i + 1 < args.Length) callbackUrl = args[++i]; break;
                    case "flash": flash = true; break;
                    case "ding": ding = true; break;
                    case "toast": useToast = true; break;
                    case "messagebox": useDialog = true; break;
                    case "xsoverlay": useXSOverlay = true; break;
                    case "ovrtoolkit": useOVRToolkit = true; break;
                }
            }
            else if (arg.StartsWith("-") || arg.StartsWith("/"))
            {
                var cmd = arg.Substring(1);
                switch (cmd)
                {
                    case "t": if (i + 1 < args.Length) title = args[++i]; break;
                    case "m": if (i + 1 < args.Length) message = args[++i]; break;
                    case "h": if (i + 1 < args.Length) heading = args[++i]; break;
                    case "f": if (i + 1 < args.Length) footer = args[++i]; break;
                    case "d": if (i + 1 < args.Length) details = args[++i]; break;
                    case "x": if (i + 1 < args.Length) checkbox = args[++i]; break;
                    case "y": if (i + 1 < args.Length) type = args[++i].ToLower(); break;
                    case "i": if (i + 1 < args.Length) icon = args[++i].ToLower(); break;
                    case "o": if (i + 1 < args.Length && int.TryParse(args[++i], out var t)) timeout = t; break;
                    case "c": useClassic = true; break;
                    case "cb": if (i + 1 < args.Length) callbackUrl = args[++i]; break;
                    case "fl": flash = true; break;
                    case "dg": ding = true; break;
                    case "ts": useToast = true; break;
                    case "mb": useDialog = true; break;
                    case "xs": useXSOverlay = true; break;
                    case "ov": useOVRToolkit = true; break;
                }
            }
        }

        if (!useToast && !useDialog) useDialog = true;

        string result = "None";
        int intResult = 0;
        bool checkboxChecked = false;
        long startTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        if (ding) SystemSounds.Exclamation.Play();
        if (useXSOverlay) SendXSOverlay(title, message, timeout);
        if (useOVRToolkit) SendOVRToolkit(title, message);

        if (useToast)
        {
            var sem = new SemaphoreSlim(0);
            string toastResult = "Dismissed";
            int toastIntResult = 2; // Default to Cancel/Dismissed

            var builder = new ToastContentBuilder()
                .AddText(string.IsNullOrEmpty(title) ? "Notification" : title)
                .AddText(message);

            if (!string.IsNullOrEmpty(heading)) builder.AddHeader("123", heading, "");
            
            switch (type)
            {
                case "okcancel": 
                    builder.AddButton(new ToastButton("OK", "ok"));
                    builder.AddButton(new ToastButton("Cancel", "cancel"));
                    break;
                case "yesno":
                    builder.AddButton(new ToastButton("Yes", "yes"));
                    builder.AddButton(new ToastButton("No", "no"));
                    break;
                case "yesnocancel":
                    builder.AddButton(new ToastButton("Yes", "yes"));
                    builder.AddButton(new ToastButton("No", "no"));
                    builder.AddButton(new ToastButton("Cancel", "cancel"));
                    break;
                case "retrycancel":
                    builder.AddButton(new ToastButton("Retry", "retry"));
                    builder.AddButton(new ToastButton("Cancel", "cancel"));
                    break;
                default:
                    builder.AddButton(new ToastButton("OK", "ok"));
                    break;
            }

            ToastNotificationManagerCompat.OnActivated += toastArgs =>
            {
                toastResult = toastArgs.Argument switch
                {
                    "ok" => "OK",
                    "cancel" => "Cancel",
                    "yes" => "Yes",
                    "no" => "No",
                    "retry" => "Retry",
                    _ => "Clicked"
                };
                toastIntResult = toastArgs.Argument switch
                {
                    "ok" => 1,
                    "cancel" => 2,
                    "yes" => 6,
                    "no" => 7,
                    "retry" => 4,
                    _ => 1
                };
                sem.Release();
            };

            builder.Show();

            if (!useDialog)
            {
                if (timeout > 0)
                {
                    if (!sem.Wait(timeout))
                    {
                        toastResult = "Timeout";
                        toastIntResult = 32000;
                    }
                }
                else
                {
                    sem.Wait();
                }

                result = toastResult;
                intResult = toastIntResult;
            }
        }

        if (useDialog)
        {
            if (useClassic)
        {
            MessageBoxButtons buttons = MessageBoxButtons.OK;
            switch (type)
            {
                case "okcancel": case "mb_okcancel": buttons = MessageBoxButtons.OKCancel; break;
                case "yesno": case "mb_yesno": buttons = MessageBoxButtons.YesNo; break;
                case "yesnocancel": case "mb_yesnocancel": buttons = MessageBoxButtons.YesNoCancel; break;
                case "retrycancel": case "mb_retrycancel": buttons = MessageBoxButtons.RetryCancel; break;
                case "abortretryignore": buttons = MessageBoxButtons.AbortRetryIgnore; break;
            }

            MessageBoxIcon msgBoxIcon = MessageBoxIcon.None;
            switch (icon)
            {
                case "info": case "information": case "mb_iconinformation": case "mb_iconasterisk": msgBoxIcon = MessageBoxIcon.Information; break;
                case "warning": case "mb_iconwarning": case "mb_iconexclamation": msgBoxIcon = MessageBoxIcon.Warning; break;
                case "error": case "mb_iconerror": case "mb_iconstop": case "mb_iconhand": msgBoxIcon = MessageBoxIcon.Error; break;
            }

            if (timeout > 0)
            {
                var timer = new System.Windows.Forms.Timer { Interval = timeout };
                timer.Tick += (s, e) => { 
                    timer.Stop(); 
                    long endTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    if (!string.IsNullOrEmpty(callbackUrl)) SendCallback(callbackUrl, "Timeout", 32000, false, "The dialog timed out.", checkbox, startTime, endTime);
                    Application.Exit(); Environment.Exit(0); 
                };
                timer.Start();
            }

            var frontTimer = new System.Windows.Forms.Timer { Interval = 100 };
            frontTimer.Tick += (s, e) => {
                frontTimer.Stop();
                var hwnd = GetActiveWindow();
                if (hwnd != IntPtr.Zero)
                {
                    SetForegroundWindow(hwnd);
                    SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                }
            };
            frontTimer.Start();

            if (flash)
            {
                var flashTimer = new System.Windows.Forms.Timer { Interval = 200 };
                flashTimer.Tick += (s, e) => {
                    flashTimer.Stop();
                    var hwnd = GetActiveWindow();
                    if (hwnd != IntPtr.Zero)
                    {
                        var info = new FLASHWINFO
                        {
                            cbSize = (uint)Marshal.SizeOf(typeof(FLASHWINFO)),
                            hwnd = hwnd,
                            dwFlags = FLASHW_ALL | FLASHW_TIMERNOFG,
                            uCount = uint.MaxValue,
                            dwTimeout = 0
                        };
                        FlashWindowEx(ref info);
                    }
                };
                flashTimer.Start();
            }

            var dialogResult = MessageBox.Show(message, string.IsNullOrEmpty(title) ? " " : title, buttons, msgBoxIcon);
            result = dialogResult.ToString();
            intResult = (int)dialogResult;
        }
        else
        {
            var page = new TaskDialogPage()
            {
                Caption = string.IsNullOrEmpty(title) ? " " : title,
                Heading = heading,
                Text = message,
                AllowCancel = true
            };

            if (!string.IsNullOrEmpty(footer)) page.Footnote = new TaskDialogFootnote { Text = footer };
            if (!string.IsNullOrEmpty(details)) page.Expander = new TaskDialogExpander { Text = details };
            if (!string.IsNullOrEmpty(checkbox)) page.Verification = new TaskDialogVerificationCheckBox { Text = checkbox };

            page.Created += (s, e) => {
                var hwnd = GetActiveWindow();
                if (hwnd != IntPtr.Zero)
                {
                    SetForegroundWindow(hwnd);
                    SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                }
            };

            if (flash)
            {
                page.Created += (s, e) => {
                    var flashTimer = new System.Windows.Forms.Timer { Interval = 100 };
                    flashTimer.Tick += (fs, fe) => {
                        flashTimer.Stop();
                        var hwnd = GetActiveWindow();
                        if (hwnd != IntPtr.Zero)
                        {
                            var info = new FLASHWINFO
                            {
                                cbSize = (uint)Marshal.SizeOf(typeof(FLASHWINFO)),
                                hwnd = hwnd,
                                dwFlags = FLASHW_ALL | FLASHW_TIMERNOFG,
                                uCount = uint.MaxValue,
                                dwTimeout = 0
                            };
                            FlashWindowEx(ref info);
                        }
                    };
                    flashTimer.Start();
                };
            }

            switch (type)
            {
                case "ok": page.Buttons.Add(TaskDialogButton.OK); break;
                case "okcancel": page.Buttons.Add(TaskDialogButton.OK); page.Buttons.Add(TaskDialogButton.Cancel); break;
                case "yesno": page.Buttons.Add(TaskDialogButton.Yes); page.Buttons.Add(TaskDialogButton.No); break;
                case "yesnocancel": page.Buttons.Add(TaskDialogButton.Yes); page.Buttons.Add(TaskDialogButton.No); page.Buttons.Add(TaskDialogButton.Cancel); break;
                case "retrycancel": page.Buttons.Add(TaskDialogButton.Retry); page.Buttons.Add(TaskDialogButton.Cancel); break;
                case "mb_ok": page.Buttons.Add(TaskDialogButton.OK); break;
                case "mb_okcancel": page.Buttons.Add(TaskDialogButton.OK); page.Buttons.Add(TaskDialogButton.Cancel); break;
                case "mb_yesno": page.Buttons.Add(TaskDialogButton.Yes); page.Buttons.Add(TaskDialogButton.No); break;
                case "mb_yesnocancel": page.Buttons.Add(TaskDialogButton.Yes); page.Buttons.Add(TaskDialogButton.No); page.Buttons.Add(TaskDialogButton.Cancel); break;
                case "mb_retrycancel": page.Buttons.Add(TaskDialogButton.Retry); page.Buttons.Add(TaskDialogButton.Cancel); break;
                default: page.Buttons.Add(TaskDialogButton.OK); break;
            }

            switch (icon)
            {
                case "info": case "information": case "mb_iconinformation": case "mb_iconasterisk": page.Icon = TaskDialogIcon.Information; break;
                case "warning": case "mb_iconwarning": case "mb_iconexclamation": page.Icon = TaskDialogIcon.Warning; break;
                case "error": case "mb_iconerror": case "mb_iconstop": case "mb_iconhand": page.Icon = TaskDialogIcon.Error; break;
                case "shield": page.Icon = TaskDialogIcon.Shield; break;
                case "shieldblue": page.Icon = TaskDialogIcon.ShieldBlueBar; break;
                case "shieldgray": page.Icon = TaskDialogIcon.ShieldGrayBar; break;
                case "shieldgreen": page.Icon = TaskDialogIcon.ShieldSuccessGreenBar; break;
                case "shieldyellow": page.Icon = TaskDialogIcon.ShieldWarningYellowBar; break;
                case "shieldred": page.Icon = TaskDialogIcon.ShieldErrorRedBar; break;
                default: page.Icon = TaskDialogIcon.None; break;
            }

            if (timeout > 0)
            {
                var timer = new System.Windows.Forms.Timer { Interval = timeout };
                timer.Tick += (s, e) => { 
                    timer.Stop(); 
                    long endTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    if (!string.IsNullOrEmpty(callbackUrl)) SendCallback(callbackUrl, "Timeout", 32000, false, "The dialog timed out.", checkbox, startTime, endTime);
                    Application.Exit(); 
                };
                timer.Start();
            }

            var button = TaskDialog.ShowDialog(page);
            result = button?.Text ?? "Cancel";
            checkboxChecked = page.Verification?.Checked ?? false;
            
            // Map TaskDialogButton to standard IDs
            if (button == TaskDialogButton.OK) intResult = 1;
            else if (button == TaskDialogButton.Cancel) intResult = 2;
            else if (button == TaskDialogButton.Abort) intResult = 3;
            else if (button == TaskDialogButton.Retry) intResult = 4;
            else if (button == TaskDialogButton.Ignore) intResult = 5;
            else if (button == TaskDialogButton.Yes) intResult = 6;
            else if (button == TaskDialogButton.No) intResult = 7;
            else if (button == TaskDialogButton.Close) intResult = 8;
            else if (button == TaskDialogButton.TryAgain) intResult = 10;
            else if (button == TaskDialogButton.Continue) intResult = 11;
            else intResult = 2; // Default to IDCANCEL if unknown
        }
    }

        long finalEndTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        string resultDesc = intResult switch
        {
            1 => "The OK button was selected.",
            2 => "The Cancel button was selected.",
            3 => "The Abort button was selected.",
            4 => "The Retry button was selected.",
            5 => "The Ignore button was selected.",
            6 => "The Yes button was selected.",
            7 => "The No button was selected.",
            8 => "The Close button was selected.",
            10 => "The Try Again button was selected.",
            11 => "The Continue button was selected.",
            32000 => "The dialog timed out.",
            _ => "The dialog was dismissed."
        };

        if (!string.IsNullOrEmpty(callbackUrl))
        {
            SendCallback(callbackUrl, result, intResult, checkboxChecked, resultDesc, checkbox, startTime, finalEndTime);
        }
    }

    private static void SendCallback(string url, string result, int intResult, bool checkboxChecked, string description, string checkboxText, long started, long finished)
    {
        try
        {
            var finalUrl = url.Replace("{return_value}", result)
                              .Replace("{return_value_string}", result)
                              .Replace("{ret_str}", result)
                              .Replace("{return_value_int}", intResult.ToString())
                              .Replace("{ret_int}", intResult.ToString())
                              .Replace("{return_description}", description)
                              .Replace("{ret_desc}", description)
                              .Replace("{checkbox_checked}", checkboxChecked.ToString().ToLower())
                              .Replace("{cb_checked}", checkboxChecked.ToString().ToLower())
                              .Replace("{checkbox_text}", checkboxText)
                              .Replace("{cb_text}", checkboxText)
                              .Replace("{time_started}", started.ToString())
                              .Replace("{time_finished}", finished.ToString());

            using var client = new HttpClient();
            _ = client.GetAsync(finalUrl).Result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Callback failed: {ex.Message}");
        }
    }

    private static void SendXSOverlay(string title, string message, int timeout)
    {
        try
        {
            var data = new
            {
                index = 0,
                timeout = (timeout > 0 ? timeout : 5000) / 1000.0,
                height = 175.0,
                opacity = 1.0,
                volume = 0.7,
                audioPath = "default",
                title = string.IsNullOrEmpty(title) ? "XSOverlay" : title,
                content = message,
                useBase64Icon = false,
                icon = "default",
                sourceApp = "msgbox.exe"
            };
            var json = JsonSerializer.Serialize(data);
            var buffer = System.Text.Encoding.UTF8.GetBytes(json);
            using var udp = new UdpClient();
            udp.Send(buffer, buffer.Length, new IPEndPoint(IPAddress.Loopback, 42069));
        }
        catch { }
    }

    private static void SendOVRToolkit(string title, string message)
    {
        try
        {
            var data = new
            {
                title = string.IsNullOrEmpty(title) ? "OVR Toolkit" : title,
                text = message
            };
            var json = JsonSerializer.Serialize(data);
            var buffer = System.Text.Encoding.UTF8.GetBytes(json);
            using var udp = new UdpClient();
            udp.Send(buffer, buffer.Length, new IPEndPoint(IPAddress.Loopback, 8077));
        }
        catch { }
    }
}
