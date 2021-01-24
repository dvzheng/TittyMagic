﻿using System.Collections.Generic;

namespace everlaster
{
    // TODO refactor to not use Dictionary
    class GravityMorphConfig
    {
        public string Name { get; set; }
        public DAZMorph Morph { get; set; }
        public Dictionary<string, float?[]> Multipliers { get; set; }

        public GravityMorphConfig(string name, Dictionary<string, float?[]> multipliers)
        {
            Name = name;
            Morph = Globals.MORPH_UI.GetMorphByDisplayName(name);
            Multipliers = multipliers;
            if(Morph == null)
            {
                Log.Error($"Morph with name {name} not found!", nameof(GravityMorphConfig));
            }
        }

        public void UpdatePitchVal(string type, float effect, float scale, float softness, float sag)
        {
            float?[] m = Multipliers[type];

            // m[0] is the base multiplier for the morph in this type (UPRIGHT etc.)
            // m[1] scales the breast softness slider for this base multiplier
            //      - if null, slider setting is ignored
            // m[2] scales the size calibration slider for this base multiplier
            //      - if null, slider setting is ignored
            float softnessFactor = m[1].HasValue ? (float) m[1] * softness : 1;
            float scaleFactor = m[2].HasValue ? scale * (float) m[2] : 1;
            float morphValue = sag * (float) m[0] * (
                (softnessFactor * effect / 2) +
                (scaleFactor * effect / 2)
            );

            if(morphValue > 0)
            {
                Morph.morphValue = morphValue >= 1.33f ? 1.33f : morphValue;
            }
            else
            {
                Morph.morphValue = morphValue < -1.33f ? -1.33f : morphValue;
            }
        }

        public void UpdateRollVal(string type, float effect, float scale, float softness, float sag)
        {
            float?[] m = Multipliers[type];

            float softnessFactor = m[1].HasValue ? (float) m[1] * softness : 1;
            float scaleFactor = m[2].HasValue ? scale * (float) m[2] : 1;
            float sagMultiplierVal = sag >= 1 ?
                1 + (sag - 1) / 2 :
                sag;
            float morphValue = sagMultiplierVal * (float) m[0] * (
                (softnessFactor * effect / 2) +
                (scaleFactor * effect / 2)
            );

            if(morphValue > 0)
            {
                Morph.morphValue = morphValue >= 1.33f ? 1.33f : morphValue;
            }
            else
            {
                Morph.morphValue = morphValue < -1.33f ? -1.33f : morphValue;
            }
        }

        public void Reset()
        {
            Morph.morphValue = 0;
        }
    }
}