using System;
using UnityEngine.UI;

namespace TittyMagic
{
    public class Multiplier
    {
        public readonly Slider slider;
        public float mainMultiplier { get; private set; }
        public float? extraMultiplier { get; set; }
        public float? oppositeExtraMultiplier { get; set; }

        public Multiplier(Slider slider, Func<float, float> curve = null)
        {
            this.slider = slider;

            if(curve == null)
            {
                slider.onValueChanged.AddListener(
                    value => { mainMultiplier = value; }
                );
                mainMultiplier = slider.value;
            }
            else
            {
                slider.onValueChanged.AddListener(
                    value => { mainMultiplier = curve(value); }
                );
                mainMultiplier = curve(slider.value);
            }
        }
    }
}
