using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Security.Principal;
using System.Diagnostics;
using System.Management;

namespace Steam_Time_Idler
{
    public partial class Form1 : Form
    {
        Dictionary<long, decimal> games = new Dictionary<long, decimal>();
        Dictionary<string, long> hiddengames = new Dictionary<string, long>();

        public Form1()
        {
            InitializeComponent();
            stopidle();

            hiddengames.Add("Greenlight", 765);
            hiddengames.Add("SteamVR", 250820);
            hiddengames.Add("Steam Cloud", 764);
            hiddengames.Add("Steam (Client)", 753);
            hiddengames.Add("Steam Music", 258280);
            hiddengames.Add("Steam Economy", 754);
            hiddengames.Add("Steam Artwork", 767767);
            hiddengames.Add("Steam Screenshots", 760);
            hiddengames.Add("Steam Cloud - Videos", 761);
            hiddengames.Add("Steam Cloud - User Logs", 764);
            hiddengames.Add("ValveTestApp200269", 200269);
            hiddengames.Add("Steam Controller Configs", 241100);
            hiddengames.Add("Steam Software Beta Access", 218800);
            hiddengames.Add("Game Library Sharing Access", 248210);
            hiddengames.Add("Steam Trading Card Beta Access", 202352);
            hiddengames.Add("Beta Access to the New Steam Community", 202351);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Two sample links: " + Environment.NewLine + "http://steamcommunity.com/id/ardaozkal" + Environment.NewLine + "http://steamcommunity.com/profiles/76561198034299068");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            MessageBox.Show("5 minutes lets you review the game and is therefore enough for most people.");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text == "me")
            {
                textBox1.Text = "http://steamcommunity.com/id/ardaozkal";
            }

            if ((textBox1.Text.Contains("http://") || textBox1.Text.Contains("https://")) && (textBox1.Text.Contains("profiles") || textBox1.Text.Contains("id")))
            {
                label4.Text = "Status: Fetching game list.";
                MessageBox.Show("This will take some time if you have a lot of games (~1 minute on slow internet + slow pc + 1100 games) and it will start after you click OK.");

                var link = (textBox1.Text.Substring(textBox1.Text.Length - 1) != "/") ? textBox1.Text + "/" : textBox1.Text; //get the link
                var wc = new WebClient();
                var returnedlines = wc.DownloadString(link + "games?tab=all&xml=1").Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
                wc.Dispose();

                long currentappid = 0;
                var currentline = 0;

                foreach (string line in returnedlines) //TODO: XML PARSEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEER
                {
                    currentline++;
                    if (line.Contains("<appID>"))
                    {
                        currentappid = int.Parse(line.Replace(" ", "").Replace("\\t", "").Replace("<appID>", "").Replace("</appID>", ""));
                        if (returnedlines.Count() > currentline + 4)
                        {
                            var plusfour = returnedlines[currentline + 4];
                            if (plusfour.Contains("<statsLink>"))
                            {
                                plusfour = returnedlines[currentline + 3];
                            }
                            if (!plusfour.Contains("<hoursOnRecord>"))
                            {
                                games.Add(currentappid, 0);
                            }
                        }
                        else
                        {
                            games.Add(currentappid, 0);
                        }
                    }
                    else if (line.Contains("<hoursOnRecord>"))
                    {
                        try
                        {
                            games.Add(currentappid, decimal.Parse(line.Replace(" ", "").Replace("\\t", "").Replace("<hoursOnRecord>", "").Replace("</hoursOnRecord>", "")));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    }
                }

                button2.Enabled = true;

                MessageBox.Show("Ready to start idling, hit start idle button please.");
                label4.Text = "Status: Ready to idle.";
            }
            else
            {
                MessageBox.Show("You don't seem to have a correct link. Example links can be accessed by pressing the ? button.");
            }
        }

        List<long> gamestoidle = new List<long>();

        private void button2_Click(object sender, EventArgs e)
        {
            label4.Text = "Status: Starting idling.";
            foreach (long game in games.Keys) //checks game times, spaghetti code
            {
                decimal timeplayed = 0;
                games.TryGetValue(game, out timeplayed);
                if (timeplayed < numericUpDown1.Value / 50) // value/50 returns the time out of 1 and not 60, for ex, 5 minutes is 0.1 hours, which is what steam returns on xml results
                {
                    gamestoidle.Add(game);
                }
            }
            timer1.Interval = 100; //starts first run immediately
            timer1.Start();

            numericUpDown1.Enabled = false;
            button1.Enabled = false;
            textBox1.Enabled = false;
            button2.Enabled = false;
            button5.Text = "Stop Idling";
            label4.Text = "Status: Started idling.";
        }

        static void stopidle()
        { //taken from idlemaster project by jshackles. 
            try
            {
                var username = WindowsIdentity.GetCurrent().Name;
                foreach (var process in Process.GetProcessesByName("steam-idle"))
                {
                    var searcher = new ManagementObjectSearcher("Select * From Win32_Process Where ProcessID = " + process.Id);
                    var processList = searcher.Get();

                    foreach (ManagementObject obj in processList)
                    {
                        var argList = new string[] { string.Empty, string.Empty };
                        var returnVal = Convert.ToInt32(obj.InvokeMethod("GetOwner", argList));
                        if (returnVal == 0)
                        {
                            if (argList[1] + "\\" + argList[0] == username)
                            {
                                process.Kill();
                            }
                        }
                    }
                    searcher.Dispose();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Error while stopping idle, please shut down all steam-idle.exe processes by hand from task manager.");
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Interval = Convert.ToInt32((numericUpDown1.Value + 1) * 60000); //does an +1, because steam considers this playtime as 4 minutes and not 5.
            var maxidle = 20; //I contacted jshackles and asked him about the limit (no answers yet) but yeah I'm hoping 20 is ok.

            if (gamestoidle.Count == 0) //If we are done
            {
                timer1.Stop(); //lets stop working shall we?
                stopidle(); //Or you'll have loads of gametime.
                numericUpDown1.Enabled = true; //lets reenable these
                button1.Enabled = true;
                textBox1.Enabled = true;
                button2.Enabled = true;
                button5.Text = "Idle hidden games";

                label4.Text = "Status: Done idling."; //updating the status
                MessageBox.Show("Done"); //And the users lived happily ever after. Thank me later.
            }
            else
            {
                stopidle(); //not the best way as it can interfere with idle master, but why does one run both at the same time?
                if (gamestoidle.Count < maxidle) //so yeah, running 20 processes when there is 19 games to idle will cause a crash
                {
                    maxidle = gamestoidle.Count; //lets set the max idle limit to the available game amount to prevent crashes. TODO: still crashes
                }

                for (int i = 0; i <= maxidle; i++)
                {
                    Process.Start(new ProcessStartInfo("steam-idle.exe", gamestoidle[0].ToString()) { WindowStyle = ProcessWindowStyle.Hidden }); //this line was borrowed from idle master. Thanks jshackles.
                    gamestoidle.Remove(gamestoidle[0]); //might be problematic sometime but who cares atm lol, will look more into this if this ever gets filed as an issue. Also tip for future self: use i instead of 0 on top and remove the idle game AFTER you play it.
                    //TODO: FIX THIS
                }
                label4.Text = "Status: Idling, " + gamestoidle.Count + " games left.";
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            var builder = new StringBuilder();
            foreach (string gamename in hiddengames.Keys)
            {
                builder.Append(gamename + ", ");
            }
            MessageBox.Show("Clicking Idle Hidden Games button will play hidden games such as " + builder.ToString().Substring(0, (builder.Length - 2)) + ". You don't need to input your steam link if you are just planning to use this feature.");
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (button5.Text == "Stop Idling")
            {
                timer1.Stop();
                stopidle();
                numericUpDown1.Enabled = true; //lets reenable these
                button1.Enabled = true;
                textBox1.Enabled = true;
                button2.Enabled = true;
                label4.Text = "Status: Stopped idling.";
                button5.Text = "Idle hidden games";
            }
            else
            {
                label4.Text = "Status: Idling hidden games. Hit the stop button to stop.";
                numericUpDown1.Enabled = false;
                button1.Enabled = false;
                textBox1.Enabled = false;
                button2.Enabled = false;
                button5.Text = "Stop Idling";

                foreach (long str in hiddengames.Values)
                {
                    Process.Start(new ProcessStartInfo("steam-idle.exe", str.ToString()) { WindowStyle = ProcessWindowStyle.Hidden }); //this line was borrowed from idle master. Thanks jshackles.
                }
            }
        }
    }
}
