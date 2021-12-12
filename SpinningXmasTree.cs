using UnityEngine;
using System;
using System.Linq;
using VLB;

namespace Oxide.Plugins
{
    [Info("Spinning Xmas Tree", "Bazz3l", "1.0.1")]
    [Description("Add lights and a star to your tree and watch it turn with awesomeness")]
    public class SpinningXmasTree : RustPlugin
    {
        #region Oxide

        void OnServerInitialized()
        {
            foreach (var christmasTree in BaseNetworkable.serverEntities.OfType<ChristmasTree>())
                CreateSpinner(christmasTree);
        }

        void Unload()
        {
            foreach (var christmasTree in BaseNetworkable.serverEntities.OfType<ChristmasTree>())
                RemoveSpinner(christmasTree);
        }

        void OnEntityBuilt(Planner plan, GameObject gameObject)
        {
            var christmasTree = gameObject.GetComponent<ChristmasTree>();
            if (christmasTree == null) return;

            NextFrame(() => CreateSpinner(christmasTree));
        }

        void OnEntityKill(ChristmasTree christmasTree) => RemoveSpinner(christmasTree);

        void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            var christmasTree = container?.entityOwner as ChristmasTree;
            if (christmasTree == null)
                return;

            christmasTree.GetParentEntity()
                ?.GetComponent<TreeSpinnerController>()
                ?.SetSpin(container);
        }

        void OnItemRemovedFromContainer(ItemContainer container, Item item)
        {
            var christmasTree = container?.entityOwner as ChristmasTree;
            if (christmasTree == null)
                return;

            if (container.itemList == null)
                return;

            christmasTree.GetParentEntity()
                ?.GetComponent<TreeSpinnerController>()
                ?.SetSpin(container);
        }

        #endregion

        #region Core

        void CreateSpinner(ChristmasTree christmasTree)
        {
            if (christmasTree == null || christmasTree.IsDestroyed)
                return;

            if (christmasTree.HasParent())
                return;

            var droppedItem = ItemManager.CreateByName("rock", 1)
                ?.Drop(christmasTree.ServerPosition, Vector3.zero)
                ?.GetComponent<DroppedItem>();

            if (droppedItem == null)
                return;

            var rb = droppedItem.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            droppedItem.syncPosition = true;
            droppedItem.enableSaving = false;
            droppedItem.allowPickup = false;
            droppedItem.Invoke("IdleDestroy", float.MaxValue);
            droppedItem.CancelInvoke((Action)Delegate.CreateDelegate(typeof(Action), droppedItem, "IdleDestroy"));

            christmasTree.SetParent(droppedItem);
            christmasTree.transform.localPosition = Vector3.zero;
            christmasTree.transform.hasChanged = true;
            christmasTree.SendNetworkUpdateImmediate();

            droppedItem.GetOrAddComponent<TreeSpinnerController>()
                ?.SetSpin(christmasTree.inventory);
        }

        void RemoveSpinner(ChristmasTree christmasTree)
        {
            if (christmasTree == null || christmasTree.IsDestroyed)
                return;

            if (!christmasTree.HasParent())
                return;

            var parent = christmasTree.GetParentEntity();

            christmasTree.SetParent(null, true, true);

            if (parent != null && !parent.IsDestroyed)
            {
	            UnityEngine.Object.Destroy(parent.GetComponent<TreeSpinnerController>());

	            parent.Kill();
            }
        }

        class TreeSpinnerController : MonoBehaviour
        {
	        private BaseEntity _entity;
            private bool _shouldSpin;

            void Awake()
            {
	            _entity = GetComponent<BaseEntity>();
            }

            void FixedUpdate()
            {
                if (!_shouldSpin) return;

                transform.Rotate(0, Time.fixedDeltaTime * 10, 0);
            }

            public void SetSpin(ItemContainer container)
            {
                if (container == null || container.itemList == null)
                    _shouldSpin = false;
                else
                    _shouldSpin = container.itemList.Count > 0;
            }
        }

        #endregion
    }
}