using System.Collections.Generic;
using System.Linq;
using Harmony;
using RimWorld;
using Verse;

namespace Hospitality.Harmony
{
    /// <summary>
    /// Allow colonists to talk to guests randomly
    /// </summary>
    internal static class Pawn_InteractionsTracker_Patch
    {
        [HarmonyPatch(typeof(Pawn_InteractionsTracker), "TryInteractRandomly")]
        public class TryInteractRandomly
        {
            [HarmonyPrefix]
            public static bool Replacement(Pawn_InteractionsTracker __instance, ref bool __result)
            {
                // Added
                var pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();

                if (!IsInteractable(pawn))
                {
                    __result = false;
                    return false;
                }
                var workingList = Traverse.Create(__instance).Field("workingList").GetValue<List<Pawn>>(); // Had to add

                // BASE
                if (__instance.InteractedTooRecentlyToInteract())
                {
                    __result = false;
                    return false;
                }
                if (!InteractionUtility.CanInitiateRandomInteraction(pawn))
                {
                    __result = false;
                    return false;
                }
                var collection = pawn.MapHeld.mapPawns.AllPawnsSpawned.Where(IsInteractable); // Added
                workingList.Clear();
                workingList.AddRange(collection);
                workingList.Shuffle<Pawn>();
                List<InteractionDef> allDefsListForReading = DefDatabase<InteractionDef>.AllDefsListForReading;
                for (int i = 0; i < workingList.Count; i++)
                {
                    Pawn p = workingList[i];
                    if (p != pawn && CanInteractNowWith(pawn, p) && InteractionUtility.CanReceiveRandomInteraction(p)
                        && !pawn.HostileTo(p))
                    {
                        InteractionDef intDef;
                        if (
                            allDefsListForReading.TryRandomElementByWeight(
                                (InteractionDef x) => x.Worker.RandomSelectionWeight(pawn, p), out intDef))
                        {
                            if (__instance.TryInteractWith(p, intDef))
                            {
                                __result = true;
                                return true;
                            }
                            Log.Error(pawn + " failed to interact with " + p);
                        }
                    }
                }
                __result = false;
                return false;
            }

            private static bool IsInteractable(Pawn pawn) // Added
            {
                return pawn != null && !pawn.Downed && pawn.RaceProps.Humanlike && pawn.relations != null
                       && pawn.story != null && pawn.story.traits != null;
            }

            private static bool CanInteractNowWith(Pawn pawn, Pawn recipient) // Had to add, copy
            {
                return recipient.Spawned
                       && ((pawn.Position - recipient.Position).LengthHorizontalSquared <= 36.0
                           && InteractionUtility.CanInitiateInteraction(pawn)
                           && (InteractionUtility.CanReceiveInteraction(recipient)
                               && GenSight.LineOfSight(pawn.Position, recipient.Position, pawn.MapHeld, true)));
            }
        }
    }
}