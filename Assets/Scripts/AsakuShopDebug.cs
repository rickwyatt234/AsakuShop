using UnityEngine;
using AsakuShop.Items;

namespace AsakuShop.Core
{
    public class AsakuShopDebug : MonoBehaviour
    {
        public Transform spawnPoint;
        public ItemRegistry itemRegistry;
        public GameClock gameClock;
        void Start()
        {
            itemRegistry = ItemRegistry.Instance;
            gameClock = GameBootstrapper.Clock;

            if (gameClock == null || itemRegistry == null)
            {
                UnityEngine.Debug.Log("GameClock or ItemRegistry not found. Make sure they are properly initialized in the scene.");
                return;
            }
            SpawnItem("item_sack_of_rice", spawnPoint.position);
            SpawnItem("item_sack_of_rice", spawnPoint.position);
            SpawnItem("item_sack_of_rice", spawnPoint.position);
            SpawnItem("item_sack_of_rice", spawnPoint.position);

        }

        public void SpawnItem(string itemID, Vector3 position)
        {
            ItemDefinition def = itemRegistry.Get(itemID);
            if (def != null && def.WorldPrefab != null)
            {
                GameTime currentTime = gameClock.CurrentTime;
                ItemInstance instance = new ItemInstance(def, currentTime);

                GameObject newObject = Instantiate(def.WorldPrefab, position, Quaternion.identity);
                newObject.name = $"{def.ItemId}_Instance";

                ItemInstance pickup = newObject.AddComponent<ItemInstance>();
                pickup.Instance = instance;

                MeshCollider collider = newObject.AddComponent<MeshCollider>();
                collider.convex = true;
            }
            else
            {
                UnityEngine.Debug.LogWarning($"ItemDefinition '{itemID}' not found or has no WorldPrefab.");
                return;
            }
        }
    }
    
}










using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CryingSnow.CheckoutFrenzy
{
    public class ShelvingUnit : Furniture
    {
        // Point in front of the shelving unit where customers stand to pick products.
        public Vector3 Front => transform.TransformPoint(Vector3.forward);

        // All child shelves of this shelving unit.
        public List<Shelf> Shelves { get; private set; } = new List<Shelf>();

        // Indicates whether the door is open for units with doors (e.g., Fridges, Freezers).
        public virtual bool IsOpen { get; protected set; } = true;

        protected override void Awake()
        {
            base.Awake();

            // Get all child Shelf components (including inactive ones) and store them in the Shelves list.
            Shelves = GetComponentsInChildren<Shelf>(true).ToList();

            // Set the ShelvingUnit property of each child Shelf to this ShelvingUnit.
            Shelves.ForEach(shelf => shelf.ShelvingUnit = this);
        }

        protected override void Start()
        {
            base.Start();

            StoreManager.Instance.ValidateShelvingUnit(this);
        }

        public virtual void Open(bool forced, bool playSFX) { }

        public virtual void Close(bool forced, bool playSFX) { }

        protected override void SetMovingState(bool isMoving)
        {
            base.SetMovingState(isMoving);

            // Disable shelf interaction during movement, and enable it when not moving.
            Shelves.ForEach(shelf => shelf.ToggleInteraction(!isMoving));

            // If the shelving unit is starting to move, unregister it from the store manager.
            // This prevent other customers from targeting it.
            if (isMoving) StoreManager.Instance.UnregisterShelvingUnit(this);
        }

        protected override void Place()
        {
            base.Place();

            StoreManager.Instance.ValidateShelvingUnit(this);
        }

        /// <summary>
        /// Returns a random child Shelf that has a Product assigned to it.
        /// </summary>
        /// <returns>A random Shelf with a Product, or null if no shelves have products.</returns>
        public Shelf GetShelf()
        {
            var validShelves = Shelves.Where(shelf => shelf.Product != null).ToList();

            if (validShelves.Count == 0) return null;

            return validShelves[Random.Range(0, validShelves.Count)];
        }

        /// <summary>
        /// Restores the products on the shelves based on saved shelf data.
        /// </summary>
        /// <param name="savedShelves">A list of ShelfData objects containing the saved product information.</param>
        public void RestoreProductsOnShelves(List<ShelfData> savedShelves)
        {
            for (int i = 0; i < savedShelves.Count; i++)
            {
                Shelf shelf = Shelves[i];
                ShelfData shelfData = savedShelves[i];

                Product assignedProduct = DataManager.Instance.GetProductById(shelfData.AssignedProductID);
                shelf.SetLabel(assignedProduct);

                // Skip this shelf if it is empty.
                if (shelfData.IsEmpty) continue;

                // Retrieve the Product from the DataManager using the saved ID.
                Product product = DataManager.Instance.GetProductById(shelfData.ProductID);

                // Restore shelf's products.
                shelf.RestoreProducts(product, shelfData.Quantity);
            }
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CryingSnow.CheckoutFrenzy
{
    public class StorageRack : Furniture
    {
        public List<Rack> Racks { get; private set; }

        public Vector3 Front => transform.TransformPoint(Vector3.forward);

        protected override void Awake()
        {
            base.Awake();

            Racks = GetComponentsInChildren<Rack>(true).ToList();
        }

        protected override void Start()
        {
            base.Start();

            WarehouseManager.Instance.ValidateStorageRack(this);
        }

        protected override void SetMovingState(bool isMoving)
        {
            base.SetMovingState(isMoving);

            // Disable rack interaction during movement, and enable it when not moving.
            Racks.ForEach(rack => rack.ToggleInteraction(!isMoving));

            // If the storage rack is starting to move, unregister it from the warehouse manager.
            // This prevent employees from targeting it.
            if (isMoving) WarehouseManager.Instance.UnregisterStorageRack(this);
        }

        protected override void Place()
        {
            base.Place();

            WarehouseManager.Instance.ValidateStorageRack(this);
        }

        public void RestoreBoxesOnRacks(List<RackData> savedRacks)
        {
            for (int i = 0; i < savedRacks.Count; i++)
            {
                RackData rackData = savedRacks[i];

                // Skip this rack if it is empty.
                if (rackData.IsEmpty) continue;

                // Retrieve the Product from the DataManager using the saved ID.
                Product product = DataManager.Instance.GetProductById(rackData.ProductID);

                // Get the corresponding Shelf and restore it's products.
                Rack rack = Racks[i];
                rack.RestoreBoxes(product, rackData.Quantities);
            }
        }
    }
}
using UnityEngine.Events;

namespace CryingSnow.CheckoutFrenzy
{
    public interface IActionUI
    {
        ActionType ActionType { get; }
        UnityEvent OnClick { get; }
        void SetActive(bool active);
    }
}
using UnityEngine;

namespace CryingSnow.CheckoutFrenzy
{
    public interface IPurchasable
    {
        string Name { get; }
        Sprite Icon { get; }
        decimal Price { get; }
        int OrderTime { get; }
        Section Section { get; }
    }
}
using System.Collections;
using System.Linq;
using UnityEngine;
using Unity.AI.Navigation;
using DG.Tweening;

namespace CryingSnow.CheckoutFrenzy
{
    [RequireComponent(typeof(Rigidbody))]
    public class Box : ProductContainer, IInteractable
    {
        [Header("Box Lids")]
        [SerializeField, Tooltip("Reference to the bone transform of the front lid of the box.")]
        private Transform lidFront;

        [SerializeField, Tooltip("Reference to the bone transform of the back lid of the box.")]
        private Transform lidBack;

        [SerializeField, Tooltip("Reference to the bone transform of the left lid of the box.")]
        private Transform lidLeft;

        [SerializeField, Tooltip("Reference to the bone transform of the right lid of the box.")]
        private Transform lidRight;

        [Header("Sound Settings")]
        [SerializeField, Tooltip("Duration (in seconds) to check for collisions after throwing the box.")]
        private float collisionCheckDuration = 3f;

        /// <summary>
        /// Gets the size of the box, with each dimension floored to the nearest tenth.
        /// The base size is derived from the box collider. This flooring is crucial for accurate inner dimension
        /// calculations, preventing issues that could arise from slight inaccuracies in the collider's reported size.
        /// </summary>
        public override Vector3 Size => base.Size.FloorToTenth();

        public float Height => boxCollider.size.y;

        public bool IsStored { get; set; }
        public bool IsOpen { get; private set; }
        public bool IsDisposable { get; private set; }
        public bool IsCheckingCollision { get; private set; }

        private Message message => UIManager.Instance.Message;

        private Rigidbody body;
        private PlayerController player;

        private Sequence lidSequence;

        private Coroutine disablePhysicsRoutine;

        private void Awake()
        {
            gameObject.layer = GameConfig.Instance.InteractableLayer.ToSingleLayer();

            body = GetComponent<Rigidbody>();
            SetActivePhysics(false);

            // Prevents the box from affecting the navigation mesh
            var navMeshMod = gameObject.AddComponent<NavMeshModifier>();
            navMeshMod.ignoreFromBuild = true;
        }

        private IEnumerator Start()
        {
            DataManager.Instance.OnSave += HandleOnSave;

            yield return new WaitUntil(() => DataManager.Instance.IsLoaded);

            if (!IsStored) SetActivePhysics(true);
        }

        private void OnDestroy()
        {
            if (DataManager.Instance != null)
            {
                DataManager.Instance.OnSave -= HandleOnSave;
            }
        }

        private void HandleOnSave()
        {
            if (IsStored) return;

            var boxData = new BoxData(this);
            DataManager.Instance.Data.SavedBoxes.Add(boxData);
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Ignore collision events if the box is not currently moving.
            if (!IsCheckingCollision) return;

            // Check if the collision impact is significant.
            if (collision.relativeVelocity.magnitude > 2)
            {
                // Play an impact sound effect.
                AudioManager.Instance.PlaySFX(AudioID.Impact);
            }
        }

        /// <summary>
        /// Handles the interaction with the box when the player taps interact button. 
        /// This includes picking up the box, attaching it to the player's hand, 
        /// updating the player's state, and enabling relevant UI elements.
        /// </summary>
        /// <param name="player">The player who is interacting with the box.</param>
        public void Interact(PlayerController player)
        {
            // Store a reference to the interacting player.
            this.player = player;

            // Prevent the box from being disposed of (e.g., thrown to trash can) while it's being held by the player.
            IsDisposable = false;

            if (disablePhysicsRoutine != null)
            {
                StopCoroutine(disablePhysicsRoutine);
            }

            disablePhysicsRoutine = StartCoroutine(DisablePhysicsDelayed());

            // Change the layer of all child objects to the "HeldObject" layer.
            // Making them rendered on top of everything else (except UI).
            foreach (Transform child in transform)
            {
                child.gameObject.layer = GameConfig.Instance.HeldObjectLayer.ToSingleLayer();
            }

            UIManager.Instance.ToggleActionUI(ActionType.Throw, true, Throw);

            // Enable the appropriate button for opening/closing the box.
            if (IsOpen) UIManager.Instance.ToggleActionUI(ActionType.Close, true, Close);
            else UIManager.Instance.ToggleActionUI(ActionType.Open, true, Open);

            UIManager.Instance.HideBoxInfo();

            // Move the box to the player's hand.
            transform.SetParent(player.HoldPoint);
            transform.DOLocalMove(Vector3.zero, 0.5f).SetEase(Ease.OutQuint);
            transform.DOLocalRotate(Vector3.zero, 0.5f).SetEase(Ease.OutQuint);

            AudioManager.Instance.PlaySFX(AudioID.Pick);

            player.StateManager.PushState(PlayerState.Holding);

            UIManager.Instance.InteractMessage.Hide();
        }

        public void OnFocused()
        {
            UIManager.Instance.DisplayBoxInfo(this);

            string message = "Tap to pick up this box";
            UIManager.Instance.InteractMessage.Display(message);
        }

        public void OnDefocused()
        {
            UIManager.Instance.HideBoxInfo();
            UIManager.Instance.InteractMessage.Hide();
        }

        private void Throw()
        {
            // Check for collisions within the box's bounds. 
            // If there's an overlap, prevent the throw.
            var center = transform.position;
            var extents = boxCollider.size / 2f;
            var orientation = transform.rotation;
            var layerMask = ~GameConfig.Instance.PlayerLayer; // Create a layer mask that excludes the "Player" layer. 

            if (Physics.OverlapBox(center, extents, orientation, layerMask).Length > 0)
            {
                message.Log("Can't throw object here!", Color.red);
                return;
            }

            if (disablePhysicsRoutine != null)
            {
                StopCoroutine(disablePhysicsRoutine);
                disablePhysicsRoutine = null;
            }

            DOTween.Kill(transform);

            // Detach the box from the player's hand.
            transform.SetParent(null);

            // Enable physics for the box and apply an impulse force.
            SetActivePhysics(true);
            body.AddForce(transform.forward * 3.5f, ForceMode.Impulse);

            StartCoroutine(StartCollisionCheck());

            AudioManager.Instance.PlaySFX(AudioID.Throw);

            // Change the layer of all child objects back to the default layer.
            foreach (Transform child in transform)
            {
                child.gameObject.layer = LayerMask.NameToLayer("Default");
            }

            // Disable UI elements related to holding and interacting with the box.
            UIManager.Instance.ToggleActionUI(ActionType.Throw, false, null);
            UIManager.Instance.ToggleActionUI(ActionType.Open, false, null);
            UIManager.Instance.ToggleActionUI(ActionType.Close, false, null);
            UIManager.Instance.ToggleActionUI(ActionType.Place, false, null);
            UIManager.Instance.ToggleActionUI(ActionType.Take, false, null);

            player.StateManager.PopState();

            player = null;

            IsDisposable = true;
        }

        /// <summary>
        /// Disables physics for the box with a slight delay. 
        /// This prevents issues where the box's position is not 
        /// fully updated by the physics engine after being moved 
        /// using `Transform` (e.g., when picking up stacked boxes).
        /// </summary>
        private IEnumerator DisablePhysicsDelayed()
        {
            yield return new WaitForSeconds(0.2f);

            SetActivePhysics(false);
        }

        public void SetActivePhysics(bool value)
        {
            body.isKinematic = !value;
            boxCollider.enabled = value;
        }

        /// <summary>
        /// Starts a timer to check for collisions after the box is thrown.
        /// Collisions are only checked within the specified `collisionCheckDuration`.
        /// </summary>
        private IEnumerator StartCollisionCheck()
        {
            float timer = collisionCheckDuration;
            IsCheckingCollision = true;

            while (timer > 0f)
            {
                timer -= Time.deltaTime;
                yield return null;
            }

            IsCheckingCollision = false;
        }

        /// <summary>
        /// Opens the box lids with a smooth animation.
        /// Sets the IsOpen flag to true and enables the "Close" button.
        /// </summary>
        private void Open()
        {
            if (lidSequence.IsActive()) return;

            IsOpen = true;
            UIManager.Instance.ToggleActionUI(ActionType.Close, true, Close);
            UIManager.Instance.ToggleActionUI(ActionType.Open, false, null);

            lidSequence = DOTween.Sequence();

            lidSequence.Append(lidFront.DOLocalRotate(Vector3.right * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .Join(lidBack.DOLocalRotate(Vector3.left * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .InsertCallback(0f, () => AudioManager.Instance.PlaySFX(AudioID.Flip))
                .Append(lidLeft.DOLocalRotate(Vector3.back * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .Join(lidRight.DOLocalRotate(Vector3.forward * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .InsertCallback(0.3f, () => AudioManager.Instance.PlaySFX(AudioID.Flip));
        }

        /// <summary>
        /// Closes the box lids with a smooth animation.
        /// Sets the IsOpen flag to false and enables the "Open" button.
        /// </summary>
        private void Close()
        {
            if (lidSequence.IsActive()) return;

            IsOpen = false;
            UIManager.Instance.ToggleActionUI(ActionType.Open, true, Open);
            UIManager.Instance.ToggleActionUI(ActionType.Close, false, null);

            lidSequence = DOTween.Sequence();

            lidSequence.Append(lidLeft.DOLocalRotate(Vector3.forward * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .Join(lidRight.DOLocalRotate(Vector3.back * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .InsertCallback(0f, () => AudioManager.Instance.PlaySFX(AudioID.Flip))
                .Append(lidFront.DOLocalRotate(Vector3.left * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .Join(lidBack.DOLocalRotate(Vector3.right * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .InsertCallback(0.3f, () => AudioManager.Instance.PlaySFX(AudioID.Flip));
        }

        /// <summary>
        /// Opens the box lids immediately without animation. 
        /// Primarily used for initialization purposes.
        /// </summary>
        public void SetLidsOpen()
        {
            lidFront.localRotation = Quaternion.Euler(Vector3.right * 160f);
            lidBack.localRotation = Quaternion.Euler(Vector3.left * 160f);
            lidLeft.localRotation = Quaternion.Euler(Vector3.back * 160f);
            lidRight.localRotation = Quaternion.Euler(Vector3.forward * 160f);

            IsOpen = true;
        }

        public IEnumerator OpenLidsSmooth()
        {
            float duration = 0.3f;

            lidFront.DOLocalRotate(Vector3.right * 250f, duration, RotateMode.LocalAxisAdd);
            lidBack.DOLocalRotate(Vector3.left * 250f, duration, RotateMode.LocalAxisAdd);

            yield return new WaitForSeconds(duration);

            lidLeft.DOLocalRotate(Vector3.back * 250f, duration, RotateMode.LocalAxisAdd);
            lidRight.DOLocalRotate(Vector3.forward * 250f, duration, RotateMode.LocalAxisAdd);

            yield return new WaitForSeconds(duration);

            IsOpen = true;
        }

        public void CloseIfOpened()
        {
            if (!IsOpen) return;

            var lidSequence = DOTween.Sequence();

            lidSequence.Append(lidLeft.DOLocalRotate(Vector3.forward * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .Join(lidRight.DOLocalRotate(Vector3.back * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .Append(lidFront.DOLocalRotate(Vector3.left * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .Join(lidBack.DOLocalRotate(Vector3.right * 250f, 0.3f, RotateMode.LocalAxisAdd));

            IsOpen = false;
        }

        /// <summary>
        /// Places the last product from the box onto the specified shelf.
        /// Performs necessary checks for compatibility (product type, shelf space) 
        /// and updates the UI accordingly.
        /// </summary>
        /// <param name="shelf">The target shelf to place the product on.</param>
        /// <returns>True if the product was placed successfully, false otherwise.</returns>        
        public bool Place(Shelf shelf)
        {
            if (shelf.AssignedProduct != null && Product != shelf.AssignedProduct)
            {
                message.Log("This shelf is assigned to a different product.");
                return false;
            }
            else if (shelf.ShelvingUnit.Section != Product.Section)
            {
                message.Log("Product doesn't belong in this section.");
                return false;
            }
            else if (shelf.Product == null)
            {
                shelf.Initialize(Product);
            }
            else if (shelf.Product != Product)
            {
                message.Log("Shelf contains a different product.");
                return false;
            }

            var productModel = productModels.LastOrDefault();
            int prevShelfQty = shelf.Quantity;

            if (shelf.PlaceProductModel(productModel, out Vector3 position))
            {
                productModel.transform.SetParent(shelf.transform);
                DOTween.Kill(productModel.transform);
                productModel.transform.DOLocalJump(position, 0.5f, 1, 0.5f);
                productModel.transform.DOLocalRotate(Vector3.zero, 0.5f);

                AudioManager.Instance.PlaySFX(AudioID.Draw);

                productModel.layer = LayerMask.NameToLayer("Default");

                productModels.Remove(productModel);

                if (Quantity == 0)
                {
                    Product = null;
                    UIManager.Instance.ToggleActionUI(ActionType.Place, false, null);
                }

                if (prevShelfQty == 0)
                    UIManager.Instance.ToggleActionUI(ActionType.Take, true, () => Take(shelf));

                return true;
            }

            return false;
        }

        /// <summary>
        /// Takes a product from the specified shelf and adds it to the box.
        /// Performs necessary checks for compatibility (box capacity, product type, box size).
        /// Handles UI updates to reflect changes in the box and shelf states.
        /// </summary>
        /// <param name="shelf">The shelf to take a product from.</param>
        /// <returns>True if a product was successfully taken from the shelf, false otherwise.</returns>
        public bool Take(Shelf shelf)
        {
            // Check if the box is full
            if (Product != null && Quantity >= Capacity)
            {
                message.Log("Box is full.");
                return false;
            }

            // If the box is not empty, check if the product types match
            if (Quantity > 0 && shelf.Product != Product)
            {
                message.Log("Box contains a different product.");
                return false;
            }

            // If the box is empty, check if the product's box size is compatible
            if (Quantity == 0 && Size != shelf.Product.Box.Size)
            {
                message.Log("Incompatible box size.");
                return false;
            }

            // Initialize the box if empty and compatible
            if (Quantity == 0)
            {
                Initialize(shelf.Product);
            }

            // Take the product from the shelf and add it to the box
            int prevQuantity = Quantity;
            var position = productPositions[prevQuantity];

            var productModel = shelf.TakeProductModel();
            productModels.Add(productModel);

            productModel.layer = GameConfig.Instance.HeldObjectLayer.ToSingleLayer();

            productModel.transform.SetParent(transform);
            DOTween.Kill(productModel.transform);
            productModel.transform.DOLocalJump(position, 0.5f, 1, 0.5f);
            productModel.transform.DOLocalRotate(Vector3.zero, 0.5f);

            AudioManager.Instance.PlaySFX(AudioID.Draw);

            // If this was the first product added, enable the Place button
            if (prevQuantity == 0)
            {
                UIManager.Instance.ToggleActionUI(ActionType.Place, true, () => Place(shelf));
            }

            if (shelf.Quantity == 0)
            {
                UIManager.Instance.ToggleActionUI(ActionType.Take, false, null);
            }

            return true;
        }

        public bool Store(Rack rack, bool isPlayer)
        {
            if (rack.Product == null)
            {
                rack.Initialize(Product);
            }
            else if (rack.Product != Product)
            {
                if (isPlayer) message.Log("Rack contains a different product.");
                return false;
            }

            if (rack.CanStoreBox(this, out Vector3 position, isPlayer))
            {
                IsStored = true;
                IsDisposable = false;

                transform.SetParent(rack.transform);
                DOTween.Kill(transform);
                transform.DOLocalJump(position, 0.5f, 1, 0.5f);
                transform.DOLocalRotate(Vector3.zero, 0.5f);

                // Change the layer of all products in the box back to the default layer.
                foreach (Transform child in transform)
                {
                    child.gameObject.layer = LayerMask.NameToLayer("Default");
                }

                if (isPlayer)
                {
                    if (IsOpen) Close();

                    // Disable UI elements related to holding and interacting with the box.
                    UIManager.Instance.ToggleActionUI(ActionType.Throw, false, null);
                    UIManager.Instance.ToggleActionUI(ActionType.Open, false, null);
                    UIManager.Instance.ToggleActionUI(ActionType.Close, false, null);
                    UIManager.Instance.ToggleActionUI(ActionType.Place, false, null);

                    AudioManager.Instance.PlaySFX(AudioID.Throw);

                    player.StateManager.PopState();

                    player = null;
                }

                return true;
            }

            return false;
        }

        public void Stock(Shelf shelfToStock)
        {
            if (shelfToStock.Product == null)
            {
                shelfToStock.Initialize(shelfToStock.AssignedProduct);
            }

            var productModel = productModels.LastOrDefault();

            if (shelfToStock.PlaceProductModel(productModel, out Vector3 position))
            {
                productModel.layer = LayerMask.NameToLayer("Default");

                productModel.transform.SetParent(shelfToStock.transform);
                DOTween.Kill(productModel.transform);
                productModel.transform.DOLocalJump(position, 0.5f, 1, 0.5f);
                productModel.transform.DOLocalRotate(Vector3.zero, 0.5f);

                productModels.Remove(productModel);

                if (Quantity == 0)
                {
                    Product = null;
                }
            }
        }
    }
}
using System.Collections.Generic;
using UnityEngine;

namespace CryingSnow.CheckoutFrenzy
{
    /// <summary>
    /// Abstract base class for containers that can hold products, 
    /// such as shelves and boxes.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public abstract class ProductContainer : MonoBehaviour
    {
        /// <summary>
        /// The product currently stored in the container.
        /// </summary>
        public Product Product { get; protected set; }

        private BoxCollider m_boxCollider;
        protected BoxCollider boxCollider
        {
            get
            {
                if (m_boxCollider == null)
                    m_boxCollider = GetComponent<BoxCollider>();

                return m_boxCollider;
            }
        }

        /// <summary>
        /// Gets the size of the container based on the size of the box collider.
        /// </summary>
        public virtual Vector3 Size => boxCollider.size;

        /// <summary>
        /// A list of possible product positions within the container.
        /// </summary>
        protected List<Vector3> productPositions = new List<Vector3>();

        /// <summary>
        /// A list of product models currently placed within the container.
        /// </summary>
        protected List<GameObject> productModels = new List<GameObject>();

        /// <summary>
        /// Gets the maximum capacity of the container (number of possible product positions).
        /// </summary>
        public int Capacity => productPositions.Count;

        /// <summary>
        /// Gets the current number of products in the container.
        /// </summary>
        public int Quantity => productModels.Count;

        public bool IsFull => Quantity >= Capacity;

        /// <summary>
        /// Initializes the container with the specified product.
        /// This method calculates and stores the possible product positions within the container.
        /// </summary>
        /// <param name="product">The product to store in the container.</param>
        public virtual void Initialize(Product product)
        {
            Product = product;

            Vector3Int fit = Product.FitOnContainer(Size);

            if (this is Box && product.OverrideBoxQuantity)
            {
                fit = product.BoxQuantity;
            }
            else if (this is Shelf && product.OverrideShelfQuantity)
            {
                fit = product.ShelfQuantity;
            }

            float cellWidth = Size.x / fit.x;
            float cellDepth = Size.z / fit.z;

            productPositions.Clear();

            for (int x = 0; x < fit.x; x++)
            {
                for (int y = 0; y < fit.y; y++)
                {
                    for (int z = 0; z < fit.z; z++)
                    {
                        Vector3 productPosition = new Vector3(
                            (x * cellWidth) + (cellWidth / 2) - (Size.x / 2),
                            (y * Product.Size.y),
                            (z * cellDepth) + (cellDepth / 2) - (Size.z / 2)
                        );

                        productPositions.Add(productPosition);
                    }
                }
            }
        }

        /// <summary>
        /// Restores the specified number of products to the container.
        /// </summary>
        /// <param name="product">The product to restore.</param>
        /// <param name="quantity">The number of products to restore.</param>
        public virtual void RestoreProducts(Product product, int quantity)
        {
            Initialize(product);

            for (int i = 0; i < quantity; i++)
            {
                var productModel = Instantiate(product.Model, transform);
                productModel.transform.localPosition = productPositions[i];
                productModel.transform.localRotation = Quaternion.identity;
                productModels.Add(productModel);
            }
        }
    }
}
using System.Linq;
using UnityEngine;
using TMPro;

namespace CryingSnow.CheckoutFrenzy
{
    /// <summary>
    /// A shelf for storing and displaying products in the store.
    /// Inherits from the ProductContainer class.
    /// </summary>
    public class Shelf : ProductContainer, IEmployeeTarget
    {
        [SerializeField, Tooltip("Text displaying product information")]
        private TMP_Text infoText;

        [SerializeField] private GameObject restockLabel;
        [SerializeField] private SpriteRenderer iconRenderer;

        /// <summary>
        /// The shelving unit this shelf belongs to.
        /// </summary>
        public ShelvingUnit ShelvingUnit { get; set; }

        public Product AssignedProduct { get; private set; }

        public bool IsTargeted { get; set; }

        /// <summary>
        /// Enables or disables interaction with the shelf collider.
        /// </summary>
        /// <param name="enabled">True to enable interaction, false to disable.</param>
        public void ToggleInteraction(bool enabled) => boxCollider.enabled = enabled;

        private void Awake()
        {
            // Set the layer of the shelf GameObject
            gameObject.layer = GameConfig.Instance.ShelfLayer.ToSingleLayer();

            // Ensures the label is correctly initialized, whether or not a product is assigned.  
            // This prevents conflicts between the shelf's initialization and the Data Manager's restore process.  
            // - If the Data Manager restores the saved product first on Awake(), calling SetLabel here ensures consistency.  
            // - If this Awake() runs first, SetLabel initializes the label, and the Data Manager will update it later if needed.  
            // - Regardless of the order, the final label state remains correct, preventing incorrect label states.
            SetLabel(AssignedProduct);
        }

        private void Start()
        {
            // Subscribe to the OnPriceChanged event from StoreManager
            DataManager.Instance.OnPriceChanged += HandlePriceChanged;
            UpdateInfoText();
        }

        private void OnDisable()
        {
            // Unsubscribe from the OnPriceChanged event when disabled
            DataManager.Instance.OnPriceChanged -= HandlePriceChanged;
        }

        /// <summary>
        /// Handles price changes for the product on this shelf.
        /// </summary>
        /// <param name="productId">The ID of the product whose price changed.</param>
        private void HandlePriceChanged(int productId)
        {
            if (Product != null && Product.ProductID == productId)
            {
                UpdateInfoText();
            }
        }

        /// <summary>
        /// Attempts to place a product model on the shelf.
        /// </summary>
        /// <param name="productModel">The product model GameObject to place.</param>
        /// <param name="productPosition">Will be set to the position where the product was placed, or Vector3.zero if placement failed.</param>
        /// <returns>True if the product was placed successfully, false if the shelf is full.</returns>
        public bool PlaceProductModel(GameObject productModel, out Vector3 productPosition)
        {
            productPosition = Vector3.zero;

            if (Quantity < Capacity)
            {
                productModels.Add(productModel);
                productPosition = productPositions[Quantity - 1];
                UpdateInfoText();
                return true;
            }
            else
            {
                UIManager.Instance.Message.Log("Shelf is full!");
                return false;
            }
        }

        /// <summary>
        /// Takes the last placed product model from the shelf.
        /// </summary>
        /// <returns>The last placed product model GameObject, or null if the shelf is empty.</returns>
        public GameObject TakeProductModel()
        {
            var productModel = productModels.LastOrDefault();

            if (productModel != null)
            {
                productModels.Remove(productModel);

                if (Quantity == 0) Product = null;

                UpdateInfoText();
            }

            return productModel;
        }

        /// <summary>
        /// Updates the text displaying product information on the shelf.
        /// </summary>
        private void UpdateInfoText()
        {
            if (infoText == null) return;

            if (Product != null)
            {
                decimal price = DataManager.Instance.GetCustomProductPrice(Product);
                infoText.text = $"[{Quantity}/{Capacity}] ${price:F2}";
            }
            else
            {
                infoText.text = $"[-/-] $--.--";
            }
        }

        public void SetLabel(Product product)
        {
            AssignedProduct = product;

            restockLabel.SetActive(product != null);
            iconRenderer.sprite = product?.Icon;
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cinemachine;
using TMPro;

namespace CryingSnow.CheckoutFrenzy
{
    public class PC : MonoBehaviour, IInteractable
    {
        public static PC Instance { get; private set; }

        [SerializeField, Tooltip("The Cinemachine virtual camera used to display the PC monitor view.")]
        private CinemachineVirtualCamera monitorCamera;

        [SerializeField, Tooltip("The text mesh pro UI element used to display information on the PC monitor.")]
        private TMP_Text monitorText;

        [SerializeField, Tooltip("The duration (in seconds) to simulate the booting of the PC.")]
        private float bootingDuration = 1.5f;

        [SerializeField, Tooltip("The total number of segments used to represent the loading bar on the PC monitor.")]
        private int totalBarSegments = 50;

        public event System.Action<Dictionary<IPurchasable, int>> OnCartChanged;

        private PlayerController player;

        private Dictionary<IPurchasable, int> cart = new Dictionary<IPurchasable, int>();

        private List<IPurchasable> purchaseOrders = new List<IPurchasable>();

        private bool isProcessing;

        private void Awake()
        {
            Instance = this;
            gameObject.layer = GameConfig.Instance.InteractableLayer.ToSingleLayer();

            monitorText.text = "<size=0.5>Standby...";
        }

        private void Start()
        {
            DataManager.Instance.OnSave += () =>
            {
                // Calculate the total price of all pending purchase orders and save it to GameData.
                decimal totalPrice = CalculateOrderPrice(purchaseOrders);
                DataManager.Instance.Data.PendingOrdersValue = totalPrice;
            };
        }

        public void Interact(PlayerController player)
        {
            this.player = player;

            monitorCamera.gameObject.SetActive(true);
            StartCoroutine(BootPC());

            player.StateManager.PushState(PlayerState.Busy);

            UIManager.Instance.InteractMessage.Hide();
        }

        public void OnFocused()
        {
            string message = "Tap to turn on the PC";
            UIManager.Instance.InteractMessage.Display(message);
        }

        public void OnDefocused()
        {
            UIManager.Instance.InteractMessage.Hide();
        }

        private IEnumerator BootPC()
        {
            float elapsedTime = 0f;

            while (elapsedTime < bootingDuration)
            {
                elapsedTime += Time.deltaTime;

                // Calculate the progress (0 to 1).
                float progress = Mathf.Clamp01(elapsedTime / bootingDuration);

                // Update the loading bar.
                int filledSegments = Mathf.RoundToInt(progress * totalBarSegments);
                string loadingBar = new string('|', filledSegments) + new string('.', totalBarSegments - filledSegments);

                // Update the monitor text.
                monitorText.text = $"Booting the PC\nPlease wait...\n<mspace=0.1>[{loadingBar}]</mspace>\n{Mathf.RoundToInt(progress * 100)}%";

                yield return null;
            }

            // Ensure the text shows 100% and a full bar at the end.
            monitorText.text = $"Booting the PC\nPlease wait...\n<mspace=0.1>[{new string('|', totalBarSegments)}]</mspace>\n100%";

            yield return new WaitForSeconds(0.5f);

            UIManager.Instance.PCMonitor.Display(onClose: () =>
            {
                monitorCamera.gameObject.SetActive(false);
                monitorText.text = "<size=0.5>Standby...";

                player.StateManager.PopState();
                player = null;
            });
        }

        /// <summary>
        /// Adds the specified purchasable item to the shopping cart.
        /// </summary>
        /// <param name="purchasable">The item to add to the cart.</param>
        /// <param name="amount">The quantity of the item to add.</param>
        public void AddToCart(IPurchasable purchasable, int amount)
        {
            if (cart.ContainsKey(purchasable)) cart[purchasable] += amount;
            else cart.Add(purchasable, amount);

            OnCartChanged?.Invoke(cart);

            AudioManager.Instance.PlaySFX(AudioID.Click);
        }

        /// <summary>
        /// Removes the specified purchasable item from the shopping cart.
        /// </summary>
        /// <param name="purchasable">The item to remove from the cart.</param>
        public void RemoveFromCart(IPurchasable purchasable)
        {
            cart.Remove(purchasable);
            OnCartChanged?.Invoke(cart);

            AudioManager.Instance.PlaySFX(AudioID.Click);
        }

        /// <summary>
        /// Clears all items from the shopping cart.
        /// </summary>
        public void ClearCart()
        {
            cart.Clear();
            OnCartChanged?.Invoke(cart);

            AudioManager.Instance.PlaySFX(AudioID.Click);
        }

        /// <summary>
        /// Processes the current order in the shopping cart.
        ///
        /// Calculates the total price, checks for sufficient funds, 
        /// creates purchase orders, updates missions, and initiates the order processing.
        /// </summary>
        public void Checkout()
        {
            if (cart.Count == 0)
            {
                UIManager.Instance.Message.Log("Cart is empty. Add products or furnitures first.", Color.red);
                return;
            }

            // Create a list to hold the individual purchase orders.
            List<IPurchasable> newOrders = new List<IPurchasable>();

            // Add each item in the cart to the purchase order list 
            // based on the quantity in the cart.
            foreach (var kvp in cart)
            {
                for (int i = 0; i < kvp.Value; i++)
                {
                    newOrders.Add(kvp.Key);
                }
            }

            // Calculate the total price of all items in the order.
            decimal totalPrice = CalculateOrderPrice(newOrders);

            // Check if the player has enough money to complete the order.
            if (DataManager.Instance.PlayerMoney >= totalPrice)
            {
                // Process each order in the list.
                foreach (var order in newOrders)
                {
                    purchaseOrders.Add(order);

                    if (order is Product product)
                    {
                        // Update the "Restock" mission progress for the product.
                        MissionManager.Instance.UpdateMission(Mission.Goal.Restock, 1, product.ProductID);
                    }
                    else if (order is Furniture furniture)
                    {
                        // Update the "Furnish" mission progress for the furniture.
                        MissionManager.Instance.UpdateMission(Mission.Goal.Furnish, 1, furniture.FurnitureID);
                    }
                }

                ClearCart();

                StartCoroutine(ProcessOrder());

                // Deduct the total price from the player's money.
                DataManager.Instance.PlayerMoney -= totalPrice;

                // Display a success message to the player.
                UIManager.Instance.Message.Log("Checkout successful!");
                AudioManager.Instance.PlaySFX(AudioID.Kaching);
            }
            else
            {
                // Display an error message if the player doesn't have enough money.
                UIManager.Instance.Message.Log("You don't have enough money!", Color.red);
            }
        }

        /// <summary>
        /// Calculates the total price of a list of purchasable items.
        /// </summary>
        /// <param name="orders">The list of purchasable items to calculate the price for.</param>
        /// <returns>The total price of all items in the list.</returns>
        private decimal CalculateOrderPrice(List<IPurchasable> orders)
        {
            decimal totalPrice = 0m;

            foreach (var order in orders)
            {
                if (order is Product product)
                {
                    // Calculate the price of the product based on its box quantity.
                    decimal defaultPrice = product.Price;
                    int boxQuantity = product.GetBoxQuantity();
                    totalPrice += defaultPrice * boxQuantity;
                }
                else
                {
                    // Add the price of the furniture directly.
                    totalPrice += order.Price;
                }
            }

            return totalPrice;
        }

        /// <summary>
        /// Processes the purchase orders in the queue.
        /// 
        /// This method simulates the order delivery process 
        /// by waiting for a specified time for each order.
        /// </summary>
        private IEnumerator ProcessOrder()
        {
            if (isProcessing) yield break;

            isProcessing = true;

            while (purchaseOrders.Count > 0)
            {
                var order = purchaseOrders.FirstOrDefault();
                int time = order.OrderTime;

                // Simulate the order delivery time by waiting for the specified duration.
                while (time > 0)
                {
                    time--;
                    UIManager.Instance.UpdateDeliveryTimer(time);
                    yield return new WaitForSeconds(1f);
                }

                // Deliver the order (instantiate the product or furniture).
                DeliverOrder(order);

                // Remove the processed order from the queue.
                purchaseOrders.Remove(order);
            }

            isProcessing = false;
        }

        /// <summary>
        /// Delivers the specified order to the delivery point.
        /// 
        /// Instantiates the product or furniture at the delivery point.
        /// </summary>
        /// <param name="order">The order to be delivered.</param>
        private void DeliverOrder(IPurchasable order)
        {
            Transform deliveryPoint = StoreManager.Instance.DeliveryPoint;

            if (order is Product product)
            {
                // Instantiate the product's box at the delivery point.
                var box = Instantiate(product.Box, deliveryPoint.position, deliveryPoint.rotation);
                box.name = product.Box.name;
                box.RestoreProducts(product, product.GetBoxQuantity());
            }
            else if (order is Furniture furniture)
            {
                // Instantiate the furniture (in a box) at the delivery point.
                var furnitureBox = Instantiate(
                    StoreManager.Instance.FurnitureBoxPrefab,
                    deliveryPoint.position,
                    deliveryPoint.rotation
                );

                furnitureBox.furnitureId = furniture.FurnitureID;
            }
        }
    }
}
using System.Globalization;
using UnityEngine;
using DG.Tweening;

namespace CryingSnow.CheckoutFrenzy
{
    public class TimeManager : MonoBehaviour
    {
        public static TimeManager Instance { get; private set; }

        [Tooltip("The total in-game minutes (0-1439 for 24 hours).")]
        [SerializeField, Range(0f, 1439f)] private float totalMinutes = 0;

        [Tooltip("The scale at which time progresses. 1 means 1 real second equals 1 in-game minute.")]
        [SerializeField, Range(1, 60)] private float timeScale = 1.0f;

        [SerializeField, Tooltip("Sets the sun's x-axis rotation at midnight, defining its initial position in the sky. (-90f places the sun directly below the horizon).")]
        private float sunMidnightOffset = -90f;

        [SerializeField, Tooltip("Sets the sun's y-axis rotation, determining the direction of its path. (-90f aligns the sun to rise in the east and set in the west).")]
        private float sunDirectionOffset = -90f;

        [SerializeField, Tooltip("Defines the range for nighttime.")]
        private TimeRange nightTime;

        [SerializeField, Tooltip("Materials for objects with night-time emission effects.")]
        private Material[] emissiveMaterials;

        [Header("Fog Colors")]
        [SerializeField] private Color nightFogColor = Color.grey;
        [SerializeField] private Color dayFogColor = Color.blue;

        private Light sun;
        private bool wasNightTime;

        public bool AllowTimeUpdate { get; set; } = true;

        /// <summary>
        /// Gets the current hour (0-23).
        /// </summary>
        public int Hour => Mathf.FloorToInt(totalMinutes / 60);

        /// <summary>
        /// Gets the current minute (0-59).
        /// </summary>
        public int Minute => Mathf.FloorToInt(totalMinutes % 60);

        public int TotalMinutes => Mathf.FloorToInt(totalMinutes);

        public event System.Action<bool> OnNightTimeChanged;
        public event System.Action OnMinutePassed;

        private int previousMinute;

        private const float MinutesPerDay = 24 * 60;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            var sunObj = new GameObject("Sun");
            sun = sunObj.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.bounceIntensity = 0f;
            sun.shadows = LightShadows.None;
            sun.renderMode = LightRenderMode.ForceVertex;
            sun.cullingMask = 0;
            RenderSettings.sun = sun;

            totalMinutes = DataManager.Instance.Data.TotalMinutes;
            wasNightTime = !nightTime.IsWithinRange(TotalMinutes);
            UpdateSunRotation();
        }

        private void Update()
        {
            if (!AllowTimeUpdate) return;

            totalMinutes = totalMinutes + Time.deltaTime * timeScale;

            if (totalMinutes >= MinutesPerDay)
            {
                totalMinutes = 0f;
                DataManager.Instance.Data.TotalDays++;
            }

            UpdateSunRotation();
            UpdateTimeState();
        }

        private void UpdateSunRotation()
        {
            // Calculate the normalized time for the current day cycle.
            float timeNormalized = totalMinutes / MinutesPerDay;

            var targetRotation = Quaternion.Euler(
                360f * timeNormalized + sunMidnightOffset,
                sunDirectionOffset,
                0f
            );

            sun.transform.rotation = targetRotation;
        }

        private void UpdateTimeState()
        {
            // Check if the current time is within the night range.
            bool isCurrentlyNightTime = IsNightTime();

            // Trigger the event if the nighttime state changes.
            if (isCurrentlyNightTime != wasNightTime)
            {
                wasNightTime = isCurrentlyNightTime;

                OnNightTimeChanged?.Invoke(isCurrentlyNightTime);

                UpdateEmissiveMaterials(isCurrentlyNightTime);
                UpdateFogColor(isCurrentlyNightTime);
            }

            if (previousMinute != Minute)
            {
                previousMinute = Minute;
                OnMinutePassed?.Invoke();
            }
        }

        private void UpdateEmissiveMaterials(bool isNight)
        {
            for (int i = 0; i < emissiveMaterials.Length; i++)
            {
                var emissiveMat = emissiveMaterials[i];

                if (isNight) emissiveMat.EnableKeyword("_EMISSION");
                else emissiveMat.DisableKeyword("_EMISSION");
            }
        }

        private void UpdateFogColor(bool isNight)
        {
            Color targetColor = isNight ? nightFogColor : dayFogColor;

            DOTween.To(() => RenderSettings.fogColor, x => RenderSettings.fogColor = x, targetColor, 3f)
               .SetEase(Ease.Linear);
        }

        public bool IsNightTime()
        {
            return nightTime.IsWithinRange(TotalMinutes);
        }

        /// <summary>
        /// Manually set the current time.
        /// </summary>
        /// <param name="newHour">The new hour (0-23).</param>
        /// <param name="newMinute">The new minute (0-59).</param>
        public void SetTime(int newHour, int newMinute)
        {
            totalMinutes = Mathf.Clamp(newHour, 0, 23) * 60 + Mathf.Clamp(newMinute, 0, 59);
            UpdateSunRotation();
        }

        /// <summary>
        /// Adjusts the time scale at runtime.
        /// </summary>
        /// <param name="newTimeScale">The new time scale.</param>
        public void SetTimeScale(float newTimeScale)
        {
            timeScale = Mathf.Max(0, newTimeScale); // Prevent negative time scale.
        }

        /// <summary>
        /// Gets the current time as a formatted string.
        /// </summary>
        /// <returns>A string in "HH:MM" format.</returns>
        public string GetFormattedTime()
        {
            System.DateTime currentTime = new System.DateTime(1, 1, 1, Hour, Minute, 0);
            return currentTime.ToString("hh:mm tt", CultureInfo.InvariantCulture);
        }
    }
}
using UnityEngine;

namespace CryingSnow.CheckoutFrenzy
{
    [System.Serializable]
    public struct TimeRange
    {
        [Range(0, 23)]
        [Tooltip("The starting hour for this time range.")]
        public int StartHour;

        [Range(0, 59)]
        [Tooltip("The starting minute for this time range.")]
        public int StartMinute;

        [Range(0, 23)]
        [Tooltip("The ending hour for this time range.")]
        public int EndHour;

        [Range(0, 59)]
        [Tooltip("The ending minute for this time range.")]
        public int EndMinute;

        /// <summary>
        /// Converts the time range to total minutes.
        /// </summary>
        /// <param name="hour">The hour component.</param>
        /// <param name="minute">The minute component.</param>
        /// <returns>Total minutes in a day.</returns>
        public static int ToMinutes(int hour, int minute) => hour * 60 + minute;

        /// <summary>
        /// Checks if a given time (in minutes) is within this range.
        /// </summary>
        /// <param name="currentTimeMinutes">The current time in total minutes.</param>
        /// <returns>True if within range, otherwise false.</returns>
        public bool IsWithinRange(int currentTimeMinutes)
        {
            int startMinutes = ToMinutes(StartHour, StartMinute);
            int endMinutes = ToMinutes(EndHour, EndMinute);

            // Handle overnight ranges (e.g., 22:00 - 06:00)
            if (startMinutes > endMinutes)
            {
                return currentTimeMinutes >= startMinutes || currentTimeMinutes < endMinutes;
            }

            return currentTimeMinutes >= startMinutes && currentTimeMinutes < endMinutes;
        }
    }
}
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace CryingSnow.CheckoutFrenzy
{
    public class CashRegister : MonoBehaviour
    {
        [SerializeField, Tooltip("The button used to undo the given change.")]
        private Button undoButton;

        [SerializeField, Tooltip("The button used to clear the given change.")]
        private Button clearButton;

        [SerializeField, Tooltip("The button used to confirm the transaction.")]
        private Button confirmButton;

        public event System.Action<int> OnDraw;
        public event System.Action OnUndo;
        public event System.Action OnClear;
        public event System.Action OnConfirm;

        private RectTransform rect;
        private float originalPosY;
        private bool allowDrawing;

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            originalPosY = rect.anchoredPosition.y;

            // Add listeners to the clear and confirm buttons to invoke the corresponding events.
            undoButton.onClick.AddListener(() => OnUndo?.Invoke());
            clearButton.onClick.AddListener(() => OnClear?.Invoke());
            confirmButton.onClick.AddListener(() => OnConfirm?.Invoke());
        }

        /// <summary>
        /// Handles drawing money from the cash register.
        /// </summary>
        /// <param name="amount">The amount of money to draw (in cents).</param>
        public void Draw(int amount)
        {
            // Prevent drawing if it's not allowed (e.g., while the register is closing).
            if (!allowDrawing) return;

            OnDraw?.Invoke(amount);

            // Play different sound effects based on the amount drawn.
            AudioID audioId = amount < 100 ? AudioID.Coin : AudioID.Draw;
            AudioManager.Instance.PlaySFX(audioId);
        }

        /// <summary>
        /// Opens the cash register UI, allowing money to be drawn.
        /// </summary>
        public void Open()
        {
            // Use DOTween to smoothly animate the cash register opening.
            rect.DOAnchorPosY(0f, 0.5f)
                .OnComplete(() => allowDrawing = true); // Enable drawing after the animation completes.
        }

        /// <summary>
        /// Closes the cash register UI, preventing further drawing.
        /// </summary>
        public void Close()
        {
            allowDrawing = false; // Disable drawing.
            rect.DOAnchorPosY(originalPosY, 0.5f); // Animate the cash register closing.
        }
    }
}
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace CryingSnow.CheckoutFrenzy
{
    public class PaymentTerminal : MonoBehaviour
    {
        [SerializeField, Tooltip("The text display showing the entered amount.")]
        private TMP_Text displayText;

        [SerializeField, Tooltip("The button used to confirm the payment.")]
        private Button confirmButton;

        public event System.Action<decimal> OnConfirm;

        private RectTransform rect;
        private float originalPosY;
        private bool allowInput;
        private string enteredAmount;

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            originalPosY = rect.anchoredPosition.y;
            confirmButton.onClick.AddListener(ConfirmAmount); // Add listener to the confirm button.
        }

        /// <summary>
        /// Appends the input to the entered amount string.
        /// </summary>
        /// <param name="input">The input string (number, "back", or ".").</param>
        public void Append(string input)
        {
            if (!allowInput) return;

            if (input == "back")
            {
                if (enteredAmount.Length > 0)
                {
                    enteredAmount = enteredAmount.Substring(0, enteredAmount.Length - 1); // Remove the last character.
                }
            }
            else if (input == "." && !enteredAmount.Contains(".")) // Allow only one decimal point.
            {
                enteredAmount += ".";
            }
            else if (int.TryParse(input, out int _)) // Only allow numeric input.
            {
                enteredAmount += input;
            }

            displayText.text = $"$ {enteredAmount}";

            AudioManager.Instance.PlaySFX(AudioID.Beep);
        }

        /// <summary>
        /// Confirms the entered amount and triggers the OnConfirm event.
        /// </summary>
        private void ConfirmAmount()
        {
            if (decimal.TryParse(enteredAmount, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal amount) && amount > 0)
            {
                OnConfirm?.Invoke(amount);
            }
            else
            {
                UIManager.Instance.Message.Log("Invalid amount. Please enter a valid amount.", Color.red);
            }

            AudioManager.Instance.PlaySFX(AudioID.Beep);
        }

        /// <summary>
        /// Opens the payment terminal UI, allowing input.
        /// </summary>
        public void Open()
        {
            enteredAmount = ""; // Clear the entered amount.
            displayText.text = "$";  // Reset the display text.

            rect.DOAnchorPosY(0f, 0.5f) // Animate the terminal opening.
                .OnComplete(() => allowInput = true); // Enable input after the animation.
        }

        /// <summary>
        /// Closes the payment terminal UI, disabling input.
        /// </summary>
        public void Close()
        {
            allowInput = false; // Disable input.
            rect.DOAnchorPosY(originalPosY, 0.5f); // Animate the terminal closing.
        }
    }
}
using UnityEngine;
using Cinemachine;
using SimpleInputNamespace;
using DG.Tweening;

namespace CryingSnow.CheckoutFrenzy
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(AudioSource))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField, Tooltip("Movement speed of the player.")]
        private float movingSpeed = 7.5f;

        [SerializeField, Tooltip("Gravity applied to the player.")]
        private float gravity = -9.81f;

        [SerializeField, Tooltip("Rotation speed of the player's view.")]
        private float lookSpeed = 2f;

        [SerializeField, Tooltip("Maximum angle the player can look up or down.")]
        private float lookXLimit = 45.0f;

        [SerializeField, Tooltip("Maximum distance for interaction.")]
        private float interactDistance = 3.5f;

        [SerializeField, Tooltip("Time in seconds the interact button must be held to trigger an interaction.")]
        private float interactHoldThreshold = 1.0f;

        [SerializeField, Tooltip("Transform representing the player's holding point (hands).")]
        private Transform holdPoint;

        [SerializeField, Tooltip("Transform representing the player's camera.")]
        private Transform playerCamera;

        [Header("Sway Settings")]
        [SerializeField, Tooltip("Amount of sway applied to the camera.")]
        private float swayAmount = 0.05f;

        [SerializeField, Tooltip("Speed of the camera sway.")]
        private float swaySpeed = 5.0f;

        [SerializeField, Tooltip("Maximum amount of sway applied to the camera.")]
        private float maxSwayAmount = 0.2f;

        [Header("Bobbing Settings")]
        [SerializeField, Tooltip("Frequency of the camera bobbing effect.")]
        private float bobFrequency = 10.0f;

        [SerializeField, Tooltip("Horizontal amplitude of the camera bobbing effect.")]
        private float bobHorizontalAmplitude = 0.04f;

        [SerializeField, Tooltip("Vertical amplitude of the camera bobbing effect.")]
        private float bobVerticalAmplitude = 0.04f;

        [SerializeField, Tooltip("Smoothing applied to the camera bobbing effect.")]
        private float bobSmoothing = 8f;

        [Header("Sound Effects")]
        [SerializeField, Tooltip("Array of audio clips used for footstep sounds.")]
        private AudioClip[] footstepClips;

        [SerializeField, Tooltip("Distance traveled before playing a footstep sound.")]
        private float stepDistance = 2.0f;

        public Transform HoldPoint => holdPoint;

        public PlayerStateManager StateManager { get; private set; }

        private Camera mainCamera;

        private CharacterController controller;
        private Vector3 movement;
        private Vector3 playerVelocity = Vector3.zero;
        private float rotationX = 0;
        private string xLookAxis;
        private string yLookAxis;
        private bool isMobileControl;

        private AudioSource audioSource;
        private CinemachineVirtualCamera playerVirtualCam;

        private IInteractable lastInteractable;
        private Shelf lastShelf;
        private Rack lastRack;

        private Vector3 holdPointOrigin;

        private float bobTimer = 0.0f;
        private float interactHoldDuration = 0f;

        private float distanceTraveled;

        private void Awake()
        {
            StateManager = new PlayerStateManager(this);

            controller = GetComponent<CharacterController>();
            audioSource = GetComponent<AudioSource>();
            playerVirtualCam = GetComponentInChildren<CinemachineVirtualCamera>();

            holdPointOrigin = holdPoint.localPosition;
        }

        private void Start()
        {
            mainCamera = Camera.main;

            isMobileControl = GameConfig.Instance.ControlMode == ControlMode.Mobile;
            xLookAxis = isMobileControl ? "Look X" : "Mouse X";
            yLookAxis = isMobileControl ? "Look Y" : "Mouse Y";

#if UNITY_EDITOR
            var touchpad = FindObjectOfType<Touchpad>();
            touchpad.sensitivity = 1f;
#endif
        }

        private void Update()
        {
            HandleMovement();
            HandleSway();
            HandleBobbing();
            HandleFootsteps();

            switch (StateManager.CurrentState)
            {
                case PlayerState.Free:
                    DetectInteractable();
                    DetectShelfToCustomize();
                    DetectRack();
                    break;

                case PlayerState.Holding:
                    DetectShelfToRestock();
                    DetectRack();
                    break;

                case PlayerState.Working:
                    Work();
                    break;

                case PlayerState.Moving:
                case PlayerState.Busy:
                case PlayerState.Paused:
                default:
                    break;
            }
        }

        private void HandleMovement()
        {
            // Gravity handling
            if (controller.isGrounded && playerVelocity.y < 0f)
            {
                playerVelocity.y = -2f;
            }

            Vector3 forward = transform.forward;
            Vector3 right = transform.right;

            float curSpeedX = !IsMovementBlocked() ? movingSpeed * SimpleInput.GetAxis("Vertical") : 0f;
            float curSpeedY = !IsMovementBlocked() ? movingSpeed * SimpleInput.GetAxis("Horizontal") : 0f;

            movement = (forward * curSpeedX) + (right * curSpeedY);
            controller.Move(movement * Time.deltaTime);

            // Gravity
            playerVelocity.y += gravity * Time.deltaTime;
            controller.Move(playerVelocity * Time.deltaTime);

            // Look rotation
            if (!IsMovementBlocked())
            {
                float lookY = SimpleInput.GetAxis(yLookAxis) * lookSpeed;
                float lookX = SimpleInput.GetAxis(xLookAxis) * lookSpeed;

                rotationX -= lookY;
                rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);

                playerCamera.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
                transform.rotation *= Quaternion.Euler(0f, lookX, 0f);
            }
        }

        private bool IsMovementBlocked()
        {
            return StateManager.CurrentState is PlayerState.Working
                or PlayerState.Busy
                or PlayerState.Paused;
        }

        private void HandleSway()
        {
            // Get the look input values (horizontal and vertical).
            float lookX = SimpleInput.GetAxis(xLookAxis);
            float lookY = SimpleInput.GetAxis(yLookAxis);

            // Calculate the target position for the hold point based on look input.
            Vector3 targetPosition = new Vector3(-lookX, -lookY, 0) * swayAmount;

            // Clamp the sway amount to prevent excessive movement.
            targetPosition.x = Mathf.Clamp(targetPosition.x, -maxSwayAmount, maxSwayAmount);
            targetPosition.y = Mathf.Clamp(targetPosition.y, -maxSwayAmount, maxSwayAmount);

            // Smoothly interpolate the hold point's position towards the target position.
            holdPoint.localPosition = Vector3.Lerp(holdPoint.localPosition, holdPointOrigin + targetPosition, Time.deltaTime * swaySpeed);
        }

        private void HandleBobbing()
        {
            // Check if the player is moving.
            if (movement.magnitude > 0.1f)
            {
                // Increment the bob timer based on movement speed and frequency.
                bobTimer += Time.deltaTime * bobFrequency;

                // Calculate the horizontal and vertical offsets for the bobbing effect.
                float horizontalOffset = Mathf.Sin(bobTimer) * bobHorizontalAmplitude;
                float verticalOffset = Mathf.Cos(bobTimer * 2) * bobVerticalAmplitude;

                // Combine the offsets into a bobbing position vector.
                Vector3 bobPosition = new Vector3(horizontalOffset, verticalOffset, 0);

                // Smoothly interpolate the hold point's position with the bobbing effect.
                holdPoint.localPosition = Vector3.Lerp(holdPoint.localPosition, holdPointOrigin + bobPosition, Time.deltaTime * bobSmoothing);
            }
            else
            {
                // Smoothly return the hold point to its origin when the player is not moving.
                holdPoint.localPosition = Vector3.Lerp(holdPoint.localPosition, holdPointOrigin, Time.deltaTime * bobSmoothing);
            }
        }

        private void HandleFootsteps()
        {
            // If the player is not moving, don't play footsteps.
            if (movement.magnitude < 0.1f) return;

            // Increase the distance traveled based on movement magnitude and time.
            distanceTraveled += movement.magnitude * Time.deltaTime;

            // Check if the traveled distance exceeds the step distance threshold.
            if (distanceTraveled >= stepDistance)
            {
                PlayFootstepSound();
                distanceTraveled = 0f;
            }
        }

        private void PlayFootstepSound()
        {
            if (footstepClips.Length == 0) return;

            AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
            audioSource.PlayOneShot(clip);
        }

        public void SetInteractable(IInteractable interactable)
        {
            lastInteractable = interactable;
            InteractWithCurrent();
        }

        private void DetectInteractable()
        {
            // Create a ray from the center of the viewport.
            Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            // Perform a raycast to detect an interactable within the interact distance.
            if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, GameConfig.Instance.InteractableLayer))
            {
                IInteractable interactable = hit.transform.GetComponent<IInteractable>();
                if (interactable != lastInteractable)
                {
                    // Defocus the previous interactable (if any)
                    lastInteractable?.OnDefocused();

                    // Focus on the new interactable
                    UIManager.Instance.ToggleInteractButton(true);
                    lastInteractable = interactable;
                    lastInteractable.OnFocused();

                    // Reset hold duration and UI
                    interactHoldDuration = 0f;
                    UIManager.Instance.UpdateHoldProgress(0f);
                }
            }
            else if (lastInteractable != null)
            {
                // Defocus the last interactable when no interactable is hit
                UIManager.Instance.ToggleInteractButton(false);
                lastInteractable.OnDefocused();
                lastInteractable = null;

                // Reset hold duration and UI
                interactHoldDuration = 0f;
                UIManager.Instance.UpdateHoldProgress(0f);
            }

            if (lastInteractable != null)
            {
                if (lastInteractable is Furniture)
                {
                    // Hold interaction for Furniture
                    if (isMobileControl ? SimpleInput.GetButton("Interact") : Input.GetMouseButton(0))
                    {
                        interactHoldDuration += Time.deltaTime;

                        // Update the radial fill UI based on hold progress
                        UIManager.Instance.UpdateHoldProgress(interactHoldDuration / interactHoldThreshold);

                        if (interactHoldDuration >= interactHoldThreshold)
                        {
                            InteractWithCurrent();
                            interactHoldDuration = 0f;
                            UIManager.Instance.UpdateHoldProgress(0f);
                        }
                    }
                    else
                    {
                        interactHoldDuration = 0f;
                        UIManager.Instance.UpdateHoldProgress(0f);
                    }
                }
                else if (isMobileControl ? SimpleInput.GetButtonDown("Interact") : Input.GetMouseButtonDown(0))
                {
                    // Immediate interaction for other interactables
                    InteractWithCurrent();
                }
            }
        }

        private void InteractWithCurrent()
        {
            lastInteractable?.Interact(this);
            UIManager.Instance.ToggleInteractButton(false);
        }

        private void DetectShelfToCustomize()
        {
            // Create a ray from the center of the viewport.
            Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            // Perform a raycast to detect a shelf within the interact distance.
            if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, GameConfig.Instance.ShelfLayer))
            {
                Shelf detectedShelf = hit.transform.GetComponent<Shelf>();

                // Check if the detected shelf is different from the last detected shelf.
                if (detectedShelf != lastShelf)
                {
                    UIManager.Instance.ToggleActionUI(ActionType.Price, false, null);
                    UIManager.Instance.ToggleActionUI(ActionType.Label, false, null);

                    // Check if the detected shelf has a product.
                    if (detectedShelf?.Product != null)
                    {
                        // Enable the set price button and set its click action.
                        UIManager.Instance.ToggleActionUI(ActionType.Price, true, () =>
                        {
                            UIManager.Instance.ToggleActionUI(ActionType.Price, false, null);

                            StateManager.PushState(PlayerState.Busy);

                            var priceCustomizer = UIManager.Instance.PriceCustomizer;

                            // Remove any existing listeners and add a new listener for the price customizer's close event.
                            priceCustomizer.OnClose.RemoveAllListeners();
                            priceCustomizer.OnClose.AddListener(() =>
                            {
                                StateManager.PopState();
                            });

                            priceCustomizer.Open(detectedShelf.Product);

                            // Open the shelving unit if it's not already open.
                            if (!detectedShelf.ShelvingUnit.IsOpen)
                            {
                                detectedShelf.ShelvingUnit.Open(true, true);
                            }

                            detectedShelf.ShelvingUnit.OnDefocused();
                        });
                    }
                    else
                    {
                        UIManager.Instance.ToggleActionUI(ActionType.Label, true, () =>
                        {
                            UIManager.Instance.ToggleActionUI(ActionType.Label, false, null);

                            StateManager.PushState(PlayerState.Busy);

                            var labelCustomizer = UIManager.Instance.LabelCustomizer;

                            // Remove any existing listeners and add a new listener for the label customizer's close event.
                            labelCustomizer.OnClose.RemoveAllListeners();
                            labelCustomizer.OnClose.AddListener(() =>
                            {
                                StateManager.PopState();
                            });

                            labelCustomizer.Open(detectedShelf);
                            detectedShelf.ShelvingUnit.OnDefocused();
                        });
                    }

                    lastShelf = detectedShelf;
                }
            }
            else if (lastShelf != null)
            {
                // Disable the set price and set label action UIs if no shelf is detected.
                UIManager.Instance.ToggleActionUI(ActionType.Price, false, null);
                UIManager.Instance.ToggleActionUI(ActionType.Label, false, null);

                // Reset the last detected shelf.
                lastShelf = null;
            }
        }

        private void DetectShelfToRestock()
        {
            // Create a ray from the center of the viewport.
            Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            // Get the currently held box.
            Box box = lastInteractable as Box;

            // Check if the box is open and perform a raycast to detect a shelf.
            if (box.IsOpen && Physics.Raycast(ray, out RaycastHit hit, interactDistance, GameConfig.Instance.ShelfLayer))
            {
                Shelf detectedShelf = hit.transform.GetComponent<Shelf>();

                // Check if the detected shelf is different from the last detected shelf.
                if (detectedShelf != lastShelf)
                {
                    // Check if the box has items to place.
                    if (box.Quantity > 0)
                        // Enable the place button and set its click action.
                        UIManager.Instance.ToggleActionUI(ActionType.Place, true, () =>
                        {
                            // Attempt to place the box's contents on the shelf.
                            bool placed = box.Place(detectedShelf);

                            // Open the shelving unit if placement was successful and it's not already open.
                            if (placed && !detectedShelf.ShelvingUnit.IsOpen)
                                detectedShelf.ShelvingUnit.Open(true, true);
                        });

                    // Check if the shelf has items to take.
                    if (detectedShelf.Quantity > 0)
                        // Enable the take button and set its click action.
                        UIManager.Instance.ToggleActionUI(ActionType.Take, true, () =>
                        {
                            // Attempt to take items from the shelf and put them in the box.
                            bool taken = box.Take(detectedShelf);

                            // Open the shelving unit if taking was successful and it's not already open.
                            if (taken && !detectedShelf.ShelvingUnit.IsOpen)
                                detectedShelf.ShelvingUnit.Open(true, true);
                        });
                    else
                        // Disable the take button if the shelf is empty.
                        UIManager.Instance.ToggleActionUI(ActionType.Take, false, null);

                    // Update the last detected shelf.
                    lastShelf = detectedShelf;
                }
            }
            else if (lastShelf != null)
            {
                // Disable the place and take buttons if no shelf is detected.
                UIManager.Instance.ToggleActionUI(ActionType.Place, false, null);
                UIManager.Instance.ToggleActionUI(ActionType.Take, false, null);

                // Reset the last detected shelf.
                lastShelf = null;
            }
        }

        private void DetectRack()
        {
            // Create a ray from the center of the viewport.
            Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, GameConfig.Instance.RackLayer))
            {
                Rack detectedRack = hit.transform.GetComponent<Rack>();

                // Check if the detected rack is different from the last detected rack.
                if (detectedRack != lastRack)
                {
                    // Get the currently held box.
                    Box box = lastInteractable as Box;

                    // Check if player held a box and it is not empty.
                    if (box != null && box.Quantity > 0)
                    {
                        // Enable the place button and set its click action.
                        UIManager.Instance.ToggleActionUI(ActionType.Place, true, () =>
                        {
                            // Attempt to store the box on the rack.
                            box.Store(detectedRack, true);
                        });
                    }
                    // Check if the player is NOT holding a box OR is holding an empty box.
                    else if (box == null && detectedRack.BoxQuantity > 0)
                    {
                        // Enable the take button and set its click action.
                        UIManager.Instance.ToggleActionUI(ActionType.Take, true, () =>
                        {
                            // Attempt to retrieve boxes from the rack.
                            detectedRack.RetrieveBox(this);
                            UIManager.Instance.ToggleActionUI(ActionType.Take, false, null);
                        });
                    }
                    else
                    {
                        // Disable the take button if the shelf is empty or the player is holding an empty box.
                        UIManager.Instance.ToggleActionUI(ActionType.Take, false, null);
                    }

                    // Update the last detected shelf.
                    lastRack = detectedRack;
                }
            }
            else if (lastRack != null)
            {
                // Disable the place and take buttons if no rack is detected.
                if (StateManager.CurrentState != PlayerState.Moving)
                    UIManager.Instance.ToggleActionUI(ActionType.Place, false, null);

                UIManager.Instance.ToggleActionUI(ActionType.Take, false, null);

                // Reset the last detected rack.
                lastRack = null;
            }
        }

        private void Work()
        {
            // Check for mouse click and if the last interactable is a CheckoutCounter.
            if (Input.GetMouseButtonDown(0) && lastInteractable is CheckoutCounter counter)
            {
                // If the counter is in the placing state (customer is still placing items), don't scan items.
                if (counter.CurrentState == CheckoutCounter.State.Placing) return;

                // Create a ray from the mouse position.
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

                // Perform a raycast to detect a checkout item within the interact distance.
                if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, GameConfig.Instance.CheckoutItemLayer))
                {
                    if (hit.transform.TryGetComponent<CheckoutItem>(out CheckoutItem item))
                    {
                        item.Scan();
                    }
                }
            }
        }

        /// <summary>
        /// Calculates a position in front of the player, taking into account the camera's pitch.
        /// </summary>
        /// <returns>A Vector3 representing the calculated front position.</returns>
        public Vector3 GetFrontPosition()
        {
            // Get the camera's pitch angle.
            float pitch = mainCamera.transform.localEulerAngles.x;

            // Adjust the pitch angle if it's greater than 180 degrees.
            if (pitch > 180) pitch -= 360;

            // Normalize the pitch angle to a 0-1 range.
            float normalizedPitch = Mathf.InverseLerp(lookXLimit, 0f, pitch);

            // Define the minimum and maximum distances for the front position.
            float minDistance = 1.5f;
            float maxDistance = 3f;

            // Interpolate between the minimum and maximum distances based on the normalized pitch.
            float offset = Mathf.Lerp(minDistance, maxDistance, normalizedPitch);

            // Calculate the front position based on the player's transform and the calculated offset.
            Vector3 front = transform.TransformPoint(Vector3.forward * offset);

            // Return the front position, zeroing out the Y-component and flooring the X and Z components to the nearest tenth.
            return new Vector3(front.x, 0f, front.z).FloorToTenth();
        }

        /// <summary>
        /// Smoothly sets the FOV of player's Cinemachine virtual camera.
        /// </summary>
        /// <param name="targetFOV">Target field of view value.</param>
        /// <param name="duration">How long the tween should take.</param>
        public void SetFOVSmooth(float targetFOV, float duration = 0.5f)
        {
            DOTween.To(
                () => playerVirtualCam.m_Lens.FieldOfView,
                fov => playerVirtualCam.m_Lens.FieldOfView = fov,
                targetFOV,
                duration
            ).SetEase(Ease.InOutSine);
        }
    }
}
using UnityEngine;

namespace CryingSnow.CheckoutFrenzy
{
    public class GameConfig : ScriptableObject
    {
        private static GameConfig _instance;

        public static GameConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<GameConfig>("GameConfig");

                    if (_instance == null)
                    {
                        Debug.LogError("GameConfig not found in Resources!\nPlease create one using Tools > Checkout Frenzy > Game Config.");
                    }
                }
                return _instance;
            }
        }

        [Header("Store Settings")]
        [SerializeField, Tooltip("The default name for the store.")]
        private string defaultStoreName = "AWESOME MART";

        [SerializeField, Tooltip("The maximum number of characters allowed for the store name.")]
        private int storeNameMaxCharacters = 15;

        [SerializeField, Tooltip("The initial amount of money the player starts with.")]
        private int startingMoney = 2000;

        [SerializeField, Tooltip("The minimum time (in seconds) between customer spawns.")]
        private float minSpawnTime = 5f;

        [SerializeField, Tooltip("The maximum time (in seconds) between customer spawns.")]
        private float maxSpawnTime = 15f;

        [SerializeField, Tooltip("The base maximum number of customers that can be in the store at the same time.")]
        private int baseMaxCustomers = 5;

        [SerializeField, Tooltip("The time range during which the store is open for business.")]
        private TimeRange openTime;



        [Header("Level System")]
        [SerializeField, Tooltip("Base experience required for level 1.")]
        private int baseExperience = 5;

        [SerializeField, Tooltip("Exponential growth factor for experience requirements.")]
        private float growthFactor = 1.1f;



        [Header("Game Layers")]
        [SerializeField, Tooltip("Layer used for interactable objects (Furnitures, Counter, PC, etc.)")]
        private LayerMask interactableLayer;

        [SerializeField, Tooltip("Layer used for items currently being processed at the checkout counter.")]
        private LayerMask checkoutItemLayer;

        [SerializeField, Tooltip("Layer used for payment method GameObjects (either cash or card).")]
        private LayerMask paymentLayer;

        [SerializeField, Tooltip("Layer used to determine valid placement locations for furniture objects.")]
        private LayerMask groundLayer;

        [SerializeField, Tooltip("Layer used by Shelves in Shelving Units.")]
        private LayerMask shelfLayer;

        [SerializeField, Tooltip("Layer used by Racks in Storage Racks.")]
        private LayerMask rackLayer;

        [SerializeField, Tooltip("Layer used by objects that can be held by the player. Objects on this layer are rendered on top of everything else using a special camera to prevent clipping when held.")]
        private LayerMask heldObjectLayer;

        [SerializeField, Tooltip("Layer used by the player.")]
        private LayerMask playerLayer;



        [Header("Customer Dialogues")]
        [SerializeField, Tooltip("Dialogue lines used when the customer can't find a product.")]
        private Dialogue notFoundDialogue;

        [SerializeField, Tooltip("Dialogue lines used when the customer thinks a product is too expensive.")]
        private Dialogue overpricedDialogue;

        [SerializeField, Tooltip("Dialogue lines used when the customer caught stealing.")]
        private Dialogue caughtThiefDialogue;



        [Header("Cleaning Settings")]
        [SerializeField, Tooltip("How many Cleanables can be spawned in the game.")]
        private int maxCleanables = 10;



        [Header("Control Settings")]
        [SerializeField, Tooltip("Selected control mode for the game.")]
        private ControlMode controlMode;

        // Store Settings
        public string DefaultStoreName => defaultStoreName;
        public int StoreNameMaxCharacters => storeNameMaxCharacters;
        public int StartingMoney => startingMoney;
        public float GetRandomSpawnTime => Random.Range(minSpawnTime, maxSpawnTime);
        public int BaseMaxCustomers => baseMaxCustomers;
        public TimeRange OpenTime => openTime;

        // Level System
        public int BaseExperience => baseExperience;
        public float GrowthFactor => growthFactor;

        // Game Layers
        public LayerMask InteractableLayer => interactableLayer;
        public LayerMask CheckoutItemLayer => checkoutItemLayer;
        public LayerMask PaymentLayer => paymentLayer;
        public LayerMask GroundLayer => groundLayer;
        public LayerMask ShelfLayer => shelfLayer;
        public LayerMask RackLayer => rackLayer;
        public LayerMask HeldObjectLayer => heldObjectLayer;
        public LayerMask PlayerLayer => playerLayer;

        // Customer Dialogues
        public Dialogue NotFoundDialogue => notFoundDialogue;
        public Dialogue OverpricedDialogue => overpricedDialogue;
        public Dialogue CaughtThiefDialogue => caughtThiefDialogue;

        // Cleaning
        public int MaxCleanables => maxCleanables;

        // Control Mode
        public ControlMode ControlMode => controlMode;
    }

    public enum ControlMode { Mobile, PC }
    public enum ActionType { Throw, Open, Close, Place, Take, Price, Rotate, Return, Label, Pack }
}
using System.Collections.Generic;

namespace CryingSnow.CheckoutFrenzy
{
    [System.Serializable]
    public class GameData
    {
        public string StoreName { get; set; }
        public string NameColor { get; set; }
        public decimal PlayerMoney { get; set; }
        public int CurrentLevel { get; set; }
        public int CurrentExperience { get; set; }

        public List<FurnitureData> SavedFurnitures { get; set; }
        public List<BoxData> SavedBoxes { get; set; }
        public List<FurnitureBoxData> SavedFurnitureBoxes { get; set; }

        public List<CustomPrice> CustomPrices { get; set; }

        public decimal PendingOrdersValue { get; set; }
        public decimal UnpaidProductsValue { get; set; }

        public HashSet<int> LicensedProducts { get; set; }
        public HashSet<int> OwnedLicenses { get; set; }

        public int ExpansionLevel { get; set; }
        public bool IsWarehouseUnlocked { get; set; }

        public int TotalDays { get; set; }
        public int TotalMinutes { get; set; }

        public SummaryData CurrentSummary { get; set; }
        public MissionData CurrentMission { get; set; }

        public List<EmployeeData> HiredEmployees { get; set; }

        public System.DateTime LastSaved { get; set; }
        public System.TimeSpan TotalPlaytime { get; set; }

        public List<Bill> Bills { get; set; }
        public List<Loan> Loans { get; set; }

        public List<CleanableData> SavedCleanables { get; set; }

        public void Initialize()
        {
            StoreName = GameConfig.Instance.DefaultStoreName;
            NameColor = "#FFFFFF";

            PlayerMoney = GameConfig.Instance.StartingMoney;
            CurrentLevel = 1;

            SavedFurnitures = new List<FurnitureData>();
            SavedBoxes = new List<BoxData>();
            SavedFurnitureBoxes = new List<FurnitureBoxData>();
            CustomPrices = new List<CustomPrice>();
            LicensedProducts = new HashSet<int>();
            OwnedLicenses = new HashSet<int>();

            TotalDays = 1;

            var openTime = GameConfig.Instance.OpenTime;
            int totalMinutes = TimeRange.ToMinutes(openTime.StartHour, openTime.StartMinute);
            TotalMinutes = totalMinutes;

            CurrentSummary = new SummaryData(PlayerMoney);
            CurrentMission = new MissionData(1);

            HiredEmployees = new List<EmployeeData>();
            Bills = new List<Bill>();
            Loans = new List<Loan>();

            SavedCleanables = new();
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;

namespace CryingSnow.CheckoutFrenzy
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class Customer : MonoBehaviour
    {
        private const float WALK_SPEED = 1.5f;
        private const float RUN_SPEED = 2.5f;
        private const float CONTINUE_SHOPPING_CHANCE = 0.5f;
        private const float STEAL_CHANCE = 0.1f;

        public event System.Action OnLeave;

        [SerializeField] private HandAttachments handAttachments;
        [SerializeField] private OverheadUI overheadUI;

        public List<Product> Inventory => inventory;
        public bool IsCaught { get; private set; }

        private Dialogue notFoundDialogue => GameConfig.Instance.NotFoundDialogue;
        private Dialogue overpricedDialogue => GameConfig.Instance.OverpricedDialogue;
        private Dialogue caughtThiefDialogue => GameConfig.Instance.CaughtThiefDialogue;

        private Animator animator;
        private NavMeshAgent agent;
        private Thief thief;

        private List<Product> inventory = new List<Product>();

        private ShelvingUnit shelvingUnit;
        private CheckoutCounter checkoutCounter;
        private int queueNumber = int.MaxValue;
        private bool isPicking;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            agent = GetComponent<NavMeshAgent>();

            // Initialize NavMeshAgent parameters
            agent.speed = WALK_SPEED;
            agent.angularSpeed = 3600f;
            agent.acceleration = 100f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            agent.SetAreaCost(3, 50f);
        }

        private void Start()
        {
            StartCoroutine(CheckEnteringStore());
            StartCoroutine(Shopping());
        }

        private void Update()
        {
            CheckStoreDoors();
        }

        // Detecting store's doors and open them if they are closed.
        private void CheckStoreDoors()
        {
            Ray ray = new Ray(transform.position + Vector3.up, transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, 1f, GameConfig.Instance.InteractableLayer))
            {
                if (hit.transform.TryGetComponent<EntranceDoor>(out EntranceDoor door))
                {
                    door.OpenIfClosed();
                }
            }
        }

        // Check the first time customer entering store, and ring the bell.
        private IEnumerator CheckEnteringStore()
        {
            while (!StoreManager.Instance.IsWithinStore(transform.position))
            {
                yield return new WaitForSeconds(0.1f);
            }

            AudioManager.Instance.PlaySFX(AudioID.Bell);
        }

        // Start running when leaving store with stolen items.
        private IEnumerator CheckLeavingStoreAsThief()
        {
            while (StoreManager.Instance.IsWithinStore(transform.position))
            {
                yield return new WaitForSeconds(0.1f);
            }

            animator.SetFloat("Speed", 1f);
            agent.speed = RUN_SPEED;
        }

        private IEnumerator Shopping()
        {
            bool continueShopping = true;

            while (continueShopping)
            {
                yield return FindShelvingUnit();

                if (Random.value < STEAL_CHANCE)
                {
                    yield return StealProduct();

                    if (inventory.Count > 0)
                    {
                        StartCoroutine(Leave());
                        StartCoroutine(CheckLeavingStoreAsThief());
                        yield break;
                    }
                }

                yield return PickProduct();

                continueShopping = Random.value < CONTINUE_SHOPPING_CHANCE;
            }

            if (shelvingUnit != null && shelvingUnit.IsOpen)
            {
                // Close the shelving unit if it's open (e.g., Fridges, Freezers)
                shelvingUnit.Close(true, false);
            }

            if (inventory.Count > 0)
            {
                yield return Checkout();
                yield return Leave();
            }
            else
            {
                // Customer leaves without buying anything
                overheadUI.ShowDialog(notFoundDialogue.GetRandomLine());
                yield return Leave();
            }
        }

        private IEnumerator FindShelvingUnit()
        {
            // Get a new shelving unit from the store manager.
            var newShelvingUnit = StoreManager.Instance.GetShelvingUnit();

            // If there's a current shelving unit that's different from the new one and is open, close it.
            if (shelvingUnit != null && shelvingUnit != newShelvingUnit && shelvingUnit.IsOpen)
            {
                shelvingUnit.Close(true, false);
            }

            // Assign the new shelving unit.
            shelvingUnit = newShelvingUnit;

            // If no shelving unit is available, exit the coroutine.
            if (shelvingUnit == null) yield break;

            // Unregister the shelving unit from the store manager so other customers don't target it.
            StoreManager.Instance.UnregisterShelvingUnit(shelvingUnit);

            // Set the agent's destination to the front of the shelving unit.
            agent.SetDestination(shelvingUnit.Front);

            // Wait until the agent has arrived at the shelving unit.
            while (!HasArrived())
            {
                // If the shelving unit is moving, stop the agent and exit the coroutine.
                if (shelvingUnit.IsMoving)
                {
                    agent.SetDestination(transform.position);
                    shelvingUnit = null;
                    yield break;
                }

                yield return null;
            }

            yield return LookAt(shelvingUnit.transform);
        }

        private IEnumerator PickProduct()
        {
            // If no shelving unit is available, exit the coroutine.
            if (shelvingUnit == null) yield break;

            // Get a shelf from the shelving unit.
            var shelf = shelvingUnit.GetShelf();

            // If no shelf is available or the shelving unit is moving, re-register the shelving unit and exit.
            if (shelf == null || shelvingUnit.IsMoving)
            {
                StoreManager.Instance.RegisterShelvingUnit(shelvingUnit);
                yield break;
            }

            var product = shelf.Product;

            if (IsWillingToBuy(product))
            {
                // Add the product to the customer's inventory.
                inventory.Add(product);

                // Take the product model from the shelf.
                var productObj = shelf.TakeProductModel();

                // Open the shelving unit if it's not already open.
                if (!shelf.ShelvingUnit.IsOpen) shelf.ShelvingUnit.Open(true, false);

                // Determine the picking animation trigger based on the shelf height.
                float height = shelf.transform.position.y;
                string pickTrigger = "PickMedium";
                if (height < 0.5f) pickTrigger = "PickLow";
                else if (height > 1.5f) pickTrigger = "PickHigh";

                // Trigger the picking animation.
                animator.SetTrigger(pickTrigger);

                // Wait until the picking animation is complete.
                yield return new WaitUntil(() => isPicking);

                // Get the grip transform for the hand attachment.
                Transform grip = handAttachments.Grip;

                // Set the picked product's parent to the grip.
                productObj.transform.SetParent(grip);

                // Reset the isPicking flag.
                isPicking = false;

                // Animate the product moving to the hand.
                productObj.transform.DOLocalRotate(Vector3.zero, 0.25f);
                productObj.transform.DOLocalMove(Vector3.zero, 0.25f);

                // Wait until the animation is complete (Idle state).
                bool isIdle = false;
                while (!isIdle)
                {
                    isIdle = animator.GetCurrentAnimatorStateInfo(0).IsName("Idle");
                    yield return null;
                }

                // Destroy the temporary product object.
                Destroy(productObj);

                // Wait for a short delay.
                yield return new WaitForSeconds(0.5f);
            }
            else
            {
                // If not willing to buy, display the "overpriced" dialogue.
                string dialog = overpricedDialogue.GetRandomLine();
                dialog = dialog.Replace("{product}", product.Name);
                overheadUI.ShowDialog(dialog);
            }

            // Re-register the shelving unit with the store manager.
            StoreManager.Instance.RegisterShelvingUnit(shelvingUnit);
        }

        private bool IsWillingToBuy(Product product)
        {
            // Calculate a price tolerance factor based on random value.
            // Higher values mean more tolerance.
            float priceToleranceFactor = 1f + Mathf.Pow(Random.value, 2f);

            // Calculate the maximum acceptable price based on the product's market price and tolerance.
            decimal maxAcceptablePrice = product.MarketPrice * (decimal)priceToleranceFactor;

            // Get the custom price for the product.
            decimal customPrice = DataManager.Instance.GetCustomProductPrice(product);

            // Return true if the custom price is within the acceptable price range, otherwise false.
            return customPrice <= maxAcceptablePrice;
        }

        private IEnumerator StealProduct()
        {
            if (shelvingUnit == null) yield break;

            var shelf = shelvingUnit.GetShelf();
            if (shelf == null || shelvingUnit.IsMoving)
            {
                StoreManager.Instance.RegisterShelvingUnit(shelvingUnit);
                yield break;
            }

            var product = shelf.Product;

            animator.SetTrigger("Suspicious");
            yield return new WaitForSeconds(3.5f);

            if (product != null)
            {
                inventory.Add(product);
                StoreManager.Instance.RegisterThief(this);

                var productObj = shelf.TakeProductModel();

                if (!shelf.ShelvingUnit.IsOpen) shelf.ShelvingUnit.Open(true, false);

                // Determine the picking animation trigger based on the shelf height.
                float height = shelf.transform.position.y;
                string pickTrigger = "PickMedium";
                if (height < 0.5f) pickTrigger = "PickLow";
                else if (height > 1.5f) pickTrigger = "PickHigh";

                animator.SetTrigger(pickTrigger);

                // Wait until the picking animation is complete.
                yield return new WaitUntil(() => isPicking);

                Transform grip = handAttachments.Grip;
                productObj.transform.SetParent(grip);

                isPicking = false;

                // Animate the product moving to the hand.
                productObj.transform.DOLocalRotate(Vector3.zero, 0.25f);
                productObj.transform.DOLocalMove(Vector3.zero, 0.25f);

                // Wait until the animation is complete (Idle state).
                bool isIdle = false;
                while (!isIdle)
                {
                    isIdle = animator.GetCurrentAnimatorStateInfo(0).IsName("Idle");
                    yield return null;
                }

                // Destroy the temporary product object.
                Destroy(productObj);

                yield return new WaitForSeconds(0.5f);
            }
            else
            {
                yield break;
            }

            CreateThiefInstance();
            overheadUI.ShowThiefIcon();

            // Re-register the shelving unit with the store manager.
            StoreManager.Instance.RegisterShelvingUnit(shelvingUnit);
        }

        private void CreateThiefInstance()
        {
            var go = new GameObject("Thief");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            thief = go.AddComponent<Thief>();
            thief.Initialize(this);
        }

        public void CatchCustomer()
        {
            if (IsCaught) return;

            IsCaught = true;
            StoreManager.Instance.UnregisterThief(this);
            StopAllCoroutines();
            StartCoroutine(CaughtSequence());
        }

        private IEnumerator CaughtSequence()
        {
            StoreManager.Instance.UnregisterThief(this);

            animator.SetBool("IsMoving", false);
            animator.SetFloat("Speed", 0f);

            agent.speed = WALK_SPEED;
            agent.SetDestination(transform.position);

            overheadUI.HideThiefIcon();
            overheadUI.ShowDialog(caughtThiefDialogue.GetRandomLine());

            animator.SetBool("IsDucking", true);
            yield return new WaitForSeconds(5f);
            animator.SetBool("IsDucking", false);

            if (thief != null)
                Destroy(thief.gameObject);

            DataManager.Instance.PlayerMoney += inventory.Sum(p => p.Price);
            inventory.Clear();
            AudioManager.Instance.PlaySFX(AudioID.Kaching);

            yield return Leave();
        }

        private IEnumerator UpdateQueue()
        {
            // While the customer's queue number is greater than 0 (meaning they are still in the queue).
            while (queueNumber > 0)
            {
                int newQueueNumber = checkoutCounter.GetQueueNumber(this);

                // Check if the customer's queue number has improved (become lower).
                if (newQueueNumber < queueNumber)
                {
                    // Update the customer's queue number.
                    queueNumber = newQueueNumber;

                    Vector3 queuePosition = checkoutCounter.GetQueuePosition(this, out Vector3 lookDirection);

                    // Move the customer to their new queue position.
                    yield return MoveTo(queuePosition);

                    // Make the customer look in the correct direction at their new position.
                    yield return LookAt(lookDirection);
                }
                else
                {
                    // If the queue number hasn't improved, wait briefly before checking again.
                    yield return new WaitForSeconds(0.1f);
                }
            }
            // When the queueNumber is 0, this coroutine will stop.
        }

        private IEnumerator Checkout()
        {
            checkoutCounter = StoreManager.Instance.GetShortestQueueCounter();
            checkoutCounter.LiningCustomers.Add(this);
            yield return UpdateQueue();
            yield return checkoutCounter.PlaceProducts(this);
            yield return new WaitUntil(() => checkoutCounter.CurrentState == CheckoutCounter.State.Standby);
        }

        private IEnumerator Leave()
        {
            if (checkoutCounter != null)
            {
                checkoutCounter.LiningCustomers.Remove(this);
            }

            if (thief != null)
            {
                StoreManager.Instance.UnregisterThief(this);
            }

            OnLeave?.Invoke();

            var exitPoint = StoreManager.Instance.GetExitPoint();
            yield return MoveTo(exitPoint);

            yield return new WaitForEndOfFrame();
            Destroy(gameObject);
        }

        public IEnumerator HandsPayment(bool isUsingCash, Cashier cashier)
        {
            bool isPaying = true;

            animator.SetBool("IsPaying", isPaying);

            handAttachments.ActivatePaymentObject(isUsingCash);

            Camera mainCamera = Camera.main;

            // Continue the payment process until isPaying is false.
            while (isPaying)
            {
                // If a cashier is available, simulate payment with the cashier (auto-scan).
                if (cashier != null)
                {
                    yield return new WaitForSeconds(0.3f);
                    cashier.TakePayment();
                    yield return new WaitForSeconds(0.7f);
                    isPaying = false;
                }
                // Otherwise, allow the player to manually process the payment (e.g., started by clicking on a payment object).
                else if (Input.GetMouseButtonDown(0))
                {
                    Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

                    // Check if the raycast hits a payment object within the specified layer and range.
                    if (Physics.Raycast(ray, 10f, GameConfig.Instance.PaymentLayer))
                    {
                        isPaying = false;
                    }
                }

                yield return null;
            }

            animator.SetBool("IsPaying", isPaying);

            handAttachments.DeactivatePaymentObjects();
        }

        private IEnumerator MoveTo(Vector3 position)
        {
            agent.SetDestination(position);

            yield return new WaitUntil(() => HasArrived());

            // Wait for the end of the frame.
            // This can be useful for ensuring animations or other visual updates have taken place.
            yield return new WaitForEndOfFrame();
        }

        public void AskToLeave()
        {
            // If the customer has items in their inventory, they shouldn't leave yet.
            if (inventory.Count > 0) return;

            // Stop all coroutines related to the customer's current activity.
            StopAllCoroutines();

            // If the customer was interacting with a shelving unit, re-register it with the store manager.
            if (shelvingUnit != null) StoreManager.Instance.RegisterShelvingUnit(shelvingUnit);

            // Start the "Leave" coroutine to handle the customer leaving the store.
            StartCoroutine(Leave());
        }

        private IEnumerator LookAt(Transform target)
        {
            var lookDirection = (target.position - transform.position).Flatten();
            var lookRotation = Quaternion.LookRotation(lookDirection);
            yield return transform.DORotateQuaternion(lookRotation, 0.5f).WaitForCompletion();
        }

        private IEnumerator LookAt(Vector3 lookDirection)
        {
            var lookRotation = Quaternion.LookRotation(lookDirection.Flatten());
            yield return transform.DORotateQuaternion(lookRotation, 0.5f).WaitForCompletion();
        }

        private bool HasArrived()
        {
            if (!agent.pathPending)
            {
                if (agent.remainingDistance <= agent.stoppingDistance)
                {
                    if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                    {
                        animator.SetBool("IsMoving", false);
                        return true;
                    }
                }
            }

            animator.SetBool("IsMoving", true);
            return false;
        }

        public void OnPick(AnimationEvent _)
        {
            isPicking = true;
        }
    }
}
using System.Collections.Generic;
using UnityEngine;

namespace CryingSnow.CheckoutFrenzy
{
    [CreateAssetMenu(fileName = "New Dialogue")]
    public class Dialogue : ScriptableObject
    {
        [System.Serializable]
        private struct Line
        {
            [TextArea(3, 5)]
            public string Text;
        }

        [SerializeField] private List<Line> lines;

        /// <summary>
        /// Returns a random dialogue line from the list.
        /// </summary>
        /// <returns>A random dialogue line as a string.</returns>
        public string GetRandomLine()
        {
            if (lines == null || lines.Count == 0)
            {
                Debug.LogWarning("Dialogue list is empty.");
                return "";
            }

            int index = Random.Range(0, lines.Count);
            return lines[index].Text;
        }
    }
}
using UnityEngine;

namespace CryingSnow.CheckoutFrenzy
{
    public class HandAttachments : MonoBehaviour
    {
        [SerializeField, Tooltip("Transform used to position and orient products held in the customer's hand.")]
        private Transform grip;

        [SerializeField, Tooltip("GameObject representing the cash payment option.")]
        private GameObject cash;

        [SerializeField, Tooltip("GameObject representing the card payment option.")]
        private GameObject card;

        public Transform Grip => grip;
        public GameObject Cash => cash;
        public GameObject Card => card;

        private void Awake()
        {
            cash.layer = card.layer = GameConfig.Instance.PaymentLayer.ToSingleLayer();

            DeactivatePaymentObjects();
        }

        /// <summary>
        /// Activates the appropriate payment method GameObject (either cash or card) based on the isUsingCash boolean.
        /// </summary>
        /// <param name="isUsingCash">True to activate the cash payment object, false to activate the card payment object.</param>
        public void ActivatePaymentObject(bool isUsingCash)
        {
            if (isUsingCash) cash.SetActive(true);
            else card.SetActive(true);
        }

        /// <summary>
        /// Deactivates both the cash and card payment method GameObjects.
        /// </summary>
        public void DeactivatePaymentObjects()
        {
            cash.SetActive(false);
            card.SetActive(false);
        }
    }
}
