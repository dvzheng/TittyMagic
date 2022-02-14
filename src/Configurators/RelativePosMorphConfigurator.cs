﻿using SimpleJSON;
using System;
using System.Collections.Generic;
using static TittyMagic.Globals;

namespace TittyMagic
{
    internal class RelativePosMorphConfigurator : MVRScript, IConfigurator
    {
        public Dictionary<string, string> titles = new Dictionary<string, string>
        {
            { Direction.DOWN_L, "Down force morphs L" },
            { Direction.DOWN_R, "Down force morphs R" },
            { Direction.UP_L, "Up force morphs L" },
            { Direction.UP_R, "Up force morphs R" },
            //{ Direction.BACK, "Back force morphs" },
            //{ Direction.FORWARD, "Forward force morphs" },
            { Direction.LEFT_L, "Left force morphs L" },
            { Direction.LEFT_R, "Left force morphs R" },
            { Direction.RIGHT_L, "Right force morphs L" },
            { Direction.RIGHT_R, "Right force morphs R" },
        };

        private string _lastBrowseDir;
        private const string _saveExt = "json";

        private JSONStorableBool _enableAdjustment;
        private JSONStorableString _debugInfo;

        public JSONStorableBool EnableAdjustment => _enableAdjustment;
        public JSONStorableString DebugInfo => _debugInfo;

        private Dictionary<string, Dictionary<string, ConfiguratorUISection>> _UISectionGroups;

        public void UpdateValueSlider(string sectionGroupName, string configName, float value)
        {
            if(!_UISectionGroups.ContainsKey(sectionGroupName))
            {
                return;
            }
            var sectionGroup = _UISectionGroups[sectionGroupName];
            if(sectionGroup.ContainsKey(configName))
            {
                var section = _UISectionGroups[sectionGroupName][configName];
                section.ValueStorable.val = value;
            }
        }

        public void InitMainUI()
        {
            ResetUISectionGroups();
            _enableAdjustment = UI.NewToggle(this, "Enable", true, false);
            _debugInfo = UI.NewTextField(this, "positionDiffInfo", "", 20, 115, true);
            var exportValuesButton = CreateButton("Export values JSON", false);
            AddExportButtonListener(exportValuesButton);
        }

        private void AddExportButtonListener(UIDynamicButton button)
        {
            button.button.onClick.AddListener(() =>
            {
                SuperController.singleton.NormalizeMediaPath($@"{PLUGIN_PATH}settings\"); // Sets dir if path exists
                SuperController.singleton.GetMediaPathDialog((string path) =>
                {
                    var json = new JSONClass();
                    _UISectionGroups.Keys.ToList().ForEach(key =>
                    {
                        var sectionGroup = _UISectionGroups[key];
                        var groupJson = new JSONClass();
                        sectionGroup.Values.ToList().ForEach(item =>
                        {
                            groupJson[item.Name]["Value"].AsFloat = Calc.RoundToDecimals(item.ValueStorable.val, 1000f);
                        });
                        json[key] = groupJson;
                    });
                    Persistence.SaveToPath(this, json, path, _saveExt, (dir) =>
                    {
                        _lastBrowseDir = dir;
                    });
                }, _saveExt);

                // Update the browser to be a Save browser
                uFileBrowser.FileBrowser browser = SuperController.singleton.mediaFileBrowserUI;
                browser.SetTextEntry(true);
                browser.fileEntryField.text = string.Format("{0}.{1}", ((int) (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds).ToString(), _saveExt);
                browser.ActivateFileNameField();
            });
        }

        public void ResetUISectionGroups()
        {
            _UISectionGroups = new Dictionary<string, Dictionary<string, ConfiguratorUISection>> {
                { Direction.DOWN_L, new Dictionary<string, ConfiguratorUISection>() },
                { Direction.DOWN_R, new Dictionary<string, ConfiguratorUISection>() },
                { Direction.UP_L, new Dictionary<string, ConfiguratorUISection>() },
                { Direction.UP_R, new Dictionary<string, ConfiguratorUISection>() },
                //{ Direction.BACK, new Dictionary<string, ConfiguratorUISection>() },
                //{ Direction.FORWARD, new Dictionary<string, ConfiguratorUISection>() },
                { Direction.LEFT_L, new Dictionary<string, ConfiguratorUISection>() },
                { Direction.LEFT_R, new Dictionary<string, ConfiguratorUISection>() },
                { Direction.RIGHT_L, new Dictionary<string, ConfiguratorUISection>() },
                { Direction.RIGHT_R, new Dictionary<string, ConfiguratorUISection>() },
            };
        }

        public void InitUISectionGroup(string key, List<Config> configs)
        {
            UI.NewTextField(this, titles[key], $"{titles[key]}", 40, 115, false);
            var saveButton = CreateButton("Save JSON", true);
            var loadButton = CreateButton("Load JSON", true);

            var group = _UISectionGroups[key];

            foreach(MorphConfig config in configs)
            {
                group.Add(config.Name, new ConfiguratorUISection(this, config));
            }

            AddSaveButtonListener(saveButton, group.Values.ToList());
            AddLoadButtonListener(loadButton, group.Values.ToList());
        }

        //dummy
        public void AddButtonListeners()
        {
        }

        private void AddSaveButtonListener(UIDynamicButton button, List<ConfiguratorUISection> sections)
        {
            button.button.onClick.AddListener(() =>
            {
                SuperController.singleton.NormalizeMediaPath(_lastBrowseDir ?? Persistence.MakeDefaultDir()); // Sets dir if path exists
                SuperController.singleton.GetMediaPathDialog((string path) => HandleSave(path, sections), _saveExt);

                // Update the browser to be a Save browser
                uFileBrowser.FileBrowser browser = SuperController.singleton.mediaFileBrowserUI;
                browser.SetTextEntry(true);
                browser.fileEntryField.text = string.Format("{0}.{1}", ((int) (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds).ToString(), _saveExt);
                browser.ActivateFileNameField();
            });
        }

        protected void AddLoadButtonListener(UIDynamicButton button, List<ConfiguratorUISection> sections)
        {
            button.button.onClick.AddListener(() =>
            {
                SuperController.singleton.NormalizeMediaPath(_lastBrowseDir ?? Persistence.MakeDefaultDir()); // Sets dir if path exists
                SuperController.singleton.GetMediaPathDialog((string path) => HandleLoad(path, sections), _saveExt);
            });
        }

        protected void HandleSave(string path, List<ConfiguratorUISection> sections)
        {
            var json = new JSONClass();
            foreach(var item in sections)
            {
                json[item.Name]["IsNegative"].AsBool = item.IsNegativeStorable.val;
                json[item.Name]["Multiplier1"].AsFloat = Calc.RoundToDecimals(item.Multiplier1Storable.val, 1000f);
                json[item.Name]["Multiplier2"].AsFloat = Calc.RoundToDecimals(item.Multiplier2Storable.val, 1000f);
            }
            Persistence.SaveToPath(this, json, path, _saveExt, (dir) =>
            {
                _lastBrowseDir = dir;
            });
        }

        // TODO fix, doesn't work
        protected void HandleLoad(string path, List<ConfiguratorUISection> sections)
        {
            Persistence.LoadFromPath(this, path, (dir, json) =>
            {
                _lastBrowseDir = dir;
                foreach(var item in sections)
                {
                    if(json.HasKey(item.Name))
                    {
                        item.Multiplier1Storable.val = json[item.Name]["Multiplier1"].AsFloat;
                        item.Multiplier2Storable.val = json[item.Name]["Multiplier2"].AsFloat;
                    }
                }
            });
        }
    }
}
