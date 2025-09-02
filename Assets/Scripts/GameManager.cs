using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Data")]
    public VocabDatabase database;

    [Header("Scene Refs")]
    public Canvas mainCanvas;
    public RectTransform playArea;          // contenedor donde aparecen los objetos
    public RectTransform dropZone;          // LeftDropBox
    public DragItem dragItemPrefab;         // prefab UI con Image+CanvasGroup+DragItem
    public UIManager ui;

    [Header("Config")]
    [Tooltip("Cantidad total de rondas a acertar para ganar")]
    public int roundsToWin = 5;

    [Tooltip("Cantidad de ítems visibles por ronda (1 correcto + N-1 distractores)")]
    public int itemsPerRound = 8;

    [Tooltip("Fallos máximos permitidos (si superas, Game Over)")]
    public int maxMistakes = 3;

    [Header("Puntaje por rapidez (segundos umbral)")]
    public float fastThreshold = 2f;
    public float mediumThreshold = 5f;
    public int fastScore = 100;
    public int mediumScore = 70;
    public int slowScore = 50;

    [Header("Opcional")]
    public float nextRoundDelay = 0.6f;

    // Estado
    private int currentRound = 0;
    private int score = 0;
    private int mistakes = 0;
    private float roundStartTime = 0f;

    private VocabItem currentTarget;
    private readonly HashSet<VocabItem> usedTargets = new HashSet<VocabItem>();
    private readonly List<DragItem> spawned = new List<DragItem>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (database == null || database.items.Count < itemsPerRound)
        {
            Debug.LogError("Configura VocabDatabase con suficientes items.");
            return;
        }

        score = 0;
        mistakes = 0;
        currentRound = 0;

        ui.SetScore(score);
        ui.SetMistakes(mistakes, maxMistakes);
        StartNextRound();
    }

    private void Update()
    {
        if (currentTarget != null)
        {
            float t = Time.time - roundStartTime;
            ui.SetTimer(t);
        }
    }

    private void ClearPlayArea()
    {
        foreach (var it in spawned)
        {
            if (it != null) Destroy(it.gameObject);
        }
        spawned.Clear();
    }

    private void StartNextRound()
    {
        // ¿Win?
        if (currentRound >= roundsToWin)
        {
            ui.ShowWin(score);
            return;
        }

        ClearPlayArea();
        currentRound++;
        ui.SetRound(currentRound, roundsToWin);

        // Elige objetivo no repetido
        currentTarget = GetRandomTargetAvoidingUsed();
        usedTargets.Add(currentTarget);
        ui.SetWord(currentTarget.englishWord);

        // Construye el set de ítems: 1 correcto + (itemsPerRound-1) distractores
        var roundItems = BuildRoundItems(currentTarget, itemsPerRound);
        Shuffle(roundItems);

        // Instanciar como UI bajo PlayArea
        foreach (var vocab in roundItems)
        {
            var item = Instantiate(dragItemPrefab, playArea);
            item.Setup(vocab, vocab == currentTarget, mainCanvas);

            // Si usas GridLayoutGroup en PlayArea, no hace falta tocar posiciones.
            // Si NO usas Grid, podrías distribuir aleatoriamente:
            // var rt = (RectTransform)item.transform;
            // rt.anchoredPosition = GetRandomPointInside(playArea);

            spawned.Add(item);
        }

        roundStartTime = Time.time;
    }

    public void ProcessDrop(DragItem item, DropZone zone)
    {
        if (item == null) return;

        if (item.isTarget)
        {
            // Acierto
            item.SnapTo(dropZone);
            AwardScoreByTime();
            DisableOthers(item);
            StartCoroutine(GoNextRoundAfterDelay());
        }
        else
        {
            // Error
            mistakes++;
            ui.SetMistakes(mistakes, maxMistakes);
            item.ReturnToOrigin();

            if (mistakes > maxMistakes)
            {
                // Superó el máximo permitido → Game Over
                GameOver();
            }
        }
    }

    private void AwardScoreByTime()
    {
        float elapsed = Time.time - roundStartTime;
        int gained = elapsed < fastThreshold ? fastScore :
                     (elapsed < mediumThreshold ? mediumScore : slowScore);
        score += gained;
        ui.SetScore(score);
    }

    private IEnumerator GoNextRoundAfterDelay()
    {
        yield return new WaitForSeconds(nextRoundDelay);

        // ¿Ya ganaste al completar 5?
        if (currentRound >= roundsToWin)
        {
            ui.ShowWin(score);
            yield break;
        }

        StartNextRound();
    }

    private void GameOver()
    {
        // Limpia y muestra panel
        ClearPlayArea();
        ui.ShowGameOver(score);
        currentTarget = null;
    }

    // -------- Helpers --------

    private VocabItem GetRandomTargetAvoidingUsed()
    {
        // Elige un item aleatorio que no haya sido usado como target en esta partida
        var pool = new List<VocabItem>();
        foreach (var it in database.items)
        {
            if (!usedTargets.Contains(it))
                pool.Add(it);
        }

        // Si te quedas sin pool (por ejemplo, menos items que rondas), resetea usados
        if (pool.Count == 0)
        {
            usedTargets.Clear();
            pool.AddRange(database.items);
        }

        int idx = Random.Range(0, pool.Count);
        return pool[idx];
    }

    private List<VocabItem> BuildRoundItems(VocabItem target, int total)
    {
        var result = new List<VocabItem> { target };

        // Haz una copia de la base excluyendo el objetivo
        var distractorPool = new List<VocabItem>(database.items);
        distractorPool.Remove(target);

        // Baraja y toma los necesarios
        Shuffle(distractorPool);

        int need = Mathf.Max(0, total - 1);
        for (int i = 0; i < need && i < distractorPool.Count; i++)
            result.Add(distractorPool[i]);

        return result;
    }

    private void DisableOthers(DragItem except)
    {
        foreach (var it in spawned)
        {
            if (it != null && it != except)
            {
                var cg = it.GetComponent<CanvasGroup>();
                if (cg != null) cg.blocksRaycasts = false;
            }
        }
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int j = Random.Range(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // Si deseas posiciones aleatorias (cuando NO uses GridLayoutGroup)
    private Vector2 GetRandomPointInside(RectTransform area)
    {
        var size = area.rect.size;
        float x = Random.Range(-size.x * 0.45f, size.x * 0.45f);
        float y = Random.Range(-size.y * 0.45f, size.y * 0.45f);
        return new Vector2(x, y);
    }
}
