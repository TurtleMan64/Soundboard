using System;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Media;

public class Soundboard
{
    public const int SW_HIDE = 0;

    [DllImport("kernel32.dll")]
    public static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    public static Form theForm = new Form();
    
    public static TrackBar trackBar = new TrackBar();
    
    public static string EXE_DIR = "";
    
    public static int BUTTON_WIDTH = 256;
    public static int BUTTON_HEIGHT = 36;
    
    public static int VOLUME_HEIGHT = 46;
    
    public static List<Button> buttons = new List<Button>();
    
    public static Dictionary<string, SoundPlayer> nameToSound = new Dictionary<string, SoundPlayer>();
    
    public static int pid = 0;
    
    // In order for the VolumeMixer functions to work, the application must be showing in the Volume Mixer.
    // In order to be showing in Volume Mixer, a sound needs to be played first. So we play a sound that is
    // completely silent to get Volume Mixer to show up, and then get the application volume level.
    // This is done on another thread to not stall the UI on startup.
    public static void InitAudio()
    {
        SoundPlayer playing = new SoundPlayer(EmptySoundStream.getEmptySoundStream());
        playing.PlaySync();
        trackBar.Value = (int)(VolumeMixer.GetApplicationVolume(pid)/10.0f);
    }
    
    [STAThread]
    public static void Main()
    {
        pid = Process.GetCurrentProcess().Id;
        
        var handle = GetConsoleWindow();
        ShowWindow(handle, SW_HIDE);
        
        EXE_DIR = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + "/";
        
        theForm = new Form();
        theForm.Text = "Soundboard";
        theForm.Size = new Size(10, 10);
        theForm.MaximizeBox = false;
        theForm.Resize += EventFormResize;
        try
        {
            theForm.Icon = new Icon(MyIcon.getIconStream());
        }
        catch {}
        
        trackBar.Location = new Point(0, 0);
        trackBar.Size = new Size(VOLUME_HEIGHT, BUTTON_HEIGHT);
        trackBar.Maximum = 10;
        trackBar.Minimum = 0;
        trackBar.TickFrequency = 1;
        trackBar.LargeChange = 1;
        trackBar.SmallChange = 1;
        trackBar.ValueChanged += EventValueChanged;
        trackBar.Value = 10;
        
        theForm.Controls.Add(trackBar);
        
        string[] files = Directory.GetFiles(EXE_DIR + "Sounds/");
        Array.Sort(files, StringComparer.InvariantCulture);
        
        foreach (string file in files)
        {
            if (file.EndsWith(".wav"))
            {
                int slashIdx = file.LastIndexOf('/') + 1;
                
                string fname = file.Substring(slashIdx, (file.Length - slashIdx) - 4);
                
                Button btn = new Button();
                btn.Text = "" + fname;
                
                buttons.Add(btn);
                
                SoundPlayer player = new SoundPlayer(EXE_DIR + "Sounds/" + fname + ".wav");

                nameToSound.Add(fname, player);
            }
        }

        for (int i = 0; i < buttons.Count; i++)
        {
            buttons[i].Size = new Size(BUTTON_WIDTH, BUTTON_HEIGHT);
            buttons[i].Location = new Point(0, VOLUME_HEIGHT + i*BUTTON_HEIGHT);
            buttons[i].Click += EventButtonClick;
            
            theForm.Controls.Add(buttons[i]);
        }
        
        Thread audioInitThread = new Thread(InitAudio);
        audioInitThread.Start();
        
        theForm.ClientSize = new Size(BUTTON_WIDTH, VOLUME_HEIGHT + buttons.Count*BUTTON_HEIGHT);
        theForm.ShowDialog();
        
        audioInitThread.Join();
    }
    
    public static void EventFormResize(object sender, EventArgs e)
    {
        trackBar.Width = theForm.ClientSize.Width;
        for (int i = 0; i < buttons.Count; i++)
        {
            buttons[i].Width = theForm.ClientSize.Width;
        }
    }
    
    public static void EventButtonClick(object sender, EventArgs e)
    {
        nameToSound[((Button)sender).Text].Play();
    }
    
    public static void EventValueChanged(object sender, EventArgs e)
    {
        VolumeMixer.SetApplicationVolume(pid, trackBar.Value*10.0f);
    }
}
