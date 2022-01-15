﻿using System;
using System.Collections.Generic;
using UnityEngine;
using static TittyMagic.Utils;

namespace TittyMagic
{
    internal class RelativePosMorphHandler
    {
        private MVRScript _script;
        private RelativePosMorphConfigurator _configurator;

        private bool _useConfigurator;

        private float mass;
        private float softness;

        private Dictionary<string, List<MorphConfig>> _configSets;

        private List<MorphConfig> _downForceConfigs;

        private List<MorphConfig> _upForceConfigs;

        private List<MorphConfig> _backForceConfigs;

        private List<MorphConfig> _forwardForceConfigs;

        private List<MorphConfig> _leftForceConfigs;

        private List<MorphConfig> _rightForceConfigs;

        public RelativePosMorphHandler(MVRScript script)
        {
            _script = script;

            try
            {
                _configurator = (RelativePosMorphConfigurator) _script;
                _configurator.InitMainUI();
                _configurator.EnableAdjustment.toggle.onValueChanged.AddListener((bool val) =>
                {
                    if(!val)
                    {
                        ResetAll();
                    }
                });
                _useConfigurator = true;
            }
            catch(Exception)
            {
                _useConfigurator = false;
            }
        }

        public void LoadSettings(string mode)
        {
            _downForceConfigs = new List<MorphConfig>();
            _upForceConfigs = new List<MorphConfig>();
            _backForceConfigs = new List<MorphConfig>();
            _forwardForceConfigs = new List<MorphConfig>();
            _leftForceConfigs = new List<MorphConfig>();
            _rightForceConfigs = new List<MorphConfig>();
            LoadSettingsFromFile(mode, "downForce", _downForceConfigs);
            LoadSettingsFromFile(mode, "upForce", _upForceConfigs);
            LoadSettingsFromFile(mode, "backForce", _backForceConfigs);
            LoadSettingsFromFile(mode, "forwardForce", _forwardForceConfigs);
            LoadSettingsFromFile(mode, "leftForce", _leftForceConfigs);
            LoadSettingsFromFile(mode, "rightForce", _rightForceConfigs);
            _configSets = new Dictionary<string, List<MorphConfig>>
            {
                { Direction.DOWN, _downForceConfigs },
                { Direction.UP, _upForceConfigs },
                { Direction.BACK, _backForceConfigs },
                { Direction.FORWARD, _forwardForceConfigs },
                { Direction.LEFT, _leftForceConfigs },
                { Direction.RIGHT, _rightForceConfigs },
            };

            //not working properly yet when changing mode on the fly
            if(_useConfigurator)
            {
                _configurator.ResetUISectionGroups();
                _configurator.InitUISectionGroup(Direction.DOWN, _downForceConfigs);
                _configurator.InitUISectionGroup(Direction.UP, _upForceConfigs);
                _configurator.InitUISectionGroup(Direction.BACK, _backForceConfigs);
                _configurator.InitUISectionGroup(Direction.FORWARD, _forwardForceConfigs);
                _configurator.InitUISectionGroup(Direction.LEFT, _leftForceConfigs);
                _configurator.InitUISectionGroup(Direction.RIGHT, _rightForceConfigs);
            }
        }

        private void LoadSettingsFromFile(string mode, string fileName, List<MorphConfig> configs)
        {
            Persistence.LoadModeMorphSettings(_script, mode, $"{fileName}.json", (dir, json) =>
            {
                foreach(string name in json.Keys)
                {
                    configs.Add(new MorphConfig(
                        name,
                        json[name]["IsNegative"].AsBool,
                        json[name]["Multiplier1"].AsFloat,
                        json[name]["Multiplier2"].AsFloat
                    ));
                }
            });
        }

        public bool IsEnabled()
        {
            if(!_useConfigurator)
            {
                return true;
            }
            return _configurator.EnableAdjustment.val;
        }

        public void UpdateDebugInfo(string text)
        {
            if(!_useConfigurator)
            {
                return;
            }
            _configurator.DebugInfo.val = text;
        }

        public void Update(
            float angleY,
            float positionDiffZ,
            float mass,
            float softness
        )
        {
            this.mass = mass;
            this.softness = softness;

            // TODO separate l/r morphs only, separate calculation of diff
            //left
            //if(x <= 0)
            //{
            //    ResetMorphs(Direction.LEFT);
            //    UpdateMorphs(Direction.RIGHT, -x);
            //}
            //// right
            //else
            //{
            //    ResetMorphs(Direction.RIGHT);
            //    UpdateMorphs(Direction.LEFT, x);
            //}

            // https://www.desmos.com/calculator/lhcynyc9wt
            // small breasts experience more effect from angle, large breasts experience less effect from angle
            // breasts with mass of about 0.33 = 0.66kg experience the normal unscaled angleY
            float scaledAngleY = angleY / Mathf.Pow(3f * mass, 1/5f);

            float effectY = Calc.InverseSmoothStep(75, Mathf.Abs(scaledAngleY), 0.15f, 0.5f);
            float effectZ = Calc.InverseSmoothStep(1, Mathf.Abs(positionDiffZ), 0.15f, 0.5f);

            // up
            if(scaledAngleY >= 0)
            {
                ResetMorphs(Direction.DOWN);
                UpdateMorphs(Direction.UP, effectY);
            }
            // down
            else
            {
                ResetMorphs(Direction.UP);
                UpdateMorphs(Direction.DOWN, effectY);
            }

            // forward
            if(positionDiffZ <= 0)
            {
                ResetMorphs(Direction.BACK);
                UpdateMorphs(Direction.FORWARD, effectZ);
            }
            // back
            else
            {
                ResetMorphs(Direction.FORWARD);
                UpdateMorphs(Direction.BACK, effectZ);
            }

            string infoText =
                    $"{NameValueString("angleY", angleY, 1000f)} \n" +
                    $"{NameValueString("effectY", effectY, 1000f)} \n" +
                    $"{NameValueString("diffZ", positionDiffZ, 10000f)} \n" +
                    $"{NameValueString("effectZ", effectZ, 1000f)} \n";
            UpdateDebugInfo(infoText);
        }

        private void UpdateMorphs(string configSetName, float effect)
        {
            foreach(var config in _configSets[configSetName])
            {
                UpdateValue(config, effect, mass, softness);
                if(_useConfigurator)
                {
                    _configurator.UpdateValueSlider(configSetName, config.Name, config.Morph.morphValue);
                }
            }
        }

        private void UpdateValue(MorphConfig config, float effect, float mass, float softness)
        {
            float value =
                softness * config.Multiplier1 * effect / 2 +
                mass * config.Multiplier2 * effect / 2;

            bool inRange = config.IsNegative ? value < 0 : value > 0;
            config.Morph.morphValue = inRange ? value : 0;
        }

        public void ResetAll()
        {
            ResetMorphs(Direction.DOWN);
            ResetMorphs(Direction.UP);
            ResetMorphs(Direction.BACK);
            ResetMorphs(Direction.FORWARD);
            ResetMorphs(Direction.LEFT);
            ResetMorphs(Direction.RIGHT);
        }

        private void ResetMorphs(string configSetName)
        {
            foreach(var config in _configSets[configSetName])
            {
                config.Morph.morphValue = 0;
                if(_useConfigurator)
                {
                    _configurator.UpdateValueSlider(configSetName, config.Name, 0);
                }
            }
        }
    }
}
