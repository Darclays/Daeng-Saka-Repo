namespace ProjectVN.Presentation.Actors
{
    public enum VNCharacterVisualFallbackLevel
    {
        None = 0,
        ExactVariant = 1,
        RequestedExpressionDefault = 2,
        CharacterDefaultExpression = 3,
        LegacyDefaultSprite = 4,
        Unresolved = 5
    }
}
