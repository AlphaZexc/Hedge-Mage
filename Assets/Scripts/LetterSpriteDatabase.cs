using System.Collections.Generic;
using UnityEngine;

public class LetterSpriteDatabase : MonoBehaviour
{
    public static LetterSpriteDatabase Instance;

    [System.Serializable]
    public struct LetterSpritePair
    {
        public char letter;
        public Sprite uncollectedSprite;
        public Sprite collectedSprite;
        public Sprite worldSprite;
    }

    public List<LetterSpritePair> letterSprites;
    private Dictionary<char, LetterSpritePair> letterSpriteDict;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            BuildDictionary();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void BuildDictionary()
    {
        letterSpriteDict = new Dictionary<char, LetterSpritePair>();
        foreach (var pair in letterSprites)
        {
            char upper = char.ToUpper(pair.letter);
            if (!letterSpriteDict.ContainsKey(upper))
            {
                letterSpriteDict.Add(upper, pair);
            }
        }
    }
    public Sprite GetWorldSprite(char c)
    {
        c = char.ToUpper(c);
        return letterSpriteDict.TryGetValue(c, out var pair) ? pair.worldSprite : null;
    }

    public Sprite GetCollectedSprite(char c)
    {
        c = char.ToUpper(c);
        return letterSpriteDict.TryGetValue(c, out var pair) ? pair.collectedSprite : null;
    }

    public Sprite GetUncollectedSprite(char c)
    {
        c = char.ToUpper(c);
        return letterSpriteDict.TryGetValue(c, out var pair) ? pair.uncollectedSprite : null;
    }
}