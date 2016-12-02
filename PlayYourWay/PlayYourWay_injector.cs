using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.UI.Screens;

namespace PlayYourWay
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class PlayYourWayScenarioInjector : MonoBehaviour
    {
        void Start()
        {
            var game = HighLogic.CurrentGame;
            ProtoScenarioModule psm = game.scenarios.Find(s => s.moduleName == typeof(PlayYourWay).Name);

            if (psm == null)
            {
                PlayYourWay.Log("Adding the controller to the game.");
                psm = game.AddProtoScenarioModule(typeof(PlayYourWay), GameScenes.EDITOR,
                                                                          GameScenes.FLIGHT,
                                                                          GameScenes.SPACECENTER,
                                                                          GameScenes.TRACKSTATION);
            }
            else 
            {
                PlayYourWay.Log("The runtime is already installed (OK).");

                SetTargetScene(psm, GameScenes.EDITOR);
                SetTargetScene(psm, GameScenes.FLIGHT);
                SetTargetScene(psm, GameScenes.SPACECENTER);
                SetTargetScene(psm, GameScenes.TRACKSTATION);
            }
        }


        private static void SetTargetScene(ProtoScenarioModule psm, GameScenes scene)
        {
            if (!psm.targetScenes.Any(s => s == scene))
            {
                psm.targetScenes.Add(scene);
            }
        }

    }
}

