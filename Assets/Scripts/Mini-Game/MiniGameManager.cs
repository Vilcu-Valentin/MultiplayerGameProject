using UnityEngine;
using System.Collections.Generic;

public class MiniGameManager : MonoBehaviour
{
    [SerializeField] private WorkstationUI workstation;
    // In production, this comes from the server. 
    // For testing, we generate it here.
    public int CurrentSeed;
    public bool UseRandomSeedOnStart = true;

    private TaskGenerator _taskGen;

    // Track tasks to simulate gameplay
    private int _tasksCompleted = 0;

    void Start()
    {
        if (UseRandomSeedOnStart)
        {
            // Generate a random seed for local testing
            CurrentSeed = Random.Range(int.MinValue, int.MaxValue);
            Debug.Log($"<color=cyan>Generated Local Test Seed: {CurrentSeed}</color>");
        }

        StartGame(CurrentSeed);
    }

    public void StartGame(int seed)
    {
        _taskGen = new TaskGenerator(seed);
        _tasksCompleted = 0;

        // Generate the first task immediately
        LoadNextTask();
    }

    public void LoadNextTask()
    {
        TaskData newTask = _taskGen.GetNextTask();

        Debug.Log($"Task #{_tasksCompleted + 1} Generated: {newTask.ToString()}");

        workstation.UpdateNixie(newTask);
    }

    public void OnTaskSolved()
    {
        _tasksCompleted++;
        LoadNextTask();
    }

    // Call this when the 2-minute timer ends
    public void FinishGame()
    {
        Debug.Log($"Game Over! Sending Result to Server. Seed: {CurrentSeed}, Tasks: {_tasksCompleted}");
        // Send (CurrentSeed, _tasksCompleted) to ASP.NET API
    }
}