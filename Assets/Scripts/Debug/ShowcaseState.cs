using System;
using System.Collections.Generic;

namespace NullProgrammer.Showcase
{
    internal sealed class ShowcaseState
    {
        internal static readonly Personality[] Personalities = (Personality[])Enum.GetValues(typeof(Personality));

        internal int CodeQuality;
        internal int Suspicion;
        internal int Connectedness;
        internal int Architect;
        internal int Pragmatist;
        internal int Detective;
        internal int SkillPoints;
        internal WeekendChoice? Weekend;
        internal bool UnlockWeekend;
        internal readonly int[] Levels = new int[Personalities.Length];
        internal readonly HashSet<string> Clues = new HashSet<string>(StringComparer.Ordinal);

        internal static ShowcaseState Capture(GameManager game)
        {
            var state = new ShowcaseState();
            foreach (Personality personality in Personalities)
                state.Levels[(int)personality] = PersonalitySkills.GetSavedLevel(personality);

            if (game == null) return state;
            state.CodeQuality = game.CodeQuality;
            state.Suspicion = game.SteveSuspicion;
            state.Connectedness = game.Connectedness;
            state.Architect = game.GrandArchitectStat;
            state.Pragmatist = game.PragmatistStat;
            state.Detective = game.DetectiveStat;
            state.SkillPoints = game.UnspentSkillPoints;
            state.Weekend = game.SelectedWeekendChoice;
            state.UnlockWeekend = game.ShowcaseWeekendUnlocked;
            state.Clues.UnionWith(game.DiscoveredClues);
            return state;
        }
    }
}
