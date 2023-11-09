using JetBrains.Annotations;

namespace TransparentValueObjects.Augments;

[PublicAPI]
public interface IHasDefaultValue<out TValueObject, TValue>
    where TValueObject : IValueObject<TValue>
    where TValue : notnull
{
    public static abstract TValueObject DefaultValue { get; }
}
