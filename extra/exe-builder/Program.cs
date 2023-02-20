﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using UptimeKuma.Properties;

namespace UptimeKuma {
    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args) {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new UptimeKumaApplicationContext());
        }
    }

    public class UptimeKumaApplicationContext : ApplicationContext
    {
        const string appName = "Uptime Kuma";

        private NotifyIcon trayIcon;
        private Process process;

        private MenuItem runWhenStarts;

        private RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

        public UptimeKumaApplicationContext()
        {
            trayIcon = new NotifyIcon();

            runWhenStarts = new MenuItem("Run when system starts", RunWhenStarts);
            runWhenStarts.Checked = registryKey.GetValue(appName) != null;

            trayIcon.Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
            trayIcon.ContextMenu = new ContextMenu(new MenuItem[] {
                new("Open", Open),
                //new("Debug Console", DebugConsole),
                runWhenStarts,
                new("Check for Update...", CheckForUpdate),
                new("Visit GitHub...", VisitGitHub),
                new("About", About),
                new("Exit", Exit),
            });

            trayIcon.MouseDoubleClick += new MouseEventHandler(Open);
            trayIcon.Visible = true;

            if (Directory.Exists("core") && Directory.Exists("node") && Directory.Exists("core/node_modules") && Directory.Exists("core/dist")) {
                // Go go go
                StartProcess();
            } else {
                DownloadFiles();
            }
        }

        void DownloadFiles() {
            var form = new DownloadForm();
            form.Closed += Exit;
            form.Show();
        }

        private void RunWhenStarts(object sender, EventArgs e) {
            if (registryKey == null) {
                MessageBox.Show("Error: Unable to set startup registry key.");
                return;
            }

            if (runWhenStarts.Checked) {
                registryKey.DeleteValue(appName, false);
                runWhenStarts.Checked = false;
            } else {
                registryKey.SetValue(appName, Application.ExecutablePath);
                runWhenStarts.Checked = true;
            }
        }

        void StartProcess() {
            var startInfo = new ProcessStartInfo {
                FileName = "node/node.exe",
                Arguments = "server/server.js --data-dir=\"../data/\"",
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = "core"
            };

            process = new Process();
            process.StartInfo = startInfo;
            process.EnableRaisingEvents = true;
            process.Exited += ProcessExited;

            try {
                process.Start();
                //Open(null, null);

            } catch (Exception e) {
                MessageBox.Show("Startup failed: " + e.Message, "Uptime Kuma Error");
            }
        }

        void Open(object sender, EventArgs e) {
            Process.Start("http://localhost:3001");
        }

        void DebugConsole(object sender, EventArgs e) {

        }

        void CheckForUpdate(object sender, EventArgs e) {
            Process.Start("https://github.com/louislam/uptime-kuma/releases");
        }

        void VisitGitHub(object sender, EventArgs e)
        {
            Process.Start("https://github.com/louislam/uptime-kuma");
        }

        void About(object sender, EventArgs e)
        {
            MessageBox.Show("Uptime Kuma Windows Runtime v1.0.0" + Environment.NewLine + "© 2023 Louis Lam", "Info");
        }

        void Exit(object sender, EventArgs e)
        {
            // Hide tray icon, otherwise it will remain shown until user mouses over it
            trayIcon.Visible = false;
            process?.Kill();
            Application.Exit();
        }

        void ProcessExited(object sender, EventArgs e) {

            if (process.ExitCode != 0) {
                var line = "";
                while (!process.StandardOutput.EndOfStream)
                {
                    line += process.StandardOutput.ReadLine();
                }

                MessageBox.Show("Uptime Kuma exited unexpectedly. Exit code: " + process.ExitCode + " " + line);
            }

            trayIcon.Visible = false;
            Application.Exit();
        }

    }
}
