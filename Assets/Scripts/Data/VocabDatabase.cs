

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Vocab/VocabDatabase")]
public class VocabDatabase : ScriptableObject
{
    public List<VocabItem> items = new List<VocabItem>();
}
