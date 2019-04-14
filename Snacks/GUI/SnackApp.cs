﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP.UI.Screens;

namespace Snacks
{
    [KSPAddon(KSPAddon.Startup.Flight | KSPAddon.Startup.EveryScene, false)]
    public class SnackApp : MonoBehaviour
    {
        static protected Texture2D appIcon = null;
        static protected ApplicationLauncherButton appLauncherButton = null;
        static protected SnackAppView snackView;

        public void Awake()
        {
            snackView = new SnackAppView();
            appIcon = GameDatabase.Instance.GetTexture("WildBlueIndustries/Snacks/Textures/snacks", false);
            GameEvents.onGUIApplicationLauncherReady.Add(SetupGUI);
        }

        public void OnGUI()
        {
            if (snackView.IsVisible())
                snackView.DrawWindow();
        }

        private void SetupGUI()
        {
            if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedScene == GameScenes.SPACECENTER || HighLogic.LoadedSceneIsEditor)
            {
                if (appLauncherButton == null)
                    appLauncherButton = ApplicationLauncher.Instance.AddModApplication(ToggleGUI, ToggleGUI, null, null, null, null, ApplicationLauncher.AppScenes.ALWAYS, appIcon);
            }
            else if (appLauncherButton != null)
                ApplicationLauncher.Instance.RemoveModApplication(appLauncherButton);
        }

        private void ToggleGUI()
        {
            snackView.SetVisible(!snackView.IsVisible());
        }
    }
}
