global using System;

using Xunit;

public sealed class InventoryTests
{
    [Fact]
    public void Add_accumulates_quantities_and_unknown_items_are_zero()
    {
        var inventory = new Inventory();

        inventory.Add("pomme", 4);
        inventory.Add("pomme", 3);

        Assert.Equal(7, inventory.Quantity("pomme"));
        Assert.Equal(0, inventory.Quantity("fantome"));
        Assert.Equal(7, inventory.Total());
    }

    [Fact]
    public void Remove_never_produces_a_negative_quantity()
    {
        var inventory = new Inventory();
        inventory.Add("stylo", 4);

        inventory.Remove("stylo", 10);

        Assert.Equal(0, inventory.Quantity("stylo"));
        Assert.Equal(0, inventory.Total());
    }
}
