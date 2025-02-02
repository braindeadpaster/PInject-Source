using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LLAInjector
{
    public partial class Injector : Form
    {
        private string errorText;
        private int len = 0;
        private string selectedDllPath = string.Empty;

        public Injector()
        {
            InitializeComponent();
            ErrorLabel.Visible = false;
        }



        private void Form1_Load(object sender, EventArgs e)
        {
            ErrorLabel.Text = "";
        }

        private void AppendToLog(string message)
        {
            guna2TextBox3.AppendText(message + Environment.NewLine);
        }

        private void ShowErrorMessage(string message, Color color)
        {
            errorText = message;
            ErrorLabel.Text = message;
            ErrorLabel.ForeColor = color;
            ErrorLabel.Visible = true;
            len = 0;
            AppendToLog(message);
            timer1.Start();
        }

        private void InjectDll()
        {
            guna2TextBox3.Clear(); 

            if (string.IsNullOrEmpty(selectedDllPath))
            {
                ShowErrorMessage("No DLL Selected!", Color.Red);
                return;
            }
            AppendToLog($"Selected DLL: {Path.GetFileName(selectedDllPath)}");

            if (string.IsNullOrEmpty(guna2TextBox1.Text))
            {
                ShowErrorMessage("No Process Selected!", Color.Red);
                return;
            }

            string processName = guna2TextBox1.Text;
            AppendToLog($"Waiting for {processName}.exe...");

            Process[] targetProcesses = Process.GetProcessesByName(processName);
            if (targetProcesses.Length == 0)
            {
                ShowErrorMessage($"Waiting for {processName}.exe...", Color.Red);
                return;
            }

            Process targetProcess = targetProcesses[0];
            AppendToLog($"Found Process: {targetProcess.ProcessName} (PID: {targetProcess.Id})");

            IntPtr hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, targetProcess.Id);
            if (hProcess == IntPtr.Zero)
            {
                ShowErrorMessage("Failed to Open Target Process!", Color.Red);
                return;
            }
            AppendToLog("Successfully Opened Process.");

            IntPtr loadLibraryAddr = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");
            byte[] dllPathBytes = Encoding.ASCII.GetBytes(selectedDllPath + '\0');
            IntPtr allocMemAddress = VirtualAllocEx(hProcess, IntPtr.Zero, (IntPtr)dllPathBytes.Length, MEM_COMMIT, PAGE_READWRITE);

            if (allocMemAddress == IntPtr.Zero)
            {
                ShowErrorMessage("Failed to Allocate Memory!", Color.Red);
                return;
            }
            AppendToLog("Memory Allocated in Process.");

            if (!WriteProcessMemory(hProcess, allocMemAddress, dllPathBytes, (uint)dllPathBytes.Length, out _))
            {
                ShowErrorMessage("Failed to Write DLL Path!", Color.Red);
                return;
            }
            AppendToLog("DLL Path Written to Process Memory.");

            if (CreateRemoteThread(hProcess, IntPtr.Zero, IntPtr.Zero, loadLibraryAddr, allocMemAddress, 0, IntPtr.Zero) == IntPtr.Zero)
            {
                ShowErrorMessage("Failed to Create Remote Thread!", Color.Red);
                return;
            }

            AppendToLog("Injected Successfully!");
            ShowErrorMessage("Injected Successfully!", Color.Green);
        }

        private void guna2Button1_Click(object sender, EventArgs e)
        {
            InjectDll();
        }

        private void guna2Button2_Click(object sender, EventArgs e)
        {
            InjectDll();
        }

        private void guna2Button3_Click(object sender, EventArgs e)
        {
            using (Form processSelectionForm = new Form())
            {
                processSelectionForm.Text = "Select a Process";
                processSelectionForm.Size = new Size(400, 500);

                ListBox processListBox = new ListBox() { Dock = DockStyle.Fill };
                Button selectButton = new Button() { Text = "Confirm", Dock = DockStyle.Bottom };

                foreach (Process proc in Process.GetProcesses())
                {
                    processListBox.Items.Add(proc.ProcessName + " (PID: " + proc.Id + ")");
                }

                selectButton.Click += (s, ev) =>
                {
                    if (processListBox.SelectedItem != null)
                    {
                        string selectedProcess = processListBox.SelectedItem.ToString();
                        string processName = selectedProcess.Split(' ')[0];
                        guna2TextBox1.Text = processName;
                        processSelectionForm.Close();
                    }
                };

                processSelectionForm.Controls.Add(processListBox);
                processSelectionForm.Controls.Add(selectButton);
                processSelectionForm.ShowDialog();
            }
        }

        private void guna2Button1_Click_1(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "DLL files (*.dll)|*.dll", Title = "Select DLL to Inject" })
            {
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    selectedDllPath = openFileDialog.FileName;
                    string dllName = Path.GetFileName(selectedDllPath);
                    guna2TextBox2.Text = dllName;
                    AppendToLog($"Selected DLL: {dllName}");
                }
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {
            
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (len < errorText.Length)
            {
                ErrorLabel.Text += errorText[len];
                len++;
            }
            else
            {
                timer1.Stop();
                Task.Delay(2000).ContinueWith(_ =>
                {
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() => ErrorLabel.Visible = false));
                    }
                    else
                    {
                        ErrorLabel.Visible = false;
                    }
                });
            }
        }

        private void guna2TextBox2_TextChanged(object sender, EventArgs e) { }

        private void guna2TextBox1_TextChanged(object sender, EventArgs e) { }

        private void guna2Panel1_Paint(object sender, PaintEventArgs e) { }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, IntPtr dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttribute, IntPtr dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        const uint PROCESS_ALL_ACCESS = 0x1F0FFF;
        const uint MEM_COMMIT = 0x1000;
        const uint PAGE_READWRITE = 4;

        private void guna2ImageButton1_Click(object sender, EventArgs e)
        {
            Process.Start("https://suicides.live/paster");
        }
    }
}
