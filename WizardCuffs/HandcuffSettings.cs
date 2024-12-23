using ThunderRoad;
using UnityEngine;
using System.Collections.Generic;
using System;

namespace WizardCuffs
{
    public class HandcuffSettings : ThunderScript
    {
        #region Option Arrays
        public static ModOptionFloat[] DelayTimeValues()
        {
            ModOptionFloat[] array = new ModOptionFloat[11];
            for (int i = 0; i < array.Length; i++)
            {
                float value = 0.5f + (i * 0.25f);
                array[i] = new ModOptionFloat($"{value:F2}s", value);
            }
            return array;
        }

        public static ModOptionFloat[] DistanceValues()
        {
            ModOptionFloat[] array = new ModOptionFloat[11];
            for (int i = 0; i < array.Length; i++)
            {
                float value = 0.025f + (i * 0.025f);
                array[i] = new ModOptionFloat($"{value:F3}", value);
            }
            return array;
        }

        public static ModOptionFloat[] MassValues()
        {
            ModOptionFloat[] array = new ModOptionFloat[11];
            for (int i = 0; i < array.Length; i++)
            {
                float value = 1.0f + (i * 1.0f);
                array[i] = new ModOptionFloat($"{value:F1}", value);
            }
            return array;
        }

        public static ModOptionFloat[] SpringValues()
        {
            ModOptionFloat[] array = new ModOptionFloat[11];
            for (int i = 0; i < array.Length; i++)
            {
                float value = 500f + (i * 100f);
                array[i] = new ModOptionFloat($"{value:F0}", value);
            }
            return array;
        }

        public static ModOptionFloat[] DamperValues()
        {
            ModOptionFloat[] array = new ModOptionFloat[11];
            for (int i = 0; i < array.Length; i++)
            {
                float value = 25f + (i * 5f);
                array[i] = new ModOptionFloat($"{value:F0}", value);
            }
            return array;
        }

        public static ModOptionInt[] ChainLengthValues()
        {
            ModOptionInt[] array = new ModOptionInt[8];
            for (int i = 0; i < array.Length; i++)
            {
                int value = i + 2; // 2 to 9 creatures
                array[i] = new ModOptionInt($"{value} creatures", value);
            }
            return array;
        }

        public static ModOptionBool[] booleanOption = new ModOptionBool[]
        {
            new ModOptionBool("Enabled", true),
            new ModOptionBool("Disabled", false)
        };

        public static ModOptionString[] TriggerObjectOptions = new ModOptionString[]
        {
            new ModOptionString("Hand", "Hand"),
            new ModOptionString("Foot", "Foot"),
            new ModOptionString("Arm", "Arm"),
            new ModOptionString("Leg", "Leg"),
            new ModOptionString("Elbow", "Elbow"),
            new ModOptionString("Knee", "Knee"),
            new ModOptionString("Shoulder", "Shoulder"),
            new ModOptionString("Hip", "Hip")
        };

        private static readonly Dictionary<string, string> TriggerObjectMappings = new Dictionary<string, string>
        {
            { "Hand", "Hand" },
            { "Foot", "Foot" },
            { "Arm", "Arm" },
            { "Leg", "Leg" },
            { "Elbow", "Elbow" },
            { "Knee", "Knee" },
            { "Shoulder", "Shoulder" },
            { "Hip", "Hip" }
        };
        #endregion

        #region Helper Methods
        private static string GetTriggerObjectName(string side, string type)
        {
            if (TriggerObjectMappings.TryGetValue(type, out string mappedValue))
            {
                return $"{side}{mappedValue}";
            }
            return $"{side}Hand";
        }
        #endregion

        #region Behavior Settings
        [ModOptionCategory("Behavior Settings", 1)]
        [ModOptionOrder(1)]
        [ModOption("No Stand Up", "Prevent cuffed creatures from standing up", "booleanOption")]
        public static bool UseNoStandupModifier = true;

        [ModOptionCategory("Behavior Settings", 1)]
        [ModOptionOrder(2)]
        [ModOption("Auto-Pacify", "Automatically pacify fully cuffed creatures", "booleanOption")]
        public static bool PacifyWhenFullyCuffed = true;

        [ModOptionCategory("Behavior Settings", 1)]
        [ModOptionOrder(3)]
        [ModOption("Multi-Creature Chaining", "Allow cuffing multiple creatures together", "booleanOption")]
        public static bool AllowMultiCreatureChaining = true;

        [ModOptionCategory("Behavior Settings", 1)]
        [ModOptionOrder(4)]
        [ModOptionSlider]
        [ModOption("Max Chain Length", "Maximum number of creatures that can be chained together", "ChainLengthValues")]
        public static int MaxChainLength = 3;
        #endregion

        #region Technical Settings
        [ModOptionCategory("Technical Settings", 2)]
        [ModOptionOrder(1)]
        [ModOptionSlider]
        [ModOption("Reset Delay", "Time before handcuffs can be used again", "DelayTimeValues")]
        public static float ResetDelayTime = 1.5f;

        [ModOptionCategory("Technical Settings", 2)]
        [ModOptionOrder(2)]
        [ModOptionSlider]
        [ModOption("Joint Travel", "How far the cuffs can move", "DistanceValues")]
        public static float JointTravelDistance = 0.025f;

        [ModOptionCategory("Technical Settings", 2)]
        [ModOptionOrder(3)]
        [ModOptionSlider]
        [ModOption("Joint Offset", "Distance between cuffs and wrists", "DistanceValues")]
        public static float JointOffsetDistance = 0.05f;

        [ModOptionCategory("Technical Settings", 2)]
        [ModOptionOrder(4)]
        [ModOptionSlider]
        [ModOption("Mass Offset", "Weight effect on cuffed hands", "MassValues")]
        public static float ConnectedMassOffset = 10.0f;

        [ModOptionCategory("Technical Settings", 2)]
        [ModOptionOrder(5)]
        [ModOptionSlider]
        [ModOption("Drag Distance", "Distance before dragged NPCs fall", "DistanceValues")]
        public static float DragActivationDistance = 2.0f;

        [ModOptionCategory("Technical Settings", 2)]
        [ModOptionOrder(6)]
        [ModOptionSlider]
        [ModOption("Spring Strength", "Strength of position holding", "SpringValues")]
        public static float SpringStrength = 1000f;

        [ModOptionCategory("Technical Settings", 2)]
        [ModOptionOrder(7)]
        [ModOptionSlider]
        [ModOption("Damper Strength", "Smoothness of movement", "DamperValues")]
        public static float DamperStrength = 50f;
        #endregion

        #region Target Settings
        [ModOptionCategory("Target Settings", 3)]
        [ModOptionOrder(1)]
        [ModOption("Left Target", "Select which body part to target for left cuff", "TriggerObjectOptions")]
        public static string LeftTriggerType = "Hand";

        [ModOptionCategory("Target Settings", 3)]
        [ModOptionOrder(2)]
        [ModOption("Right Target", "Select which body part to target for right cuff", "TriggerObjectOptions")]
        public static string RightTriggerType = "Hand";

        public static string TriggerObjectLeftName => GetTriggerObjectName("Left", LeftTriggerType);
        public static string TriggerObjectRightName => GetTriggerObjectName("Right", RightTriggerType);
        #endregion
    }
}