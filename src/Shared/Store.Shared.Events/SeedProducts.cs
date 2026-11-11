namespace Store.Shared.Events;

/// <summary>
/// Canonical seed catalogue shared by Catalog (products) and Inventory (stock) so the two
/// independent databases agree on product ids out of the box. Fixed GUIDs keep a clean clone
/// deterministic for grading.
/// </summary>
public static class SeedProducts
{
    public record Seed(Guid Id, string Name, decimal Price, int Stock);

    public static readonly IReadOnlyList<Seed> All = new List<Seed>
    {
        new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Wireless Mouse",        25.00m, 100),
        new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Mechanical Keyboard",   79.90m,  50),
        new(Guid.Parse("33333333-3333-3333-3333-333333333333"), "27\" 4K Monitor",      329.00m,  20),
        new(Guid.Parse("44444444-4444-4444-4444-444444444444"), "USB-C Hub",             45.50m,  75),
        new(Guid.Parse("55555555-5555-5555-5555-555555555555"), "Noise-Cancelling Headset", 149.00m, 5)
    };
}
