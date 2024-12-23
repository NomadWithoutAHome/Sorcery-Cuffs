using ThunderRoad;
using UnityEngine;

namespace WizardCuffs
{
    public class HandcuffModule : ItemModule
    {
        [Header("Item Settings")]
        public string handcuffID = "WizardHandcuffs"; 

        [Header("Timing Settings")]
        public float resetDelayTime = 0.5f;

        [Header("Joint Settings")]
        public float jointOffsetDistance = 0.1f;
        public float jointTravelDistance = 0.05f;
        public float dragActivationDistance = 2f;
        public float connectedMassOffset = 1f;

        [Header("Behavior Settings")]
        public bool useNoStadupModifier = true;
        public bool pacifyWhenFullyCuffed = true;

        [Header("Unity References")]
        public string unitySoundRef = "[SoundEffect]";
        public string unityBodyRightRef = "[RightCuff]";
        public string unityBodyLeftRef = "[LeftCuff]";
        public string unityBodyMiddleRef = "[MiddleBody]";

        [Header("Trigger Names")]
        public string triggerObjectRightName = "RightWrist";
        public string triggerObjectLeftName = "LeftWrist";

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.gameObject.AddComponent<Handcuffs>();
        }
    }
}