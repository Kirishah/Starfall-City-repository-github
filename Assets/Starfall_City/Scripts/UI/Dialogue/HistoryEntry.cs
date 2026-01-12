namespace DialogueSystem
{
    [System.Serializable]
    public struct HistoryEntry
    {
        public string speaker;
        public string text;
        public bool isNarrative;
        public bool isPlayer;

        public HistoryEntry(string speaker, string text, bool isNarrative = false, bool isPlayer = false)
        {
            this.speaker = speaker;
            this.text = text;
            this.isNarrative = isNarrative;
            this.isPlayer = isPlayer;
        }
    }
}
