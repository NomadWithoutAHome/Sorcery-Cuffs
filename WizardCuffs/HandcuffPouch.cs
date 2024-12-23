using ThunderRoad;
using UnityEngine;

namespace WizardCuffs
{
    public class HandcuffPouch : ThunderBehaviour
    {
        #region Component Management
        private readonly struct Components
        {
            public readonly Item Item;
            public readonly HandcuffModule Module;
            public readonly Holder Holder;

            public Components(GameObject gameObject)
            {
                Item = gameObject.GetComponent<Item>();
                Module = Item?.data.GetModule<HandcuffModule>();
                Holder = Item?.GetComponentInChildren<Holder>();
            }

            public bool IsValid => Item != null && Module != null && Holder != null;
        }
        #endregion

        #region Private Fields
        private Components components;
        private readonly object spawnLock = new object();
        private volatile bool waitingForSpawn;
        private bool isInitialized;
        #endregion

        #region Logging
        private static void LogError(string message) => Debug.LogError($"[HandcuffPouch] {message}");
        private static void LogWarning(string message) => Debug.LogWarning($"[HandcuffPouch] {message}");
        private static void LogInfo(string message) => Debug.Log($"[HandcuffPouch] {message}");
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Initialize();
        }

        protected new void OnEnable()
        {
            base.OnEnable(); 
            if (isInitialized && components.IsValid)
            {
                components.Holder.UnSnapped += OnHandcuffItemRemoved;
            }
        }

        protected new void OnDisable()
        {
            base.OnDisable(); 
            if (components.IsValid)
            {
                components.Holder.UnSnapped -= OnHandcuffItemRemoved;
            }
        }

        private void Start()
        {
            if (isInitialized)
            {
                SpawnAndSnap(components.Module.handcuffID, components.Holder);
            }
        }
        #endregion

        #region Initialization
        private void Initialize()
        {
            if (isInitialized) return;

            try
            {
                components = new Components(gameObject);
                if (!components.IsValid)
                {
                    LogError("Failed to initialize required components");
                    enabled = false;
                    return;
                }

                isInitialized = true;
                LogInfo("Successfully initialized HandcuffPouch");
            }
            catch (System.Exception ex)
            {
                LogError($"Failed to initialize: {ex.Message}");
                enabled = false;
            }
        }
        #endregion

        #region Event Handlers
        private void OnHandcuffItemRemoved(Item interactiveObject)
        {
            if (!waitingForSpawn && isInitialized)
            {
                SpawnAndSnap(components.Module.handcuffID, components.Holder);
            }
        }
        #endregion

        #region Spawn Management
        public void SpawnAndSnap(string spawnedItemID, Holder holder)
        {
            lock (spawnLock)
            {
                if (waitingForSpawn) return;
                waitingForSpawn = true;
            }

            try
            {
                ItemData data = Catalog.GetData<ItemData>(spawnedItemID, true);
                if (data == null)
                {
                    LogError($"Failed to get ItemData for ID: {spawnedItemID}");
                    return;
                }

                data.SpawnAsync(OnItemSpawned, null, null, null, true, null);
            }
            catch (System.Exception ex)
            {
                LogError($"Failed to spawn item: {ex.Message}");
                lock (spawnLock)
                {
                    waitingForSpawn = false;
                }
            }
        }

        private void OnItemSpawned(Item spawnedItem)
        {
            try
            {
                if (components.Holder != null)
                {
                    components.Holder.Snap(spawnedItem, false);
                    spawnedItem.SetMeshLayer(GameManager.GetLayer(LayerName.Ragdoll));
                    LogInfo($"Successfully spawned and snapped item: {spawnedItem.name}");
                }
            }
            catch (System.Exception ex)
            {
                LogError($"Exception in snapping: {ex.Message}");
            }
            finally
            {
                lock (spawnLock)
                {
                    waitingForSpawn = false;
                }
            }
        }
        #endregion
    }
}