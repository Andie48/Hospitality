using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HugsLib;
using Verse;

namespace Hospitality
{
    internal class ModBaseHospitality : ModBase
    {
        private static List<Action> TickActions = new List<Action>();
        private static Assembly Assembly { get { return Assembly.GetAssembly(typeof(ModBaseHospitality)); } }
        private static string AssemblyName { get { return Assembly.FullName.Split(',').First(); } }

        private static void Inject()
        {
            var injector = new Hospitality_SpecialInjector();
            if (injector.Inject()) Log.Message(AssemblyName + " injected.");
            else Log.Error(AssemblyName + " failed to get injected properly.");
        }

        public override string ModIdentifier { get { return "Hospitality"; } }

        public override void Initialize()
        {
            Inject();
        }

        public override void Tick(int currentTick)
        {
            foreach (var action in TickActions)
            {
                action();
            }
            TickActions.Clear();
        }

        public static void RegisterTickAction(Action action)
        {
            TickActions.Add(action);
        }
    }
}