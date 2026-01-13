using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class StackDispenser : DispenserBase
{
    [Header("Stack Settings")]
    [SerializeField] private int capacity = 5;
    [SerializeField] private List<Item> itemStack = new List<Item>();
    [SerializeField] private TMP_Text amountText;

    [Header("Hover Animation")]
    [SerializeField] private float pushBackDistance = 0.2f;
    [SerializeField] private float animationSpeed = 10f; 


    private Coroutine _hoverRoutine;
    private bool _isHoveringWithItem;

    private void Start()
    {
        targetSocket.manualInteraction = true;
        targetSocket.OnManualInteract.AddListener(OnSocketClicked);
        targetSocket.OnHoverStart.AddListener(OnHoverEnter);
        targetSocket.OnHoverEnd.AddListener(OnHoverExit);

        UpdateText(); 
    }

    private void UpdateText()
    {
        if (amountText != null)
            amountText.text = $"{itemStack.Count}/{capacity}";
    }

    private void OnSocketClicked()
    {
        PlayerHand hand = PlayerHand.Instance;
        bool socketHasItem = targetSocket.HeldItem != null;
        bool handHasItem = hand.HasItem();

        // TAKE from Stack
        if (socketHasItem && !handHasItem)
        {
            Item itemTaken = targetSocket.RemoveItem();
            hand.EquipItem(itemTaken);

            if (itemStack.Count > 0)
            {
                Item nextItem = itemStack[itemStack.Count - 1];
                itemStack.RemoveAt(itemStack.Count - 1);

                UpdateText();

                nextItem.gameObject.SetActive(true);
                StartCoroutine(AnimateToSocket(nextItem));
            }
        }
        //  PLACE/SWAP
        else if (handHasItem)
        {
            if (!socketHasItem)
            {
                targetSocket.PlaceItem(hand.RemoveItem());
                return;
            }

            if (itemStack.Count < capacity)
            {
                // PUSH INTO STACK
                Item itemToStack = targetSocket.HeldItem;

                if (_hoverRoutine != null) StopCoroutine(_hoverRoutine);

                StartCoroutine(AnimateFromSocket(itemToStack, () =>
                {
                    itemStack.Add(itemToStack);
                    itemToStack.gameObject.SetActive(false);

                    UpdateText();
                }));

                targetSocket.PlaceItem(hand.RemoveItem());
            }
            else
            {
                // SWAP (Stack Full)
                Item returnItem = targetSocket.RemoveItem();
                returnItem.transform.localPosition = Vector3.zero;

                Item handItem = hand.RemoveItem();
                hand.EquipItem(returnItem);
                targetSocket.PlaceItem(handItem);

                UpdateText();
            }
        }
    }

    // --- Hover Logic ---

    private void OnHoverEnter()
    {
        // Only animate if WE have an item AND the SOCKET has an item (Push preview)
        if (PlayerHand.Instance.HasItem() && targetSocket.HeldItem != null && itemStack.Count != capacity)
        {
            _isHoveringWithItem = true;
            if (_hoverRoutine != null) StopCoroutine(_hoverRoutine);
            _hoverRoutine = StartCoroutine(AnimatePushOffset(true));
        }
    }

    private void OnHoverExit()
    {
        // If we leave, or if we stop holding an item, reset
        if (_isHoveringWithItem)
        {
            _isHoveringWithItem = false;
            if (_hoverRoutine != null) StopCoroutine(_hoverRoutine);
            _hoverRoutine = StartCoroutine(AnimatePushOffset(false));
        }
    }

    private IEnumerator AnimatePushOffset(bool pushBack)
    {
        // Get the item. If null, we can't animate.
        Item item = targetSocket.HeldItem;
        if (item == null) yield break;

        // Calculate the "Into Machine" vector in Local Space of the PIVOT
        // This ensures that no matter how the dispenser is rotated, "Back" means "Towards internal point"
        Vector3 worldDirection = (internalSpawnPoint.position - targetSocket.Pivot.position).normalized;

        // Transform that world vector into the Item/Pivot's local space
        Vector3 localDirection = targetSocket.Pivot.InverseTransformDirection(worldDirection);

        // Determine Target
        // If pushing back, move along that vector. If reverting, go to Zero (Standard attached position).
        Vector3 targetLocalPos = pushBack ? localDirection * pushBackDistance : Vector3.zero;

        while (Vector3.Distance(item.transform.localPosition, targetLocalPos) > 0.001f)
        {
            // Check if item was removed mid-animation to prevent errors
            if (item == null || item.transform.parent != targetSocket.Pivot) yield break;

            item.transform.localPosition = Vector3.Lerp(item.transform.localPosition, targetLocalPos, Time.deltaTime * animationSpeed);
            yield return null;
        }

        if (item != null) item.transform.localPosition = targetLocalPos;
    }
}