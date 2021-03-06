﻿using System;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;

namespace Hospitality
{

    public sealed class Hospitality_SpecialInjector
    {
        public bool Inject()
        {
            InjectTab(typeof(ITab_Pawn_Guest), def => def.race != null && def.race.Humanlike);

            InjectComp(typeof(CompProperties_Guest), def => def.race != null && def.race.Humanlike);

            CreateGuestBedDefs();

            return true;
        }

        private void CreateGuestBedDefs()
        {
            var bedDefs = DefDatabase<ThingDef>.AllDefsListForReading.Where(def => def.thingClass == typeof(Building_Bed) && def.building.bed_humanlike).ToArray();

            var fields = typeof(ThingDef).GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var bedDef in bedDefs)
            {
                var guestBedDef = new ThingDef();
                foreach (var field in fields)
                {
                    field.SetValue(guestBedDef, field.GetValue(bedDef));
                }
                guestBedDef.defName += "Guest";
                guestBedDef.label = "GuestBedFormat".Translate(guestBedDef.label);
                guestBedDef.thingClass = typeof(Building_GuestBed);
                guestBedDef.shortHash = 0;
                guestBedDef.minifiedDef = null;
                guestBedDef.tradeability = Tradeability.Never;
                guestBedDef.scatterableOnMapGen = false;
                guestBedDef.canBeSpawningInventory = false;

                typeof(ShortHashGiver).GetMethod("GiveShortHash", BindingFlags.NonPublic|BindingFlags.Static).Invoke(null, new object[] {guestBedDef, typeof(ThingDef)});
                DefDatabase<ThingDef>.Add(guestBedDef);
            }
        }

        private void InjectComp(Type compType, Func<ThingDef, bool> qualifier)
        {
            var defs = DefDatabase<ThingDef>.AllDefsListForReading.Where(qualifier).ToList();
            defs.RemoveDuplicates();

            foreach (var def in defs)
            {
                if (def.comps == null) continue;

                if (!def.comps.Any(c=>c.GetType() == compType))
                {
                    def.comps.Add((CompProperties) Activator.CreateInstance(compType));
                    //Log.Message(def.defName+": "+def.inspectorTabsResolved.Select(d=>d.GetType().Name).Aggregate((a,b)=>a+", "+b));
                }
            }
        }

        private void InjectTab(Type tabType, Func<ThingDef, bool> qualifier)
        {
            var defs = DefDatabase<ThingDef>.AllDefs.Where(qualifier).ToList();
            defs.RemoveDuplicates();

            var tabBase = InspectTabManager.GetSharedInstance(tabType);

            foreach (var def in defs)
            {
                if (def.inspectorTabs == null || def.inspectorTabsResolved == null) continue;

                if (!def.inspectorTabs.Contains(tabType))
                {
                    def.inspectorTabs.Add(tabType);
                    def.inspectorTabsResolved.Add(tabBase);
                    //Log.Message(def.defName+": "+def.inspectorTabsResolved.Select(d=>d.GetType().Name).Aggregate((a,b)=>a+", "+b));
                }
            }
        }

    }
}
