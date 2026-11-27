using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

class Program
{
    delegate bool Cb(IntPtr h, IntPtr l);
    [DllImport("user32.dll")] static extern bool EnumWindows(Cb cb, IntPtr l);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr l);
    [DllImport("user32.dll")] static extern bool IsWindow(IntPtr h);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetWindowText(IntPtr h, StringBuilder sb, int max);
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr h, int cmd);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h, int[] r);
    [DllImport("user32.dll")] static extern bool GetCursorPos(out Point p);
    [DllImport("user32.dll")] static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] static extern short GetAsyncKeyState(int vk);
    [DllImport("user32.dll")] static extern void mouse_event(uint f, uint dx, uint dy, uint d, UIntPtr e);
    [DllImport("user32.dll")] static extern void     keybd_event(byte vk, byte sc, uint f, UIntPtr e);

    static void Activate(IntPtr h)
    {
        ShowWindow(h, 9);
        keybd_event(0x12, 0, 0, UIntPtr.Zero);
        keybd_event(0x12, 0, 2, UIntPtr.Zero);
        SetForegroundWindow(h);
    }

    static void Run(IntPtr h, int max, int delay, CancellationToken t)
    {
        Activate(h);
        if (t.WaitHandle.WaitOne(200)) return;

        var r = new int[4];
        GetWindowRect(h, r);
        GetCursorPos(out Point p);
        if (p.X < r[0] || p.X > r[2] || p.Y < r[1] || p.Y > r[3])
        {
            SetCursorPos((r[0] + r[2]) / 2, (r[1] + r[3]) / 2);
        }
        mouse_event(2, 0, 0, 0, UIntPtr.Zero);
        mouse_event(4, 0, 0, 0, UIntPtr.Zero);
        if (t.WaitHandle.WaitOne(300)) return;
        for (int i=1;i<=max;i++)
        {
            if (!IsWindow(h)) { Console.WriteLine("window closed"); return; }
            if (GetForegroundWindow() != h) { Activate(h); Thread.Sleep(150); }

            SendKeys.SendWait(i + "{ENTER}");
            Console.WriteLine(i);

            if (t.WaitHandle.WaitOne(delay)) return;
        }
        Console.WriteLine("counted up to " + max);
    }

    [STAThread]
    static void Main() 
    {
        var wins = new List<(IntPtr h, string title)>();
        EnumWindows((h, l) =>
        {
            var sb = new StringBuilder(256);
            GetWindowText(h, sb, 256);
            if (IsWindowVisible(h) && sb.Length > 0) wins.Add((h, sb.ToString()));
            return true;
        }, IntPtr.Zero);


        for (int i = 0; i < wins.Count; i++)
        {
            Console.WriteLine($"{i}: {wins[i].title}");
        }

        Console.Write("window number: ");
        var target = wins[int.Parse(Console.ReadLine()!)].h;
        Console.Write("Count up to:");
        int max = int.Parse(Console.ReadLine()!);
        Console.Write("Delay, ms:");
        int delay = int.Parse(Console.ReadLine()!);
        Console.WriteLine("move the cursor to the input field in the selected window\nESC - start/stop");

        CancellationTokenSource? cts = null;
        Thread? worker = null;
        bool was = false;

        while (true)
        {
            bool now = (GetAsyncKeyState(0x1B) & 0x8000) != 0;
            if (now && !was)
            {
                if (worker != null && worker.IsAlive) { cts!.Cancel(); Console.WriteLine("stopped"); }
                else
                {
                    cts = new CancellationTokenSource();
                    var tok = cts.Token;
                    worker = new Thread(() => Run(target, max, delay, tok));
                    worker.SetApartmentState(ApartmentState.STA);
                    worker.IsBackground = true;
                    worker.Start();
                }
            }
            was = now;
            Thread.Sleep(20);
        }
    }
}