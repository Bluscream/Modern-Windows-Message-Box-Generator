using System.Runtime.InteropServices;
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
        int timeout = 0;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "--title": if (i + 1 < args.Length) title = args[++i]; break;
                case "--message": if (i + 1 < args.Length) message = args[++i]; break;
                case "--type": if (i + 1 < args.Length) type = args[++i].ToLower(); break;
                case "--icon": if (i + 1 < args.Length) icon = args[++i].ToLower(); break;
                case "--timeout": if (i + 1 < args.Length && int.TryParse(args[++i], out var t)) timeout = t; break;
            }
        }

        var page = new TaskDialogPage()
        {
            Caption = string.IsNullOrEmpty(title) ? " " : title,
            Heading = "",
            Text = message,
            AllowCancel = true
        };

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
            timer.Tick += (s, e) => { timer.Stop(); Application.Exit(); };
            timer.Start();
        }

        TaskDialog.ShowDialog(page);
    }
}
