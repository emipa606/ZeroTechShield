using System;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace LingGame
{
    public class Command_IntVec33 : Command
    {
        public Action<IntVec3> action;

        public TargetingParameters targetingParams;

        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);
            SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
            Find.Targeter.BeginTargeting(targetingParams, delegate(LocalTargetInfo target) { action(target.Cell); });
        }

        public override bool InheritInteractionsFrom(Gizmo other)
        {
            return false;
        }
    }
}