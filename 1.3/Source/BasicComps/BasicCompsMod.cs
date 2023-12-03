using System.Collections.Generic;
using System.Linq;
using ModBase;
using RimWorld;
using Verse;

namespace BasicComps
{
    public class BasicCompsMod : BaseMod<BasicCompsSettings>
    {
        public static ThingDef BasicComp;
        public static List<ThingDef> PossibleDefs;
        private static readonly Dictionary<ThingDef, RecipeDef> recipes = new Dictionary<ThingDef, RecipeDef>();

        public BasicCompsMod(ModContentPack content) : base("argon.basic_components", null, content, false)
        {
            SettingsRenderer.__DEBUG = true;
        }

        public override void ApplySettings()
        {
            base.ApplySettings();
            foreach (var (cc, def) in from def in PossibleDefs let tdcc = def.CostList.FirstOrDefault(cc => cc.thingDef == BasicComp) where tdcc != null select (tdcc, def))
            {
                cc.thingDef = ThingDefOf.ComponentIndustrial;
                if (!recipes.ContainsKey(def)) continue;
                var ic = recipes[def].ingredients.FirstOrDefault(i => i.filter.Allows(BasicComp));
                if (ic == null) continue;
                ic.filter.SetAllow(BasicComp, false);
                ic.filter.SetAllow(ThingDefOf.ComponentIndustrial, true);
            }

            foreach (var (cc, def) in from def in Settings.SelectedDefs
                let tdcc = def.CostList.FirstOrDefault(cc => cc.thingDef == ThingDefOf.ComponentIndustrial)
                where tdcc != null
                select (tdcc, def))
            {
                cc.thingDef = BasicComp;
                if (!recipes.ContainsKey(def)) continue;
                var ic = recipes[def].ingredients.FirstOrDefault(i => i.filter.Allows(ThingDefOf.ComponentIndustrial));
                if (ic == null) continue;
                ic.filter.SetAllow(ThingDefOf.ComponentIndustrial, false);
                ic.filter.SetAllow(BasicComp, true);
            }
        }

        public override void DoPostLoadSetup()
        {
            base.DoPostLoadSetup();
            PossibleDefs = DefDatabase<ThingDef>.AllDefs.Where(def => def.CostList != null && def.CostList.Any(cc => cc.thingDef == ThingDefOf.ComponentIndustrial))
                .OrderBy(def => def.label).ToList();
            foreach (var def in PossibleDefs)
            {
                var recipe = DefDatabase<RecipeDef>.GetNamedSilentFail("Make_" + def.defName);
                if (recipe != null) recipes.Add(def, recipe);
            }

            BasicComp = ThingDef.Named("BC_ComponentBasic");
        }
    }

    public class DefSelector : ICustomSettingsDraw
    {
        private Dictionary<ThingDef, bool> cache = new Dictionary<ThingDef, bool>();
        public List<ThingDef> SelectedDefs = new List<ThingDef>();

        public void Render(Listing_Standard listing, string label, string tooltip)
        {
            foreach (var def in BasicCompsMod.PossibleDefs)
            {
                var flag = cache[def];
                listing.CheckboxLabeled(def.LabelCap, ref flag);
                if (flag == cache[def]) continue;
                cache[def] = flag;
                if (flag)
                    SelectedDefs.Add(def);
                else
                    SelectedDefs.Remove(def);
            }
        }

        public float Height => BasicCompsMod.PossibleDefs.Count * 40f + 50f;

        public void Init()
        {
            if (SelectedDefs == null)
                SelectedDefs = DefDatabase<DefaultSettingsDef>.AllDefs.SelectMany(def => def.DefaultDefs.Select(DefDatabase<ThingDef>.GetNamedSilentFail).Where(d => d != null))
                    .ToList();
            if (SelectedDefs != null && BasicCompsMod.PossibleDefs != null) cache = BasicCompsMod.PossibleDefs.ToDictionary(def => def, def => SelectedDefs.Contains(def));
        }
    }

    public class BasicCompsSettings : BaseModSettings
    {
        public readonly DefSelector Selector = new DefSelector();

        public List<ThingDef> SelectedDefs => Selector.SelectedDefs;

        public override void ExposeData()
        {
            var list = SelectedDefs.Select(def => def.defName).ToList();
            Scribe_Collections.Look(ref list, "selected", LookMode.Value);
            Selector.SelectedDefs = list.Select(def => DefDatabase<ThingDef>.GetNamedSilentFail(def)).Where(def => def != null).ToList();
        }

        public override void Init()
        {
            base.Init();
            Selector.Init();
        }
    }

    public class DefaultSettingsDef : Def
    {
        public List<string> DefaultDefs;
    }
}