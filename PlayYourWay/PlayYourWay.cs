using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.UI.Screens;

namespace PlayYourWay
{
    public class OneShotTimer
    {
        public delegate void TimerEvent();
        float? start_time = null;
        public TimerEvent OnTimer;
        public float Interval = 1f;

        public void Start()
        {
            this.start_time = UnityEngine.Time.time;
        }

        public void Update()
        {
            if (this.start_time != null && (Time.time - start_time) > this.Interval)
            {
                this.start_time = null;
                this.OnTimer();
            }
        }
    }

    public struct ScienceReport
    {
        public float funds;
        public float rep;
        public string subject;

        public ScienceReport(float funds, float reputation, string subject)
        {
            this.funds = funds;
            this.rep = reputation;
            this.subject = subject;
        }

        public override string ToString()
        {
            return " * " + this.subject + ":\n" +
                   "     " + this.funds.ToString("F1") + " funds, " +
                             this.rep.ToString("F1") + " rep.";
        }

        public ConfigNode ToConfigNode()
        {
            ConfigNode retval = new ConfigNode("REPORT");
            retval.AddValue("funds", this.funds);
            retval.AddValue("rep", this.rep);
            retval.AddValue("subject", this.subject);

            return retval;
        }

        public static ScienceReport FromConfigNode(ConfigNode node)
        {
            return new ScienceReport(
                funds: float.Parse(node.GetValue("funds")),
                reputation: float.Parse(node.GetValue("rep")),
                subject: node.GetValue("subject")
            );
        }
    }

    public class PlayYourWay : ScenarioModule
    {

        public float fundsMult;

        public float repMult;

        public int queueLength = 5;
        public Queue<ScienceReport> queue = new Queue<ScienceReport>();

        OneShotTimer timer = new OneShotTimer();

        public override void OnAwake()
        {
            this.timer.OnTimer = this.OnTimer;

            GameEvents.OnScienceRecieved.Add(ScienceReceivedHandler);
            PlayYourWay.Log("listening for science...");
        }

        public override void OnLoad(ConfigNode node)
        {
            PlayYourWay.Log("Loading configuration");

            LoadConfiguration();

            this.queue = new Queue<ScienceReport>(this.queueLength);
            if (node.HasNode("QUEUE"))
            {
                foreach (ConfigNode reportNode in node.GetNodes("REPORT"))
                {
                    try
                    {
                        this.queue.Enqueue(ScienceReport.FromConfigNode(reportNode));
                    }
                    catch (Exception)
                    {
                        PlayYourWay.Log("Bad value found in queue, skipping:\n" + reportNode.ToString());
                        continue;
                    }
                }
            }
            else
            {
                PlayYourWay.Log("No node to load");
                node.AddNode(new ConfigNode("QUEUE"));
            }

            PlayYourWay.Log("Loaded " + this.queue.Count.ToString() + " records");
        }

        public void Update()
        {
            this.timer.Update();
        }

        public void OnTimer()
        {
            if (this.queue.Count > this.queueLength)
                SendReport();
        }

        public void ScienceReceivedHandler(float science, ScienceSubject sub, ProtoVessel v, bool whoKnows)
        {
            PlayYourWay.Log("Received " + science + " science points");


            if (science == 0)
                return;

            float funds = science * this.fundsMult;
            float rep = science * this.repMult;


            if (Funding.Instance != null)
            {
                Funding.Instance.AddFunds(funds, TransactionReasons.ScienceTransmission);
                PlayYourWay.Log("Added " + funds + " funds");
            }


            if (Reputation.Instance != null)
            {
                Reputation.Instance.AddReputation(rep, TransactionReasons.ScienceTransmission);
                PlayYourWay.Log("Added " + rep + " reputation");
            }


            this.queue.Enqueue(new ScienceReport(funds, rep, sub.title));
            this.timer.Start();
        }


        private void SendReport()
        {
            PlayYourWay.Log("Posting the user notification");

            if (this.queue.Count == 0)
                return;


            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Your recent research efforts have granted you the following rewards:");
            builder.AppendLine("");

            float totalFunds = 0;
            float totalRep = 0;


            foreach (ScienceReport item in this.queue.ToList())
            {
                totalFunds += item.funds;
                totalRep += item.rep;

                builder.AppendLine(item.ToString());
            }


            builder.AppendLine("");
            builder.AppendLine("Total: " + totalFunds + " funds, " + totalRep + " reputation.");


            this.queue = new Queue<ScienceReport>();

            PostMessage(
                "New funds available!",
                builder.ToString(),
                MessageSystemButton.MessageButtonColor.BLUE,
                MessageSystemButton.ButtonIcons.MESSAGE
            );
        }

        /// <param name="node"></param>
        public override void OnSave(ConfigNode node)
        {
            try
            {
                PlayYourWay.Log("Saving message queue");

                if (node.HasNode("QUEUE"))
                    node.RemoveNode("QUEUE");
                ConfigNode queueNode = new ConfigNode("QUEUE");
                node.AddNode(queueNode);

                foreach (ScienceReport report in this.queue)
                {
                    queueNode.AddNode(report.ToConfigNode());
                }

                PlayYourWay.Log("Saved " + this.queue.Count.ToString() + " records");
            }
            catch (Exception e)
            {
                PlayYourWay.Log("Something went orribly wrong while saving: " + e.ToString());
                node.SetNode("QUEUE", new ConfigNode("QUEUE"));
            }
        }

        public void OnDestroy()
        {
            try
            {
                GameEvents.OnScienceRecieved.Remove(ScienceReceivedHandler);
                PlayYourWay.Log("OnDestroy, removing handler.");
            }
            catch (Exception)
            {
                // do nothing
            }
        }

        #region Utilities

        public void LoadConfiguration()
        {
            try
            {
                ConfigNode node = GetConfig();
                this.fundsMult = float.Parse(node.GetValue("funds"));
                this.repMult = float.Parse(node.GetValue("rep"));
                this.queueLength = int.Parse(node.GetValue("queueLength"));
            }
            catch (Exception e)
            {

                this.fundsMult = 1000;
                this.repMult = 1f;
                this.queueLength = 5;

                PlayYourWay.Log("There was an exception while loading the configuration: " + e.ToString());

                PostMessage(
                    "PlayYourWay error!",
                    "I'm sorry to break your immersion, but there seems to be an error in the configuration" +
                    " and PlayYourWay is not working properly right now. You should check the values in the config file.",
                    MessageSystemButton.MessageButtonColor.RED, MessageSystemButton.ButtonIcons.ALERT
                );
            }

            PlayYourWay.Log("Configuration is " + this.fundsMult.ToString() + ", " + this.repMult.ToString() + ", " + this.queueLength.ToString());
        }

        static ConfigNode GetConfig()
        {
            string assemblyPath = Path.GetDirectoryName(typeof(PlayYourWay).Assembly.Location);
            string filePath = Path.Combine(assemblyPath, "PYWSettings.cfg");

            PlayYourWay.Log("Loading settings file:" + filePath);

            ConfigNode result = ConfigNode.Load(filePath).GetNode("PYW_SETTINGS");
            PlayYourWay.Log(result.ToString());

            return result;
        }

        static void PostMessage(string title,
                                string message,
                                MessageSystemButton.MessageButtonColor messageButtonColor,
                                MessageSystemButton.ButtonIcons buttonIcons)
        {
            MessageSystem.Message msg = new MessageSystem.Message(
                    title,
                    message,
                    messageButtonColor,
                    buttonIcons);
            MessageSystem.Instance.AddMessage(msg);
        }

        public static void Log(string msg)
        {
            Debug.Log("[PlayYourWay]: " + msg);
        }

        #endregion
    }
}