﻿//#define SHOW_DEBUG

using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TittyMagic
{
    internal class Script : MVRScript
    {
        private static readonly Version v2_1 = new Version("2.1.0");
        public static readonly Version version = new Version("0.0.0");

        private bool enableUpdate = true;
        private bool physicsUpdateInProgress = false;

        private Transform chest;
        private DAZCharacterSelector geometry;

        private List<DAZPhysicsMeshSoftVerticesSet> rightBreastMainGroupSets;
        private Mesh inMemoryMesh;
        private float massEstimate;
        private float softVolume; // cm^3; spheroid volume estimation of right breast
        private float gravityLogAmount;

        private AtomScaleListener atomScaleListener;
        private BreastMorphListener breastMorphListener;

        private GravityMorphHandler gravityMorphH;
        private NippleErectionMorphHandler nippleMorphH;
        private StaticPhysicsHandler staticPhysicsH;
        private GravityPhysicsHandler gravityPhysicsH;

        private JSONStorableString titleUIText;
        private JSONStorableString statusUIText;

        //registered storables

        private JSONStorableString pluginVersionStorable;
        private JSONStorableFloat softness;
        private JSONStorableFloat gravity;
        private JSONStorableBool linkSoftnessAndGravity;
        private JSONStorableFloat nippleErection;

        private bool restoringFromJson = false;
        private float? legacySoftnessFromJson;
        private float? legacyGravityFromJson;

#if SHOW_DEBUG
        protected JSONStorableString baseDebugInfo = new JSONStorableString("Base Debug Info", "");
        protected JSONStorableString physicsDebugInfo = new JSONStorableString("Physics Debug Info", "");
        protected JSONStorableString morphDebugInfo = new JSONStorableString("Morph Debug Info", "");
#endif

        public override void Init()
        {
            try
            {
                pluginVersionStorable = new JSONStorableString("Version", "");
                pluginVersionStorable.val = $"{version}";
                RegisterString(pluginVersionStorable);

                if(containingAtom.type != "Person")
                {
                    Log.Error($"Plugin is for use with 'Person' atom, not '{containingAtom.type}'");
                    return;
                }

                if(!UserPreferences.singleton.softPhysics)
                {
                    UserPreferences.singleton.softPhysics = true;
                    Log.Message($"Soft physics has been enabled in VaM preferences.");
                }

                AdjustJoints breastControl = containingAtom.GetStorableByID("BreastControl") as AdjustJoints;
                DAZPhysicsMesh breastPhysicsMesh = containingAtom.GetStorableByID("BreastPhysicsMesh") as DAZPhysicsMesh;
                chest = containingAtom.GetStorableByID("chest").transform;
                geometry = containingAtom.GetStorableByID("geometry") as DAZCharacterSelector;
                rightBreastMainGroupSets = breastPhysicsMesh.softVerticesGroups
                    .Find(it => it.name == "right")
                    .softVerticesSets;
                inMemoryMesh = new Mesh();

                Globals.BREAST_CONTROL = breastControl;
                Globals.BREAST_PHYSICS_MESH = breastPhysicsMesh;
                Globals.MORPH_UI = geometry.morphsControlUI;

                atomScaleListener = new AtomScaleListener(containingAtom.GetStorableByID("rescaleObject").GetFloatJSONParam("scale"));
                breastMorphListener = new BreastMorphListener(geometry.morphBank1.morphs);

                gravityMorphH = new GravityMorphHandler();
                nippleMorphH = new NippleErectionMorphHandler();
                gravityPhysicsH = new GravityPhysicsHandler();
                staticPhysicsH = new StaticPhysicsHandler();

                InitPluginUILeft();
                InitPluginUIRight();
                InitSliderListeners();
                UpdateLogarithmicGravityAmount(gravity.val);

                SetPhysicsDefaults();
                StartCoroutine(RefreshStaticPhysics(atomScaleListener.Value));

                StartCoroutine(MigrateFromPre2_1());
            }
            catch(Exception e)
            {
                Log.Error("Exception caught: " + e);
            }
        }

        #region User interface

        private void InitPluginUILeft()
        {
            bool rightSide = false;
            titleUIText = NewTextField("titleText", 36, 100, rightSide);
            titleUIText.SetVal($"{nameof(TittyMagic)}\n<size=28>v{version}</size>");

            // doesn't just init UI, also variables...
            softness = NewFloatSlider("Breast softness", 50f, Const.SOFTNESS_MIN, Const.SOFTNESS_MAX, "F0", rightSide);
            gravity = NewFloatSlider("Breast gravity", 50f, Const.GRAVITY_MIN, Const.GRAVITY_MAX, "F0", rightSide);
            linkSoftnessAndGravity = NewToggle("Link softness and gravity", false);
            linkSoftnessAndGravity.val = true;

            CreateNewSpacer(10f);

            nippleErection = NewFloatSlider("Erect nipples", 0f, 0f, 1.0f, "F2", rightSide);

#if SHOW_DEBUG
            UIDynamicTextField angleInfoField = CreateTextField(baseDebugInfo, rightSide);
            angleInfoField.height = 125;
            angleInfoField.UItext.fontSize = 26;
            UIDynamicTextField physicsInfoField = CreateTextField(physicsDebugInfo, rightSide);
            physicsInfoField.height = 450;
            physicsInfoField.UItext.fontSize = 26;
#endif
        }

        private void InitPluginUIRight()
        {
            bool rightSide = true;
            statusUIText = NewTextField("statusText", 28, 100, rightSide);
#if SHOW_DEBUG
            UIDynamicTextField morphInfo = CreateTextField(morphDebugInfo, rightSide);
            morphInfo.height = 1085;
            morphInfo.UItext.fontSize = 26;
#else
            JSONStorableString usage1Area = NewTextField("Usage Info Area 1", 28, 255, rightSide);
            string usage1 = "\n";
            usage1 += "Breast softness adjusts soft physics settings from very firm to very soft.\n\n";
            usage1 += "Breast gravity adjusts how much pose morphs shape the breasts in all orientations.";
            usage1Area.SetVal(usage1);
#endif
        }

        private JSONStorableFloat NewFloatSlider(
            string paramName,
            float startingValue,
            float minValue,
            float maxValue,
            string valueFormat,
            bool rightSide
        )
        {
            JSONStorableFloat storable = new JSONStorableFloat(paramName, startingValue, minValue, maxValue);
            storable.storeType = JSONStorableParam.StoreType.Physical;
            RegisterFloat(storable);
            UIDynamicSlider slider = CreateSlider(storable, rightSide);
            slider.valueFormat = valueFormat;
            return storable;
        }

        private JSONStorableString NewTextField(string paramName, int fontSize, int height = 100, bool rightSide = false)
        {
            JSONStorableString storable = new JSONStorableString(paramName, "");
            UIDynamicTextField textField = CreateTextField(storable, rightSide);
            textField.UItext.fontSize = fontSize;
            textField.height = height;
            return storable;
        }

        private JSONStorableBool NewToggle(string paramName, bool rightSide = false)
        {
            JSONStorableBool storable = new JSONStorableBool(paramName, false);
            CreateToggle(storable, rightSide);
            RegisterBool(storable);
            return storable;
        }

        private void CreateNewSpacer(float height, bool rightSide = false)
        {
            UIDynamic spacer = CreateSpacer(rightSide);
            spacer.height = height;
        }

        #endregion User interface

        private void InitSliderListeners()
        {
            softness.slider.onValueChanged.AddListener((float val) =>
            {
                if(linkSoftnessAndGravity.val)
                {
                    gravity.val = val;
                    UpdateLogarithmicGravityAmount(val);
                }
                staticPhysicsH.FullUpdate(massEstimate, val, nippleErection.val);
            });
            gravity.slider.onValueChanged.AddListener((float val) =>
            {
                if(linkSoftnessAndGravity.val)
                {
                    softness.val = val;
                }
                UpdateLogarithmicGravityAmount(val);
                staticPhysicsH.FullUpdate(massEstimate, softness.val, nippleErection.val);
            });
            nippleErection.slider.onValueChanged.AddListener((float val) =>
            {
                nippleMorphH.Update(val);
                staticPhysicsH.UpdateNipplePhysics(massEstimate, softness.val, val);
            });
        }

        private void UpdateLogarithmicGravityAmount(float val)
        {
            gravityLogAmount = Mathf.Log(10 * Const.ConvertToLegacyVal(val) - 3.35f);
        }

        // TODO merge
        private void SetPhysicsDefaults()
        {
            // In/Out auto morphs off
            containingAtom.GetStorableByID("BreastInOut").SetBoolParamValue("enabled", false);
            containingAtom.GetStorableByID("SoftBodyPhysicsEnabler").SetBoolParamValue("enabled", true);
            // Hard colliders on
            geometry.useAuxBreastColliders = true;
            staticPhysicsH.SetPhysicsDefaults();
        }

        private IEnumerator MigrateFromPre2_1()
        {
            yield return new WaitForEndOfFrame();
            if(!restoringFromJson)
            {
                yield break;
            }

            if(legacySoftnessFromJson.HasValue)
            {
                softness.val = Const.ConvertFromLegacyVal(legacySoftnessFromJson.Value);
                Log.Message($"Converted legacy Breast softness {legacySoftnessFromJson.Value} in savefile to new slider value {softness.val}.");
            }

            if(legacyGravityFromJson.HasValue)
            {
                gravity.val = Const.ConvertFromLegacyVal(legacyGravityFromJson.Value);
                Log.Message($"Converted legacy Breast gravity {legacyGravityFromJson.Value} in savefile to new slider value {gravity.val}.");
            }

            restoringFromJson = false;
        }

        public void Update()
        {
            try
            {
                if(enableUpdate)
                {
                    if(!physicsUpdateInProgress && (breastMorphListener.Changed() || atomScaleListener.Changed()))
                    {
                        StartCoroutine(RefreshStaticPhysics(atomScaleListener.Value));
                    }

                    float roll = AngleCalc.Roll(chest.rotation);
                    float pitch = AngleCalc.Pitch(chest.rotation);
                    float scaleVal = BreastMassCalc.LegacyScale(massEstimate);

                    gravityMorphH.Update(roll, pitch, scaleVal, gravityLogAmount);
                    gravityPhysicsH.Update(roll, pitch, scaleVal, Const.ConvertToLegacyVal(gravity.val));
#if SHOW_DEBUG
                    SetBaseDebugInfo(roll, pitch);
                    morphDebugInfo.SetVal(gravityMorphH.GetStatus());
                    physicsDebugInfo.SetVal(staticPhysicsH.GetStatus() + gravityPhysicsH.GetStatus());
#endif
                }
            }
            catch(Exception e)
            {
                Log.Error("Exception caught: " + e);
                enableUpdate = false;
            }
        }

        private IEnumerator RefreshStaticPhysics(float atomScale)
        {
            physicsUpdateInProgress = true;
            while(breastMorphListener.Changed())
            {
                yield return null;
            }

            // Iterate the update a few times because each update changes breast shape and thereby the mass estimate.
            for(int i = 0; i < 6; i++)
            {
                // update only non-soft physics settings to improve performance
                UpdateMassEstimate(atomScale);
                staticPhysicsH.UpdateMainPhysics(massEstimate, softness.val);
                if(i > 0)
                {
                    yield return new WaitForSeconds(0.12f);
                }
            }

            UpdateMassEstimate(atomScale, updateUIStatus: true);
            staticPhysicsH.FullUpdate(massEstimate, softness.val, nippleErection.val);
            physicsUpdateInProgress = false;
        }

        private void UpdateMassEstimate(float atomScale, bool updateUIStatus = false)
        {
            Vector3 dimensions = BoundsSize();

            softVolume = BreastMassCalc.EstimateVolume(dimensions, atomScale);
            float mass = BreastMassCalc.VolumeToMass(softVolume);

            if(mass > Const.MASS_MAX)
            {
                massEstimate = Const.MASS_MAX;
                if(updateUIStatus)
                {
                    float excess = Calc.RoundToDecimals(mass - Const.MASS_MAX, 1000f);
                    statusUIText.SetVal(MassExcessStatus(excess));
                }
            }
            else if(mass < Const.MASS_MIN)
            {
                massEstimate = Const.MASS_MIN;
                if(updateUIStatus)
                {
                    float shortage = Calc.RoundToDecimals(Const.MASS_MIN - mass, 1000f);
                    statusUIText.SetVal(MassShortageStatus(shortage));
                }
            }
            else
            {
                massEstimate = mass;
                if(updateUIStatus)
                {
                    statusUIText.SetVal("");
                }
            }
        }

        private Vector3 BoundsSize()
        {
            Vector3[] vertices = rightBreastMainGroupSets
                .Select(it => it.jointRB.position).ToArray();

            inMemoryMesh.vertices = vertices;
            inMemoryMesh.RecalculateBounds();
            return inMemoryMesh.bounds.size;
        }

        private string MassExcessStatus(float value)
        {
            Color color = Color.Lerp(
                new Color(0.5f, 0.5f, 0.0f, 1f),
                Color.red,
                value
            );
            return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}><size=28>" +
                $"Estimated mass is <b>{value}</b> over the 2.000 maximum.\n" +
                $"</size></color>";
        }

        private string MassShortageStatus(float value)
        {
            Color color = Color.Lerp(
                new Color(0.5f, 0.5f, 0.0f, 1f),
                Color.red,
                value*10
            );
            return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}><size=28>" +
                $"Estimated mass is <b>{value}</b> below the 0.100 minimum.\n" +
                $"</size></color>";
        }

        public override void RestoreFromJSON(JSONClass json, bool restorePhysical = true, bool restoreAppearance = true, JSONArray presetAtoms = null, bool setMissingToDefault = true)
        {
            restoringFromJson = true;

            try
            {
                CheckSavedVersion(json, () =>
                {
                    //should never occur
                    if(version.CompareTo(v2_1) < 0)
                    {
                        return;
                    }

                    //needs conversion from legacy values
                    if(json.HasKey("Breast softness"))
                    {
                        float val = json["Breast softness"].AsFloat;
                        if(val <= Const.LEGACY_MAX)
                        {
                            legacySoftnessFromJson = val;
                        }
                    }

                    if(json.HasKey("Breast gravity"))
                    {
                        float val = json["Breast gravity"].AsFloat;
                        if(val <= Const.LEGACY_MAX)
                        {
                            legacyGravityFromJson = val;
                        }
                    }
                });
            }
            catch(Exception)
            {
            }

            base.RestoreFromJSON(json, restorePhysical, restoreAppearance, presetAtoms, setMissingToDefault);
        }

        private void CheckSavedVersion(JSONClass json, Action callback)
        {
            if(json["Version"] != null)
            {
                Version vSave = new Version(json["Version"].Value);
                //no conversion from legacy values needed
                if(vSave.CompareTo(v2_1) >= 0)
                {
                    return;
                }
            }

            callback();
        }

        private void OnDestroy()
        {
            gravityPhysicsH.ResetAll();
            gravityMorphH.ResetAll();
            nippleMorphH.ResetAll();
        }

        private void OnDisable()
        {
            gravityPhysicsH.ResetAll();
            gravityMorphH.ResetAll();
            nippleMorphH.ResetAll();
        }

#if SHOW_DEBUG

        private void SetBaseDebugInfo(float roll, float pitch)
        {
            float currentSoftVolume = BreastMassCalc.EstimateVolume(BoundsSize(), atomScaleListener.Value);
            baseDebugInfo.SetVal(
                $"{Formatting.NameValueString("Roll", roll, 100f, 15)}\n" +
                $"{Formatting.NameValueString("Pitch", pitch, 100f, 15)}\n" +
                $"volume: {softVolume}\n" +
                $"current volume: {currentSoftVolume}"
            );
        }

#endif
    }
}
