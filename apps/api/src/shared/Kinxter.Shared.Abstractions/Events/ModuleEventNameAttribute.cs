namespace Kinxter.Shared.Abstractions.Events;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ModuleEventNameAttribute : Attribute
{
    public ModuleEventNameAttribute(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
    }

    public string Name { get; }
}
