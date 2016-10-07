﻿/**
The MIT License (MIT)
Copyright (c) 2014 Troy Gruetzmacher

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 * 
 * 
 * */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

namespace Snacks
{

    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class SnackController : MonoBehaviour
    {

        public static event EventHandler SnackTime;

        protected virtual void OnSnackTime(EventArgs e)
        {
            EventHandler handler = SnackTime;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private double snackTime = -1;
        private System.Random random = new System.Random();

        private SnackConsumer consumer;
        private double snacksPerMeal;
        private double lossPerDayPerKerbal;
        private int snackResourceId;
        private int snackFrequency;

        void Awake()
        {
            if (HighLogic.LoadedScene != GameScenes.SPACECENTER &&
                HighLogic.LoadedScene != GameScenes.FLIGHT &&
                HighLogic.LoadedScene != GameScenes.SPACECENTER &&
                HighLogic.LoadedScene != GameScenes.TRACKSTATION)
                return;

            try
            {
                GameEvents.onCrewOnEva.Add(OnCrewOnEva);
                GameEvents.onCrewBoardVessel.Add(OnCrewBoardVessel);
                GameEvents.onGameStateLoad.Add(onLoad);
                GameEvents.onVesselRename.Add(OnRename);
                GameEvents.onVesselChange.Add(OnVesselChange);
                GameEvents.onVesselWasModified.Add(OnVesselWasModified);
                GameEvents.OnGameSettingsApplied.Remove(onGameSettingsApplied);
                SnackConfiguration snackConfig = SnackConfiguration.Instance();
                snackResourceId = snackConfig.SnackResourceId;
                updatesnackParameters();
            }
            catch (Exception ex)
            {
                Debug.Log("Snacks - Awake error: " + ex.Message + ex.StackTrace);
            }
            
        }

        void Start()
        {
            if (HighLogic.LoadedScene != GameScenes.SPACECENTER &&
                HighLogic.LoadedScene != GameScenes.FLIGHT &&
                HighLogic.LoadedScene != GameScenes.SPACECENTER &&
                HighLogic.LoadedScene != GameScenes.TRACKSTATION)
                return; 
            
            try
            {
                double randomFactor = 1;

                System.Random rand = new System.Random();
                if (SnackConfiguration.Instance().randomSnackingEnabled)
                    randomFactor = rand.NextDouble();

                snackTime = randomFactor * snackFrequency + Planetarium.GetUniversalTime();
            }
            catch (Exception ex)
            {
                Debug.Log("Snacks - Start error: " + ex.Message + ex.StackTrace);
            }
        }

        private void onGameSettingsApplied()
        {
            updatesnackParameters();
        }

        private void updatesnackParameters()
        {
            SnackConfiguration snackConfig = SnackConfiguration.Instance();

            snackFrequency = 6 * 60 * 60 * 2 / snackConfig.MealsPerDay;
            snacksPerMeal = snackConfig.SnacksPerMeal;
            lossPerDayPerKerbal = snackConfig.repLostWhenHungry;
            consumer = new SnackConsumer(snackConfig.SnacksPerMeal, snackConfig.repLostWhenHungry);
        }

        private void OnVesselWasModified(Vessel data)
        {
            //Debug.Log("OnVesselWasModified");
            SnackSnapshot.Instance().SetRebuildSnapshot();
        }

        private void OnVesselChange(Vessel data)
        {
            //Debug.Log("OnVesselChange");
            SnackSnapshot.Instance().SetRebuildSnapshot();
        }

        private void OnRename(GameEvents.HostedFromToAction<Vessel, string> data)
        {
            //Debug.Log("OnRename");
            SnackSnapshot.Instance().SetRebuildSnapshot();
        }

        private void onLoad(ConfigNode node)
        {
            //Debug.Log("onLoad");
            SnackSnapshot.Instance().SetRebuildSnapshot();
        }

        private void OnCrewBoardVessel(GameEvents.FromToAction<Part, Part> data)
        {
            try
            {
                Part evaKerbal = data.from;
                Part boardedPart = data.to;
                double kerbalSnacks = consumer.GetSnackResource(evaKerbal, 1.0);
                boardedPart.RequestResource(snackResourceId, -kerbalSnacks, ResourceFlowMode.ALL_VESSEL);
                SnackSnapshot.Instance().SetRebuildSnapshot();
            }
            catch (Exception ex)
            {
                Debug.Log("Snacks - OnCrewBoardVessel: " + ex.Message + ex.StackTrace);
            }
        }

        private void OnCrewOnEva(GameEvents.FromToAction<Part, Part> data)
        {
            try
            {
                Part evaKerbal = data.to;
                Part partExited = data.from;
                double snacksAmount = consumer.GetSnackResource(partExited, 1.0);

                if (evaKerbal.Resources.Contains(snackResourceId) == false)
                {
                    ConfigNode node = new ConfigNode("RESOURCE");
                    node.AddValue("name", "Snacks");
                    node.AddValue("maxAmount", "1");
                    evaKerbal.Resources.Add(node);
                }
                evaKerbal.Resources[SnackUtils.kSnacksResource].amount = snacksAmount;
                SnackSnapshot.Instance().SetRebuildSnapshot();
            }
            catch (Exception ex)
            {
                Debug.Log("Snacks - OnCrewOnEva " + ex.Message + ex.StackTrace);
            }

        }

        void FixedUpdate()
        {
            if (HighLogic.LoadedScene != GameScenes.SPACECENTER && 
                HighLogic.LoadedScene != GameScenes.FLIGHT && 
                HighLogic.LoadedScene != GameScenes.SPACECENTER && 
                HighLogic.LoadedScene != GameScenes.TRACKSTATION)
                return;
            try
            {

                double currentTime = Planetarium.GetUniversalTime();

                if (currentTime > snackTime)
                {
                    double randomFactor = 1;

                    System.Random rand = new System.Random();
                    if (SnackConfiguration.Instance().randomSnackingEnabled)
                        randomFactor = rand.NextDouble();

                    snackTime = randomFactor * snackFrequency + currentTime;
                    Debug.Log("Snack time!  Next Snack Time!:" + snackTime);
                    EatSnacks();
                    SnackSnapshot.Instance().SetRebuildSnapshot();
                }
            }
            catch (Exception ex)
            {
                Debug.Log("Snacks - FixedUpdate: " + ex.Message + ex.StackTrace);
            }
        }

        void OnDestroy()
        {
            try
            {
                GameEvents.onCrewOnEva.Remove(OnCrewOnEva);
                GameEvents.onCrewBoardVessel.Remove(OnCrewBoardVessel);
            }
            catch (Exception ex)
            {
                Debug.Log("Snacks - OnDestroy: " + ex.Message + ex.StackTrace);
            }
        }

        private void EatSnacks()
        {
            try
            {
                List<Guid> activeVessels = new List<Guid>();
                double snacksMissed = 0;
                foreach (Vessel v in FlightGlobals.Vessels)
                {
                    if (v.GetCrewCount() > 0 && v.loaded)
                    {
                        activeVessels.Add(v.id);
                        double snacks = consumer.RemoveSnacks(v);
                        snacksMissed += snacks;
                        if (snacks > 0)
                        {
                            if (SnackConfiguration.Instance().losePartialControlWhenHungry)
                            {
                            }
                            Debug.Log("No snacks for: " + v.vesselName);
                        }
                    }

                }

                foreach (ProtoVessel pv in HighLogic.CurrentGame.flightState.protoVessels)
                {
                    if (pv.GetVesselCrew().Count > 0)
                    {
                        if (!pv.vesselRef.loaded && !activeVessels.Contains(pv.vesselID))
                        {
                            double snacks = consumer.RemoveSnacks(pv);
                            snacksMissed += snacks;
                            if (snacks > 0)
                            {
                                if (SnackConfiguration.Instance().losePartialControlWhenHungry)
                                {
                                }
                                Debug.Log("No snacks for: " + pv.vesselName);
                            }
                        }
                    }
                }

                if (snacksMissed > 0)
                {
                    int fastingKerbals = Convert.ToInt32(snacksMissed / snacksPerMeal);
                    if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
                    {
                        //Funding loss
                        if (SnackConfiguration.Instance().loseFundsWhenHungry)
                        {
                            double fine = SnackConfiguration.Instance().fundsLostWhenHungry * fastingKerbals;
                            Funding.Instance.AddFunds(-fine, TransactionReasons.Any);
                            ScreenMessages.PostScreenMessage(fastingKerbals + " Kerbals didn't have any snacks (fined " + Convert.ToInt32(fine) + ")", 5, ScreenMessageStyle.UPPER_LEFT);
                        }

                        //Reputation loss
                        if (SnackConfiguration.Instance().loseRepWhenHungry)
                        {
                            double repLoss;
                            if (Reputation.CurrentRep > 0)
                                repLoss = fastingKerbals * lossPerDayPerKerbal * Reputation.Instance.reputation;
                            else
                                repLoss = fastingKerbals;

                            Reputation.Instance.AddReputation(Convert.ToSingle(-1 * repLoss), TransactionReasons.Any);
                            ScreenMessages.PostScreenMessage(fastingKerbals + " Kerbals didn't have any snacks (reputation decreased by " + Convert.ToInt32(repLoss) + ")", 5, ScreenMessageStyle.UPPER_LEFT);
                        }
                    }
                    else
                    {
                        ScreenMessages.PostScreenMessage(fastingKerbals + " Kerbals didn't have any snacks.", 5, ScreenMessageStyle.UPPER_LEFT);
                    }
                }
                OnSnackTime(EventArgs.Empty);
                
            }
            catch (Exception ex)
            {
                Debug.Log("Snacks - EatSnacks: " + ex.Message + ex.StackTrace);
            }
        }

        private void SetVesselOutOfSnacks(Vessel v)
        {
            //v.GetVesselCrew()[0].

            //Debug.Log(pv.ctrlState);
        }


    }
}