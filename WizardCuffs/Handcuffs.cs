using ThunderRoad;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

namespace WizardCuffs
{
    public class Handcuffs : ThunderBehaviour
    {
        #region Component Management
        private readonly struct Components
        {
            public readonly Item Item;
            public readonly HandcuffModule Module;
            public readonly AudioSource SoundEffect;
            public readonly Collider[] Colliders;
            public readonly List<Collider> MainBodyColliders;

            public Components(GameObject gameObject, HandcuffModule module)
            {
                Item = gameObject.GetComponent<Item>();
                Module = module;
                SoundEffect = Item?.GetCustomReference(module.unitySoundRef, true)?.GetComponent<AudioSource>();

                Colliders = new Collider[2] {
                    Item?.GetCustomReference(module.unityBodyRightRef, true)?.GetComponent<Collider>(),
                    Item?.GetCustomReference(module.unityBodyLeftRef, true)?.GetComponent<Collider>()
                };

                MainBodyColliders = new List<Collider>(
                    Item?.GetCustomReference(module.unityBodyMiddleRef, true)?.GetComponentsInChildren<Collider>()
                    ?? Array.Empty<Collider>());
            }

            public bool IsValid => Item != null && Module != null && SoundEffect != null
                               && Colliders[0] != null && Colliders[1] != null
                               && MainBodyColliders != null && MainBodyColliders.Count > 0;
        }
        #endregion

        #region Private Fields
        private class CreatureState
        {
            public bool LeftCuffed { get; set; }
            public bool RightCuffed { get; set; }
            public ConfigurableJoint LeftJoint { get; set; }
            public ConfigurableJoint RightJoint { get; set; }
            public bool IsFullyCuffed => LeftCuffed && RightCuffed;
        }

        private Components components;
        private Dictionary<Creature, CreatureState> creatureStates = new Dictionary<Creature, CreatureState>();
        private List<Creature> chainedCreatures = new List<Creature>();
        private Creature currentCreature;
        private Renderer[] itemRenderers;
        private readonly object cuffLock = new object();

        private float lastResetTime;
        private bool isGripped;
        private bool isInitialized;
        private bool isChainComplete;

        private Vector3 rightJointOffset;
        private Vector3 leftJointOffset;
        #endregion

        #region Logging
        private static void LogError(string message) => Debug.LogError($"[Handcuffs] {message}");
        private static void LogWarning(string message) => Debug.LogWarning($"[Handcuffs] {message}");
        private static void LogInfo(string message) => Debug.Log($"[Handcuffs] {message}");
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            itemRenderers = GetComponentsInChildren<Renderer>();
            SetHandcuffsVisibility(false);
            Initialize();
        }

        protected new void OnEnable()
        {
            base.OnEnable();
            if (!isInitialized) return;
            SubscribeToEvents();
        }

        protected new void OnDisable()
        {
            base.OnDisable();
            if (!isInitialized) return;
            UnsubscribeFromEvents();
        }

        private void LateUpdate()
        {
            if (!isInitialized || !isGripped || currentCreature == null || currentCreature.isKilled) return;
            CheckDragDistance();
        }
        #endregion

        #region Initialization and Event Management
        private void Initialize()
        {
            if (isInitialized) return;

            try
            {
                var module = GetComponent<Item>()?.data.GetModule<HandcuffModule>();
                if (module == null) throw new InvalidOperationException("Failed to get HandcuffModule");

                components = new Components(gameObject, module);
                if (!components.IsValid) throw new InvalidOperationException("Invalid component initialization");

                InitializeOffsets(module);
                isInitialized = true;
                LogInfo("Successfully initialized Handcuffs");
            }
            catch (Exception ex)
            {
                LogError($"Failed to initialize: {ex.Message}");
                enabled = false;
            }
        }

        private void InitializeOffsets(HandcuffModule module)
        {
            rightJointOffset = new Vector3(0f, 0f, module.jointOffsetDistance);
            leftJointOffset = new Vector3(0f, 0f, -module.jointOffsetDistance);
        }

        private void SubscribeToEvents()
        {
            var item = components.Item;
            item.OnHeldActionEvent += OnHeldAction;
            item.OnGrabEvent += OnGrabEvent;
            item.OnUngrabEvent += OnUnGrabEvent;
            item.OnSnapEvent += OnSnapEvent;
            item.OnUnSnapEvent += OnUnSnapEvent;
        }

        private void UnsubscribeFromEvents()
        {
            var item = components.Item;
            item.OnHeldActionEvent -= OnHeldAction;
            item.OnGrabEvent -= OnGrabEvent;
            item.OnUngrabEvent -= OnUnGrabEvent;
            item.OnSnapEvent -= OnSnapEvent;
            item.OnUnSnapEvent -= OnUnSnapEvent;
        }
        #endregion

        #region Event Handlers
        private void OnHeldAction(RagdollHand interactor, Handle handle, Interactable.Action action)
        {
            if (action == Interactable.Action.AlternateUseStart)
            {
                ResetHandcuffs();
            }
        }

        private void OnSnapEvent(Holder holder)
        {
            if (!isInitialized || holder == null) return;
            SetHandcuffsVisibility(false);
        }

        private void OnUnSnapEvent(Holder holder)
        {
            if (!isInitialized || holder == null) return;
            SetHandcuffsVisibility(true);
        }

        private void OnGrabEvent(Handle handle, RagdollHand ragdollHand)
        {
            isGripped = true;
        }

        private void OnUnGrabEvent(Handle handle, RagdollHand ragdollHand, bool throwing)
        {
            isGripped = false;
        }
        #endregion

        #region Collision Handling
        private void OnCollisionEnter(Collision hit)
        {
            if (!isInitialized || Time.time - lastResetTime <= components.Module.resetDelayTime) return;

            Creature hitCreature = hit.transform.root.GetComponentInChildren<Creature>();
            if (hitCreature == null || hitCreature == Player.local.creature) return;

            HandleCollision(hit, hitCreature);
        }

        private void HandleCollision(Collision hit, Creature hitCreature)
        {
            if (!isInitialized) return;

            lock (cuffLock)
            {
                if (isChainComplete && !chainedCreatures.Contains(hitCreature))
                {
                    LogInfo("Chain is complete - rejecting new creature");
                    return;
                }
                if (!CanAddCreatureToChain(hitCreature))
                {
                    return;
                }
                if (!chainedCreatures.Contains(hitCreature))
                {
                    AddCreatureToChain(hitCreature);
                }
                ProcessCuffHit(hit, hitCreature);
            }
        }

        private bool CanAddCreatureToChain(Creature creature)
        {
            LogInfo($"Checking chain for creature {creature.name}:");
            LogInfo($"Current chain length: {chainedCreatures.Count}");
            LogInfo($"Current creature: {(currentCreature?.name ?? "none")}");

            if (creatureStates.TryGetValue(creature, out var state))
            {
                LogInfo($"Creature {creature.name} cuff state - Left: {state.LeftCuffed}, Right: {state.RightCuffed}");

                if (state.LeftCuffed && state.RightCuffed)
                {
                    LogInfo($"Creature {creature.name} has both cuffs - cannot chain");
                    return false;
                }
            }
            if (HandcuffSettings.AllowMultiCreatureChaining)
            {
                if (chainedCreatures.Count >= HandcuffSettings.MaxChainLength && !chainedCreatures.Contains(creature))
                {
                    LogInfo($"Maximum chain length ({HandcuffSettings.MaxChainLength}) reached");
                    return false;
                }
            }
            else if (chainedCreatures.Count > 0 && !chainedCreatures.Contains(creature))
            {
                return false;
            }

            return true;
        }


        private void AddCreatureToChain(Creature creature)
        {
            if (currentCreature == null)
            {
                LogInfo($"Started new chain with creature: {creature.name}");
            }
            else
            {
                LogInfo($"Added creature to chain: {creature.name} (Chain length: {chainedCreatures.Count + 1}/{HandcuffSettings.MaxChainLength})");
            }

            chainedCreatures.Add(creature);
            currentCreature = creature;
        }

        private void ProcessCuffHit(Collision hit, Creature creature)
        {
            string hitName = hit.gameObject.name;
            LogInfo($"Processing hit on {hitName} for creature {creature.name}");

            if (!creatureStates.TryGetValue(creature, out var state))
            {
                state = new CreatureState();
                creatureStates[creature] = state;
            }

            LogInfo($"Current cuff state - Left: {state.LeftCuffed}, Right: {state.RightCuffed}");

            if (hitName.Equals(components.Module.triggerObjectLeftName))
            {
                if (!state.LeftCuffed)
                {
                    LogInfo($"Applying left cuff to {creature.name}");
                    ApplyCuff(true, hit.gameObject, leftJointOffset);
                }
            }
            else if (hitName.Equals(components.Module.triggerObjectRightName))
            {
                if (!state.RightCuffed)
                {
                    LogInfo($"Applying right cuff to {creature.name}");
                    ApplyCuff(false, hit.gameObject, rightJointOffset);
                }
            }
        }

        private bool IsCreatureFullyCuffed(Creature creature)
        {
            return creatureStates.TryGetValue(creature, out var state) && state.IsFullyCuffed;
        }

        private void CheckChainCompletion()
        {
            if (chainedCreatures.Count >= HandcuffSettings.MaxChainLength && IsCreatureFullyCuffed(currentCreature))
            {
                isChainComplete = true;
                LogInfo("Chain is now complete - no more creatures can be added");
            }
        }
        #endregion

        #region Cuff Management
        private void ApplyCuff(bool isLeft, GameObject hitObject, Vector3 offset)
        {
            try
            {
                if (!creatureStates.TryGetValue(currentCreature, out var state))
                {
                    state = new CreatureState();
                    creatureStates[currentCreature] = state;
                }

                LogInfo($"Applying {(isLeft ? "left" : "right")} cuff");
                LogInfo($"Current settings - Spring: {HandcuffSettings.SpringStrength}, Damper: {HandcuffSettings.DamperStrength}");

                Transform wristTransform = hitObject.transform;
                Transform cuffTransform = isLeft ? components.Colliders[1].transform : components.Colliders[0].transform;
                cuffTransform.position = wristTransform.position;
                cuffTransform.rotation = wristTransform.rotation;

                var joint = InitializeConfigurableJoint(hitObject, offset);

                if (isLeft)
                {
                    state.LeftCuffed = true;
                    state.LeftJoint = joint;
                    components.Colliders[1].enabled = false;
                    ForceReleaseHand(currentCreature?.handLeft);
                    LogInfo("Left cuff applied successfully");
                }
                else
                {
                    state.RightCuffed = true;
                    state.RightJoint = joint;
                    components.Colliders[0].enabled = false;
                    ForceReleaseHand(currentCreature?.handRight);
                    LogInfo("Right cuff applied successfully");
                }

                components.SoundEffect?.Play();

                if (state.IsFullyCuffed)
                {
                    LogInfo($"Creature {currentCreature.name} is now fully cuffed");
                    FinalizeCuffing(currentCreature);
                    CheckChainCompletion();
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to apply cuff: {ex.Message}");
                ResetCuffState(isLeft);
            }
        }

        private void ForceReleaseHand(RagdollHand hand)
        {
            if (hand?.grabbedHandle != null)
            {
                hand.UnGrab(false);
                LogInfo($"{hand.name} forced to release grip");
            }
        }

        private void ResetCuffState(bool isLeft)
        {
            if (isLeft)
            {
                if (components.Colliders[1] != null) components.Colliders[1].enabled = true;
            }
            else
            {
                if (components.Colliders[0] != null) components.Colliders[0].enabled = true;
            }
        }

        private void ResetHandcuffs()
        {
            try
            {
                // Remove modifiers and destroy joints
                foreach (var kvp in creatureStates)
                {
                    var creature = kvp.Key;
                    var state = kvp.Value;

                    if (components.Module.useNoStadupModifier && creature != null)
                    {
                        creature.brain.RemoveNoStandUpModifier(this);
                    }

                    if (state.LeftJoint != null) Destroy(state.LeftJoint);
                    if (state.RightJoint != null) Destroy(state.RightJoint);
                }
                foreach (var collider in components.Colliders.Concat(components.MainBodyColliders))
                {
                    if (collider != null) collider.enabled = true;
                }

                creatureStates.Clear();
                chainedCreatures.Clear();
                currentCreature = null;
                isChainComplete = false;
                lastResetTime = Time.time;

                components.SoundEffect?.Play();
                LogInfo("Handcuffs reset successfully");
            }
            catch (Exception ex)
            {
                LogError($"Failed to reset handcuffs: {ex.Message}");
            }
        }
        #endregion

        #region Creature Effects
        private void CheckDragDistance()
        {
            if (!isInitialized || !isGripped) return;

            var playerPos = Player.local.creature.transform.position;
            var dragDistance = components.Module.dragActivationDistance;

            foreach (var creature in chainedCreatures.ToList())
            {
                if (creature == null || creature.isKilled)
                {
                    RemoveCreatureFromChain(creature);
                    continue;
                }

                if (!IsCreatureFullyCuffed(creature)) continue;

                if (Vector3.Distance(creature.transform.position, playerPos) >= dragDistance)
                {
                    ApplyDragEffects(creature);
                }
            }
        }

        private void RemoveCreatureFromChain(Creature creature)
        {
            chainedCreatures.Remove(creature);
            creatureStates.Remove(creature);

            if (creature == currentCreature)
            {
                currentCreature = chainedCreatures.LastOrDefault();
            }
        }

        private void ApplyDragEffects(Creature creature)
        {
            if (HandcuffSettings.UseNoStandupModifier)  
            {
                creature.brain.AddNoStandUpModifier(this);
                LogInfo($"Added NoStandUp modifier to {creature.name} - drag distance exceeded");
            }

            if (HandcuffSettings.PacifyWhenFullyCuffed) 
            {
                CreaturePacify(creature);
                LogInfo($"Pacified creature {creature.name} - drag distance exceeded");
            }
        }

        private void FinalizeCuffing(Creature creature)
        {
            if (!IsCreatureFullyCuffed(creature)) return;

            if (HandcuffSettings.UseNoStandupModifier)
            {
                creature.brain.AddNoStandUpModifier(this);
                LogInfo($"Added NoStandUp modifier to {creature.name} - fully cuffed");
            }

            if (HandcuffSettings.PacifyWhenFullyCuffed) 
            {
                CreaturePacify(creature);
                LogInfo($"Pacified creature {creature.name} - fully cuffed");
            }
        }

        private void CreaturePacify(Creature creature)
        {
            if (creature == null) return;

            try
            {
                BrainData instance = creature.brain.instance;
                instance.StopModuleUsingAnyBodyPart(null);
                instance.tree.Reset();
                creature.ragdoll.SetState(creature.isKilled ? Ragdoll.State.Inert : Ragdoll.State.Destabilized);
                LogInfo($"Successfully pacified creature: {creature.name}");
            }
            catch (Exception ex)
            {
                LogError($"Failed to pacify creature {creature.name}: {ex.Message}");
            }
        }
        #endregion
        #region Joint Configuration
        private ConfigurableJoint InitializeConfigurableJoint(GameObject baseObject, Vector3 connectedAnchor)
        {
            var rigidbody = baseObject.GetComponent<Rigidbody>() ?? baseObject.AddComponent<Rigidbody>();
            ConfigureRigidbody(rigidbody);

            var joint = gameObject.AddComponent<ConfigurableJoint>();
            ConfigureJoint(joint, rigidbody, connectedAnchor);

            return joint;
        }

        private void ConfigureRigidbody(Rigidbody rigidbody)
        {
            rigidbody.mass = 1f;
            rigidbody.drag = 0f;
            rigidbody.angularDrag = 0.05f;
            rigidbody.useGravity = true;
            rigidbody.isKinematic = false;
            rigidbody.interpolation = RigidbodyInterpolation.None;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
        }

        private void ConfigureJoint(ConfigurableJoint joint, Rigidbody connectedBody, Vector3 connectedAnchor)
        {
            ConfigureJointBasics(joint, connectedBody, connectedAnchor);
            ConfigureJointMotion(joint);
            ConfigureJointDrives(joint);
            ConfigureJointLimits(joint);
            ConfigureJointProjection(joint);
        }

        private void ConfigureJointBasics(ConfigurableJoint joint, Rigidbody connectedBody, Vector3 connectedAnchor)
        {
            joint.connectedBody = connectedBody;
            joint.anchor = connectedAnchor;
            joint.axis = Vector3.right;
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = Vector3.zero;
            joint.secondaryAxis = Vector3.up;
            joint.configuredInWorldSpace = false;
        }

        private void ConfigureJointMotion(ConfigurableJoint joint)
        {
            joint.xMotion = ConfigurableJointMotion.Limited;
            joint.yMotion = ConfigurableJointMotion.Limited;
            joint.zMotion = ConfigurableJointMotion.Limited;
            joint.angularXMotion = ConfigurableJointMotion.Limited;
            joint.angularYMotion = ConfigurableJointMotion.Limited;
            joint.angularZMotion = ConfigurableJointMotion.Limited;
        }

        private void ConfigureJointDrives(ConfigurableJoint joint)
        {
            var posDrive = new JointDrive
            {
                positionSpring = HandcuffSettings.SpringStrength,
                positionDamper = HandcuffSettings.DamperStrength,
                maximumForce = 1000f
            };

            var rotDrive = new JointDrive
            {
                positionSpring = HandcuffSettings.SpringStrength / 2f,
                positionDamper = HandcuffSettings.DamperStrength / 2f,
                maximumForce = 500f
            };

            joint.xDrive = posDrive;
            joint.yDrive = posDrive;
            joint.zDrive = posDrive;
            joint.angularXDrive = rotDrive;
            joint.angularYZDrive = rotDrive;
        }

        private void ConfigureJointLimits(ConfigurableJoint joint)
        {
            var linearLimit = new SoftJointLimit
            {
                limit = components.Module.jointTravelDistance,
                bounciness = 0f,
                contactDistance = 0.001f
            };

            var angularLimit = new SoftJointLimit
            {
                limit = 5f,
                bounciness = 0f,
                contactDistance = 0.001f
            };

            joint.linearLimit = linearLimit;
            joint.lowAngularXLimit = angularLimit;
            joint.highAngularXLimit = angularLimit;
            joint.angularYLimit = angularLimit;
            joint.angularZLimit = angularLimit;
        }

        private void ConfigureJointProjection(ConfigurableJoint joint)
        {
            joint.projectionMode = JointProjectionMode.PositionAndRotation;
            joint.projectionDistance = 0.001f;
            joint.projectionAngle = 1f;
            joint.massScale = 1f;
            joint.connectedMassScale = components.Module.connectedMassOffset;
        }
        #endregion

        #region Utility Methods
        private void SetHandcuffsVisibility(bool visible)
        {
            if (itemRenderers == null) return;

            foreach (var renderer in itemRenderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = visible;
                }
            }
        }
        #endregion
    }
}
