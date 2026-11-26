public readonly struct DamageResult
{
    public float Amount { get; }
    public bool IsCritical { get; }

    public DamageResult(float amount, bool isCritical)
    {
        Amount = amount;
        IsCritical = isCritical;
    }
}
