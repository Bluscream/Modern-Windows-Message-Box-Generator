using System.Runtime.InteropServices;
using System.Net.Http;
using Windows_Task_Dialog_Generator;

namespace Modern_Windows_Message_Box_Generator.CLI;

internal static partial class Program
{
    [LibraryImport("user32.dll", EntryPoint = "SendMessageW")]
    private static partial IntPtr SendMessage(IntPtr hWnd, uint Msg, UIntPtr wParam, IntPtr lParam);

    [STAThread]
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: --title \"...\" --message \"...\" [--type ok|okcancel|yesno|...] [--icon info|warning|error|shield|...] [--timeout ms]");
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
                }
            }
            else if (arg.StartsWith("-") || arg.StartsWith("/"))
            {
                var cmd = arg.Substring(1);
                switch (cmd)
                {
                    case "title": case "t": if (i + 1 < args.Length) title = args[++i]; break;
                    case "message": case "m": if (i + 1 < args.Length) message = args[++i]; break;
                    case "heading": case "h": if (i + 1 < args.Length) heading = args[++i]; break;
                    case "footer": case "f": if (i + 1 < args.Length) footer = args[++i]; break;
                    case "details": case "d": if (i + 1 < args.Length) details = args[++i]; break;
                    case "checkbox": case "x": if (i + 1 < args.Length) checkbox = args[++i]; break;
                    case "type": if (i + 1 < args.Length) type = args[++i].ToLower(); break;
                    case "icon": case "i": if (i + 1 < args.Length) icon = args[++i].ToLower(); break;
                    case "timeout": if (i + 1 < args.Length && int.TryParse(args[++i], out var t)) timeout = t; break;
                    case "classic": case "c": useClassic = true; break;
                    case "callback": case "cb": if (i + 1 < args.Length) callbackUrl = args[++i]; break;
                }
            }
        }

        string result = "None";
        int intResult = 0;
        bool checkboxChecked = false;
        long startTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

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
}
