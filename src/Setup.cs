using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("Welcome Korea Setup")]
[assembly: AssemblyProduct("Welcome Korea")]
[assembly: AssemblyVersion("1.8.0.0")]

namespace WelcomeKoreaSetup
{
    public sealed class Installer
    {
        public readonly string Root, Menu;
        readonly bool register;
        const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\WelcomeKorea";
        const string Marker = "Welcome Korea installation v1";
        public Installer(string root, string menu, bool registerWindows)
        {
            Root = Path.GetFullPath(root); Menu = Path.GetFullPath(menu); register = registerWindows;
            if (Root == Path.GetPathRoot(Root) || Menu == Path.GetPathRoot(Menu)) throw new ArgumentException("An app-specific installation folder is required.");
        }
        public static Installer ForUser()
        {
            return new Installer(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "WelcomeKorea"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Welcome Korea"), true);
        }
        static void WriteAtomic(string path, Stream input)
        {
            string temporary = path + ".new";
            using (FileStream output = File.Create(temporary)) input.CopyTo(output);
            if (File.Exists(path)) File.Replace(temporary, path, null); else File.Move(temporary, path);
        }
        void CheckOwnership()
        {
            if (!Directory.Exists(Root) || Directory.GetFileSystemEntries(Root).Length == 0) return;
            string marker = Path.Combine(Root, "installed.txt");
            if (!File.Exists(marker) || File.ReadAllText(marker) != Marker) throw new IOException("The target folder contains files from another source. No files were changed: " + Root);
        }
        public void Install()
        {
            CheckOwnership();
            foreach (Process p in Process.GetProcessesByName("WelcomeKorea"))
            {
                using (p)
                {
                    try { if (String.Equals(p.MainModule.FileName, Path.Combine(Root, "WelcomeKorea.exe"), StringComparison.OrdinalIgnoreCase)) throw new IOException("Please close Welcome Korea before updating it."); }
                    catch (System.ComponentModel.Win32Exception) { }
                }
            }
            Directory.CreateDirectory(Root);
            File.WriteAllText(Path.Combine(Root, "installed.txt"), Marker);
            using (Stream input = typeof(Installer).Assembly.GetManifestResourceStream("WelcomeKorea.exe"))
            {
                if (input == null) throw new InvalidDataException("The application payload is missing.");
                WriteAtomic(Path.Combine(Root, "WelcomeKorea.exe"), input);
            }
            string self = typeof(Installer).Assembly.Location;
            string setup = Path.Combine(Root, "WelcomeKoreaSetup.exe");
            if (!String.Equals(self, setup, StringComparison.OrdinalIgnoreCase)) using (Stream input = File.OpenRead(self)) WriteAtomic(setup, input);
            Directory.CreateDirectory(Menu);
            CreateShortcut(Path.Combine(Menu, "Welcome Korea.lnk"), Path.Combine(Root, "WelcomeKorea.exe"), "");
            CreateShortcut(Path.Combine(Menu, "Uninstall Welcome Korea.lnk"), setup, "--uninstall");
            if (register)
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(KeyPath))
                {
                    key.SetValue("DisplayName", "Welcome Korea"); key.SetValue("DisplayVersion", "1.8.0"); key.SetValue("Publisher", "Welcome Korea Project");
                    key.SetValue("InstallLocation", Root); key.SetValue("DisplayIcon", Path.Combine(Root, "WelcomeKorea.exe"));
                    key.SetValue("UninstallString", "\"" + setup + "\" --uninstall");
                    key.SetValue("NoModify", 1, RegistryValueKind.DWord); key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                }
            }
        }
        public void Remove()
        {
            if (!Directory.Exists(Root)) return;
            CheckOwnership();
            // Delete only named app files. Preserve unknown files and all saved places.
            File.Delete(Path.Combine(Root, "WelcomeKorea.exe"));
            string setup = Path.Combine(Root, "WelcomeKoreaSetup.exe");
            for (int i = 0; ; i++)
            {
                try { File.Delete(setup); break; }
                catch (IOException) { if (i >= 25) throw; Thread.Sleep(100); }
            }
            File.Delete(Path.Combine(Root, "WelcomeKorea.exe.new")); File.Delete(Path.Combine(Root, "WelcomeKoreaSetup.exe.new"));
            File.Delete(Path.Combine(Menu, "Welcome Korea.lnk")); File.Delete(Path.Combine(Menu, "Uninstall Welcome Korea.lnk"));
            File.Delete(Path.Combine(Root, "installed.txt"));
            if (Directory.Exists(Menu) && Directory.GetFileSystemEntries(Menu).Length == 0) Directory.Delete(Menu);
            if (Directory.GetFileSystemEntries(Root).Length == 0) Directory.Delete(Root);
            if (register) Registry.CurrentUser.DeleteSubKeyTree(KeyPath, false);
        }
        public static void CreateShortcut(string path, string target, string arguments)
        {
            Type type = Type.GetTypeFromProgID("WScript.Shell"); object shell = Activator.CreateInstance(type); object link = null;
            try
            {
                link = type.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { path }); Type lt = link.GetType();
                lt.InvokeMember("TargetPath", BindingFlags.SetProperty, null, link, new object[] { target });
                lt.InvokeMember("Arguments", BindingFlags.SetProperty, null, link, new object[] { arguments });
                lt.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, link, new object[] { Path.GetDirectoryName(target) });
                lt.InvokeMember("Save", BindingFlags.InvokeMethod, null, link, null);
            }
            finally { if (link != null) Marshal.FinalReleaseComObject(link); Marshal.FinalReleaseComObject(shell); }
        }
    }
    sealed class SetupWindow : Form
    {
        readonly Installer installer = Installer.ForUser();
        readonly Label status = new Label(); readonly Button install = new Button(), remove = new Button(), launch = new Button();
        public SetupWindow()
        {
            Text = "Welcome Korea Setup"; Font = new Font("Segoe UI", 10); AutoScaleMode = AutoScaleMode.Dpi; ClientSize = new Size(600, 390); FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; StartPosition = FormStartPosition.CenterScreen; BackColor = Color.FromArgb(244, 247, 246);
            Controls.Add(new Label { Text = "WELCOME KOREA", Font = new Font("Segoe UI", 25, FontStyle.Bold), ForeColor = Color.FromArgb(24,49,58), Location = new Point(28, 27), Size = new Size(545, 52) });
            Controls.Add(new Label { Text = "Your first day, made easier.\nWindows desktop preview 1.8 · Journey planner", ForeColor = Color.FromArgb(91,108,115), Location = new Point(30, 90), Size = new Size(535, 55) });
            Controls.Add(new Label { Text = "Installs for your Windows account. Adds Start menu shortcuts\nand an entry in Installed apps. No account or API key required.\n\nLocation: " + installer.Root, Location = new Point(30, 158), Size = new Size(535, 100) });
            Configure(install, "Install / Update", 30); Configure(launch, "Launch app", 215); Configure(remove, "Uninstall", 400);
            install.Click += delegate { Work(delegate { installer.Install(); status.Text = "Installation complete. Use Launch app or the Start menu."; }); };
            launch.Click += delegate { Work(delegate { string exe = Path.Combine(installer.Root, "WelcomeKorea.exe"); if (!File.Exists(exe)) throw new IOException("Install the app first."); Process.Start(exe); }); };
            remove.Click += delegate { Uninstall(false); }; status.SetBounds(30, 331, 540, 48); Controls.Add(status);
        }
        void Configure(Button button, string text, int x)
        {
            button.Text = text; button.SetBounds(x, 275, 170, 42); button.FlatStyle = FlatStyle.Flat; button.FlatAppearance.BorderSize = 0; button.BackColor = Color.FromArgb(18,112,104); button.ForeColor = Color.White; Controls.Add(button);
        }
        void Work(Action action)
        {
            try { action(); }
            catch (Exception ex) { status.Text = "Could not complete the action."; MessageBox.Show(this, ex.Message + "\nClose the app before installing an update or uninstalling.", "Welcome Korea Setup", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
        public void Uninstall(bool confirmed)
        {
            if (!confirmed && MessageBox.Show(this, "Remove Welcome Korea?\nYour saved places will stay in your user profile.", "Uninstall", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            Work(delegate
            {
                string own = Path.Combine(installer.Root, "WelcomeKoreaSetup.exe");
                if (String.Equals(Application.ExecutablePath, own, StringComparison.OrdinalIgnoreCase))
                {
                    string dir = Path.Combine(Path.GetTempPath(), "WelcomeKorea-Uninstall-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(dir);
                    string helper = Path.Combine(dir, "WelcomeKoreaSetup.exe"); File.Copy(own, helper); Process.Start(helper, "--remove-confirmed"); Close(); return;
                }
                installer.Remove(); status.Text = "Uninstalled. Saved places have been kept.";
            });
        }
    }
    static class Program
    {
        [STAThread] static void Main(string[] args)
        {
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); SetupWindow window = new SetupWindow();
            if (args.Length == 1 && args[0] == "--uninstall") window.Shown += delegate { window.Uninstall(false); };
            if (args.Length == 1 && args[0] == "--remove-confirmed") window.Shown += delegate { window.Uninstall(true); };
            Application.Run(window);
        }
    }
}
