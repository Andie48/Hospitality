using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Hospitality
{
    public abstract class JobDriver_GuestBase : JobDriver_ChatWithPrisoner
    {
        public static readonly JobDef therapyJobDef = DefDatabase<JobDef>.GetNamedSilentFail("ReceiveTherapy");
        
        public static Toil GotoGuest(Pawn pawn, Pawn talkee, bool mayBeSleeping = false)
        {
            var toil = new Toil
            {
                initAction = () => pawn.pather.StartPath(talkee, PathEndMode.Touch),
                defaultCompleteMode = ToilCompleteMode.PatherArrival
            };
            toil.AddFailCondition(() => !GuestUtility.ViableGuestTarget(talkee, mayBeSleeping));
            return toil;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOnDowned(TargetIndex.A);
            this.FailOnNotCasualInterruptible(TargetIndex.A);

            var gotoGuest = GotoGuest(pawn, Talkee); // Jump target
            yield return gotoGuest;
            //yield return Toils_Reserve.ReserveQueue(TargetIndex.A);

            //yield return Toils_Interpersonal.WaitToBeAbleToInteract(pawn);
            yield return Toils_General.Wait(10).JumpIf(() => !CanInteract(pawn, Talkee), gotoGuest);

            yield return Interact(Talkee, InteractionDef, GuestUtility.InteractIntervalAbsoluteMin);

            foreach (var toil in Perform())
            {
                yield return toil;
            }
            yield return Toils_Interpersonal.SetLastInteractTime(TargetIndex.A);
        }

        protected abstract InteractionDef InteractionDef { get; }

        protected abstract IEnumerable<Toil> Perform(); 

        protected Toil Interact(Pawn talkee, InteractionDef intDef, int duration)
        {
            var toil = new Toil {
                initAction = () => {
                    if (talkee.interactions.InteractedTooRecentlyToInteract()
                        || pawn.interactions.InteractedTooRecentlyToInteract()) return;

                    PawnUtility.ForceWait(talkee, duration, pawn);
                    TargetThingB = pawn;
                    MoteMaker.MakeInteractionBubble(pawn, talkee, intDef.interactionMote, intDef.Symbol);
                }, 
                socialMode = RandomSocialMode.Off,
                defaultCompleteMode = ToilCompleteMode.Delay,
                defaultDuration =  duration
            };
            return toil.WithProgressBarToilDelay(TargetIndex.B);
        }

        protected virtual bool FailCondition()
        {
            return !GuestUtility.ViableGuestTarget(Talkee) || (!pawn.HasReserved(Talkee) && !pawn.CanReserve(Talkee));
        }

        protected static bool CanInteract(Pawn pawn, Pawn talkee)
        {
            if (IsBusy(talkee)) return false;
            if (talkee.Map.reservationManager.FirstRespectedReserver(talkee, pawn) == pawn) return true;
            return false;
        }

        protected static bool IsBusy(Pawn p)
        {
            return p.interactions.InteractedTooRecentlyToInteract() || IsInTherapy(p);
        }

        // Compatibility fix to Therapy mod
        private static bool IsInTherapy(Pawn p)
        {
            return therapyJobDef != null && p.CurJob != null && p.CurJob.def == therapyJobDef;
        }
    }
}