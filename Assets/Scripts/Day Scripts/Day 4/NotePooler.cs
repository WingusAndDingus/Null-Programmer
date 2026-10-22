using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NotePooler : MonoBehaviour
{
    public static NotePooler Instance;

    [SerializeField] private GameObject notePrefab;
    [SerializeField] private int initialPoolSize = 30;
    [SerializeField] private Transform poolContainer;

    private Queue<GameObject> pool = new Queue<GameObject>();

    private void Awake()
    {
        Instance = this;
        InitializePool();
    }

    private void InitializePool()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            GameObject note = Instantiate(notePrefab, poolContainer);
            note.SetActive(false);
            pool.Enqueue(note);
        }
    }

    public GameObject GetNote(Transform parent)
    {
        GameObject note = pool.Count > 0 ? pool.Dequeue() : Instantiate(notePrefab, poolContainer);
        note.transform.SetParent(parent, false);
        note.SetActive(true);
        return note;
    }

    public void ReturnNote(GameObject note)
    {
        note.SetActive(false);
        note.transform.SetParent(poolContainer, false);
        pool.Enqueue(note);
    }
}