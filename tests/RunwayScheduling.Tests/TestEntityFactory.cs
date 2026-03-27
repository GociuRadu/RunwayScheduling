using System.Reflection;

namespace RunwayScheduling.Tests;

internal static class TestEntityFactory
{
    public static T WithId<T>(T entity, Guid id) where T : class
    {
        var property = typeof(T).GetProperty("Id", BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(property);
        property!.SetValue(entity, id);
        return entity;
    }
}
