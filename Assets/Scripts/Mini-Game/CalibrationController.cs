using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CalibrationController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private VennResolver vennData; // The static truth
    [SerializeField] private WaveMatchingController waveGame;

    [Header("Switchboard Inputs")]
    [SerializeField] private bool[] letterSwitches = new bool[6]; // A-F
    [SerializeField] private bool[] numberSwitches = new bool[6]; // 1-6

    [Header("Socket References")]
    [SerializeField] private ItemSocket[] sockets;
    [SerializeField] private Image[] recipeIcons; // UI above sockets

    [Header("Recipe Icons")]
    [SerializeField] private Sprite iconTube;
    [SerializeField] private Sprite iconCapacitor;
    [SerializeField] private Sprite iconInductor;
    [SerializeField] private Sprite iconUnknown;

    [Header("Settings")]
    [SerializeField] private float stabilityBonus = 0.3f; // Jitter reduced to 30%

    // --- Internal State ---

    // The "Correct" answer for the current task
    private int _targetLetter;
    private int _targetNumber;

    // The random recipes generated for THIS round. 
    // Key: string "0_3" (Letter A, Number 4). Value: List of Items.
    private Dictionary<string, Item.ItemType[]> _roundRecipes = new Dictionary<string, Item.ItemType[]>();

    // The recipe the player is currently trying to build (based on their switchboard input)
    private Item.ItemType[] _activeRecipe;
    private bool _isCurrentRecipeCorrect; // Did they input the RIGHT code to get this recipe?
    private bool _isCalibrationActive;

    private void Start()
    {
        // Listen to socket changes
        foreach (var socket in sockets)
        {
            socket.OnItemPlaced.AddListener((x) => CheckSockets());
            socket.OnItemRemoved.AddListener(CheckSockets);
            socket.OnItemConsumed.AddListener(CheckSockets);
        }

        HideIcons();
    }

    // --- PHASE 1: New Task Started ---

    public void OnNewTaskStarted(TaskData task)
    {
        _isCalibrationActive = true;
        _activeRecipe = null;
        _isCurrentRecipeCorrect = false;

        // 1. Reset everything
        waveGame.SetStability(1.0f); // Hard mode
        HideIcons();

        // 2. Determine the "Correct" Answer from static data
        var solution = vennData.GetCoordinate(task);
        _targetLetter = solution.letterIndex;
        _targetNumber = solution.numberIndex;

        // 3. Generate Random Recipes for EVERY possible combination (A1 to F6)
        // This ensures that even if they input the wrong code, they get a valid-looking recipe.
        _roundRecipes.Clear();
        for (int l = 0; l < 6; l++)
        {
            for (int n = 0; n < 6; n++)
            {
                string key = $"{l}_{n}";
                _roundRecipes[key] = GenerateRandomRecipe();
            }
        }

        Debug.Log($"Calibration Target: {ToLetter(_targetLetter)}{_targetNumber + 1}");
    }

    private Item.ItemType[] GenerateRandomRecipe()
    {
        Item.ItemType[] r = new Item.ItemType[3];
        for (int i = 0; i < 3; i++)
        {
            int rnd = Random.Range(0, 3); // 0, 1, 2
            r[i] = (Item.ItemType)rnd;
        }
        return r;
    }

    // --- PHASE 2: Switchboard Input ---

    public void SetLetter(int index)
    {
        // Optional: Ensure only one switch is active at a time?
        // For now, we just set true/false as per your snippet
        for (int i = 0; i < letterSwitches.Length; i++) letterSwitches[i] = false;
        letterSwitches[index] = true;
    }

    public void SetNumber(int index)
    {
        for (int i = 0; i < numberSwitches.Length; i++) numberSwitches[i] = false;
        numberSwitches[index] = true;
    }

    public void SubmitSwitchBoard()
    {
        if (!_isCalibrationActive) return;

        int l_index = GetActiveSwitch(letterSwitches);
        int n_index = GetActiveSwitch(numberSwitches);

        if (l_index == -1 || n_index == -1)
        {
            Debug.Log("SwitchBoard Incomplete");
            return;
        }

        // 1. Lookup the recipe for WHATEVER they typed (Right or Wrong)
        string key = $"{l_index}_{n_index}";
        if (_roundRecipes.TryGetValue(key, out Item.ItemType[] recipe))
        {
            _activeRecipe = recipe;

            // 2. Display requirements
            ShowIcons(recipe);

            // 3. Check if they were actually right
            if (l_index == _targetLetter && n_index == _targetNumber)
            {
                _isCurrentRecipeCorrect = true;
                Debug.Log("Correct Code Input. Waiting for components...");
            }
            else
            {
                _isCurrentRecipeCorrect = false;
                Debug.Log($"Wrong Code! (Player input {ToLetter(l_index)}{n_index + 1}, True is {ToLetter(_targetLetter)}{_targetNumber + 1}). Showing decoy recipe.");
            }

            // check immediately in case items are already there
            CheckSockets();
        }
    }

    // --- PHASE 3: Socket Checking ---

    private void CheckSockets()
    {
        if (_activeRecipe == null) return;

        int matchCount = 0;
        for (int i = 0; i < 3; i++)
        {
            if (sockets[i].HeldItem != null && sockets[i].HeldItem.itemType == _activeRecipe[i])
            {
                matchCount++;
            }
        }

        if (matchCount == 3)
        {
            // They built the recipe!
            // But was it the RIGHT recipe?
            if (_isCurrentRecipeCorrect)
            {
                Debug.Log("<color=green>Calibration Successful! Wave Stabilized.</color>");
                waveGame.SetStability(stabilityBonus);
                _isCalibrationActive = false; // Disable further changes until next task
                HideIcons();
            }
            else
            {
                // They built a decoy recipe. Nothing happens.
                // This is the "Valid looking but actually wrong" result.
                Debug.Log("<color=red>Decoy Recipe Built. No effect on wave.</color>");
            }
        }
    }

    // --- Helpers ---

    private int GetActiveSwitch(bool[] switches)
    {
        for (int i = 0; i < switches.Length; i++)
            if (switches[i]) return i;
        return -1;
    }

    private void ResetSwitches()
    {
        for (int i = 0; i < letterSwitches.Length; i++) letterSwitches[i] = false;
        for (int i = 0; i < numberSwitches.Length; i++) numberSwitches[i] = false;
    }

    private string ToLetter(int i) => ((char)('A' + i)).ToString();

    private void ShowIcons(Item.ItemType[] recipe)
    {
        for (int i = 0; i < 3; i++)
        {
            recipeIcons[i].gameObject.SetActive(true);
            recipeIcons[i].sprite = GetSprite(recipe[i]);
        }
    }

    private void HideIcons()
    {
        foreach (var img in recipeIcons) img.gameObject.SetActive(false);
    }

    private Sprite GetSprite(Item.ItemType type)
    {
        switch (type)
        {
            case Item.ItemType.VacuumTube: return iconTube;
            case Item.ItemType.Capacitor: return iconCapacitor;
            case Item.ItemType.Inductor: return iconInductor;
            default: return iconUnknown;
        }
    }
}