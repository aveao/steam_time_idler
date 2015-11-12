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
        public Form1()
        {
            InitializeComponent();
            stopidle();
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
            label4.Text = "Status: please wait, this will take some time";
            MessageBox.Show("This will take some time if you have a lot of games (~1 minute on slow internet + slow pc + 1100 games) and it will start after you click OK.");
            if (textBox1.Text == "me")
            {
                textBox1.Text = "http://steamcommunity.com/id/ardaozkal";
            }

            var link = (textBox1.Text.Substring(textBox1.Text.Length - 1) != "/") ? textBox1.Text + "/" : textBox1.Text; //get the link
            var wc = new WebClient();
            var returnedlines = wc.DownloadString(link + "games?tab=all&xml=1").Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
            wc.Dispose();

            /*
            <game>
			<appID>252950</appID>
			<name><![CDATA[Rocket League]]></name>
			<logo><![CDATA[http://cdn.akamai.steamstatic.com/steamcommunity/public/images/apps/252950/58d7334290672887fdd47e25251f291b812c895e.jpg]]></logo>
			<storeLink><![CDATA[http://steamcommunity.com/app/252950]]></storeLink>
			<hoursLast2Weeks>5.3</hoursLast2Weeks>
			<hoursOnRecord>5.3</hoursOnRecord>
			<statsLink><![CDATA[http://steamcommunity.com/id/ardaozkal/stats/252950]]></statsLink>
			<globalStatsLink><![CDATA[http://steamcommunity.com/stats/252950/achievements/]]></globalStatsLink>
		    </game>
            */
            long currentappid = 0;
            var currentline = 0;

            foreach (string line in returnedlines)
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

        List<long> gamestoidle = new List<long>();

        private void button2_Click(object sender, EventArgs e)
        {
            label4.Text = "Status: Starting idling.";
            foreach (long game in games.Keys)
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
            label4.Text = "Status: Started idling.";
        }

        static void stopidle()
        {
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
            timer1.Interval = Convert.ToInt32(numericUpDown1.Value * 60000); //sets the time of the future ones to longer time intervals than 100msec
            var maxidle = 20; //I contacted jshackles and asked him about the limit (no answers yet) but yeah I'm hoping 20 is ok.

            if (gamestoidle.Count == 0)
            {
                timer1.Stop();
                numericUpDown1.Enabled = true;
                button1.Enabled = true;
                textBox1.Enabled = true;

                label4.Text = "Status: Done idling.";
                MessageBox.Show("Done");
            }
            else
            {
                stopidle(); //not the best way as it can interfere with idle master, but why does one run both at the same time?
                if (gamestoidle.Count < maxidle) //so yeah, running 30 processes when there is 29 games to idle will cause a crash
                {
                    maxidle = gamestoidle.Count; //lets ste the max idle limit to the available game amount to prevent crashes
                }

                for (int i = 0; i <= maxidle; i++)
                {
                    Process.Start(new ProcessStartInfo("steam-idle.exe", gamestoidle[0].ToString()) { WindowStyle = ProcessWindowStyle.Hidden }); //this line was GLORIOUSLY ~~stolen~~ borrowed from idle master. Thanks jshackles.
                    gamestoidle.Remove(gamestoidle[0]); //might be problematic sometime but who cares atm lol, will look more into this if this ever gets filed as an issue. Also tip for future self: use i instead of 0 on top and remove the idle game AFTER you play it.
                }
                label4.Text = "Status: Idling, " + gamestoidle.Count + " games left.";
            }
        }
    }
}
