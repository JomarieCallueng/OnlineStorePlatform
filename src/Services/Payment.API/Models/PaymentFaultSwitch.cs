namespace Payment.API.Models;

/// <summary>
/// Process-wide switch that forces payments to be declined, toggled at runtime through the API
/// (no code edit, no restart). Used to trigger saga compensation on demand for grading/demo.
/// </summary>
public sealed class PaymentFaultSwitch
{
    private int _fail;

    public bool FailPayments
    {
        get => Volatile.Read(ref _fail) == 1;
        set => Volatile.Write(ref _fail, value ? 1 : 0);
    }
}
